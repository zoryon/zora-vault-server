using ZoraVault.Models.Internal.Enum;

namespace ZoraVault.Models.DTOs.Users
{
    public class UpdateUserSettingsRequest
    {
        // Server-enforced technical settings
        public int SessionTimeoutMinutes { get; set; } = 3;

        // Preference Settings
        public ThemeType Theme { get; set; } = ThemeType.Dark;
    }
}
