using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace ZoraVault.Helpers
{
    public class SecurityHelpers
    {
        public static string GenerateSalt(int size = 32)
        {
            // 256-bit salt (32 bytes) for PBKDF2
            var saltBytes = new byte[size];
            RandomNumberGenerator.Fill(saltBytes);

            return Convert.ToBase64String(saltBytes);
        }

        public static string HashPassword(string pepper, string password, string salt, int iterations, int keyLength)
        {
            byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
            byte[] saltBytes = Convert.FromBase64String(salt);

            using var pbkdf2 = new Rfc2898DeriveBytes(passwordBytes + pepper, saltBytes, iterations, HashAlgorithmName.SHA256);
            byte[] hashBytes = pbkdf2.GetBytes(keyLength);

            return Convert.ToBase64String(hashBytes);
        }

        public static string ComputeSHA256HashHex(string input)
        {
            if (string.IsNullOrEmpty(input))
                throw new ArgumentException("Input cannot be null or empty", nameof(input));

            using SHA256 sha256 = SHA256.Create();
            byte[] bytes = Encoding.UTF8.GetBytes(input);
            byte[] hash = sha256.ComputeHash(bytes);

            // Convert byte array to hex string
            StringBuilder sb = new StringBuilder();
            foreach (byte b in hash)
            {
                sb.Append(b.ToString("x2")); // "x2" = lowercase hex, always 2 chars
            }

            return sb.ToString();
        }

        public static string GenerateRandomBase64String(int byteLength)
        {
            byte[] randomBytes = new byte[byteLength];
            using RandomNumberGenerator rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);
            return Convert.ToBase64String(randomBytes);
        }

        public static string EncryptWithPublicKey(string plaintext, string publicKeyPem)
        {
            using RSA rsa = RSA.Create();
            rsa.ImportFromPem(publicKeyPem.ToCharArray());

            byte[] data = Encoding.UTF8.GetBytes(plaintext);
            byte[] encrypted = rsa.Encrypt(data, RSAEncryptionPadding.OaepSHA256);

            return Convert.ToBase64String(encrypted);
        }

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

        public static string GenerateAccessToken(Guid userId, Guid deviceId, string accessTokenSecret, int expireMinutes = 3)
        {
            return GenerateJWT(userId, accessTokenSecret, expireMinutes, deviceId);
        }

        public static string GenerateRefreshToken(Guid userId, Guid deviceId, string refreshTokenSecret, int expireDays = 1)
        {
            return GenerateJWT(userId, refreshTokenSecret, expireDays * 24 * 60, deviceId);
        }

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

                // Optional: check token algorithm explicitly
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
