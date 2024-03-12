using System.Collections.Generic;
using System.Text;
using symdump.exefile.operands;
using symdump.symfile;

namespace symdump.exefile.instructions
{
    public class SwitchInstruction : Instruction
    {
        private readonly Register _boolTestRegister;
        private readonly uint _caseCount;
        private readonly LabelOperand? _caseTable;
        private readonly Register _caseValueRegister;
        private readonly LabelOperand? _defaultLabel;
        private readonly Register _shiftedCaseValue;

        public readonly IList<LabelOperand?> Cases = new List<LabelOperand?>();

        public SwitchInstruction(LabelOperand? caseTable, uint caseCount, LabelOperand? defaultLabel,
            Register caseValueRegister, Register boolTestRegister, Register shiftedCaseValue)
        {
            _caseTable = caseTable;
            _caseCount = caseCount;
            _defaultLabel = defaultLabel;
            _caseValueRegister = caseValueRegister;
            _boolTestRegister = boolTestRegister;
            _shiftedCaseValue = shiftedCaseValue;

            Operands =
            [
                caseTable,
                new ImmediateOperand(caseCount),
                defaultLabel,
                new RegisterOperand(caseValueRegister),
                new RegisterOperand(boolTestRegister)
            ];
        }

        public override IOperand?[] Operands { get; }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append(
                $"switch {_caseTable}[${_caseValueRegister} < {_caseCount}] ?? {_defaultLabel}, clobber ${_boolTestRegister} ${_shiftedCaseValue}");
            for (var i = 0; i < Cases.Count; i++) sb.Append($"\n# - case {i}: {Cases[i]}");

            return sb.ToString();
        }

        public override string AsReadable()
        {
            return ToString();
        }
    }
}
