using System.ComponentModel.DataAnnotations;

namespace ZoraVault.Models.Internal
{
    public class KdfParams
    {
        [MaxLength(32)]
        public required string Algorithm { get; set; }

        [Range(100_000, 10_000_000)]
        public required int Iterations { get; set; }

        [Range(128, 4096)]
        public required int KeyLength { get; set; }

        [Range(32, 1024 * 1024)]
        public required int MemoryKb { get; set; }

        [Range(1, 16)]
        public required int Parallelism { get; set; }
    }
}
