namespace exefile.controlflow
{
    public static class BlockUtil
    {
        public static string GetNodeName(this IBlock block) => $"state_{block.Start:X}";
    }
}
