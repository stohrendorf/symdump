using System.Diagnostics.CodeAnalysis;

namespace mips.disasm
{
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "IdentifierTypo")]
    public enum C2Register
    {
        vxy0,
        vz0,
        vxy1,
        vz1,
        vxy2,
        vz2,
        rgb,
        otz,
        ir0,
        ir1,
        ir2,
        ir3,
        sxy0,
        sxy1,
        sxy2,
        sxyp,
        sz0,
        sz1,
        sz2,
        sz3,
        rgb0,
        rgb1,
        rgb2,
        r23,
        mac0,
        mac1,
        mac2,
        mac3,
        irgb,
        orgb,
        lzcs,
        lzcr,
        Sentinel
    }
}
