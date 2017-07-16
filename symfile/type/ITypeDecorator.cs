namespace symfile.type
{
    public interface ITypeDecorator
    {
        int precedence { get; }

        string asDeclaration(string identifier, string argList);
    }
}
