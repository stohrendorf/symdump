using System;
using JetBrains.Annotations;

namespace symdump.exefile.operands
{
    public class OptionalMatching : IDisposable
    {
        private readonly bool _except;
        [NotNull] private readonly Matcher _matcher;

        public OptionalMatching([NotNull] Matcher matcher, bool except)
        {
            _matcher = matcher;
            _except = except;
            _matcher.Savepoint();
        }

        public void Dispose()
        {
            if (_matcher.Matches != _except)
            {
                _matcher.Continue();
                _matcher.Matches = true;
            }
            else
            {
                _matcher.Restore();
            }
        }
    }
}
