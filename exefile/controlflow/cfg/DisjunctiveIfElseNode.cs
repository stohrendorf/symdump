using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using core;
using core.util;
using JetBrains.Annotations;

namespace exefile.controlflow.cfg
{
    public class DisjunctiveIfElseNode : Node
    {
        private class Term
        {
            public readonly bool Inverted;
            [NotNull] public readonly INode Condition;

            public Term(bool inverted, INode condition)
            {
                Inverted = inverted;
                Condition = condition;
            }
        }

        [NotNull] private readonly List<Term> _conditions = new List<Term>();

        [NotNull] private readonly INode _trueBody;
        [NotNull] private readonly INode _falseBody;

        public DisjunctiveIfElseNode([NotNull] INode trueBody)
            : base(trueBody.Graph)
        {
            Debug.Assert(IsCandidate(trueBody));

            var conditions = trueBody.Ins.Select(e => e.From).ToList();
            var current = conditions
                .First(n => n.Ins.Count(e => e is TrueEdge) != 1 || n.Ins.Count(e => e is TrueEdge) != 1);
            var entry = current;

            _trueBody = trueBody;
            while (conditions.Count > 0)
            {
                Debug.Assert(conditions.Contains(current));
                conditions.Remove(current);
                bool inverted = current.Outs.First(e => e is FalseEdge).To.Equals(trueBody);
                _conditions.Add(new Term(inverted, current));
                current = inverted
                    ? current.Outs.First(e => e is TrueEdge).To
                    : current.Outs.First(e => e is FalseEdge).To;
            }

            _falseBody = _conditions.Last().Condition.Outs.First(e => !e.To.Equals(trueBody)).To;
            var common = trueBody.Outs.First().To;
            Debug.Assert(_falseBody.Outs.Count() == 1 && _falseBody.Outs.First() is AlwaysEdge);
            Debug.Assert(_falseBody.Outs.First().To.Equals(common));
            
            Graph.ReplaceNode(entry, this);
            Graph.RemoveNode(trueBody);
            Graph.RemoveNode(_falseBody);
            foreach (var c in _conditions.Skip(1))
                Graph.RemoveNode(c.Condition);

            Graph.AddEdge(new AlwaysEdge(this, common));
        }

        public override SortedDictionary<uint, Instruction> Instructions
        {
            get
            {
                var tmp = new SortedDictionary<uint, Instruction>();

                foreach (var c in _conditions)
                foreach (var insn in c.Condition.Instructions)
                    tmp.Add(insn.Key, insn.Value);

                foreach (var insn in _trueBody.Instructions)
                    tmp.Add(insn.Key, insn.Value);

                return tmp;
            }
        }

        public override bool ContainsAddress(uint address) =>
            _conditions.Any(c => c.Condition.ContainsAddress(address)) || _trueBody.ContainsAddress(address);

        public override void Dump(IndentedTextWriter writer)
        {
            writer.WriteLine("if{");
            bool first = true;
            foreach (var c in _conditions)
            {
                if (!first)
                    writer.WriteLine(c.Inverted ? "|| !" : "||");
                else if (c.Inverted)
                    writer.WriteLine("!");
                first = false;

                ++writer.Indent;
                c.Condition.Dump(writer);
                --writer.Indent;
            }
            writer.WriteLine("} {");
            ++writer.Indent;
            _trueBody.Dump(writer);
            --writer.Indent;
            writer.WriteLine("} else {");
            ++writer.Indent;
            _falseBody.Dump(writer);
            --writer.Indent;
            writer.WriteLine("}");
        }

        public static bool IsCandidate([NotNull] INode trueBody)
        {
            if (trueBody is EntryNode || trueBody is ExitNode)
                return false;

            Console.WriteLine("Check: " + trueBody.Id);
            if (trueBody.Ins.Count() < 2 || trueBody.Outs.Count() != 1)
                return false;

            if (!(trueBody.Outs.First() is AlwaysEdge))
                return false;

            if (!trueBody.Ins.All(e => e is TrueEdge || e is FalseEdge))
                return false;

            var entries = trueBody.Ins
                .Select(e => e.From)
                .Where(n => n.Ins.Count(e => e is TrueEdge || e is FalseEdge) != 1 || n.Ins.Count() != 1);

            if (entries.Count() != 1)
                return false;

            var allIns = trueBody.Ins.Select(e => e.From).ToImmutableHashSet();
            var last = allIns
                .Where(n => n.Outs.Select(e => e.To).Any(n2 => !n2.Equals(trueBody) && !allIns.Contains(n2)))
                .ToList();
            
            if (last.Count != 1)
                return false;

            var falseBody = last[0].Outs.First(e => !e.To.Equals(trueBody)).To;
            if (falseBody.Outs.Count() != 1 || !(falseBody.Outs.First() is AlwaysEdge))
                return false;

            var common = falseBody.Outs.First().To;
            return trueBody.Outs.First().To.Equals(common);
        }
    }
}
