namespace DisasmPeApp.ViewModels
{
    internal class SubViewModel
    {
        public string Name { get; set; }
        public uint Rva { get; set; }
        public int Line { get; set; }

        public override string ToString()
        {
            return Rva.ToString("X8") + " " + Name;
        }
    }
}
