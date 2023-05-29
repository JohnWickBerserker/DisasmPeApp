using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DisasmPeApp
{
    internal class PeImportDescriptor
    {
        public uint OriginalFirstThunkRva { get; set; }
        public uint TimeDateStamp { get; set; }
        public uint ForwarderChain { get; set; }
        public uint NameRva { get; set; }
        public uint FirstThunkRva { get; set; }
        public string? Name { get; set; }
        public List<PeImportFunction> Functions { get; set; } = new List<PeImportFunction>();

        public bool IsNull()
        {
            return OriginalFirstThunkRva == 0 && NameRva == 0 && FirstThunkRva == 0;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
