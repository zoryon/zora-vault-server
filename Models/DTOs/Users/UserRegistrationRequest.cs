using ZoraVault.Models.Internal;

namespace ZoraVault.Models.DTOs.Users
{
    public class UserRegistrationRequest
    {
        public required string Username { get; set; }
        public required string Email { get; set; }
        public required string PasswordHash { get; set; }
        public required KdfParams KdfParams { get; set; }
    }
}
    