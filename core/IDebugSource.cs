using System.Collections.Generic;
using JetBrains.Annotations;

namespace core
{
    public interface IDebugSource
    {
        [ItemNotNull]
        IList<IFunction> functions { get; }

        [NotNull]
        SortedDictionary<uint, IList<NamedLocation>> labels { get; }

        [CanBeNull]
        IFunction findFunction(uint addr);

        [CanBeNull]
        IFunction findFunction(string name);

        [CanBeNull]
        IMemoryLayout findTypeDefinitionForLabel(string label);

        [NotNull]
        string getSymbolName(uint addr, int rel = 0);

        [CanBeNull]
        IMemoryLayout findTypeDefinition(string tag);
    }
}
