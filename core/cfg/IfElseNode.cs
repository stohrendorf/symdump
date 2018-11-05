using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using core.microcode;
using core.util;
using JetBrains.Annotations;

namespace core.cfg
{
    public class IfElseNode : Node
    {
        [NotNull] private readonly INode _condition;

        [NotNull] private readonly INode _trueBody;
        [NotNull] private readonly INode _falseBody;

        public override string Id => "ifelse_" + _condition.Id;

        public IfElseNode([NotNull] INode condition) : base(condition.Graph)
        {
            Debug.Assert(IsCandidate(condition));

            Debug.Assert(condition.Outs.Count() == 2);

            var trueEdge = condition.Outs.First(e => e is TrueEdge);
            Debug.Assert(trueEdge != null);
            Debug.Assert(trueEdge.From.Equals(condition));

            var falseEdge = condition.Outs.First(e => e is FalseEdge);
            Debug.Assert(falseEdge != null);
            Debug.Assert(falseEdge.From.Equals(condition));

            _trueBody = trueEdge.To;
            _falseBody = falseEdge.To;

            Debug.Assert(!_trueBody.Equals(_falseBody));
            Debug.Assert(_trueBody.Ins.Count() == 1);
            Debug.Assert(_falseBody.Ins.Count() == 1);
            
            Debug.Assert(_trueBody.Outs.Count() == 1);
            Debug.Assert(_falseBody.Outs.Count() == 1);

            var common = _trueBody.Outs.FirstOrDefault(e => e is AlwaysEdge)?.To;
            Debug.Assert(common != null);
            Debug.Assert(common.Equals(_falseBody.Outs.FirstOrDefault(e => e is AlwaysEdge)?.To));

            _condition = condition;
            
            Graph.ReplaceNode(_condition, this);
            Graph.RemoveNode(_trueBody);
            Graph.RemoveNode(_falseBody);
            Graph.AddEdge(new AlwaysEdge(this, common));
        }

        public override IEnumerable<MicroInsn> Instructions
        {
            get
            {
                foreach (var insn in _condition.Instructions) yield return insn;
                foreach (var insn in _trueBody.Instructions) yield return insn;
                foreach (var insn in _falseBody.Instructions) yield return insn;
            }
        }

        public override bool ContainsAddress(uint address) =>
            _condition.ContainsAddress(address) || _trueBody.ContainsAddress(address) ||
            _falseBody.ContainsAddress(address);

        public static bool IsCandidate([NotNull] INode condition)
        {
            if (condition is EntryNode || condition is ExitNode)
                return false;
            
            if (condition.Outs.Count() != 2)
                return false;

            var trueNode = condition.Outs.FirstOrDefault(e => e is TrueEdge)?.To;
            if (trueNode == null)
                return false;

            var falseNode = condition.Outs.FirstOrDefault(e => e is FalseEdge)?.To;
            if (falseNode == null)
                return false;

            if(trueNode.Ins.Count() != 1 || falseNode.Ins.Count() != 1)
                return false;
                
            if(trueNode.Outs.Count() != 1 || falseNode.Outs.Count() != 1)
                return false;

            if (trueNode.Equals(falseNode))
                return false;

            var common1 = trueNode.Outs.FirstOrDefault(e => e is AlwaysEdge)?.To;
            if(common1 == null)
                return false;
            var common2 = falseNode.Outs.FirstOrDefault(e => e is AlwaysEdge)?.To;
            if(common2 == null)
                return false;
                
            return common1.Equals(common2);
        }
    }
}
