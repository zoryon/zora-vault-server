namespace ZoraVault.Models.DTOs
{
    public class UserRegistrationReq
    {
        public required string Username { get; set; }
        public required string Email { get; set; }
        public required string PasswordHash { get; set; }
        public required KdfParams KdfParams { get; set; }
    }
}
    