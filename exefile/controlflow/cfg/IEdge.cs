using JetBrains.Annotations;

namespace exefile.controlflow.cfg
{
    public interface IEdge
    {
        [NotNull] INode From { get; set; }
        [NotNull] INode To { get; set; }
    }
}
