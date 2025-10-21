using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using ZoraVault.Models.Internal.Common;

namespace ZoraVault.Helpers
{
    /// <summary>
    /// SecurityHelpers provides cryptographic utilities for:
    /// - Password hashing and salting
    /// - SHA256 hashing
    /// - Random string generation
    /// - Public-key encryption
    /// - JWT generation and validation
    /// </summary>
    public class SecurityHelpers
    {
        /// <summary>
        /// Generates a cryptographically secure random salt for password hashing.
        /// </summary>
        /// <param name="size">Length of salt in bytes (default: 32 bytes / 256 bits)</param>
        /// <returns>Base64-encoded salt string</returns>
        public static string GenerateSalt(int size = 32)
        {
            // 256-bit salt (32 bytes) for PBKDF2
            var saltBytes = new byte[size];
            RandomNumberGenerator.Fill(saltBytes);

            return Convert.ToBase64String(saltBytes);
        }

        /// <summary>
        /// Hashes a password using PBKDF2 with SHA256, a salt, iterations, and a server-side pepper.
        /// </summary>
        /// <param name="pepper">Server-side secret added to the password</param>
        /// <param name="password">Client-side hashed password</param>
        /// <param name="salt">Base64-encoded salt</param>
        /// <param name="iterations">Number of PBKDF2 iterations</param>
        /// <param name="keyLength">Length of derived key in bytes</param>
        /// <returns>Base64-encoded hash</returns>
        public static string HashPassword(string pepper, string password, string salt, int iterations, int keyLength)
        {
            byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
            byte[] saltBytes = Convert.FromBase64String(salt);

            using var pbkdf2 = new Rfc2898DeriveBytes(passwordBytes + pepper, saltBytes, iterations, HashAlgorithmName.SHA256);
            byte[] hashBytes = pbkdf2.GetBytes(keyLength);

            return Convert.ToBase64String(hashBytes);
        }

        /// <summary>
        /// Computes the SHA256 hash of an input string and returns it as a lowercase hex string.
        /// </summary>
        /// <param name="input">Input string to hash</param>
        /// <returns>Hexadecimal SHA256 hash</returns>
        public static string ComputeSHA256HashHex(string input)
        {
            if (string.IsNullOrEmpty(input))
                throw new ArgumentException("Input cannot be null or empty", nameof(input));

            byte[] bytes = Encoding.UTF8.GetBytes(input);
            byte[] hash = SHA256.HashData(bytes);

            // Convert byte array to hex string
            StringBuilder sb = new StringBuilder();
            foreach (byte b in hash)
            {
                sb.Append(b.ToString("x2")); // "x2" = lowercase hex, always 2 chars
            }

            return sb.ToString();
        }

        /// <summary>
        /// Generates a cryptographically secure random string, Base64-encoded.
        /// </summary>
        /// <param name="byteLength">Number of random bytes</param>
        /// <returns>Base64-encoded random string</returns>
        public static string GenerateRandomBase64String(int byteLength)
        {
            byte[] randomBytes = new byte[byteLength];
            using RandomNumberGenerator rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);
            return Convert.ToBase64String(randomBytes);
        }

        /// <summary>
        /// Encrypts a plaintext string using an RSA public key (PEM format).
        /// </summary>
        /// <param name="plaintext">Text to encrypt</param>
        /// <param name="PublicKeyBase64">RSA public key bytes (expected to be SubjectPublicKeyInfo/SPKI format) encoded as a Base64 string.</param>
        /// <returns>Base64-encoded encrypted string</returns>
        public static string EncryptWithPublicKey(string plaintext, string publicKeyBase64)
        {
            // Decode the Base64 string into raw key bytes
            byte[] publicKeyBytes;
            try
            {
                publicKeyBytes = Convert.FromBase64String(publicKeyBase64);
            }
            catch (FormatException)
            {
                // Handle case where the input string is not valid Base64
                throw new ArgumentException("The public key string is not valid Base64.", nameof(publicKeyBase64));
            }

            using RSA rsa = RSA.Create();
            try
            {
                // Import the public key using the standard SubjectPublicKeyInfo (SPKI) format
                rsa.ImportSubjectPublicKeyInfo(publicKeyBytes, out _);
            }
            catch (CryptographicException ex)
            {
                // Handle case where the Base64 data is not a valid public key
                throw new ArgumentException("The decoded Base64 data is not a valid RSA public key.", nameof(publicKeyBase64), ex);
            }

            // Encrypt the plaintext
            byte[] data = Encoding.UTF8.GetBytes(plaintext);
            byte[] encrypted = rsa.Encrypt(data, RSAEncryptionPadding.OaepSHA256);

            return Convert.ToBase64String(encrypted);
        }

        /// <summary>
        /// Generates a signed JWT containing user ID (and optionally device ID).
        /// </summary>
        /// <param name="userId">User GUID</param>
        /// <param name="tokenSecret">Secret key for signing</param>
        /// <param name="expireMinutes">Token expiration in minutes</param>
        /// <param name="deviceId">Optional device GUID to include as claim</param>
        /// <returns>JWT as string</returns>
        public static string GenerateJWT(Guid userId, string tokenSecret, int expireMinutes, Guid? deviceId = null)
        {
            // Create signing credentials
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(tokenSecret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            // Create claims
            var claims = new[]
            {
                // Subject claim with user ID
                new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            };

            // Add deviceId claim if provided
            claims = deviceId.HasValue
                ? [.. claims, new Claim("deviceId", deviceId.Value.ToString())]
                : claims;

            // Create the JWT token
            var token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expireMinutes),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        /// <summary>Generates a short-lived access token for a device and user</summary>
        public static string GenerateAccessToken(Guid userId, Guid deviceId, string accessTokenSecret, int expireMinutes = 3)
        {
            return GenerateJWT(userId, accessTokenSecret, expireMinutes, deviceId);
        }

        /// <summary>Generates a refresh token for a device and user</summary>
        public static string GenerateRefreshToken(Guid userId, Guid deviceId, string refreshTokenSecret, int expireHours = 3)
        {
            if (expireHours > 3)
                throw new ArgumentException("Refresh token expiration cannot exceed 3 hours", nameof(expireHours));

            return GenerateJWT(userId, refreshTokenSecret, expireHours * 60, deviceId);
        }

        /// <summary>
        /// Validates a JWT token and returns its claims principal if valid.
        /// </summary>
        /// <param name="token">JWT string</param>
        /// <param name="tokenSecret">Secret key for signature verification</param>
        /// <param name="validateLifetime">Whether to validate expiration</param>
        /// <returns>ClaimsPrincipal representing token claims</returns>
        /// <exception cref="SecurityTokenException">Thrown if token is invalid</exception>
        public static ClaimsPrincipal? ValidateJWT(string token, string tokenSecret, bool validateLifetime = true)
        {
            if (string.IsNullOrWhiteSpace(token))
                throw new ArgumentException("Token cannot be null or empty", nameof(token));

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(tokenSecret);

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = validateLifetime,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuerSigningKey = true,
                ClockSkew = TimeSpan.Zero
            };

            try
            {
                var principal = tokenHandler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);

                // Ensure token uses expected algorithm
                if (validatedToken is JwtSecurityToken jwt &&
                    !jwt.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                {
                    throw new SecurityTokenException("Invalid token algorithm");
                }

                return principal;
            }
            catch
            {
                throw new SecurityTokenException("Invalid token");
            }
        }
    }
}
