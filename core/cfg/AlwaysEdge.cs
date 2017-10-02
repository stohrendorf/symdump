using JetBrains.Annotations;

namespace core.cfg
{
    public class AlwaysEdge : Edge
    {
        public AlwaysEdge([NotNull] INode from, [NotNull] INode to) : base(from, to)
        {
        }

        public override IEdge CloneTyped(INode from, INode to)
        {
            return new AlwaysEdge(from, to);
        }
    }
}
