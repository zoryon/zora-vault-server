using Microsoft.EntityFrameworkCore;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using ZoraVault.Configuration;
using ZoraVault.Data;
using ZoraVault.Helpers;
using ZoraVault.Models.DTOs.Sessions;
using ZoraVault.Models.DTOs.Users;
using ZoraVault.Models.Entities;
using ZoraVault.Models.Internal;
using ZoraVault.Models.Internal.Enum;

namespace ZoraVault.Services
{
    /// <summary>
    /// AuthService handles all authentication-related logic:
    /// - User registration & authentication
    /// - Session creation and refresh
    /// - Challenge API and session token issuance
    /// </summary>
    public class AuthService
    {
        private readonly ApplicationDbContext _db;
        private readonly Secrets _secrets;  // Application secrets

        public AuthService(ApplicationDbContext db, Secrets secrets)
        {
            _db = db;
            _secrets = secrets;
        }

        /// <summary>
        /// Registers a new user by applying one new layer of hashing:
        /// (before) client-side hash + (now) server-side hash (PBKDF2 + pepper)
        /// </summary>
        public async Task<PublicUser> RegisterUserAsync(UserRegistrationRequest req)
        {
            // Check for duplicates (username or email must be unique)
            if (await _db.Users.AnyAsync(u => u.Username == req.Username))
                throw new DuplicateNameException("A user with the same username already exists");

            if (await _db.Users.AnyAsync(u => u.Email == req.Email))
                throw new DuplicateNameException("A user with the same email already exists");

            // The request's PasswordHash is the client-side hashed password (using argon2id)
            // Here, it's added another layer of security by hashing it again with PBKDF2 + server-side secret (pepper)
            string serverSalt = SecurityHelpers.GenerateSalt();
            string serverPasswdHash = SecurityHelpers.HashPassword(
                _secrets.ServerSecret,  // pepper (secret stored only on server)
                req.PasswordHash,       // client-side hash
                serverSalt,             // per-user salt
                req.KdfParams.Iterations,
                req.KdfParams.KeyLength
            );

            // Create and persist user record
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

            // Return only public data, no sensitive fields
            return new PublicUser(user);
        }

        /// <summary>
        /// Authenticates the user by re-hashing the provided password hash and comparing.
        /// </summary>
        public async Task<PublicUser> AuthenticateUserAsync(UserAuthRequest req)
        {
            // Try finding user by username OR email
            User? user = await _db.Users.FirstOrDefaultAsync(u => 
                u.Username == req.UsernameOrEmail || 
                u.Email == req.UsernameOrEmail
            ) ?? throw new KeyNotFoundException("User not found");

            // Get KDF parameters used when the user was registered
            int serverIterations = user.KdfParams.Iterations; 
            int keyLength = user.KdfParams.KeyLength;

            // Recompute the server-side hash using provided (client-hashed) password
            string serverComputedHash = SecurityHelpers.HashPassword(
                _secrets.ServerSecret, 
                req.PasswordHash, 
                user.ServerSalt, 
                serverIterations, 
                keyLength
            );

            // Compare the computed hash with the stored hash
            if (serverComputedHash != user.ServerPasswordHash)
                throw new UnauthorizedAccessException("Invalid credentials");

            return new PublicUser(user);
        }

        /// <summary>
        /// Creates or updates a user session.
        /// Generates both refresh and access tokens, and stores session info with IP address.
        /// </summary>
        public async Task<CreateSessionResponse> CreateSessionAsync(Guid userId, Guid deviceId, string ipAddress)
        {
            // Ensure user settings exist for this device
            UserSettings? existingSettings = await _db.UserSettings
                .FirstOrDefaultAsync(us => us.UserId == userId && us.DeviceId == deviceId);
            if (existingSettings == null)
            {
                await _db.UserSettings.AddAsync(new UserSettings
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    DeviceId = deviceId,
                    SessionTimeoutMinutes = 3,
                    Theme = ThemeType.Dark,
                });
            }

            // Try to find an existing session for the user and device
            Session? session = await _db.Sessions.FirstOrDefaultAsync(s => s.UserId == userId && s.DeviceId == deviceId);

