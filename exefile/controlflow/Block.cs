using System.Collections.Generic;
using System.Linq;
using core;
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

        public bool containsAddress(uint address)
        {
            if (instructions.Count == 0)
                return false;
            
            return address >= instructions.Keys.First() && address <= instructions.Keys.Last();
        }
    }
}
