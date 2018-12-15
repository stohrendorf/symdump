using System.Collections.Generic;

namespace core.microcode
{
    public sealed class MicroAssemblyBlock
    {
        public readonly uint Address;
        public readonly IDictionary<uint, JumpType> Ins = new Dictionary<uint, JumpType>();
        public readonly List<MicroInsn> Insns = new List<MicroInsn>();
        public readonly ISet<uint> OwningFunctions = new HashSet<uint>();
        public IDictionary<uint, JumpType> Outs = new Dictionary<uint, JumpType>();

        public MicroAssemblyBlock(uint address)
        {
            Address = address;
        }

        public void Add(MicroInsn insn)
        {
            Insns.Add(insn);
        }

        public void Add(MicroOpcode opcode, params IMicroArg[] args)
        {
            Add(new MicroInsn(opcode, args));
        }

        public override string ToString()
        {
            return string.Join("\n", Insns);
        }

        public void Optimize(IDebugSource debugSource, ref long before, ref long after,
            ICollection<Peephole1Delegate> customPeephole1, ICollection<Peephole2Delegate> customPeephole2)
        {
            before += Insns.Count;
            PeepholeOptimizer.Optimize(Insns, debugSource, customPeephole1, customPeephole2);
            after += Insns.Count;
        }
    }
}