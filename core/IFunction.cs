using System.Collections.Generic;
using core.util;
using JetBrains.Annotations;

namespace core
{
    public interface IFunction
    {
        [NotNull] IEnumerable<KeyValuePair<uint, IDeclaration>> RegisterParameters { get; }

        [NotNull] IEnumerable<KeyValuePair<int, IDeclaration>> StackParameters { get; }

        [NotNull] string Name { get; }

        uint GlobalAddress { get; }

        IMemoryLayout ReturnType { get; }
        void Dump([NotNull] IndentedTextWriter writer);

        [NotNull]
        string GetSignature();
    }
}