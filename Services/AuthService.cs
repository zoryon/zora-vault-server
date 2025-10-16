using Microsoft.EntityFrameworkCore;
using System.Data;
using ZoraVault.Data;
using ZoraVault.Helpers;
using ZoraVault.Models.DTOs;
using ZoraVault.Models.Entities;

namespace ZoraVault.Services
{
    public class AuthService
    {
        private readonly ApplicationDbContext _db;
        private readonly DeviceService _deviceService;
        private readonly string _serverSecret;
        private readonly string _refreshSecret;
        private readonly string _accessSecret;

        public AuthService(ApplicationDbContext db, string serverSecret, string refreshSecret, string accessSecret)
        {
            _db = db;
            _deviceService = new DeviceService(db);
            _serverSecret = serverSecret;
            _refreshSecret = refreshSecret;
            _accessSecret = accessSecret;
        }

        public async Task<PublicUserDTO> RegisterUserAsync(UserRegistrationReqDTO req)
        {
            if (await _db.Users.AnyAsync(u => u.Username == req.Username))
                throw new DuplicateNameException("A user with the same username already exists");

            if (await _db.Users.AnyAsync(u => u.Email == req.Email))
                throw new DuplicateNameException("A user with the same email already exists");

            // The request's PasswordHash is the client-side hashed password (using argon2id)
            // Now, add another layer of security by hashing it again with PBKDF2 + server-side secret (pepper)
            string serverSalt = SecurityHelpers.GenerateSalt();
            string serverPasswdHash = SecurityHelpers.HashPassword(
                _serverSecret, 
                req.PasswordHash,
                serverSalt, 
                req.KdfParams.Iterations,
                req.KdfParams.KeyLength
            );

            User user = new()
            {
                Id = Guid.NewGuid(),
                Username = req.Username,
                Email = req.Email,
                ServerSalt = serverSalt,
                ServerPasswordHash = serverPasswdHash,
                KdfParams = req.KdfParams,
                CreatedAt = DateTime.UtcNow
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            return new PublicUserDTO(user);
        }

        public async Task<DeviceChallengeDTO> AuthenticateUserAsync(UserLoginReqDTO req)
        {
            User? user = await _db.Users.FirstOrDefaultAsync(u => 
                u.Username == req.UsernameOrEmail || 
                u.Email == req.UsernameOrEmail
            ) ?? throw new KeyNotFoundException("User not found");

            // Server-side hash
            int serverIterations = user.KdfParams.Iterations; 
            int keyLength = user.KdfParams.KeyLength;

            // Generate the server-side hash from the "unverified" client-provided hash
            string serverComputedHash = SecurityHelpers.HashPassword(
                _serverSecret, 
                req.PasswordHash, 
                user.ServerSalt, 
                serverIterations, 
                keyLength
            );

            // Compare the computed hash with the stored hash
            if (serverComputedHash != user.ServerPasswordHash)
                throw new UnauthorizedAccessException("Invalid credentials");

            // Update last login timestamp
            user.LastLogin = DateTime.UtcNow;

            // Device handling
            string fingerprint = SecurityHelpers.ComputeSHA256HashHex(req.PublicKey);
            Device? device = await _db.Devices.FirstOrDefaultAsync(d => d.Fingerprint == fingerprint && d.UserId == user.Id);

            if (device == null)
            {
                // It's a new device, register it
                device = new Device()
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    PublicKey = req.PublicKey,
                    Fingerprint = fingerprint,
                    CreatedAt = DateTime.UtcNow,
                    LastSeen = DateTime.UtcNow
                };
                await _db.Devices.AddAsync(device);
                await _db.SaveChangesAsync();
            }

            string randomPart = SecurityHelpers.GenerateRandomBase64String(32);
            string challenge = $"{device.Id}-END-{randomPart}";
            string encryptedChallenge = SecurityHelpers.EncryptWithPublicKey(challenge, req.PublicKey);
            await _deviceService.SendChallengeAsync(fingerprint, challenge);

            return new DeviceChallengeDTO(encryptedChallenge);
        }

        public async Task<CreateSessionResDTO> CreateSessionAsync(CreateSessionReqDTO req)
        {
            Session session = new()
            {
                Id = Guid.NewGuid(),
                UserId = req.UserId,
                DeviceId = req.DeviceId,
                RefreshToken = SecurityHelpers.GenerateRefreshToken(req.UserId, req.DeviceId, _refreshSecret),
                CreatedAt = DateTime.UtcNow,
                IpAddress = "0.0.0.0" // TODO: still to be implemented
            };

            await _db.Sessions.AddAsync(session);
            await _db.SaveChangesAsync();

            string accessToken = SecurityHelpers.GenerateAccessToken(req.UserId, req.DeviceId, _accessSecret);

            return new CreateSessionResDTO
            {
                AccessToken = accessToken,
                RefreshToken = session.RefreshToken,
            };
        }
    }
}
