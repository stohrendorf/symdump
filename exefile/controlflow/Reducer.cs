using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using core.util;
using JetBrains.Annotations;
using mips.instructions;
using NLog;
using NLog.Config;
using NLog.Targets;
using Xunit;

namespace exefile.controlflow
{
    public class Reducer
    {
        private static readonly ILogger logger = LogManager.GetCurrentClassLogger();

        public SortedDictionary<uint, IBlock> Blocks { get; private set; } = new SortedDictionary<uint, IBlock>();

        public Reducer([NotNull] IReadOnlyDictionary<uint, IBlock> blocks)
        {
            foreach (var block in blocks)
            {
                Blocks.Add(block.Key, block.Value);
            }
        }

        private int CountReferencesTo(IBlock block)
        {
            return Blocks.Values.Count(b => ReferenceEquals(block, b.TrueExit) || ReferenceEquals(block, b.FalseExit));
        }

        public void Reduce()
        {
            bool reduced;
            do
            {
                reduced = Blocks.Values.Reverse().Any(ReduceIfWhile)
                          || Blocks.Values.Reverse().Any(ReduceIfElse)
                          || Blocks.Values.Reverse().Any(ReduceSequence);

                var recursionProtection = new HashSet<uint>();
                foreach (var b in Blocks.Values)
                {
                    b.UpdateReferences(Blocks, recursionProtection);
                }
            } while (reduced);

            // clean up unreferenced blocks, except the start block
            var cleaned = new SortedDictionary<uint, IBlock>();
            var first = Blocks.Keys.First();
            foreach (var b in Blocks.Where(kv => kv.Key == first || CountReferencesTo(kv.Value) > 0))
            {
                cleaned.Add(b.Key, b.Value);
            }
            Blocks = cleaned;
        }

        private bool ReduceSequence([NotNull] IBlock block)
        {
            var next = block.TrueExit;
            logger.Debug(
                $"SEQ check {block.Start:X} {block.Start:X}={block.ExitType} {next?.Start:X}={next?.ExitType}");
            if (block.ExitType != ExitType.Unconditional ||
                (next?.ExitType != ExitType.Unconditional && next?.ExitType != ExitType.Return))
                return false;

            if (CountReferencesTo(next) != 1)
                return false;

            if (block is SequenceBlock)
            {
                var existing = (SequenceBlock) block;
                Debug.Assert(existing.TrueExit != null);

                if (existing.Sequence.Count(b => b.Start == next.Start) > 0)
                {
                    logger.Debug($"Sequence {block.Start:X}: block {next.Start:X} already in sequence");
                    return false;
                }

                logger.Debug($"Sequence {block.Start:X}: attach block {next.Start:X}");

                existing.Sequence.Add(next);
                Blocks.Remove(next.Start);
                return true;
            }

            logger.Debug($"New sequence {block.Start:X} with block {next.Start:X}");

            var seq = new SequenceBlock();
            seq.Sequence.Add(block);
            seq.Sequence.Add(next);
            Blocks.Remove(block.Start);
            Blocks.Remove(next.Start);
            Blocks.Add(seq.Start, seq);
            return true;
        }

        internal bool ReduceIfWhile([NotNull] IBlock condition)
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

            return TryMakeIfWhileBlock(condition, common, body, false) ||
                   TryMakeIfWhileBlock(condition, body, common, true);
        }

        internal bool ReduceIfElse([NotNull] IBlock condition)
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
            if (falseBody.TrueExit == null)
                return false;
            if (CountReferencesTo(falseBody) > 1)
                return false;

            if (trueBody.ExitType != ExitType.Unconditional || trueBody.ExitType != ExitType.Unconditional)
                return false;
            if (CountReferencesTo(trueBody) > 1)
                return false;

            var common = trueBody.TrueExit;
            Debug.Assert(common != null);
            if (common.Start != falseBody.TrueExit.Start)
                return false;

            var compound = new IfElseBlock(condition, trueBody, falseBody, common);
            Blocks.Remove(condition.Start);
            Blocks.Remove(trueBody.Start);
            Blocks.Remove(falseBody.Start);
            Blocks.Add(compound.Start, compound);

