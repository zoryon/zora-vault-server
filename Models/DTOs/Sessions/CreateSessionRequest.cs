namespace ZoraVault.Models.DTOs.Sessions
{
    public class CreateSessionRequest
    {
        public string ClientResponse { get; set; } = null!;         // The decrypted challenge for device authentication
        public string AccessSessionApiToken { get; set; } = null!;  // The API token used to access the API related to create a session
    }
}
