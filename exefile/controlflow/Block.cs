using System.Collections.Generic;
using System.IO;
using System.Linq;
using core;
using core.util;
using mips.instructions;

namespace exefile.controlflow
{
    public class Block
    {
        public ConditionalBranchInstruction condition;

        public Block trueExit;
        
        public Block falseExit;

        public uint start => instructions.Keys.First();
        
        public SortedDictionary<uint, Instruction> instructions { get; } = new SortedDictionary<uint, Instruction>();

        public ExitType? exitType; 

        public bool containsAddress(uint address)
        {
            if (instructions.Count == 0)
                return false;
            
            return address >= instructions.Keys.First() && address <= instructions.Keys.Last();
        }

        public void dump(IndentedTextWriter writer)
        {
            writer.WriteLine($"// exitType={exitType} start=0x{start:X}");
            if(trueExit != null)
                writer.WriteLine($"// trueExit=0x{trueExit.start:X}");
            if(falseExit != null)
                writer.WriteLine($"// falseExit=0x{falseExit.start:X}");
            
            ++writer.indent;
            foreach (var insn in instructions)
            {
                writer.WriteLine($"0x{insn.Key:X}  {insn.Value.asReadable()}");
            }
            --writer.indent;
        }

        public string plantUmlName => $"state_{start:X}";
        
        public void dumpPlantUml(TextWriter writer)
        {
            writer.WriteLine($"note left of {plantUmlName} : exitType={exitType}");
            if (exitType == ExitType.Return)
            {
                writer.WriteLine($"{plantUmlName} --> [*]");
            }

            foreach (var insn in instructions.Values)
            {
                writer.WriteLine($"{plantUmlName} : {insn.asReadable()}");
            }
            
            if(trueExit != null)
                writer.WriteLine($"{plantUmlName} -->  {trueExit.plantUmlName} : true");
            if(falseExit != null)
                writer.WriteLine($"{plantUmlName} -->  {falseExit.plantUmlName} : false");
        }
    }
}
