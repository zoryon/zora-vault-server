using ZoraVault.Models.Internal.Enum;

namespace ZoraVault.Models.DTOs.Users
{
    public class UpdateUserSettingsRequest
    {
        // Technical Settings
        public bool UnlockWithBiometrics { get; set; } = false;
        public int SessionTimeoutMinutes { get; set; } = 3;
        public bool AllowScreenCapture { get; set; } = false;

        // Preference Settings
        public ThemeType Theme { get; set; } = ThemeType.Dark;
        public bool EnableAutoFill { get; set; } = true;
        public bool EnableAccessibility { get; set; } = true;
        public bool EnableClipboardClearing { get; set; } = true;
        public int ClipboardClearDelaySeconds { get; set; } = 15;
    }
}
