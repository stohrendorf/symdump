namespace symdump.symfile.type
{
    public interface IWrappedType
    {
        int precedence { get; }
        
        IWrappedType inner { get; }

        string asCode(string name, string argList);
    }
}
