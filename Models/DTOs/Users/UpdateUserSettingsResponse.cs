using ZoraVault.Models.Entities;
using ZoraVault.Models.Internal.Enum;

namespace ZoraVault.Models.DTOs.Users
{
    public class UpdateUserSettingsResponse
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid DeviceId { get; set; }

        // Server-enforced technical settings
        public int SessionTimeoutMinutes { get; set; } = 3;

        // Preference Settings
        public ThemeType Theme { get; set; } = ThemeType.Dark;

        public UpdateUserSettingsResponse(UserSettings us)
        {
            Id = us.Id;
            UserId = us.UserId;
            DeviceId = us.DeviceId;
            SessionTimeoutMinutes = us.SessionTimeoutMinutes;
            Theme = us.Theme;
        }
    }
}
