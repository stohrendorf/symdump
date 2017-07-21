using JetBrains.Annotations;

namespace core
{
    public interface IDeclaration
    {
        [NotNull]
        string name { get; }

        [NotNull]
        IMemoryLayout memoryLayout { get; }
    }
}
