using Microsoft.EntityFrameworkCore;
using System.Data;
using System.IdentityModel.Tokens.Jwt;

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
        private readonly string _challengesApiSecret;
        private readonly string _sessionApiSecret;

        public AuthService(ApplicationDbContext db, string serverSecret, string refreshSecret, string accessSecret, string challengesApiSecret, string sessionApiSecret)
        {
            _db = db;
            _serverSecret = serverSecret;
            _refreshSecret = refreshSecret;
            _accessSecret = accessSecret;
            _challengesApiSecret = challengesApiSecret;
            _sessionApiSecret = sessionApiSecret;

            _deviceService = new DeviceService(db, _sessionApiSecret);
        }

        public async Task<PublicUserDTO> RegisterUserAsync(UserRegistrationReqDTO req)
        {
            if (await _db.Users.AnyAsync(u => u.Username == req.Username))
                throw new DuplicateNameException("A user with the same username already exists");

            if (await _db.Users.AnyAsync(u => u.Email == req.Email))
                throw new DuplicateNameException("A user with the same email already exists");

            // The request's PasswordHash is the client-side hashed password (using argon2id)
            // Now, it's added another layer of security by hashing it again with PBKDF2 + server-side secret (pepper)
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

        public async Task<PublicUserDTO> AuthenticateUserAsync(UserAuthReqDTO req)
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

            return new PublicUserDTO(user);
        }

        public async Task<CreateSessionResDTO> CreateSessionAsync(Guid userId, Guid deviceId, string ipAddress)
        {
            Session? session = await _db.Sessions.FirstOrDefaultAsync(s => s.UserId == userId && s.DeviceId == deviceId);
            
            if (session == null)
                session = new()
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    DeviceId = deviceId,
                    RefreshToken = SecurityHelpers.GenerateRefreshToken(userId, deviceId, _refreshSecret),
                    CreatedAt = DateTime.UtcNow,
                    IpAddress = ipAddress
                };

            await _db.Sessions.AddAsync(session);
            await _db.SaveChangesAsync();

            string accessToken = SecurityHelpers.GenerateAccessToken(userId, deviceId, _accessSecret);

            return new CreateSessionResDTO
            {
                AccessToken = accessToken,
                RefreshToken = session.RefreshToken,
            };
        }

        public string GrantChallengesAPIAccess(Guid userId)
        {
            return SecurityHelpers.GenerateJWT(userId, _challengesApiSecret, 2); // Token valid for 2 minutes
        }

        public string GrantSessionAPIAccess(Guid userId, Guid deviceId)
        {
            return SecurityHelpers.GenerateJWT(userId, _sessionApiSecret, 2, deviceId); // Token valid for 2 minutes
        }

        public Guid VerifyDeviceChallengeAccessTokenAsync(string token)
        {
            var claims = SecurityHelpers.ValidateJWT(token, _challengesApiSecret)
                ?? throw new UnauthorizedAccessException("Invalid or expired token");

            string userIdStr = claims.FindFirst(JwtRegisteredClaimNames.Sub)?.Value 
                ?? throw new UnauthorizedAccessException("Invalid token: missing user ID");

            if (!Guid.TryParse(userIdStr, out Guid userId))
                throw new UnauthorizedAccessException("Invalid token: user ID is not a valid GUID");

            return userId;
        }
    }
}
