using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using core.microcode;
using JetBrains.Annotations;

namespace core.cfg
{
    public class SequenceNode : Node
    {
        public readonly IList<INode> Nodes = new List<INode>();

        public override string Id => "seq_" + Nodes[0].Id;

        public SequenceNode(INode first) : base(first.Graph)
        {
            Debug.Assert(IsCandidate(first));

            Debug.Assert(first.Outs.Count() == 1);
            Debug.Assert(first.Outs.First() is AlwaysEdge);

            var next = first.Outs.First().To;
            Debug.Assert(next.Ins.Count() == 1);

            if (!(first is SequenceNode sequence))
            {
                sequence = this;
                sequence.Nodes.Add(first);
                Simplify();
                Graph.ReplaceNode(first, this);
            }

            sequence.Nodes.Add(next);
            Simplify();

            var toReplace = next.Outs.ToList();
            Graph.RemoveNode(next);
            foreach (var e in toReplace)
            {
                Graph.AddEdge(e.CloneTyped(sequence, e.To));
            }

            Debug.Assert(!Graph.Nodes.Contains(next));
        }

        /// <summary>
        /// Joins adjoining instruction sequences. Assumes that only the last node needs to be joined. 
        /// </summary>
        private void Simplify()
        {
            if (Nodes.Count < 2)
                return;

            if (!(Nodes[Nodes.Count - 2] is InstructionCollection))
            {
                if (!(Nodes[Nodes.Count - 2] is InstructionSequence tmp))
                    return;

                Nodes[Nodes.Count - 2] = new InstructionCollection(tmp);
            }
            var first = Nodes[Nodes.Count - 2] as InstructionCollection;
            Debug.Assert(first != null);

            switch (Nodes[Nodes.Count - 1])
            {
                case InstructionSequence sequence:
                    foreach (var insn in sequence.InstructionList)
                    {
                        first.InstructionList.Add(insn);
                    }
                    Nodes.RemoveAt(Nodes.Count - 1);
                    return;

                case InstructionCollection collection:
                    foreach (var insn in collection.InstructionList)
                    {
                        first.InstructionList.Add(insn);
                    }
                    Nodes.RemoveAt(Nodes.Count - 1);
                    return;
            }
        }

        public override bool ContainsAddress(uint address)
        {
            return Nodes.Any(n => n.ContainsAddress(address));
        }

        public override IEnumerable<MicroInsn> Instructions => Nodes.SelectMany(node => node.Instructions);

        public static bool IsCandidate([NotNull] INode seq)
        {
            if (seq is EntryNode || seq is ExitNode)
                return false;

            if (seq.Outs.Count() != 1)
                return false;

            var next = seq.Outs.FirstOrDefault(e => e is AlwaysEdge)?.To;
            if (next == null || next is ExitNode)
                return false;

            if (next.Outs.Any(e => e.To.Equals(seq)))
                return false;

            return next.Ins.Count() == 1;
        }
    }
}
