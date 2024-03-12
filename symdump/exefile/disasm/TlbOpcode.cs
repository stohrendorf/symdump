using System.Diagnostics.CodeAnalysis;

namespace symdump.exefile.disasm
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public enum TlbOpcode
    {
        tlbr = 1,
        tlbwi = 2,
        tlbwr = 6,
        tlbp = 8,
        rfe = 16
    }
}