            return true;
        }

        private bool TryMakeIfWhileBlock([NotNull] IBlock condition, [NotNull] IBlock body, [NotNull] IBlock common,
            bool inverted)
        {
            Debug.Assert(condition != null);
            Debug.Assert(body != null);
            Debug.Assert(common != null);

            switch (body.ExitType)
            {
                case ExitType.Return:
                    break;
                case ExitType.Unconditional:
                    Debug.Assert(body.TrueExit != null);
                    if (body.TrueExit.Start != common.Start && body.TrueExit.Start != condition.Start)
                        return false;
                    if (body.TrueExit.Start == common.Start && CountReferencesTo(body) > 1)
                        return false;
                    break;
                default:
                    return false;
            }

            logger.Debug($"Reduce: condition={condition.Start:X} body={body.Start:X} common={common.Start:X}");

            IBlock compound;
            if (body.TrueExit?.Start != condition.Start)
                compound = new IfBlock(condition, body, common, inverted);
            else
                compound = new WhileBlock(condition, body, common, inverted);

            Blocks.Remove(condition.Start);
            if (CountReferencesTo(body) == 0)
                Blocks.Remove(body.Start);
            Blocks.Add(compound.Start, compound);

            return true;
        }

        public void Dump(IndentedTextWriter writer)
        {
            foreach (var block in Blocks.Values)
            {
                writer.WriteLine($"-- Block 0x{block.Start:x8} {block.GetType()}");
                block.Dump(writer);
                writer.WriteLine();
            }
        }
    }

    public static class ReducerTest
    {
        private static Block CreateNopBlock(IDictionary<uint, IBlock> blocks, uint addr)
        {
            var b = new Block();
            b.Instructions.Add(addr, new NopInstruction());
            blocks.Add(addr, b);
            return b;
        }

        [Fact]
        public static void TestIf()
        {
            var blocks = new Dictionary<uint, IBlock>();

            var common = CreateNopBlock(blocks, 8);
            common.ExitType = ExitType.Return;

            var body = CreateNopBlock(blocks, 4);
            body.ExitType = ExitType.Unconditional;
            body.TrueExit = common;

            var condition = CreateNopBlock(blocks, 0);
            condition.ExitType = ExitType.Conditional;
            condition.TrueExit = body;
            condition.FalseExit = common;

            var reducer = new Reducer(blocks);
            reducer.ReduceIfWhile(condition);

            Assert.Equal(2, reducer.Blocks.Count);
            Assert.True(reducer.Blocks.ContainsKey(0));
            Assert.True(reducer.Blocks.ContainsKey(8));

            var b1 = reducer.Blocks[0];
            Assert.IsType<IfBlock>(b1);
            Assert.NotNull(b1.TrueExit);
            Assert.Null(b1.FalseExit);
            Assert.Equal(0u, b1.Start);
            Assert.Equal(ExitType.Unconditional, b1.ExitType);

            var b2 = reducer.Blocks[8];
            Assert.IsType<Block>(b2);
            Assert.Null(b2.TrueExit);
            Assert.Null(b2.FalseExit);
            Assert.Equal(8u, b2.Start);
            Assert.Equal(ExitType.Return, b2.ExitType);

            Assert.Same(b2, b1.TrueExit);
        }

        [Fact]
        public static void TestWhile()
        {
            var blocks = new Dictionary<uint, IBlock>();

            var common = CreateNopBlock(blocks, 8);
            common.ExitType = ExitType.Return;

            var body = CreateNopBlock(blocks, 4);
            body.ExitType = ExitType.Unconditional;

            var condition = CreateNopBlock(blocks, 0);
            condition.ExitType = ExitType.Conditional;
            condition.TrueExit = body;
            condition.FalseExit = common;

            body.TrueExit = condition;

            var reducer = new Reducer(blocks);
            reducer.ReduceIfWhile(condition);

            Assert.Equal(2, reducer.Blocks.Count);
            Assert.True(reducer.Blocks.ContainsKey(0));
            Assert.True(reducer.Blocks.ContainsKey(8));

            var b1 = reducer.Blocks[0];
            Assert.IsType<WhileBlock>(b1);
            Assert.NotNull(b1.TrueExit);
            Assert.Null(b1.FalseExit);
            Assert.Equal(0u, b1.Start);
            Assert.Equal(ExitType.Unconditional, b1.ExitType);

            var b2 = reducer.Blocks[8];
            Assert.IsType<Block>(b2);
            Assert.Null(b2.TrueExit);
            Assert.Null(b2.FalseExit);
            Assert.Equal(8u, b2.Start);
            Assert.Equal(ExitType.Return, b2.ExitType);

            Assert.Same(b2, b1.TrueExit);
        }

        [Fact]
        public static void TestIfElse()
        {
            var blocks = new Dictionary<uint, IBlock>();

            var common = CreateNopBlock(blocks, 12);
            common.ExitType = ExitType.Return;

            var falseBody = CreateNopBlock(blocks, 8);
            falseBody.ExitType = ExitType.Unconditional;
            falseBody.TrueExit = common;

            var trueBody = CreateNopBlock(blocks, 4);
            trueBody.ExitType = ExitType.Unconditional;
            trueBody.TrueExit = common;

            var condition = CreateNopBlock(blocks, 0);
            condition.ExitType = ExitType.Conditional;
            condition.TrueExit = trueBody;
            condition.FalseExit = falseBody;

            var reducer = new Reducer(blocks);
            reducer.ReduceIfElse(condition);

            Assert.Equal(2, reducer.Blocks.Count);
            Assert.True(reducer.Blocks.ContainsKey(0));
            Assert.True(reducer.Blocks.ContainsKey(12));

            var b1 = reducer.Blocks[0];
            Assert.IsType<IfElseBlock>(b1);
            Assert.NotNull(b1.TrueExit);
            Assert.Null(b1.FalseExit);
            Assert.Equal(0u, b1.Start);
            Assert.Equal(ExitType.Unconditional, b1.ExitType);

            var b2 = reducer.Blocks[12];
            Assert.IsType<Block>(b2);
            Assert.Null(b2.TrueExit);
            Assert.Null(b2.FalseExit);
            Assert.Equal(12u, b2.Start);
            Assert.Equal(ExitType.Return, b2.ExitType);

            Assert.Same(b2, b1.TrueExit);

            var compound = (IfElseBlock) b1;
            Assert.Same(trueBody, compound.TrueBody);
            Assert.Same(falseBody, compound.FalseBody);
            Assert.Same(common, compound.Exit);
        }
    }
}
