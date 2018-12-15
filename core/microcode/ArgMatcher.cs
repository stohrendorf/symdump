namespace core.microcode
{
    public sealed class ArgMatcher
    {
        public static readonly ArgMatcher NotMatched = new ArgMatcher(null, -1);
        private readonly int _index;
        private readonly MicroInsn _insn;

        public ArgMatcher(MicroInsn insn) : this(insn, 0)
        {
        }

        private ArgMatcher(MicroInsn insn, int index)
        {
            _insn = insn;
            _index = index;
        }

        public ArgMatcher AnyArg()
        {
            return Next();
        }

        public ArgMatcher Arg<T>(out T result) where T : IMicroArg
        {
            result = default(T);

            if (_insn == null || _index >= _insn.Args.Count || !(_insn.Args[_index] is T casted))
                return NotMatched;

            result = casted;
            return Next();
        }

        public ArgMatcher Arg<T>() where T : IMicroArg
        {
            return Arg<T>(out _);
        }

        public ArgMatcher ArgRegIs(RegisterArg r)
        {
            return ArgRegIs(r.Register);
        }

        public ArgMatcher ArgRegIs(uint r)
        {
            if (!Arg<RegisterArg>(out var tmp).Matches() || r != tmp.Register)
                return NotMatched;
            return Next();
        }

        public ArgMatcher ArgRegIsNot(RegisterArg r)
        {
            if (Arg<RegisterArg>(out var tmp).Matches() && r.Register != tmp.Register)
                return Next();
            if (Arg<RegisterMemArg>(out var tmp2).Matches() && r.Register != tmp2.Register)
                return Next();
            return NotMatched;
        }

        public ArgMatcher ArgMemRegIs(RegisterArg r, out RegisterMemArg result)
        {
            result = null;

            if (!Arg(out result) || r.Register != result.Register)
                return NotMatched;
            return Next();
        }

        public ArgMatcher ArgMemRegIs(RegisterArg r)
        {
            return ArgMemRegIs(r, out _);
        }

        private bool Matches()
        {
            return _insn != null;
        }

        public static implicit operator bool(ArgMatcher m)
        {
            return m.Matches();
        }

        private ArgMatcher Next()
        {
            return new ArgMatcher(_insn, _index + 1);
        }
    }
}