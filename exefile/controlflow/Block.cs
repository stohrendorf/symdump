using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using core;
using core.util;

namespace exefile.controlflow
{
    public class Block : IBlock
    {
        public IBlock TrueExit { get; set; }

        public IBlock FalseExit { get; set; }

        public uint Start => Instructions.Keys.First();

        public SortedDictionary<uint, Instruction> Instructions { get; } = new SortedDictionary<uint, Instruction>();

        public ExitType? ExitType { get; set; }

        public bool ContainsAddress(uint address)
        {
            if (Instructions.Count == 0)
                return false;

            return address >= Instructions.Keys.First() && address <= Instructions.Keys.Last();
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            var writer = new IndentedTextWriter(new StringWriter(sb));
            Dump(writer);
            return sb.ToString();
        }
        
        public void Dump(IndentedTextWriter writer)
        {
            writer.WriteLine($"// exitType={ExitType} start=0x{Start:X}");
            if (TrueExit != null)
                writer.WriteLine($"// trueExit=0x{TrueExit.Start:X}");
            if (FalseExit != null)
                writer.WriteLine($"// falseExit=0x{FalseExit.Start:X}");

            ++writer.Indent;
            foreach (var insn in Instructions)
            {
                writer.WriteLine($"0x{insn.Key:X}  {insn.Value.AsReadable()}");
            }
            --writer.Indent;
        }
    }
}
