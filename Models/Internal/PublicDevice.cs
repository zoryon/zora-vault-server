using ZoraVault.Models.Entities;

namespace ZoraVault.Models.Internal
{
    public class PublicDevice
    {
        public Guid Id { get; set; }
        public byte[] PublicKey { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastSeen { get; set; } = null!;

        // Navigation properties
        public User User { get; set; } = null!;

        public PublicDevice(Device device) 
        {
            Id = device.Id;
            PublicKey = device.PublicKey;
            CreatedAt = device.CreatedAt;
            LastSeen = device.LastSeen;
        }
    }
}
