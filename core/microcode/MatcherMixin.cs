using System.Diagnostics;
using System.Linq;

namespace core.microcode
{
    internal static class MatcherMixin
    {
        public static ArgMatcher Is(this MicroInsn insn, params MicroOpcode[] opcode)
        {
            Debug.Assert(opcode.Length > 0);
            if (!opcode.Contains(insn.Opcode))
                return ArgMatcher.NotMatched;

            return new ArgMatcher(insn);
        }
    }
}
