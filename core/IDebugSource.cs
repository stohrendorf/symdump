using System.Collections.Generic;

namespace core
{
    public interface IDebugSource
    {
        IList<IFunction> functions { get; }
        
        IDictionary<uint, IList<NamedLocation>> labels { get; }
        
        IFunction findFunction(uint addr);
        IFunction findFunction(string name);
        IMemoryLayout findTypeDefinitionForLabel(string label);
        string getSymbolName(uint addr, int rel = 0);
        IMemoryLayout findTypeDefinition(string tag);
    }
}
