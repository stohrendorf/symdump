using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using exefile.controlflow.cfg;
using NLog;

namespace exefile.controlflow
{
    public class Reducer
    {
        private static readonly ILogger logger = LogManager.GetCurrentClassLogger();

        public readonly Graph Graph;

        public Reducer(Graph graph)
        {
            Graph = graph;
        }

        public void Reduce()
        {
            while(true)
            {
                Debug.Assert(Graph.Validate());
                
                logger.Debug("Analysis cycle results:");
                
                var ifCandidates = FindCandidatesForIf().ToImmutableHashSet();
                logger.Debug($" - {ifCandidates.Count} if-candidates");

                var ifElseCandidates = FindCandidatesForIfElse().ToImmutableHashSet();
                logger.Debug($" - {ifElseCandidates.Count} if-else-candidates");

                var whileCandidates = FindCandidatesForWhile().ToImmutableHashSet();
                logger.Debug($" - {whileCandidates.Count} while-candidates");

                var sequenceCandidates = FindCandidatesForSequence().ToImmutableHashSet();
                logger.Debug($" - {sequenceCandidates.Count} sequence-candidates");

                var whileTrueCandidates = FindCandidatesForWhileTrue().ToImmutableHashSet();
                logger.Debug($" - {whileTrueCandidates.Count} while-true-candidates");

                var doWhileCandidates = FindCandidatesForDoWhile().ToImmutableHashSet();
                logger.Debug($" - {doWhileCandidates.Count} do-while-candidates");

                if (whileTrueCandidates.Count > 0)
                {
                    var candidate = whileTrueCandidates.First();
                    logger.Debug("Doing while-true with:");
                    logger.Debug(candidate);
                    
                    // ReSharper disable once ObjectCreationAsStatement
                    new WhileTrueNode(candidate);
                    continue;
                }

                if (ifCandidates.Count > 0)
                {
                    var candidate = ifCandidates.First();
                    logger.Debug("Doing if with:");
                    logger.Debug(candidate);

                    // ReSharper disable once ObjectCreationAsStatement
                    new IfNode(candidate);
                    continue;
                }

                break;
            }
        }

        private IEnumerable<INode> FindCandidatesForIf()
        {
            foreach (var condition in Graph.Nodes)
            {
                if (condition.Outs.Count() != 2)
                    continue;

                var trueNode = condition.Outs.FirstOrDefault(e => e is TrueEdge)?.To;
                if (trueNode == null)
                    continue;

                var falseNode = condition.Outs.FirstOrDefault(e => e is FalseEdge)?.To;
                if (falseNode == null)
                    continue;

                // if(condition) trueNode;
                if (Graph.CountIns(trueNode) == 1 && trueNode.Outs.Count() == 1 && trueNode.Outs.First() is AlwaysEdge)
                {
                    if (trueNode.Outs.First().To.Equals(falseNode))
                        yield return condition;
                }

                // if(!condition) falseNode;
                if (Graph.CountIns(falseNode) == 1 && falseNode.Outs.Count() == 1 && falseNode.Outs.First() is AlwaysEdge)
                {
                    if (falseNode.Outs.First().To.Equals(trueNode))
                        yield return condition;
                }
            }
        }

        private IEnumerable<INode> FindCandidatesForIfElse()
        {
            foreach (var condition in Graph.Nodes)
            {
                if (condition.Outs.Count() != 2)
                    continue;

                var trueNode = condition.Outs.FirstOrDefault(e => e is TrueEdge)?.To;
                if (trueNode == null)
                    continue;

                var falseNode = condition.Outs.FirstOrDefault(e => e is FalseEdge)?.To;
                if (falseNode == null)
                    continue;

                if(trueNode.Outs.Count() != 1 || falseNode.Outs.Count() != 1)
                    continue;

                var common = trueNode.Outs.FirstOrDefault(e => e is AlwaysEdge)?.To;
                if(common == null)
                    continue;
                
                if(!common.Equals(falseNode.Outs.FirstOrDefault(e => e is AlwaysEdge)?.To))
                    continue;

                yield return condition;
            }
        }

        private IEnumerable<INode> FindCandidatesForWhile()
        {
            foreach (var condition in Graph.Nodes)
            {
                if (condition.Outs.Count() != 2)
                    continue;

                var trueNode = condition.Outs.FirstOrDefault(e => e is TrueEdge)?.To;
                if (trueNode == null)
                    continue;

                var falseNode = condition.Outs.FirstOrDefault(e => e is FalseEdge)?.To;
                if (falseNode == null)
                    continue;

                if (Graph.CountIns(trueNode) == 1 && trueNode.Outs.Count() == 1 && trueNode.Outs.First() is AlwaysEdge)
                {
                    if (trueNode.Outs.First().To.Equals(condition))
                        yield return condition;
                }

                if (Graph.CountIns(falseNode) == 1 && falseNode.Outs.Count() == 1 &&
                    falseNode.Outs.First() is AlwaysEdge)
                {
                    if (falseNode.Outs.First().To.Equals(condition))
                        yield return condition;
                }
            }
        }

        private IEnumerable<INode> FindCandidatesForSequence()
        {
            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (var seq in Graph.Nodes)
            {
                if (seq.Outs.Count() != 1)
                    continue;

                var next = seq.Outs.FirstOrDefault(e => e is AlwaysEdge)?.To;
                if (next == null)
                    continue;

                if(Graph.CountIns(next) != 1)
                    continue;

                yield return seq;
            }
        }
        
        private IEnumerable<INode> FindCandidatesForWhileTrue()
        {
            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (var seq in Graph.Nodes)
            {
                if (seq.Outs.Count() != 1)
                    continue;

                var next = seq.Outs.FirstOrDefault(e => e is AlwaysEdge)?.To;
                if (next == null)
                    continue;

                if (next.Equals(seq))
                    yield return seq;
            }
        }
        
        private IEnumerable<INode> FindCandidatesForDoWhile()
        {
            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (var body in Graph.Nodes)
            {
                if (body.Outs.Count() != 1)
                    continue;

                var condition = body.Outs.FirstOrDefault(e => e is AlwaysEdge)?.To;
                if (condition == null)
                    continue;
                
                if(Graph.CountIns(condition) != 1)
                    continue;

                if(condition.Outs.Count() != 2)
                    continue;

                var trueEdge = condition.Outs.FirstOrDefault(e => e is TrueEdge);
                if(trueEdge == null)
                    continue;
                
                var falseEdge = condition.Outs.FirstOrDefault(e => e is FalseEdge);
                if(falseEdge == null)
                    continue;

                if (trueEdge.To.Equals(body) || falseEdge.To.Equals(body))
                    yield return body;
            }
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
