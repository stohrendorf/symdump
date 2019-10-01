using JetBrains.Annotations;
using symdump.exefile.operands;
using symdump.symfile;

namespace symdump.exefile.instructions
{
    public class CallPtrInstruction : Instruction
    {
        public CallPtrInstruction([NotNull] IOperand target, [CanBeNull] IOperand returnAddressTarget)
        {
            Operands = returnAddressTarget == null ? new[] {target} : new[] {target, returnAddressTarget};
        }

        public override IOperand[] Operands { get; }

        [NotNull] private IOperand Target => Operands[0];

        [CanBeNull]
        public RegisterOperand ReturnAddressTarget => (RegisterOperand) (Operands.Length > 1 ? Operands[1] : null);

        public override string AsReadable()
        {
            if (ReturnAddressTarget != null)
                return ReturnAddressTarget.Register == Register.ra
                    ? $"{Target}()"
                    : $"{ReturnAddressTarget} = __RET_ADDR; {Target}()";

            return $"goto {Target}";
        }
    }
}