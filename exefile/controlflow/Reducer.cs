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
            bool reduced;
            do
            {
                reduced = false;
                
                Debug.Assert(Graph.Validate());

                logger.Debug($"Analysis cycle ({Graph.Nodes.Count()} nodes, {Graph.Edges.Count()} edges)...");

                var whileTrueCandidates = FindCandidatesForWhileTrue().ToList();
                logger.Debug($" - {whileTrueCandidates.Count} while-true-candidates");
                if (whileTrueCandidates.Count > 0)
                {
                    var candidate = whileTrueCandidates.First();
                    logger.Debug("Doing while-true with: " + candidate.Id);

                    // ReSharper disable once ObjectCreationAsStatement
                    new WhileTrueNode(candidate);
                    reduced = true;
                    continue;
                }

                var doWhileCandidates = FindCandidatesForDoWhile().ToList();
                logger.Debug($" - {doWhileCandidates.Count} do-while-candidates");
                while (doWhileCandidates.Count > 0)
                {
                    var candidate = doWhileCandidates.First();
                    if (!Graph.Contains(candidate) || !IsCandidateForDoWhile(candidate))
                    {
                        doWhileCandidates.Remove(candidate);
                        continue;
                    }
                    logger.Debug("Doing do-while with: " + candidate.Id);

                    // ReSharper disable once ObjectCreationAsStatement
                    new DoWhileNode(candidate);
                    doWhileCandidates.Remove(candidate);
                    reduced = true;
                }

                var ifCandidates = FindCandidatesForIf().ToList();
                logger.Debug($" - {ifCandidates.Count} if-candidates");
                while (ifCandidates.Count > 0)
                {
                    var candidate = ifCandidates.First();
                    if (!Graph.Contains(candidate) || !IsCandidateForIf(candidate))
                    {
                        ifCandidates.Remove(candidate);
                        continue;
                    }
                    logger.Debug("Doing if with: " + candidate.Id);

                    // ReSharper disable once ObjectCreationAsStatement
                    new IfNode(candidate);
                    ifCandidates.Remove(candidate);
                    reduced = true;
                }

                var ifElseCandidates = FindCandidatesForIfElse().ToList();
                logger.Debug($" - {ifElseCandidates.Count} if-else-candidates");
                while (ifElseCandidates.Count > 0)
                {
                    var candidate = ifElseCandidates.First();
                    if (!Graph.Contains(candidate) || !IsCandidateForIfElse(candidate))
                    {
                        ifElseCandidates.Remove(candidate);
                        continue;
                    }

                    logger.Debug("Doing if-else with: " + candidate.Id);

                    // ReSharper disable once ObjectCreationAsStatement
                    new IfElseNode(candidate);
                    ifElseCandidates.Remove(candidate);
                    reduced = true;
                }

                var sequenceCandidates = FindCandidatesForSequence().ToList();
                logger.Debug($" - {sequenceCandidates.Count} sequence-candidates");
                while (sequenceCandidates.Count > 0)
                {
                    var candidate = sequenceCandidates.First();
                    if (!Graph.Contains(candidate) || !IsCandidateForSequence(candidate))
                    {
                        sequenceCandidates.Remove(candidate);
                        continue;
                    }
                    logger.Debug("Doing sequence with: " + candidate.Id);

                    // ReSharper disable once ObjectCreationAsStatement
                    new SequenceNode(candidate);
                    sequenceCandidates.Remove(candidate);
                    reduced = true;
                }

                var whileCandidates = FindCandidatesForWhile().ToList();
                logger.Debug($" - {whileCandidates.Count} while-candidates");
            } while (reduced);
        }

        private static bool IsCandidateForIf(INode condition)
        {
            if (condition.Outs.Count() != 2)
                return false;

            var trueNode = condition.Outs.FirstOrDefault(e => e is TrueEdge)?.To;
            if (trueNode == null)
                return false;

            var falseNode = condition.Outs.FirstOrDefault(e => e is FalseEdge)?.To;
            if (falseNode == null)
                return false;

            // if(condition) trueNode;
            if (trueNode.Ins.Count() == 1 && trueNode.Outs.Count() == 1 && trueNode.Outs.First() is AlwaysEdge)
            {
                if (trueNode.Outs.First().To.Equals(falseNode))
                {
                    return true;
                }
            }

            // ReSharper disable once InvertIf
            // if(!condition) falseNode;
            if (falseNode.Ins.Count() == 1 && falseNode.Outs.Count() == 1 && falseNode.Outs.First() is AlwaysEdge)
            {
                // ReSharper disable once InvertIf
                if (falseNode.Outs.First().To.Equals(trueNode))
                {
                    return true;
                }
            }

            return false;
        }
        
        private IEnumerable<INode> FindCandidatesForIf()
        {
            return Graph.Nodes
                .Where(n => !(n is EntryNode) && !(n is ExitNode))
                .Where(IsCandidateForIf);
        }

        private static bool IsCandidateForIfElse(INode condition)
        {
            if (condition.Outs.Count() != 2)
                return false;

            var trueNode = condition.Outs.FirstOrDefault(e => e is TrueEdge)?.To;
            if (trueNode == null)
                return false;

            var falseNode = condition.Outs.FirstOrDefault(e => e is FalseEdge)?.To;
            if (falseNode == null)
                return false;

            if(trueNode.Ins.Count() != 1 || falseNode.Ins.Count() != 1)
                return false;
                
            if(trueNode.Outs.Count() != 1 || falseNode.Outs.Count() != 1)
                return false;

            if (trueNode.Equals(falseNode))
                return false;

            var common1 = trueNode.Outs.FirstOrDefault(e => e is AlwaysEdge)?.To;
            if(common1 == null)
                return false;
            var common2 = falseNode.Outs.FirstOrDefault(e => e is AlwaysEdge)?.To;
            if(common2 == null)
                return false;
                
            return common1.Equals(common2);
        }
        
        private IEnumerable<INode> FindCandidatesForIfElse()
        {
            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (var condition in Graph.Nodes.Where(n => !(n is EntryNode) && !(n is ExitNode)))
            {
                if (IsCandidateForIfElse(condition))
                    yield return condition;
            }
        }

        private IEnumerable<INode> FindCandidatesForWhile()
        {
            foreach (var condition in Graph.Nodes.Where(n => !(n is EntryNode) && !(n is ExitNode)))
            {
                if (condition.Outs.Count() != 2)
                    continue;

                var trueNode = condition.Outs.FirstOrDefault(e => e is TrueEdge)?.To;
                if (trueNode == null)
                    continue;

                var falseNode = condition.Outs.FirstOrDefault(e => e is FalseEdge)?.To;
                if (falseNode == null)
                    continue;

                if (trueNode.Ins.Count() == 1 && trueNode.Outs.Count() == 1 && trueNode.Outs.First() is AlwaysEdge)
                {
                    if (trueNode.Outs.First().To.Equals(condition))
                        yield return condition;
                }

                if (falseNode.Ins.Count() == 1 && falseNode.Outs.Count() == 1 &&
                    falseNode.Outs.First() is AlwaysEdge)
                {
                    if (falseNode.Outs.First().To.Equals(condition))
                        yield return condition;
                }
            }
        }

        private static bool IsCandidateForSequence(INode seq)
        {
            if (seq.Outs.Count() != 1)
                return false;

            var next = seq.Outs.FirstOrDefault(e => e is AlwaysEdge)?.To;
            if (next == null || next is ExitNode)
                return false;

            return next.Ins.Count() == 1;
        }

        private IEnumerable<INode> FindCandidatesForSequence()
        {
            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (var seq in Graph.Nodes.Where(n => !(n is EntryNode) && !(n is ExitNode)))
            {
                if (IsCandidateForSequence(seq))
                    yield return seq;
            }
        }
        
        private IEnumerable<INode> FindCandidatesForWhileTrue()
        {
            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (var seq in Graph.Nodes.Where(n => !(n is EntryNode) && !(n is ExitNode)))
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

        private static bool IsCandidateForDoWhile(INode body)
        {
            if (body.Outs.Count() != 1)
                return false;

            var condition = body.Outs.FirstOrDefault(e => e is AlwaysEdge)?.To;
            if (condition == null)
                return false;
                
            if(condition.Ins.Count() != 1)
                return false;

            if(condition.Outs.Count() != 2)
                return false;

            var trueEdge = condition.Outs.FirstOrDefault(e => e is TrueEdge);
            if(trueEdge == null)
                return false;
                
            var falseEdge = condition.Outs.FirstOrDefault(e => e is FalseEdge);
            if(falseEdge == null)
                return false;

            return trueEdge.To.Equals(body) || falseEdge.To.Equals(body);
        }
        
        private IEnumerable<INode> FindCandidatesForDoWhile()
        {
            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (var body in Graph.Nodes.Where(n => !(n is EntryNode) && !(n is ExitNode)))
            {
                if (IsCandidateForDoWhile(body))
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
