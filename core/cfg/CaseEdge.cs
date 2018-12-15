using JetBrains.Annotations;

namespace core.cfg
{
    public class CaseEdge : Edge
    {
        public CaseEdge([NotNull] INode from, [NotNull] INode to, uint caseIndex) : base(from, to)
        {
            CaseIndex = caseIndex;
        }

        public uint CaseIndex { get; }

        public override IEdge CloneTyped(INode from, INode to)
        {
            return new CaseEdge(from, to, CaseIndex);
        }
    }
}