            // Always generate a new refresh token (safe rotation principle)
            string refreshToken = SecurityHelpers.GenerateRefreshToken(userId, deviceId, _secrets.RefreshTokenSecret);
            if (session == null)
            {
                // Create new session entry
                session = new()
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    DeviceId = deviceId,
                    RefreshToken = refreshToken,
                    CreatedAt = DateTime.UtcNow,
                    IpAddress = ipAddress
                };
                await _db.Sessions.AddAsync(session);
            } else
            {
                // Update existing session (rotate refresh token and update IP)
                session.RefreshToken = refreshToken;
                session.IpAddress = ipAddress;
                _db.Sessions.Update(session);
            }

            await _db.SaveChangesAsync();

            // Generate short-lived access token for immediate use
            string accessToken = SecurityHelpers.GenerateAccessToken(userId, deviceId, _secrets.AccessTokenSecret);

            return new CreateSessionResponse
            {
                AccessToken = accessToken,
                RefreshToken = session.RefreshToken,
            };
        }

        /// <summary>
        /// Issues a very short-lived token (2 min) that allows a client to access the Challenges API.
        /// </summary>
        public string GrantChallengesAPIAccess(Guid userId)
        {
            return SecurityHelpers.GenerateJWT(userId, _secrets.ChallengesApiSecret, 2); // Token valid for 2 minutes
        }

        /// <summary>
        /// Issues a short-lived token (2 min) for session-related APIs.
        /// </summary>
        public string GrantSessionAPIAccess(Guid userId, Guid deviceId)
        {
            return SecurityHelpers.GenerateJWT(userId, _secrets.SessionApiSecret, 2, deviceId); // Token valid for 2 minutes
        }

        /// <summary>
        /// Verifies a token used to access device challenge API endpoints.
        /// Returns the userId if valid.
        /// </summary>
        public Guid VerifyDeviceChallengeAccessTokenAsync(string token)
        {
            var claims = SecurityHelpers.ValidateJWT(token, _secrets.ChallengesApiSecret)
                ?? throw new UnauthorizedAccessException("Invalid or expired token");

            string userIdStr = claims.FindFirst(JwtRegisteredClaimNames.Sub)?.Value 
                ?? throw new UnauthorizedAccessException("Invalid token: missing user ID");

            if (!Guid.TryParse(userIdStr, out Guid userId))
                throw new UnauthorizedAccessException("Invalid token: user ID is not a valid GUID");

            return userId;
        }

        /// <summary>
        /// Validates a refresh token and issues a new access token.
        /// Ensures that the token belongs to a valid user session and device.
        /// </summary>
        public async Task<string> RefreshAccessTokenAsync(string refreshToken)
        {
            // Validate the refresh token
            var claims = SecurityHelpers.ValidateJWT(refreshToken, _secrets.RefreshTokenSecret)
                ?? throw new UnauthorizedAccessException("Invalid or expired refresh token");

            // Extract userId and deviceId from claims
            string? userIdStr = claims.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            string? deviceIdStr = claims.FindFirst("deviceId")?.Value;
            if (string.IsNullOrEmpty(userIdStr) || string.IsNullOrEmpty(deviceIdStr))
                throw new UnauthorizedAccessException("Invalid token: missing claims");

            // Convert IDs from string to Guid
            if (!Guid.TryParse(userIdStr, out Guid userId) || !Guid.TryParse(deviceIdStr, out Guid deviceId))
                throw new UnauthorizedAccessException("Invalid token");

            // Ensure the refresh token matches what is stored in DB for this user & device
            Session? session = await _db.Sessions.FirstOrDefaultAsync(s => s.UserId == userId && s.DeviceId == deviceId);
            if (session == null || session.RefreshToken != refreshToken)
                throw new UnauthorizedAccessException("Invalid refresh token");

            // Update device's last seen
            Device? device = await _db.Devices.FirstOrDefaultAsync(d => d.Id == deviceId)
                ?? throw new KeyNotFoundException("Device not found");
            device.LastSeen = DateTime.UtcNow;
            _db.Devices.Update(device);
            await _db.SaveChangesAsync();

            // Return a freshly issued access token
            return SecurityHelpers.GenerateAccessToken(userId, deviceId, _secrets.AccessTokenSecret);
        }

        /// <summary>
        /// Delete a user session by its ID.
        /// Ensures that the user owns the session before revoking it.
        /// </summary>
        public async Task<bool> RevokeSessionAsync(Guid userId, Guid SessionId)
        {
            int affectedRows = await _db.Sessions.Where(s => s.UserId == userId && s.Id == SessionId)
                .ExecuteDeleteAsync();

            if (!(affectedRows > 0))
                throw new KeyNotFoundException("Session not found");

            return affectedRows > 0;
        }
    }
}
