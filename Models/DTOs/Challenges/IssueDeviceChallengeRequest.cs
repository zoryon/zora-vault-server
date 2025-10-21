namespace ZoraVault.Models.DTOs.Challenges
{
    public class IssueDeviceChallengeRequest
    {
        public required string AccessChallengeApiToken { get; set; }    // The API token used to access the API related to create challenges for device authentication
        public required string PublicKey { get; set; }                  // Device public key
    }
}
