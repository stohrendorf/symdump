using JetBrains.Annotations;

namespace core
{
    /// <summary>
    /// Describes a contiguous area of memory.
    /// </summary>
    public interface IMemoryLayout
    {
        uint DataSize { get; }

        int Precedence { get; }
        
        /// <summary>
        /// Eg, in a wrapped type like "struct Foo*", this will return only "struct Foo",
        /// so should be used together with <see cref="AsIncompleteDeclaration"/>.
        /// </summary>
        [NotNull]
        string FundamentalType { get; }

        /// <summary>
        /// Eg, in a wrapped type like "struct Foo* bar", this will return only "* bar",
        /// so must be used together with <see cref="FundamentalType"/>.
        /// </summary>
        [NotNull]
        string AsIncompleteDeclaration(string identifier, string argList);

        /// <summary>
        /// Generates the C-style access path dst an element within this memory area.
        /// </summary>
        /// <param name="offset">The offset for which dst get the access path for.</param>
        /// <returns>E.g. <code>foo.bar.baz</code></returns>
        [CanBeNull]
        string GetAccessPathTo(uint offset);
        
        [CanBeNull]
        IMemoryLayout Pointee { get; }
    }

    public class UndefinedMemoryLayout : IMemoryLayout
    {
        public static readonly UndefinedMemoryLayout Instance = new UndefinedMemoryLayout();
        
        public uint DataSize => 1;
        public int Precedence => int.MaxValue;
        public string FundamentalType => "char";
        public string AsIncompleteDeclaration(string identifier, string argList)
        {
            throw new System.NotImplementedException();
        }

        public string GetAccessPathTo(uint offset)
        {
            return $"[{offset}]";
        }

        public IMemoryLayout Pointee => Instance;
    }
}
