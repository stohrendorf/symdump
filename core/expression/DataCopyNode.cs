using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using JetBrains.Annotations;

namespace core.expression
{
    public class DataCopyNode : IExpressionNode
    {
        [NotNull] public readonly IExpressionNode Dst;
        [NotNull] public readonly IExpressionNode Src;

        public readonly byte DstSize;
        public readonly byte SrcSize;

        public IEnumerable<int> UsedRegisters => Dst.UsedRegisters.Concat(Src.UsedRegisters);
        public IEnumerable<int> UsedStack => Dst.UsedStack.Concat(Src.UsedStack);
        public IEnumerable<uint> UsedMemory => Dst.UsedMemory.Concat(Src.UsedMemory);

        public DataCopyNode([NotNull] IExpressionNode dst, byte dstSize, [NotNull] IExpressionNode src, byte srcSize)
        {
            Debug.Assert(dstSize == 1 || dstSize == 2 || dstSize == 4);
            Debug.Assert(srcSize == 1 || srcSize == 2 || srcSize == 4);

            Dst = dst;
            Src = src;
            DstSize = dstSize;
            SrcSize = srcSize;
        }

        public string ToCode()
        {
            if (SrcSize == DstSize)
                return $"{Dst.ToCode()} = {Src.ToCode()}";
            if (SrcSize < DstSize)
                return $"{Dst.ToCode()} = (int{SrcSize * 8}_t)({Src.ToCode()})";

            Debug.Assert(SrcSize > DstSize);
            return $"{Dst.ToCode()} = (int{DstSize * 8}_t)({Src.ToCode()})";
        }

        public override string ToString()
        {
            return $"{Dst} <{DstSize}> := {Src} <{SrcSize}>";
        }
    }
}
