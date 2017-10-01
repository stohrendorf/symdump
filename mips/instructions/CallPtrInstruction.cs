using System.Collections.Generic;
using core;
using mips.disasm;
using mips.operands;
using static mips.disasm.RegisterUtil;

namespace mips.instructions
{
    public class CallPtrInstruction : Instruction
    {
        public override IOperand[] Operands { get; }

        public IOperand Target => Operands[0];
        public RegisterOperand ReturnAddressTarget => (RegisterOperand) (Operands.Length > 1 ? Operands[1] : null);

        public override uint? JumpTarget => (Target as LabelOperand)?.Address;

        public override IEnumerable<int> InputRegisters
        {
            get
            {
                switch (Target)
                {
                    case RegisterOperand r:
                        yield return ToInt(r.Register);
                        break;
                    case RegisterOffsetOperand r:
                        yield return ToInt(r.Register);
                        break;
                    case C0RegisterOperand r:
                        yield return ToInt(r.Register);
                        break;
                    case C2RegisterOperand r:
                        yield return ToInt(r.Register);
                        break;
                }
            }
        }

        public override IEnumerable<int> OutputRegisters
        {
            get
            {
                if (ReturnAddressTarget != null)
                    yield return ToInt(ReturnAddressTarget.Register);
            }
        }

        // ReSharper disable once SuggestBaseTypeForParameter
        public CallPtrInstruction(IOperand target, RegisterOperand returnAddressTarget)
        {
            Operands = returnAddressTarget == null ? new[] {target} : new[] {target, returnAddressTarget};
        }

        public override string AsReadable()
        {
            if (ReturnAddressTarget != null)
            {
                return ReturnAddressTarget.Register == Register.ra
                    ? $"{Target}()"
                    : $"${ReturnAddressTarget} = __RET_ADDR; {Target}()";
            }

            return $"goto {Target}";
        }

        public override IExpressionNode ToExpressionNode(IDataFlowState dataFlowState)
        {
            throw new System.NotImplementedException();
        }
    }
}
