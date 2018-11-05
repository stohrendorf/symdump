using System.Collections.Generic;
using core.microcode;
using core.util;
using JetBrains.Annotations;

namespace core.cfg
{
    // We need this because nodes are considered equal if their Ids are equal,
    // thus adding a cloned node would effectively be a no-op, and can also
    // destroy the graph.
    public class DuplicatedNode<T> : Node where T : INode
    {
        [NotNull]
        public T Inner { get; }

        public override string Id => "dup_" + Inner.Id;

        public DuplicatedNode([NotNull] T inner) : base(inner.Graph)
        {
            Inner = inner;
        }

        public override bool ContainsAddress(uint address)
        {
            return Inner.ContainsAddress(address);
        }

        public override IEnumerable<MicroInsn> Instructions => Inner.Instructions;
    }
}
