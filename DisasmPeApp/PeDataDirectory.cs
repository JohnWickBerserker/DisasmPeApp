using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DisasmPeApp
{
    internal class PeDataDirectory
    {
        public uint Rva { get; set; }
        public uint Size { get; set; }
    }
}
