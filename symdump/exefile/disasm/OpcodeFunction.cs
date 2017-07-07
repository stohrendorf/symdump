using System.Diagnostics.CodeAnalysis;

namespace symdump.exefile.disasm
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public enum OpcodeFunction
    {
        sll = 0,
        srl = 2,
        sra = 3,
        sllv = 4,
        srlv = 6,
        srav = 7,
        jr = 8,
        jalr = 9,
        syscall = 12,
        break_ = 13,
        mfhi = 16,
        mthi = 17,
        mflo = 18,
        mtlo = 19,
        mult = 24,
        multu = 25,
        div = 26,
        divu = 27,
        add = 32,
        addu = 33,
        sub = 34,
        subu = 35,
        and = 36,
        or = 37,
        xor = 38,
        nor = 39,
        slt = 42,
        sltu = 43
    }
}