using System.IO;

namespace exefile.controlflow
{
    public static class BlockUtil
    {
        public static string GetPlantUmlName(this IBlock block) => $"state_{block.Start:X}";
        
        public static void DumpPlantUml(this IBlock block, TextWriter writer)
        {
            writer.WriteLine($"note left of {block.GetPlantUmlName()} : exitType={block.ExitType}");
            if (block.ExitType == ExitType.Return)
            {
                writer.WriteLine($"{block.GetPlantUmlName()} --> [*]");
            }

            foreach (var insn in block.Instructions.Values)
            {
                writer.WriteLine($"{block.GetPlantUmlName()} : {insn.AsReadable()}");
            }

            if (block.TrueExit != null)
                writer.WriteLine($"{block.GetPlantUmlName()} --> {block.TrueExit.GetPlantUmlName()} : true");
            if (block.FalseExit != null)
                writer.WriteLine($"{block.GetPlantUmlName()} --> {block.FalseExit.GetPlantUmlName()} : false");
        }
    }
}
