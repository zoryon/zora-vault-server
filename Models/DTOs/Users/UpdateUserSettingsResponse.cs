using ZoraVault.Models.Entities;
using ZoraVault.Models.Internal.Enum;

namespace ZoraVault.Models.DTOs.Users
{
    public class UpdateUserSettingsResponse
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid DeviceId { get; set; }

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

        public UpdateUserSettingsResponse(UserSettings us)
        {
            Id = us.Id;
            UserId = us.UserId;
            DeviceId = us.DeviceId;
            UnlockWithBiometrics = us.UnlockWithBiometrics;
            SessionTimeoutMinutes = us.SessionTimeoutMinutes;
            AllowScreenCapture = us.AllowScreenCapture;
            Theme = us.Theme;
            EnableAutoFill = us.EnableAutoFill;
            EnableAccessibility = us.EnableAccessibility;
            EnableClipboardClearing = us.EnableClipboardClearing;
            ClipboardClearDelaySeconds = us.ClipboardClearDelaySeconds;
        }
    }
}
