namespace ZoraVault.Models.DTOs
{
    public class CreateSessionResDTO
    {
        public required string AccessToken { get; set;  }
        public required string RefreshToken { get; set; }
    }
}
