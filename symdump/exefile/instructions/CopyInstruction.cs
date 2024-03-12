using symdump.exefile.operands;
using symdump.symfile;

namespace symdump.exefile.instructions
{
    public class CopyInstruction : Instruction
    {
        public CopyInstruction(IOperand? target, IOperand? source,
            string? castTarget = null, string? castSource = null)
        {
            if (source is RegisterOperand regOp && regOp.Register == Register.zero)
                source = ImmediateOperand.Zero;

            Operands = [target, source];
            CastTarget = castTarget;
            CastSource = castSource;
        }

        public string? CastTarget { get; }

        public string? CastSource { get; }

        public override IOperand?[] Operands { get; }

        public IOperand? Target => Operands[0];
        public IOperand? Source => Operands[1];

        public override string AsReadable()
        {
            var src = string.IsNullOrEmpty(CastSource) ? Source.ToString() : $"*(({CastSource}*){Source})";
            var dest = string.IsNullOrEmpty(CastTarget) ? Target.ToString() : $"*(({CastTarget}*){Target})";
            return $"{dest} = {src}";
        }

        public override string ToString()
        {
            return AsReadable();
        }
    }
}
