namespace ZoraVault.Models.DTOs
{
    public class UserRegistrationReqDTO
    {
        public required string Username { get; set; }
        public required string Email { get; set; }
        public required string PasswordHash { get; set; }
        public required KdfParamsDTO KdfParams { get; set; }
    }
}
    