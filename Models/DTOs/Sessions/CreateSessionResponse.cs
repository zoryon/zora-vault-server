namespace ZoraVault.Models.DTOs.Sessions
{
    public class CreateSessionResponse
    {
        public required string AccessToken { get; set; }    // Short-lived access token for authenticated requests
        public required string RefreshToken { get; set; }   // Long-lived refresh token to obtain new access tokens
    }
}
