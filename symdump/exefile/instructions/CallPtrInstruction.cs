using symdump.exefile.operands;
using symdump.symfile;

namespace symdump.exefile.instructions
{
    public class CallPtrInstruction : Instruction
    {
        public CallPtrInstruction(IOperand? target, IOperand? returnAddressTarget)
        {
            Operands = returnAddressTarget == null ? [target] : [target, returnAddressTarget];
        }

        public override IOperand?[] Operands { get; }

        private IOperand? Target => Operands[0];

        public RegisterOperand? ReturnAddressTarget => (RegisterOperand?) (Operands.Length > 1 ? Operands[1] : null);

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
