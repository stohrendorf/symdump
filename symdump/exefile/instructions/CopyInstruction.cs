using JetBrains.Annotations;
using symdump.exefile.operands;
using symdump.symfile;

namespace symdump.exefile.instructions
{
    public class CopyInstruction : Instruction
    {
        public CopyInstruction([NotNull] IOperand target, [NotNull] IOperand source,
            [CanBeNull] string castTarget = null, [CanBeNull] string castSource = null)
        {
            if (source is RegisterOperand regOp && regOp.Register == Register.zero)
                source = ImmediateOperand.Zero;

            Operands = new[] {target, source};
            CastTarget = castTarget;
            CastSource = castSource;
        }

        [CanBeNull] public string CastTarget { get; }

        [CanBeNull] public string CastSource { get; }

        public override IOperand[] Operands { get; }

        public IOperand Target => Operands[0];
        public IOperand Source => Operands[1];

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
