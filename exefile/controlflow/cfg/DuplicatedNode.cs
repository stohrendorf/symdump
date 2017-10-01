using System.Collections.Generic;
using core;
using core.util;
using JetBrains.Annotations;

namespace exefile.controlflow.cfg
{
    // We need this because nodes are considered equal if their Ids are equal,
    // thus adding a cloned node would effectively be a no-op, and can also
    // destroy the graph.
    public class DuplicatedNode<T> : Node where T : INode
    {
        [NotNull]
        public T Inner { get; }

        public override string Id => "dup_" + Inner.Id;

        public override IEnumerable<int> InputRegisters => Inner.InputRegisters;

        public override IEnumerable<int> OutputRegisters => Inner.OutputRegisters;

        public DuplicatedNode([NotNull] T inner) : base(inner.Graph)
        {
            Inner = inner;
        }

        public override bool ContainsAddress(uint address)
        {
            return Inner.ContainsAddress(address);
        }

        public override IEnumerable<Instruction> Instructions => Inner.Instructions;

        public override void Dump(IndentedTextWriter writer, IDataFlowState dataFlowState)
            => Inner.Dump(writer, dataFlowState);
    }
}
