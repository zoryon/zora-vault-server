using Microsoft.EntityFrameworkCore;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security;
using System.Text.Json;
using ZoraVault.Data;
using ZoraVault.Helpers;
using ZoraVault.Models.DTOs;
using ZoraVault.Models.Entities;

namespace ZoraVault.Services
{
    public class DeviceService
    {
        private readonly ApplicationDbContext _db;
        private readonly string _sessionApiSecret;

        public DeviceService(ApplicationDbContext db, string sessionApiSecret)
        {
            _db = db;
            _sessionApiSecret = sessionApiSecret;
        }

        public async Task<PublicDevice> FindOrRegisterDeviceAsync(Guid userId, string publicKey)
        {
            // Compute a fingerprint of the public key to uniquely identify the device
            string fingerprint = SecurityHelpers.ComputeSHA256HashHex(publicKey);

            // Prevent duplicates
            Device? existingDevice = await _db.Devices.FirstOrDefaultAsync(d => d.Fingerprint == fingerprint);
            if (existingDevice != null)
                return new PublicDevice(existingDevice);

            Device device = new()
            {
                Id = Guid.NewGuid(),
                Fingerprint = fingerprint,
                PublicKey = publicKey,
                CreatedAt = DateTime.UtcNow,
            };

            await _db.Devices.AddAsync(device);
            await _db.SaveChangesAsync();

            return new PublicDevice(device);
        }

        public async Task<bool> SaveTempChallengeAsync(PublicDevice pubDevice, string plainChallenge)
        {
            Device? device = await _db.Devices.FirstOrDefaultAsync(d => d.Id == pubDevice.Id)
                ?? throw new KeyNotFoundException("Device not found");

            device.TempChallenge = plainChallenge;
            device.TempChallengeIssuedAt = DateTime.UtcNow;

            _db.Devices.Update(device);

            return await _db.SaveChangesAsync() != 0;
        }

        public async Task<VerifyDeviceResDTO> VerifyChallengeAsync(CreateSessionReqDTO req)
        {
            var claims = SecurityHelpers.ValidateJWT(req.AccessApiToken, _sessionApiSecret)
                ?? throw new UnauthorizedAccessException("Invalid or expired token");

            string userIdStr = claims.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                ?? throw new UnauthorizedAccessException("Invalid token: missing user ID");

            string deviceIdStr = claims.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                ?? throw new UnauthorizedAccessException("Invalid token: missing device ID");

            if (!Guid.TryParse(userIdStr, out Guid userId) || !Guid.TryParse(deviceIdStr, out Guid deviceId))
                throw new UnauthorizedAccessException("Invalid token");

            // Find the device as written inside the JWT
            Device? device = await _db.Devices.FirstOrDefaultAsync(d => d.Id == deviceId) 
                ?? throw new UnauthorizedAccessException("Invalid challenge response");

            // Challenge expiration safeguard (e.g. 2 minutes)
            if (device.TempChallengeIssuedAt == null || DateTime.UtcNow - device.TempChallengeIssuedAt > TimeSpan.FromMinutes(2))
                throw new SecurityException("Challenge expired");

            // Deserialize challenge payload
            ChallengeDTO? payload;
            try
            {
                payload = JsonSerializer.Deserialize<ChallengeDTO>(req.ClientResponse);
            }
            catch
            {
                throw new FormatException("Invalid challenge format");
            }

            if (payload == null)
                throw new UnauthorizedAccessException("Invalid challenge");

            // Check device ID matches
            if (payload.DeviceId != device.Id)
                throw new SecurityException("Challenge device ID mismatch");

            // Check stored challenge matches client response
            if (device.TempChallenge != req.ClientResponse)
                throw new UnauthorizedAccessException("Invalid challenge response");

            // Register user-device link if it doesn't exist
            bool exists = await _db.UserDevices.AnyAsync(ud => ud.UserId == payload.UserId && ud.DeviceId == device.Id);
            if (!exists)
            {
                UserDevice userDevice = new()
                {
                    UserId = payload.UserId,
                    DeviceId = device.Id
                };
                await _db.UserDevices.AddAsync(userDevice);
            }

            // Clean up challenge
            device.TempChallenge = null;
            device.TempChallengeIssuedAt = null;
            device.LastSeen = DateTime.UtcNow;

            _db.Devices.Update(device);
            await _db.SaveChangesAsync();

            return new VerifyDeviceResDTO
            {
                IsVerified = true,
                DeviceId = device.Id,
                UserId = payload.UserId,
            };
        }
    }
}
