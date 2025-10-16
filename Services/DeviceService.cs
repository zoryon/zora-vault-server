using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Security;
using ZoraVault.Data;
using ZoraVault.Helpers;
using ZoraVault.Models.DTOs;
using ZoraVault.Models.Entities;

namespace ZoraVault.Services
{
    public class DeviceService
    {
        private readonly ApplicationDbContext _db;

        public DeviceService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<PublicDevice> RegisterDeviceAsync(string publicKey, Guid userId)
        {
            // Compute a fingerprint of the public key to uniquely identify the device
            string fingerprint = SecurityHelpers.ComputeSHA256HashHex(publicKey);

            // Prevent duplicates
            bool exists = await _db.Devices.AnyAsync(d => d.Fingerprint == fingerprint && d.UserId == userId);
            if (exists)
                throw new DuplicateNameException("This device is already registered for the user");

            Device device = new()
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Fingerprint = fingerprint,
                PublicKey = publicKey,
                CreatedAt = DateTime.UtcNow,
                LastSeen = DateTime.UtcNow
            };

            await _db.Devices.AddAsync(device);
            await _db.SaveChangesAsync();

            return new PublicDevice(device);
        }

        public async Task SendChallengeAsync(string fingerprint, string encryptedChallenge)
        {
            Device? device = await _db.Devices.FirstOrDefaultAsync(d => d.Fingerprint == fingerprint);
            if (device == null)
                throw new KeyNotFoundException("Device not found");

            device.TempChallenge = encryptedChallenge;
            device.TempChallengeIssuedAt = DateTime.UtcNow;

            _db.Devices.Update(device);
            await _db.SaveChangesAsync();
        }

        public async Task<VerifyDeviceResDTO> VerifyChallengeAsync(VerifyDeviceReqDTO req)
        {
            Device? device = await _db.Devices.FirstOrDefaultAsync(d =>
                d.TempChallenge != null &&
                d.TempChallenge.StartsWith(req.ClientResponse)
            );
            if (device == null)
                throw new UnauthorizedAccessException("Invalid challenge response");

            // Challenge expiration safeguard (e.g. valid for 2 minutes)
            if (device.TempChallengeIssuedAt == null || DateTime.UtcNow - device.TempChallengeIssuedAt > TimeSpan.FromMinutes(2))
                throw new SecurityException("Challenge expired");

            // The client must have decrypted and returned the correct plaintext challenge
            if (device.TempChallenge != req.ClientResponse)
                throw new UnauthorizedAccessException("Invalid challenge response");

            // Extract device ID from the challenge for session creation
            string[] parts = device.TempChallenge.Split("-END-");
            Guid deviceId = Guid.Parse(parts[1]);
            if (deviceId != device.Id)
                throw new SecurityException("Challenge device ID mismatch");

            Guid userId = deviceId;

            // Clean up
            device.TempChallenge = null;
            device.TempChallengeIssuedAt = null;
            device.LastSeen = DateTime.UtcNow;

            _db.Devices.Update(device);
            await _db.SaveChangesAsync();

            return new VerifyDeviceResDTO
            {
                Verified = true,
                DeviceId = deviceId,
                UserId = userId
            };
        }
    }
}
