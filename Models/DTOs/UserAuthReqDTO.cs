namespace ZoraVault.Models.DTOs
{
    public class UserAuthReqDTO
    {
        public required string UsernameOrEmail { get; set; }
        public required string PasswordHash { get; set; } // Client-side hashed password
    }
}
