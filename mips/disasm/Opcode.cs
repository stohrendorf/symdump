using System.Diagnostics.CodeAnalysis;

namespace mips.disasm
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "IdentifierTypo")]
    public enum Opcode
    {
        RegisterFormat = 0,
        PCRelative = 1,
        j = 2,
        jal = 3,
        beq = 4,
        bne = 5,
        blez = 6,
        bgtz = 7,
        addi = 8,
        addiu = 9,
        subi = 10,
        subiu = 11,
        andi = 12,
        ori = 13,
        xori = 14,
        lui = 15,
        cop0 = 16,
        cop1 = 17,
        cop2 = 18,
        cop3 = 19,
        beql = 20,
        bnel = 21,
        blezl = 22,
        bgtzl = 23,
        CpuControl = 24,
        FloatingPoint = 25,
        lb = 32,
        lh = 33,
        lwl = 34,
        lw = 35,
        lbu = 36,
        lhu = 37,
        lwr = 38,
        sb = 40,
        sh = 41,
        swl = 42,
        sw = 43,
        swr = 46,
        swc1 = 49,
        lwc1 = 57
    }
}
