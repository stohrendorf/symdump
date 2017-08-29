namespace exefile.controlflow.cfg
{
    public class TrueEdge : Edge
    {
        public TrueEdge(INode from, INode to) : base(from, to)
        {
        }

        public override IEdge CloneTyped(INode from, INode to)
        {
            return new TrueEdge(from, to);
        }
    }
}
