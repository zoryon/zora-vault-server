using System.ComponentModel.DataAnnotations;

namespace ZoraVault.Configuration
{
    public class Secrets
    {
        [Required] public string ServerSecret { get; set; } = default!;
        [Required] public string AccessTokenSecret { get; set; } = default!;
        [Required] public string RefreshTokenSecret { get; set; } = default!;
    }
}
