using ZoraVault.Models.Entities;

namespace ZoraVault.Models.Internal
{
    public class PublicUser
    {
        public Guid Id { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public KdfParams KdfParams { get; set; }
        public DateTime CreatedAt { get; set; }

        public PublicUser(User user)
        {
            Id = user.Id;
            Username = user.Username;
            Email = user.Email;
            KdfParams = user.KdfParams;
            CreatedAt = user.CreatedAt;
        }
    }
}
