namespace DisasmPeApp
{
    internal class PeExportFunction
    {
        public uint FunctionRva;
        public string? Name;
        public uint Ordinal;

        public PeExportFunction(uint functionRva)
        {
            this.FunctionRva = functionRva;
        }

        public PeExportFunction(uint functionRva, string name, ushort ordinal)
        {
            this.FunctionRva = functionRva;
            this.Name = name;
            this.Ordinal = ordinal;
        }

        public override string ToString()
        {
            return FunctionRva.ToString("X8") + " " + Ordinal + " " + Name;
        }
    }
}