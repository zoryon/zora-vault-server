namespace ZoraVault.Models.DTOs.Challenges
{
    public class IssueDeviceChallengeResponse
    {
        public string EncryptedChallenge { get; set; } = null!;     // Encrypted challenge for device authentication
        public string AccessSessionApiToken { get; set; } = null!;  // The API token used to access the API related to create challenges for device authentication
    }
}
