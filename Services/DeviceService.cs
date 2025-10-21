using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ZoraVault.Configuration;
using ZoraVault.Data;
using ZoraVault.Helpers;
using ZoraVault.Models.DTOs.Sessions;
using ZoraVault.Models.Entities;
using ZoraVault.Models.Internal;

namespace ZoraVault.Services
{
    /// <summary>
    /// DeviceService manages all device-related operations, including:
    /// - Registering new devices
    /// - Generating and saving cryptographic challenges for device verification
    /// - Verifying device challenge responses and linking devices to users
    /// </summary>
    public class DeviceService
    {
        private readonly ApplicationDbContext _db;  // Database context for entity access
        private readonly string _sessionApiSecret;  // Secret used to sign session-related JWTs

        /// <summary>
        /// Constructor: injects database context and session API secret
        /// </summary>
        /// <param name="db">The application database context</param>
        /// <param name="sessionApiSecret">Secret key for signing session JWTs</param>
        public DeviceService(ApplicationDbContext db, Secrets secrets)
        {
            _db = db;
            _sessionApiSecret = secrets.SessionApiSecret;
        }

        /// <summary>
        /// Finds a device by its public key fingerprint or registers a new one if it does not exist.
        /// </summary>
        /// <param name="publicKey">Device's public key in base64 format</param>
        /// <returns>A <see cref="PublicDevice"/> containing device info</returns>
        public async Task<PublicDevice> FindOrRegisterDeviceAsync(string publicKeyBase64)
        {
            byte[] publicKeyBytes = Convert.FromBase64String(publicKeyBase64);

            // Compute fingerprint (SHA256 hash of the public key)
            byte[] fingerprintBytes = SHA256.HashData(publicKeyBytes);

            // Look for an existing device with the same fingerprint and return it if found
            Device? existingDevice = await _db.Devices.FirstOrDefaultAsync(d => d.Fingerprint.SequenceEqual(fingerprintBytes));
            if (existingDevice != null)
                return new PublicDevice(existingDevice);

            // Register a new device if not found
            Device device = new()
            {
                Id = Guid.NewGuid(),
                Fingerprint = fingerprintBytes,
                PublicKey = publicKeyBytes,
                CreatedAt = DateTime.UtcNow,
            };

            await _db.Devices.AddAsync(device);
            await _db.SaveChangesAsync();

            return new PublicDevice(device);
        }

        /// <summary>
        /// Stores a temporary challenge for a device in the database.
        /// The challenge will later be used to verify device ownership.
        /// </summary>
        /// <param name="pubDevice">The public device object</param>
        /// <param name="plainChallenge">Plaintext challenge string</param>
        /// <returns>True if saved successfully, false otherwise</returns>
        public async Task<bool> SaveTempChallengeAsync(PublicDevice pubDevice, string plainChallenge)
        {
            // Find device in database
            Device? device = await _db.Devices.FirstOrDefaultAsync(d => d.Id == pubDevice.Id)
                ?? throw new KeyNotFoundException("Device not found");

            // Save challenge and timestamp
            device.TempChallenge = Encoding.UTF8.GetBytes(plainChallenge);
            device.TempChallengeIssuedAt = DateTime.UtcNow;

            _db.Devices.Update(device);

            return await _db.SaveChangesAsync() > 0;
        }

        /// <summary>
        /// Verifies a client's challenge response and, if valid, links the device to the user.
        /// </summary>
        /// <param name="req">Request DTO containing client response and session token</param>
        /// <returns>A <see cref="DeviceVerificationResult"/> indicating verification result</returns>
        /// <exception cref="UnauthorizedAccessException">Thrown if the token or challenge is invalid</exception>
        /// <exception cref="SecurityException">Thrown if the challenge is expired or mismatched</exception>
        public async Task<DeviceVerificationResult> VerifyChallengeAsync(CreateSessionRequest req)
        {
            // Validate the session API token
            var claims = SecurityHelpers.ValidateJWT(req.AccessSessionApiToken, _sessionApiSecret)
                ?? throw new UnauthorizedAccessException("Invalid or expired token");

            // Extract userId and deviceId from JWT claims
            string? userIdStr = claims.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            string? deviceIdStr = claims.FindFirst("deviceId")?.Value;

            if (string.IsNullOrEmpty(userIdStr) || string.IsNullOrEmpty(deviceIdStr))
                throw new UnauthorizedAccessException("Invalid token: missing claims");

            if (!Guid.TryParse(userIdStr, out Guid userId) || !Guid.TryParse(deviceIdStr, out Guid deviceId))
                throw new UnauthorizedAccessException("Invalid token");

            // Find device in DB
            Device? device = await _db.Devices.FirstOrDefaultAsync(d => d.Id == deviceId) 
                ?? throw new UnauthorizedAccessException("Invalid challenge response");

            // Challenge expiration safeguard (e.g. 2 minutes)
            if (
                device.TempChallenge == null || 
                device.TempChallengeIssuedAt == null || 
                DateTime.UtcNow - device.TempChallengeIssuedAt > TimeSpan.FromMinutes(2)
            ) 
                throw new SecurityException("Challenge expired");

            // Deserialize client-provided challenge payload
            Challenge? payload;
            try
            {
                payload = JsonSerializer.Deserialize<Challenge>(req.ClientResponse);
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

            byte[] clientResponseBytes = Encoding.UTF8.GetBytes(req.ClientResponse);

            // Verify stored challenge matches client response
            if (!device.TempChallenge.SequenceEqual(clientResponseBytes))
                throw new UnauthorizedAccessException("Invalid challenge response");

            // Link user and device if not already linked
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

            // Cleanup: clear temporary challenge and update last seen timestamp
            device.TempChallenge = null;
            device.TempChallengeIssuedAt = null;
            device.LastSeen = DateTime.UtcNow;

            _db.Devices.Update(device);
            await _db.SaveChangesAsync();

            // Return verification result
            return new DeviceVerificationResult
            {
                IsVerified = true,
                DeviceId = device.Id,
                UserId = payload.UserId,
            };
        }
    }
}
