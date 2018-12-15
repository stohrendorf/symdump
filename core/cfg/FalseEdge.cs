namespace core.cfg
{
    public class FalseEdge : Edge
    {
        public FalseEdge(INode from, INode to) : base(from, to)
        {
        }

        public override IEdge CloneTyped(INode from, INode to)
        {
            return new FalseEdge(from, to);
        }
    }
}