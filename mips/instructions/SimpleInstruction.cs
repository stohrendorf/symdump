using System;
using System.Collections.Generic;
using System.Linq;
using core;
using mips.operands;
using static mips.disasm.RegisterUtil;

namespace mips.instructions
{
    public class SimpleInstruction : Instruction
    {
        public readonly string Format;
        public readonly string Mnemonic;

        public override uint? JumpTarget => null;
        public override IEnumerable<int> InputRegisters => OutputRegisters;

        public override IEnumerable<int> OutputRegisters
        {
            get
            {
                foreach (var operand in Operands)
                {
                    switch (operand)
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
        }

        public SimpleInstruction(string mnemonic, string format, params IOperand[] operands)
        {
            Mnemonic = mnemonic;
            Operands = operands;
            Format = format;
        }

        public override IOperand[] Operands { get; }

        public override string ToString()
        {
            var args = string.Join(", ", Operands.Select(o => o.ToString()));
            return $"{Mnemonic} {args}".Trim();
        }

        public override string AsReadable()
        {
            return Format == null ? ToString() : string.Format(Format, Operands);
        }

        public override IExpressionNode ToExpressionNode(IDataFlowState dataFlowState)
        {
            throw new Exception("Cannot convert simple instruction to expression node");
        }
    }
}
