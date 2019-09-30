using System;
using JetBrains.Annotations;
using symdump.exefile.instructions;

namespace symdump.exefile.operands
{
    public class InsnMatcher<T> where T : Instruction
    {
        public delegate bool ArgPredicate<in T2>([NotNull] T instruction, [NotNull] T2 operand) where T2 : IOperand;

        public delegate bool InsnPredicate([NotNull] T instruction);

        [CanBeNull] private readonly T _instruction;
        [NotNull] private readonly Matcher _matcher;
        private int _argIndex;


        internal InsnMatcher([NotNull] Matcher matcher, [CanBeNull] T instruction)
        {
            _matcher = matcher;
            _instruction = instruction;
        }

        public InsnMatcher<T> Arg<T2>(out T2 operand, [CanBeNull] ArgPredicate<T2> argPredicate = null)
            where T2 : class, IOperand
        {
            operand = null;

            if (!_matcher.Matches)
                return this;

            if (_instruction == null)
                throw new NullReferenceException("Matcher matches, but instruction is null");

            if (++_argIndex > _instruction.Operands.Length) _matcher.Matches = false;

            if (!_matcher.Matches)
                return this;

            operand = _instruction.Operands[_argIndex - 1] as T2;
            if (operand == null)
                _matcher.Matches = false;

            if (operand != null && argPredicate != null && _matcher.Matches)
                _matcher.Matches = argPredicate(_instruction, operand);

            if (!_matcher.Matches)
                operand = null;

            return this;
        }

        public InsnMatcher<T> Arg<T2>([CanBeNull] ArgPredicate<T2> argPredicate = null) where T2 : class, IOperand
        {
            return Arg(out _, argPredicate);
        }

        public InsnMatcher<T> AnyArg(out IOperand arg)
        {
            arg = null;

            if (!_matcher.Matches)
                return this;

            if (_instruction == null)
                throw new NullReferenceException("Matcher matches, but instruction is null");

            if (++_argIndex > _instruction.Operands.Length)
                _matcher.Matches = false;

            arg = _instruction.Operands[_argIndex - 1];
            return this;
        }

        public InsnMatcher<T> AnyArg()
        {
            return AnyArg(out _);
        }

        public InsnMatcher<T> Where([NotNull] InsnPredicate insnPredicate)
        {
            if (!_matcher.Matches)
                return this;

            if (_instruction == null)
                throw new NullReferenceException("Matcher matches, but instruction is null");

            _matcher.Matches = insnPredicate(_instruction);
            return this;
        }

        public InsnMatcher<T> ArgsDone()
        {
            if (_matcher.Matches)
            {
                if (_instruction == null)
                    throw new NullReferenceException("Matcher matches, but instruction is null");

                _matcher.Matches = _argIndex == _instruction.Operands.Length;
            }

            return this;
        }

        public InsnMatcher<T2> NextInsn<T2>() where T2 : Instruction
        {
            return _matcher.NextInsn<T2>();
        }
    }
}