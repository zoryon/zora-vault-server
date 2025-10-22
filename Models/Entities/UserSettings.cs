using ZoraVault.Models.Internal.Enum;

namespace ZoraVault.Models.Entities
{
    public class UserSettings
    {
        public required Guid Id { get; set; }
        public required Guid UserId { get; set; }
        public required Guid DeviceId { get; set; }

        // Server-enforced technical settings
        public int SessionTimeoutMinutes { get; set; } = 3;

        // Preference Settings
        public ThemeType Theme { get; set; } = ThemeType.Dark;  // Could be client-side only, but stored for consistency across devices

        /*
         * - - - CLIENT SIDE ONLY SETTINGS - - -
         * public bool UnlockWithBiometrics { get; set; } = false;
         * public bool AllowScreenCapture { get; set; } = false;
         * public bool EnableAutoFill { get; set; } = true;
         * public bool EnableAccessibility { get; set; } = true;
         * public bool EnableClipboardClearing { get; set; } = true;
         * public int ClipboardClearDelaySeconds { get; set; } = 15;
         * - - - - - - - - - - - - - - - - - - -
         */

        // Navigation properties
        public User User { get; set; } = null!;
        public Device Device { get; set; } = null!;
    }
}
