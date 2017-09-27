using System.Collections.Generic;
using core;
using core.util;
using JetBrains.Annotations;

namespace exefile.controlflow.cfg
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
        
        void Dump([NotNull] IndentedTextWriter writer);
        
        string Id { get; }
    }
}
