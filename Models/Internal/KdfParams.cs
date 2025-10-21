namespace ZoraVault.Models.Internal
{
    public class KdfParams
    {
        public required string Algorithm { get; set; }
        public required int Iterations { get; set; }
        public required int KeyLength { get; set; }
        public required int MemoryKb { get; set; }
        public required int Parallelism { get; set; }
    }
}
