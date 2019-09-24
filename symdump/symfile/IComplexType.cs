using System.Collections.Generic;
using symdump.util;

namespace symdump.symfile
{
    public interface IComplexType
    {
        string Name { get; }

        bool IsFake { get; }

        IDictionary<string, TaggedSymbol> Typedefs { get; set; }
        bool Inlined { get; set; }

        void Dump(IndentedTextWriter writer, bool forInline);

        void ResolveTypedefs(ObjectFile objectFile);
    }
}