using JetBrains.Annotations;

namespace core.cfg
{
    public interface IEdge
    {
        [NotNull] INode From { get; }

        [NotNull] INode To { get; }

        [NotNull]
        IEdge CloneTyped([NotNull] INode from, [NotNull] INode to);
    }
}