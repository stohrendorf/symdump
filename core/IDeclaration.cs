using JetBrains.Annotations;

namespace core
{
    public interface IDeclaration
    {
        [NotNull]
        string Name { get; }

        [NotNull]
        IMemoryLayout MemoryLayout { get; }
    }
}
