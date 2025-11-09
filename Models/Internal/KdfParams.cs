using System.ComponentModel.DataAnnotations;

namespace ZoraVault.Models.Internal
{
    public class KdfParams
    {
        [MaxLength(32)]
        public required string Algorithm { get; set; }

        [Range(2, 30)]
        public required int Iterations { get; set; }

        [Range(128, 4096)]
        public required int KeyLength { get; set; }

        [Range(32 * 1024, 512 * 1024)]
        public required int MemoryKiB { get; set; }

        [Range(1, 64)]
        public required int Parallelism { get; set; }
    }
}
