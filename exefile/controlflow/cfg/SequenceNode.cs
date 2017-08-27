using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using core;
using core.util;
using JetBrains.Annotations;

namespace exefile.controlflow.cfg
{
    public class SequenceNode : Node
    {
        public readonly IList<INode> Nodes = new List<INode>();

        public SequenceNode(INode first) : base(first.Graph)
        {
            Debug.Assert(IsCandidate(first));

            Debug.Assert(first.Outs.Count() == 1);
            Debug.Assert(first.Outs.First() is AlwaysEdge);

            var next = first.Outs.First().To;
            Debug.Assert(next.Ins.Count() == 1);

            var seq = first as SequenceNode;
            if (seq == null)
            {
                seq = this;
                seq.Nodes.Add(first);
                Graph.ReplaceNode(first, this);
            }

            seq.Nodes.Add(next);

            var toReplace = next.Outs.ToList();
            Graph.RemoveNode(next);
            foreach (var e in toReplace)
            {
                e.From = seq;
                Graph.AddEdge(e);
            }
            
            Debug.Assert(!Graph.Nodes.Contains(next));
        }

        public override bool ContainsAddress(uint address)
        {
            return Nodes.Any(n => n.ContainsAddress(address));
        }

        public override SortedDictionary<uint, Instruction> Instructions
        {
            get
            {
                var tmp = new SortedDictionary<uint, Instruction>();
                foreach (var node in Nodes)
                {
                    foreach (var insn in node.Instructions)
                    {
                        tmp.Add(insn.Key, insn.Value);
                    }
                }
                return tmp;
            }
        }

        public override void Dump(IndentedTextWriter writer)
        {
            foreach (var node in Nodes)
            {
                node.Dump(writer);
            }
        }

        public static bool IsCandidate([NotNull] INode seq)
        {
            if (seq is EntryNode || seq is ExitNode)
                return false;
            
            if (seq.Outs.Count() != 1)
                return false;

            var next = seq.Outs.FirstOrDefault(e => e is AlwaysEdge)?.To;
            if (next == null || next is ExitNode)
                return false;

            return next.Ins.Count() == 1;
        }
    }
}
