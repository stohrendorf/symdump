using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace core.disasm
{
    public class FunctionProperties
    {
        public readonly string Name;
        public readonly ISet<uint> InRegs = new HashSet<uint>();
        public readonly ISet<uint> OutRegs = new HashSet<uint>();
        public readonly ISet<uint> SpoiledRegs = new HashSet<uint>();

        public FunctionProperties([NotNull] string name)
        {
            Name = name;
        }

        public override string ToString()
        {
            string result = "";
            switch (OutRegs.Count)
            {
                case 0:
                    break;
                case 1:
                    result = $"$r{OutRegs.First()} := ";
                    break;
                default:
                    result = "(" + string.Join(", ", OutRegs.Select(r => $"$r{r}")) + ") := ";
                    break;
            }

            result += Name + "(" + string.Join(", ", InRegs.Select(r => $"$r{r}")) + ")";
            if (SpoiledRegs.Count > 0)
            {
                result += " {spoils " + SpoiledRegs.Select(r => $"$r{r}") + "}";
            }

            return result;
        }
    }
}
