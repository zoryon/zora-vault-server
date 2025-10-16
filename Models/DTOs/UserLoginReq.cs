namespace ZoraVault.Models.DTOs
{
    public class UserLoginReq
    {
        public required string UsernameOrEmail { get; set; }
        public required string PasswordHash { get; set; } // Client-side hashed password (using argon2id)
    }
}
