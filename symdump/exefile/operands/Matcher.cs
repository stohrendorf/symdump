using System.Collections.Generic;
using JetBrains.Annotations;
using symdump.exefile.instructions;

namespace symdump.exefile.operands
{
    public class Matcher
    {
        [NotNull] private readonly IReadOnlyDictionary<uint, Instruction> _instructions;
        [NotNull] private readonly Stack<KeyValuePair<uint, bool>> _pcs = new Stack<KeyValuePair<uint, bool>>();
        internal bool Matches = true;

        public Matcher(uint pc, [NotNull] IReadOnlyDictionary<uint, Instruction> instructions)
        {
            Pc = pc;
            _instructions = instructions;
        }

        public uint Pc { get; private set; }

        internal InsnMatcher<T> NextInsn<T>(out T typedInsn) where T : Instruction
        {
            typedInsn = null;

            Pc += 4;
            if (_instructions.TryGetValue(Pc - 4, out var insn) && (typedInsn = insn as T) != null)
                return new InsnMatcher<T>(this, typedInsn);

            Matches = false;
            return new InsnMatcher<T>(this, null);
        }

        internal InsnMatcher<T> NextInsn<T>() where T : Instruction
        {
            return NextInsn<T>(out _);
        }

        public void Savepoint()
        {
            _pcs.Push(new KeyValuePair<uint, bool>(Pc, Matches));
        }

        public void Restore()
        {
            (Pc, Matches) = _pcs.Pop();
        }

        public void Continue()
        {
            _pcs.Pop();
        }

        public void Retry()
        {
            Restore();
            Savepoint();
        }
    }
}
