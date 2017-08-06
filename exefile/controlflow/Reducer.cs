using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using core.util;
using NLog;

namespace exefile.controlflow
{
    public class Reducer
    {
        private static readonly ILogger logger = LogManager.GetCurrentClassLogger();

        public readonly SortedDictionary<uint, IBlock> Blocks = new SortedDictionary<uint, IBlock>();

        public Reducer(ControlFlowProcessor processor)
        {
            foreach (var block in processor.Blocks)
            {
                Blocks.Add(block.Key, block.Value);
            }
        }

        public void Reduce()
        {
            bool reduced;
            do
            {
                reduced = Blocks.Values.Any(ReduceSequence)
                          || Blocks.Values.Reverse().Any(ReduceIf)
                          || Blocks.Values.Reverse().Any(ReduceIfElse);
            } while (reduced);
        }

        private bool ReduceSequence(IBlock block)
        {
            var next = block.TrueExit;
            logger.Debug(
                $"SEQ check {block.Start:X} {block.Start:X}={block.ExitType} {next?.Start:X}={next?.ExitType}");
            if (block.ExitType != ExitType.Unconditional ||
                (next?.ExitType != ExitType.Unconditional && next?.ExitType != ExitType.Return))
                return false;


            // count refs to the next block
            if (Blocks.Values.Count(b => b.TrueExit?.Start == next.Start || b.FalseExit?.Start == next.Start) > 1)
                return false;

            if (block is SequenceBlock)
            {
                var existing = (SequenceBlock) block;
                Debug.Assert(existing.TrueExit != null);

                logger.Debug($"Sequence {block.Start:X}: attach block {next.Start:X}");

                existing.Sequence.Add(existing.TrueExit.Start, existing.TrueExit);
                Blocks.Remove(existing.TrueExit.Start);
                return true;
            }

            logger.Debug($"New sequence {block.Start:X} with block {next.Start:X}");

            var seq = new SequenceBlock();
            seq.Sequence.Add(block.Start, block);
            seq.Sequence.Add(next.Start, next);
            Blocks.Remove(block.Start);
            Blocks.Remove(next.Start);
            Blocks.Add(seq.Start, seq);
            return true;
        }

        private bool ReduceIf(IBlock condition)
        {
            /*
            if(condition<exit=conditional>)
              body<exit=unconditional|return>;
            commonCode;
            */

            if (condition.ExitType != ExitType.Conditional)
                return false;

            var common = condition.TrueExit;
            Debug.Assert(common != null);
            var body = condition.FalseExit;
            Debug.Assert(body != null);

            return TryMakeIfBlock(condition, body, common, true)
                   || TryMakeIfBlock(condition, common, body, false);

            // swap and try again
        }

        private bool ReduceIfElse(IBlock condition)
        {
            /*
            if(condition<exit=conditional>) trueBody<exit=unconditional>;
            else falseBody<exit=unconditional>;
            commonCode;
            */

            if (condition.ExitType != ExitType.Conditional)
                return false;

            var trueBody = condition.TrueExit;
            Debug.Assert(trueBody != null);
            var falseBody = condition.FalseExit;
            Debug.Assert(falseBody != null);

            if (trueBody.ExitType != ExitType.Unconditional || trueBody.ExitType != ExitType.Unconditional)
                return false;

            var common = trueBody.TrueExit;
            Debug.Assert(common != null);
            if (common.Start != falseBody.Start)
                return false;
            
            var compound = new IfElseBlock(condition, trueBody, falseBody, common);
            Blocks.Remove(condition.Start);
            Blocks.Remove(trueBody.Start);
            Blocks.Remove(falseBody.Start);
            Blocks.Add(compound.Start, compound);

            return true;
        }

        private bool TryMakeIfBlock(IBlock condition, IBlock body, IBlock common, bool inverted)
        {
            Debug.Assert(condition != null);
            Debug.Assert(body != null);
            Debug.Assert(common != null);

            if (body.ExitType != ExitType.Return &&
                (body.ExitType != ExitType.Unconditional || body.TrueExit != common))
                return false;

            logger.Debug($"Reduce: condition={condition.Start:X} body={body.Start:X} common={common.Start:X}");

            var compound = new IfBlock(condition, body, common, inverted);
            Blocks.Remove(condition.Start);
            if (body.ExitType != ExitType.Return)
                Blocks.Remove(body.Start);
            Blocks.Add(compound.Start, compound);

            return true;
        }

        public void Dump(IndentedTextWriter writer)
        {
            foreach (var block in Blocks.Values)
            {
                block.Dump(writer);
                writer.WriteLine();
            }
        }
    }
}
