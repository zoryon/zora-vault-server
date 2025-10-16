namespace ZoraVault.Models.DTOs
{
    public class DeviceChallengeDTO
    {
        public string EncryptedChallenge { get; set; } = string.Empty;

        public DeviceChallengeDTO(string encryptedChallenge) 
        {
            EncryptedChallenge = encryptedChallenge;
        }
    }
}
