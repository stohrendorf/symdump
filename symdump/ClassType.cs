namespace symdump
{
    public enum ClassType
    {
        AutoVar = 1,
        External = 2,
        Static = 3,
        Register = 4,
        Label = 6,
        StructMember = 8,
        Argument = 9,
        Struct = 10,
        UnionMember = 11,
        Union = 12,
        Typedef = 13,
        Enum = 15,
        EnumMember = 16,
        RegParam = 17,
        Bitfield = 18,
        EndOfStruct = 102,
        FileName = 103
    }
}