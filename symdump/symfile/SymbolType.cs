namespace symdump.symfile
{
    public enum SymbolType : short
    {
        EndFunction = -1,
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

        EnumMember = 16,
        RegParam = 17,

        Bitfield = 18,
        AutoArgument = 19,
        LastEntry = 20,

        Block = 100,
        Function = 101,
        EndOfStruct = 102,
        FileName = 103,
        Line = 104,
        Alias = 105,
        Hidden = 106
    }
}
