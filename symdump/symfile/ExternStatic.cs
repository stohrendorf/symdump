using System;

namespace symdump.symfile
{
    public class ExternStatic
    {
        private readonly string? _name;
        private readonly int _offset;
        private readonly TaggedSymbol _taggedSymbol;

        public ExternStatic(TaggedSymbol taggedSymbol, string? name, int offset)
        {
            _taggedSymbol = taggedSymbol;
            _name = name;
            _offset = offset;

            if (taggedSymbol.Type != SymbolType.External && taggedSymbol.Type != SymbolType.Static)
                throw new ArgumentException($"Symbol type must be {SymbolType.External} or {SymbolType.Static}",
                    nameof(taggedSymbol));
        }

        public override string ToString()
        {
            var storage = _taggedSymbol.Type == SymbolType.Static ? "static" : "extern";
            return $"{storage} {_taggedSymbol.AsCode(_name)}; // offset 0x{_offset:x8}";
        }

        public void ResolveTypedef(ObjectFile objectFile)
        {
            _taggedSymbol.ResolveTypedef(objectFile);
        }
    }
}
