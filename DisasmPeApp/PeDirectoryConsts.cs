using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DisasmPeApp
{
    internal class PeDirectoryConsts
    {
        public const int IMAGE_EXPORT_DIRECTORY = 0;
        public const int IMAGE_DIRECTORY_ENTRY_IMPORT = 1;
        public const int IMAGE_DIRECTORY_ENTRY_RESOURCE = 2;
        public const int IMAGE_DIRECTORY_ENTRY_BOUND_IMPORT = 11;
        public const int IMAGE_DIRECTORY_ENTRY_DELAY_IMPORT = 15;
    }
}
