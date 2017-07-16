using System.Collections.Generic;
using core.util;

namespace core
{
    public interface IFunction
    {
        void dump(IndentedTextWriter writer);
        
        IEnumerable<KeyValuePair<int, IDeclaration>> registerParameters { get; }

        string getSignature();
        
        string name { get; }
        
        uint address { get; }
    }
}
