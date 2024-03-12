﻿using System.Diagnostics.CodeAnalysis;

namespace symdump.exefile.disasm
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public enum CpuControlOpcode
    {
        mfc0 = 0,
        mtc0 = 4,
        bc0 = 8,
        tlb = 16
    }
}
