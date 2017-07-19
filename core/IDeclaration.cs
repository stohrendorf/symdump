namespace core
{
    public interface IDeclaration
    {
        string name { get; }
        IMemoryLayout memoryLayout { get; }
    }
}
