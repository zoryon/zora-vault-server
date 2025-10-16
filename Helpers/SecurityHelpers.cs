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
    }
}
