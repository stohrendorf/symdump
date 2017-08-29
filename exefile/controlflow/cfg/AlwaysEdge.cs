namespace exefile.controlflow.cfg
{
    public class AlwaysEdge : Edge
    {
        public AlwaysEdge(INode from, INode to) : base(from, to)
        {
        }

        public override IEdge CloneTyped(INode from, INode to)
        {
            return new AlwaysEdge(from, to);
        }
    }
}
