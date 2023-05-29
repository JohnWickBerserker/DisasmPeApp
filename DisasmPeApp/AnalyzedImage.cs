using SharpDisasm;
using System.Collections.Generic;

namespace DisasmPeApp
{
    internal class AnalyzedImage
    {
        public PeImage Image = new PeImage();
        public SortedDictionary<uint, Instruction> Instructions = new SortedDictionary<uint, Instruction>();
        public SortedDictionary<uint, string> Subs = new SortedDictionary<uint, string>();
    }
}
