using System.Collections.Generic;
using JetBrains.Annotations;

namespace core
{
    public interface IDebugSource
    {
        [ItemNotNull]
        IList<IFunction> Functions { get; }

        [NotNull]
        SortedDictionary<uint, IList<NamedLocation>> Labels { get; }

        [CanBeNull]
        IFunction FindFunction(uint globalAddress);

        [CanBeNull]
        IFunction FindFunction(string name);

        [CanBeNull]
        IMemoryLayout FindTypeDefinitionForLabel(string label);

        [NotNull]
        string GetSymbolName(uint localAddress, int relative = 0);

        [CanBeNull]
        IMemoryLayout FindTypeDefinition(string tag);
    }
}
