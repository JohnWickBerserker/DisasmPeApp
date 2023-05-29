using SharpDisasm;
using System.Linq;

namespace DisasmPeApp
{
    internal class DisasmHelper
    {
        public static Instruction Disasm(byte[] buffer, uint offset, uint codeOffset)
        {
            if (offset >= buffer.Length)
            {
                return null;
            }
            var disasm = new Disassembler(buffer, ArchitectureMode.x86_32, codeOffset, true, Vendor.Any, offset);
            return disasm.Disassemble().First();
        }
    }
}
