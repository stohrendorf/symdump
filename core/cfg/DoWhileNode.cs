using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using core.microcode;
using JetBrains.Annotations;

namespace core.cfg
{
    public class DoWhileNode : Node
    {
        [NotNull] private readonly INode _body;
        [NotNull] private readonly INode _condition;

        private readonly bool _invertedCondition;

        public DoWhileNode([NotNull] INode body)
            : base(body.Graph)
        {
            Debug.Assert(IsCandidate(body));

            Debug.Assert(body.Outs.Count() == 1);
            Debug.Assert(body.Outs.All(e => e is AlwaysEdge));

            var condition = body.Outs.First().To;
            Debug.Assert(condition.Outs.Count() == 2);
            Debug.Assert(condition.Outs.Count(e => e is TrueEdge) == 1);
            Debug.Assert(condition.Outs.Count(e => e is FalseEdge) == 1);

            var trueEdge = condition.Outs.First(e => e is TrueEdge);
            var falseEdge = condition.Outs.First(e => e is FalseEdge);

            var trueNode = trueEdge.To;
            var falseNode = falseEdge.To;
            Debug.Assert(body.Equals(falseNode) || body.Equals(trueNode));
            _invertedCondition = !body.Equals(trueNode);

            _condition = condition;
            _body = body;

            var loop = _condition.Outs.First(e => e.To.Equals(_body));
            Graph.RemoveEdge(loop);

            Graph.RemoveEdge(_body.Outs.First());
            Graph.ReplaceNode(_body, this);
            Debug.Assert(_condition.Outs.Count() == 1);
            var outgoing = _condition.Outs.First();
            Graph.RemoveEdge(outgoing);
            Graph.AddEdge(new AlwaysEdge(this, outgoing.To));
            Graph.RemoveNode(_condition);
        }

        public override string Id => "dowhile_" + _body.Id;

        public override IEnumerable<MicroInsn> Instructions
        {
            get
            {
                foreach (var insn in _condition.Instructions) yield return insn;
                foreach (var insn in _body.Instructions) yield return insn;
            }
        }

        public override bool ContainsAddress(uint address)
        {
            return _condition.ContainsAddress(address) || _body.ContainsAddress(address);
        }

        public static bool IsCandidate([NotNull] INode body)
        {
            if (body is EntryNode || body is ExitNode)
                return false;

            if (body.Outs.Count() != 1)
                return false;

            var condition = body.Outs.FirstOrDefault(e => e is AlwaysEdge)?.To;
            if (condition == null)
                return false;

            if (condition.Ins.Count() != 1)
                return false;

            if (condition.Outs.Count() != 2)
                return false;

            var trueEdge = condition.Outs.FirstOrDefault(e => e is TrueEdge);
            if (trueEdge == null)
                return false;

            var falseEdge = condition.Outs.FirstOrDefault(e => e is FalseEdge);
            if (falseEdge == null)
                return false;

            return trueEdge.To.Equals(body) || falseEdge.To.Equals(body);
        }
    }
}