namespace exefile.controlflow.cfg
{
    public abstract class Edge : IEdge
    {
        protected Edge(INode from, INode to)
        {
            From = from;
            To = to;
        }

        public INode From { get; set; }
        public INode To { get; set; }

        public override string ToString()
        {
            return $"-- 0x{From.Start:x8} -- {GetType().Name} -- 0x{To.Start:x8} -->";
        }
    }
}
