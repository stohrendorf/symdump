using System.Collections.Generic;
using core.util;
using JetBrains.Annotations;

namespace core.cfg
{
    public interface INode
    {
        [NotNull]
        IGraph Graph { get; }

        [NotNull]
        IEnumerable<IEdge> Ins { get; }
        
        [NotNull]
        IEnumerable<IEdge> Outs { get; }
        
        bool ContainsAddress(uint address);
        
        [NotNull]
        IEnumerable<Instruction> Instructions { get; }
        
        void Dump([NotNull] IndentedTextWriter writer, [CanBeNull] IDataFlowState dataFlowState);
        
        string Id { get; }
        
        [NotNull]
        IEnumerable<int> InputRegisters { get; }
        
        [NotNull]
        IEnumerable<int> OutputRegisters { get; }
    }
}
