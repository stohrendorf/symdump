using System.Collections.Generic;
using core.util;
using JetBrains.Annotations;

namespace core
{
    public interface IFunction
    {
        void Dump([NotNull] IndentedTextWriter writer);
        
        [NotNull]
        IEnumerable<KeyValuePair<int, IDeclaration>> RegisterParameters { get; }

        [NotNull]
        IEnumerable<KeyValuePair<int, IDeclaration>> StackParameters { get; }

        [NotNull]
        string GetSignature();
        
        [NotNull]
        string Name { get; }
        
        uint Address { get; }
        
        IMemoryLayout ReturnType { get; }
    }
}
