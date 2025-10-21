namespace ZoraVault.Models.Internal.Common
{
    public class AuthContext
    {
        public Guid UserId { get; set; }
        public Guid DeviceId { get; set; }

        public AuthContext(Guid userId, Guid deviceId)
        {
            UserId = userId;
            DeviceId = deviceId;
        }
    }
}
