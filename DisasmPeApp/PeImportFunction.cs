namespace DisasmPeApp
{
    internal class PeImportFunction
    {
        public PeImportFunction(ushort hint, string name)
        {
            Hint = hint;
            Name = name;
        }

        public ushort Hint { get; set; }
        public string Name { get; set; }

        public override string ToString()
        {
            return Hint + " " + Name;
        }
    }
}
