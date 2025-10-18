namespace ZoraVault.Configuration
{
    public class Endpoints
    {
        public readonly static (string Path, string Method)[] PublicEndpoints =
        [
            ("/api/users", "POST"),  
            ("/api/sessions/credentials", "POST"), 
            ("/api/sessions/challenges", "POST"), 
            ("/api/sessions/tokens/refresh-tokens", "POST"),
        ];
    }
}
