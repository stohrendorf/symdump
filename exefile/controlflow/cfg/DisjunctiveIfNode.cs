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
    public class DisjunctiveIfNode : Node
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

        [NotNull] private readonly INode _body;

        public DisjunctiveIfNode([NotNull] INode body)
            : base(body.Graph)
        {
            Debug.Assert(IsCandidate(body));

            var conditions = body.Ins.Select(e => e.From).ToList();
            var current = conditions
                .First(n => n.Ins.Count(e => e is TrueEdge) != 1 || n.Ins.Count(e => e is TrueEdge) != 1);
            var entry = current;

            _body = body;
            while (conditions.Count > 0)
            {
                Debug.Assert(conditions.Contains(current));
                conditions.Remove(current);
                bool inverted = current.Outs.First(e => e is FalseEdge).To.Equals(body);
                _conditions.Add(new Term(inverted, current));
                current = inverted
                    ? current.Outs.First(e => e is TrueEdge).To
                    : current.Outs.First(e => e is FalseEdge).To;
            }

            var common = body.Outs.First().To;
            Graph.ReplaceNode(entry, this);
            Graph.RemoveNode(body);
            var outs = Outs.ToList();
            foreach (var e in outs)
                Graph.RemoveEdge(e);
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

                foreach (var insn in _body.Instructions)
                    tmp.Add(insn.Key, insn.Value);

                return tmp;
            }
        }

        public override bool ContainsAddress(uint address) =>
            _conditions.Any(c => c.Condition.ContainsAddress(address)) || _body.ContainsAddress(address);

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
            _body.Dump(writer);
            --writer.Indent;
            writer.WriteLine("}");
        }

        public static bool IsCandidate([NotNull] INode body)
        {
            if (body is EntryNode || body is ExitNode)
                return false;

            if (body.Ins.Count() < 2 || body.Outs.Count() != 1)
                return false;

            if (!(body.Outs.First() is AlwaysEdge))
                return false;

            if (!body.Ins.All(e => e is TrueEdge || e is FalseEdge))
                return false;

            var entries = body.Ins
                .Select(e => e.From)
                .Where(n => n.Ins.Count(e => e is TrueEdge || e is FalseEdge) != 1 || n.Ins.Count() != 1);

            if (entries.Count() != 1)
                return false;

            var allIns = body.Ins.Select(e => e.From).ToImmutableHashSet();
            var last = allIns
                .Where(n => n.Outs.Select(e => e.To).Any(n2 => !n2.Equals(body) && !allIns.Contains(n2)))
                .ToList();
            
            if (last.Count != 1)
                return false;

            var common = last[0].Outs.First(e => !e.To.Equals(body)).To;
            return body.Outs.First().To.Equals(common);
        }
    }
}
