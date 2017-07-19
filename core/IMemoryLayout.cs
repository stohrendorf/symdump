namespace core
{
    /// <summary>
    /// Describes a contiguous area of memory.
    /// </summary>
    public interface IMemoryLayout
    {
        uint dataSize { get; }

        int precedence { get; }

        string asDeclaration(string identifier, string argList);

        /// <summary>
        /// Generates the C-style access path to an element within this memory area.
        /// </summary>
        /// <param name="offset">The offset for which to get the access path for.</param>
        /// <returns>E.g. <code>foo.bar.baz</code></returns>
        string getAccessPathTo(uint offset);
    }
}
