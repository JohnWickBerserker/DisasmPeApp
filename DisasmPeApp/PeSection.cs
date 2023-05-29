namespace DisasmPeApp
{
    public class PeSection
    {
        public string Name { get; set; }
        public uint RawAddress { get; set; }
        public uint RawLength { get; set; }
        public uint RvaAddress { get; set; }
        public uint RvaLength { get; set; }
    }
}