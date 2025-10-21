using System.ComponentModel.DataAnnotations;

namespace ZoraVault.Models.DTOs.Challenges
{
    public class IssueDeviceChallengeRequest
    {
        public required string AccessChallengeApiToken { get; set; }    // The API token used to access the API related to create challenges for device authentication

        [Base64String]
        public required string PublicKeyBase64 { get; set; }            // Device public key
    }
}
