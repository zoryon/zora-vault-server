namespace ZoraVault.Models.DTOs
{
    public class CreateSessionReqDTO
    {
        public Guid UserId { get; set; }
        public Guid DeviceId { get; set; }

        public CreateSessionReqDTO(Guid userId, Guid deviceId)
        {
            this.UserId = userId;
            this.DeviceId = deviceId;
        }
    }
}
