using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using JetBrains.Annotations;

namespace core.expression
{
    public class DataCopyNode : IExpressionNode
    {
        [NotNull] public readonly IExpressionNode dst;
        [NotNull] public readonly IExpressionNode src;

        public readonly byte dstSize;
        public readonly byte srcSize;

        public IEnumerable<int> usedRegisters => dst.usedRegisters.Concat(src.usedRegisters);
        public IEnumerable<int> usedStack => dst.usedStack.Concat(src.usedStack);
        public IEnumerable<uint> usedMemory => dst.usedMemory.Concat(src.usedMemory);

        public DataCopyNode([NotNull] IExpressionNode dst, byte dstSize, [NotNull] IExpressionNode src, byte srcSize)
        {
            Debug.Assert(dstSize == 1 || dstSize == 2 || dstSize == 4);
            Debug.Assert(srcSize == 1 || srcSize == 2 || srcSize == 4);

            this.dst = dst;
            this.src = src;
            this.dstSize = dstSize;
            this.srcSize = srcSize;
        }

        public string toCode()
        {
            if (srcSize == dstSize)
                return $"{dst.toCode()} = {src.toCode()}";
            if (srcSize < dstSize)
                return $"{dst.toCode()} = (int{srcSize * 8}_t)({src.toCode()})";

            Debug.Assert(srcSize > dstSize);
            return $"{dst.toCode()} = (int{dstSize * 8}_t)({src.toCode()})";
        }

        public override string ToString()
        {
            return $"{dst} <{dstSize}> := {src} <{srcSize}>";
        }
    }
}
