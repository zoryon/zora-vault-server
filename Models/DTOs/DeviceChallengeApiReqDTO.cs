namespace ZoraVault.Models.DTOs
{
    public class DeviceChallengeApiReqDTO
    {
        public required string AccessApiToken { get; set; } // Challenges API token
        public required string PublicKey { get; set; } // Device public key
    }
}
