using symdump.exefile.operands;

namespace symdump.exefile.instructions
{
    public class CaseTableEntry : Instruction
    {
        private readonly LabelOperand _labelOperand;

        public CaseTableEntry(LabelOperand labelOperand)
        {
            _labelOperand = labelOperand;
        }

        public override IOperand[] Operands { get; } = new IOperand[0];

        public override string ToString()
        {
            return $".word {_labelOperand}";
        }

        public override string AsReadable()
        {
            return ToString();
        }
    }
}