using System.IO;
using core.util;

namespace exefile.controlflow
{
    public static class BlockUtil
    {
        public static string getPlantUmlName(this IBlock block) => $"state_{block.start:X}";
        
        public static void dumpPlantUml(this IBlock block, TextWriter writer)
        {
            writer.WriteLine($"note left of {block.getPlantUmlName()} : exitType={block.exitType}");
            if (block.exitType == ExitType.Return)
            {
                writer.WriteLine($"{block.getPlantUmlName()} --> [*]");
            }

            foreach (var insn in block.instructions.Values)
            {
                writer.WriteLine($"{block.getPlantUmlName()} : {insn.asReadable()}");
            }

            if (block.trueExit != null)
                writer.WriteLine($"{block.getPlantUmlName()} --> {block.trueExit.getPlantUmlName()} : true");
            if (block.falseExit != null)
                writer.WriteLine($"{block.getPlantUmlName()} --> {block.falseExit.getPlantUmlName()} : false");
        }
    }
}
