using System.Collections.Generic;
using core.microcode;
using JetBrains.Annotations;

namespace core.cfg
{
    public interface INode
    {
        [NotNull] IGraph Graph { get; }

        [NotNull] IEnumerable<IEdge> Ins { get; }

        [NotNull] IEnumerable<IEdge> Outs { get; }

        bool ContainsAddress(uint address);

        [NotNull] IEnumerable<MicroInsn> Instructions { get; }

        string Id { get; }
    }
}
