namespace ZoraVault.Models.Entities
{
    public class UserDevice
    {
        public Guid UserId { get; set; }
        public Guid DeviceId { get; set; }

        public DateTime LinkedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public User User { get; set; } = null!;
        public Device Device { get; set; } = null!;
    }
}
