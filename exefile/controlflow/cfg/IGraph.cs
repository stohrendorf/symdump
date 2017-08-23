using System.Collections.Generic;
using JetBrains.Annotations;

namespace exefile.controlflow.cfg
{
    public interface IGraph
    {
        [NotNull] IEnumerable<INode> Nodes { get; }
        [NotNull] IEnumerable<IEdge> Edges { get; }

        [NotNull]
        IEnumerable<IEdge> GetIns([NotNull] INode node);

        [NotNull]
        IEnumerable<IEdge> GetOuts([NotNull] INode node);

        void ReplaceNode([NotNull] INode oldNode, [NotNull] INode newNode);
        
        void RemoveNode([NotNull] INode node);
        
        void AddEdge([NotNull] IEdge edge);
        
        void RemoveEdge([NotNull] IEdge edge);

        bool Contains([NotNull] INode node);
    }
}
