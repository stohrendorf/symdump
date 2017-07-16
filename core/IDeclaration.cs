namespace core
{
    public interface IDeclaration
    {
        string name { get; }
        ICompoundType compoundType { get; }
    }
}
