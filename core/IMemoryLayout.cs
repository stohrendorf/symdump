using JetBrains.Annotations;

namespace core
{
    /// <summary>
    /// Describes a contiguous area of memory.
    /// </summary>
    public interface IMemoryLayout
    {
        uint dataSize { get; }

        int precedence { get; }
        
        /// <summary>
        /// Eg, in a wrapped type like "struct Foo*", this will return only "struct Foo",
        /// so should be used together with <see cref="asIncompleteDeclaration"/>.
        /// </summary>
        [NotNull]
        string fundamentalType { get; }

        /// <summary>
        /// Eg, in a wrapped type like "struct Foo* bar", this will return only "* bar",
        /// so must be used together with <see cref="fundamentalType"/>.
        /// </summary>
        [NotNull]
        string asIncompleteDeclaration(string identifier, string argList);

        /// <summary>
        /// Generates the C-style access path to an element within this memory area.
        /// </summary>
        /// <param name="offset">The offset for which to get the access path for.</param>
        /// <returns>E.g. <code>foo.bar.baz</code></returns>
        [CanBeNull]
        string getAccessPathTo(uint offset);
    }
}
