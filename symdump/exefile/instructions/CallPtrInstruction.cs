using System.Collections.Generic;
using symdump.exefile.expression;
using symdump.exefile.operands;
using symdump.symfile;

namespace symdump.exefile.instructions
{
    public class CallPtrInstruction : Instruction
    {
        public override IOperand[] operands { get; }

        public IOperand target => operands[0];
        public RegisterOperand returnAddressTarget => (RegisterOperand) (operands.Length > 1 ? operands[1] : null);

        public CallPtrInstruction(IOperand target, RegisterOperand returnAddressTarget)
        {
            operands = returnAddressTarget == null ? new[] {target} : new[] {target, returnAddressTarget};
        }

        public override string asReadable()
        {
            if (returnAddressTarget != null)
            {
                return returnAddressTarget.register == Register.ra ? $"{target}()" : $"{returnAddressTarget} = __RET_ADDR; {target}()";
            }

            return $"goto {target}";
        }

        public override IExpressionNode toExpressionNode(IReadOnlyDictionary<Register, IExpressionNode> registers)
        {
            throw new System.NotImplementedException();
        }
    }
}