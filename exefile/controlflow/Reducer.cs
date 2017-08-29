using System;
using System.Diagnostics;
using System.Linq;
using exefile.controlflow.cfg;
using JetBrains.Annotations;
using NLog;

namespace exefile.controlflow
{
    public class Reducer
    {
        private static readonly ILogger logger = LogManager.GetCurrentClassLogger();

        public readonly Graph Graph;

        public Reducer([NotNull] Graph graph)
        {
            Graph = graph;
        }

        private bool Reduce(string name, Func<INode, bool> predicate, Func<INode, INode> converter)
        {
            var candidates = Graph.Nodes.Where(predicate).ToList();
            logger.Debug($" - {candidates.Count} {name} candidates");
            bool reduced = false;
            while (candidates.Count > 0)
            {
                var candidate = candidates.First();
                if (!Graph.Contains(candidate) || !predicate(candidate))
                {
                    candidates.Remove(candidate);
                    continue;
                }
                logger.Debug($"Doing {name} with: {candidate.Id}");

                converter(candidate);
                candidates.Remove(candidate);
                reduced = true;
            }
            return reduced;
        }
        
        public void Reduce()
        {
            bool reduced;
            do
            {
                reduced = false;
                
                Debug.Assert(Graph.Validate());

                logger.Debug($"Analysis cycle ({Graph.Nodes.Count()} nodes, {Graph.Edges.Count()} edges)...");

                reduced |= Reduce("and clause", AndNode.IsCandidate, n => new AndNode(n));
                reduced |= Reduce("or clause", OrNode.IsCandidate, n => new OrNode(n));
                if(reduced)
                    continue;
                
                reduced |= Reduce("if", IfNode.IsCandidate, n => new IfNode(n));
                reduced |= Reduce("if-else", IfElseNode.IsCandidate, n => new IfElseNode(n));
                reduced |= Reduce("while", WhileNode.IsCandidate, n => new WhileNode(n));
                reduced |= Reduce("do-while", DoWhileNode.IsCandidate, n => new DoWhileNode(n));
                reduced |= Reduce("while-true", WhileTrueNode.IsCandidate, n => new WhileTrueNode(n));
                reduced |= Reduce("sequence", SequenceNode.IsCandidate, n => new SequenceNode(n));
            } while (reduced);
        }
    }
    
#if false
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
#endif
}
