using System.Collections.Generic;
using core.util;
using JetBrains.Annotations;

namespace core
{
    public interface IFunction
    {
        void dump([NotNull] IndentedTextWriter writer);
        
        [NotNull]
        IEnumerable<KeyValuePair<int, IDeclaration>> registerParameters { get; }

        [NotNull]
        IEnumerable<KeyValuePair<int, IDeclaration>> stackParameters { get; }

        [NotNull]
        string getSignature();
        
        [NotNull]
        string name { get; }
        
        uint address { get; }
        
        IMemoryLayout returnType { get; }
    }
}
