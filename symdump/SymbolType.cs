namespace symdump
{
    public enum SymbolType
    {
        Null = 0,

        // ebp relative
        AutoVar = 1,
        External = 2,
        Static = 3,

        // register number
        Register = 4,
        ExternalDefinition = 5,
        Label = 6,
        UndefinedLabel = 7,

        // member offset
        StructMember = 8,

        // ebp relative
        Argument = 9,
        Struct = 10,
        UnionMember = 11,
        Union = 12,
        Typedef = 13,
        UndefinedStatic = 14,
        Enum = 15,

        // member value
        EnumMember = 16,
        RegParam = 17,

        // bitmask
        Bitfield = 18,
        AutoArgument = 19,
        LastEntry = 20,

        // struct size
        EndOfStruct = 102,
        FileName = 103
    }
}