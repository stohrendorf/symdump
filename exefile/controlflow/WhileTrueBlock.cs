using System.Collections.Generic;
using System.Diagnostics;
using core;
using core.util;
using JetBrains.Annotations;

namespace exefile.controlflow
{
    public class WhileTrueBlock : IBlock
    {
        public WhileTrueBlock([NotNull] IBlock body)
        {
            Debug.Assert(body.ExitType == controlflow.ExitType.Unconditional);
            Debug.Assert(body.TrueExit != null);
            Debug.Assert(body.TrueExit.Start == body.Start);

            Body = body;
        }

        [NotNull]
        public IBlock Body { get; }

        public IBlock TrueExit => null;
        public IBlock FalseExit => null;
        public uint Start => Body.Start;
        public SortedDictionary<uint, Instruction> Instructions => Body.Instructions;
        public ExitType? ExitType => controlflow.ExitType.Return; // there's no other way to exit this

        public bool ContainsAddress(uint address) => Body.ContainsAddress(address);

        public void Dump(IndentedTextWriter writer)
        {
            writer.WriteLine("while(true) {");
            ++writer.Indent;
            Body.Dump(writer);
            --writer.Indent;
            writer.WriteLine("}");
        }

        public void UpdateReferences(IReadOnlyDictionary<uint, IBlock> blocks, ISet<uint> processed)
            => Body.UpdateReferences(blocks, processed);
    }
}
