using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using core.util;
using JetBrains.Annotations;

namespace core.cfg
{
    public class NotNode : Node
    {
        [NotNull] private readonly INode _inner;

        public override string Id => "not_" + _inner.Id;

        public override IEnumerable<int> InputRegisters => _inner.InputRegisters;

        public override IEnumerable<int> OutputRegisters => _inner.OutputRegisters;

        public NotNode([NotNull] INode inner) : base(inner.Graph)
        {
            Debug.Assert(!(inner is NotNode));
            Debug.Assert(inner.Outs.Count() >= 2);
            Debug.Assert(inner.Outs.All(e => e is TrueEdge || e is FalseEdge));

            _inner = inner;
            Graph.ReplaceNode(inner, this);
            
            var edges = Outs.ToList();
            foreach (var e in edges)
            {
                Graph.RemoveEdge(e);
                switch (e)
                {
                    case TrueEdge _:
                        Graph.AddEdge(new FalseEdge(e.From, e.To));
                        break;
                    case FalseEdge _:
                        Graph.AddEdge(new TrueEdge(e.From, e.To));
                        break;
                    default:
                        throw new Exception("Unexpected edge type: " + e.GetType().FullName);
                }
            }
        }

        public override bool ContainsAddress(uint address)
            => _inner.ContainsAddress(address);

        public override IEnumerable<Instruction> Instructions => _inner.Instructions;

        public override void Dump(IndentedTextWriter writer, IDataFlowState dataFlowState)
        {
            writer.WriteLine("!{");
            ++writer.Indent;
            _inner.Dump(writer, dataFlowState);
            --writer.Indent;
            writer.WriteLine("}");
        }
    }
}
