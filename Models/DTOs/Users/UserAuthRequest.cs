namespace ZoraVault.Models.DTOs.Users
{
    public class UserAuthRequest
    {
        public required string UsernameOrEmail { get; set; }    // Either username or email for authentication
        public required string PasswordHash { get; set; }       // Client-side hashed password
    }
}
