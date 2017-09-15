using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using core;
using exefile.controlflow.cfg;
using JetBrains.Annotations;
using mips.disasm;
using mips.instructions;
using mips.operands;
using NLog;

namespace exefile.controlflow
{
    public class ControlFlowProcessor
    {
        private static readonly ILogger logger = LogManager.GetCurrentClassLogger();

        public readonly Graph Graph = new Graph();

        [NotNull]
        private InstructionSequence GetOrCreateSequence(SortedDictionary<uint, InstructionSequence> sequences, ICollection<IEdge> edges,  uint addr)
        {
            if (!sequences.TryGetValue(addr, out var sequence))
            {
                // we don't have a sequence starting at addr, so find the best matching candidate,
                // which then either needs splitting, or we can safely add a new sequence
                var candidate = sequences.Keys.LastOrDefault(a => a <= addr);
                if (sequences.TryGetValue(candidate, out sequence))
                {
                    if (!sequence.ContainsAddress(addr))
                        sequence = null;
                }
            }

            if (sequence != null && sequence.Instructions.Count > 0 && sequence.Start == addr)
                return sequence;
            
            if (sequence == null)
            {
                sequences.Add(addr, sequence = new InstructionSequence(Graph));
                return sequence;
            }

            if (sequence.Instructions.Count <= 0 || sequence.Start == addr)
            {
                return sequence;
            }

            logger.Debug($"Splitting {sequence.Id} at 0x{addr:x8}");

            if (sequence.Instructions[addr].IsBranchDelaySlot)
            {
                throw new Exception("Cannot split branch delay slots");
            }
            
            var chopped = sequence.Chop(addr);
            Debug.Assert(chopped.Instructions.Count > 0);
            sequences.Add(chopped.Start, chopped);

            foreach (var e in edges.ToList())
            {
                if (!e.From.Equals(sequence))
                    continue;
                
                edges.Remove(e);
                edges.Add(e.CloneTyped(chopped, e.To));
            }

            edges.Add(new AlwaysEdge(sequence, chopped));

            return chopped;
        }

        public void Process(uint start, [NotNull] IReadOnlyDictionary<uint, Instruction> instructions, [NotNull] IReadOnlyCollection<uint> callees)
        {
            var entryPoints = new Queue<uint>();
            entryPoints.Enqueue(start);

            var sequences = new SortedDictionary<uint, InstructionSequence>();
            var edges = new List<IEdge>();
            
            var entry = new EntryNode(Graph);
            Graph.AddNode(entry);
            var exit = new ExitNode(Graph);
            Graph.AddNode(exit);
            
            edges.Add(new AlwaysEdge(entry, GetOrCreateSequence(sequences, edges, start)));
            
            while (entryPoints.Count > 0)
            {
                var addr = entryPoints.Dequeue();
                if (addr < start)
                    continue;

                var block = GetOrCreateSequence(sequences, edges, addr);
                if (block.ContainsAddress(addr))
                {
                    Debug.Assert(addr == block.Start);
                    logger.Debug($"Already processed: 0x{addr:X}");
                    continue;
                }

                logger.Debug($"=== Start analysis of block: 0x{addr:X} ===");

                for (;; addr += 4)
                {
                    if (block.Instructions.Count > 0 && sequences.ContainsKey(addr))
                    {
                        edges.Add(new AlwaysEdge(block, sequences[addr]));
                        break;
                    }

                    var insn = instructions[addr];
                    block.Instructions.Add(addr, insn);

                    logger.Debug($"[eval 0x{addr:X}] {insn.AsReadable()}");

                    if (insn is NopInstruction)
                    {
                        continue;
                    }

                    if (insn is ConditionalBranchInstruction instruction)
                    {
                        block.Instructions.Add(addr + 4, instructions[addr + 4]);

                        edges.Add(new FalseEdge(block, GetOrCreateSequence(sequences, edges, addr + 8)));
                        entryPoints.Enqueue(addr + 8);

                        var target = instruction.JumpTarget;
                        if (target != null)
                        {
                            edges.Add(new TrueEdge(block, GetOrCreateSequence(sequences, edges, target.Value)));
                            entryPoints.Enqueue(target.Value);
                        }

                        break;
                    }

                    var cpi = insn as CallPtrInstruction;
                    if (cpi?.Target is RegisterOperand)
                    {
                        block.Instructions.Add(addr + 4, instructions[addr + 4]);
                        var target = (RegisterOperand) cpi.Target;
                        if (target.Register == Register.ra)
                        {
                            edges.Add(new AlwaysEdge(block, exit));
                            logger.Debug("return");
                        }
                        break;
                    }
                    else if (cpi?.Target is LabelOperand && cpi.ReturnAddressTarget == null)
                    {
                        block.Instructions.Add(addr + 4, instructions[addr + 4]);
                        var lbl = cpi.JumpTarget;
                        Debug.Assert(lbl.HasValue);
                        if (!callees.Contains(lbl.Value))
                        {
                            edges.Add(new AlwaysEdge(block, GetOrCreateSequence(sequences, edges, lbl.Value)));
                            entryPoints.Enqueue(lbl.Value);
                        }
                        else
                        {
                            edges.Add(new AlwaysEdge(block, exit));
                        }
                        break;
                    }
                }
            }
            
            // split branching instructions for better analysis
            var branching = sequences
                .SelectMany(s => s.Value.Instructions)
                .Where(i => i.Value is CallPtrInstruction || i.Value is ConditionalBranchInstruction)
                .Select(i => i.Key)
                .ToList();
            foreach (var splitAt in branching)
            {
                GetOrCreateSequence(sequences, edges, splitAt);
            }
            
#if false
            // duplicate the branch-delay instructions.
            var branches = sequences
                .Where(s => edges.Count(e => e.From.Equals(s.Value)) == 2)
                .Select(s => s.Value)
                .ToList();
            foreach (var branch in branches)
            {
                // the situation we have:  if+delay -T-> true;       if+delay -F-> false
                // what we want is:        if -T-> delay -A-> true;  if -F-> delay(dup) -A-> false
                logger.Debug("xxxx " + branch.Id);
                foreach (var e in edges.Where(e => e.From.Equals(branch)))
                {
                    logger.Debug(e);
                }

                var delayInsn = GetOrCreateSequence(sequences, edges, branch.Instructions.Keys.Last());
                Debug.Assert(delayInsn.Instructions.Count == 1);
                Debug.Assert(branch.Instructions.Count > 0);
                
                logger.Debug($"Duplicating: {branch.Id} | {delayInsn.Id}");
                foreach (var e in edges.Where(e => e.From.Equals(delayInsn)))
                {
                    logger.Debug(e);
                }
                
                Debug.Assert(edges.Count(e => e.From.Equals(delayInsn) && e is FalseEdge) == 1);
                var f = edges.First(e => e.From.Equals(delayInsn) && e is FalseEdge);
                Debug.Assert(edges.Count(e => e.From.Equals(delayInsn) && e is TrueEdge) == 1);
                var t = edges.First(e => e.From.Equals(delayInsn) && e is TrueEdge); 

                var dup = new DuplicatedNode<InstructionSequence>(delayInsn);
                Graph.AddNode(dup);
                
                Debug.Assert(!dup.Equals(delayInsn));
                
                Debug.Assert(edges.Count(e => e.To.Equals(delayInsn)) == 1);
                Debug.Assert(edges.Count(e => e.From.Equals(t.From)) == 2);
                Debug.Assert(edges.Count(e => e.From.Equals(f.From)) == 2);
                if(!edges.Remove(edges.First(e => e.To.Equals(delayInsn))))
                    throw new Exception("Failed to detach branch delay slot");
                if(!edges.Remove(t))
                    throw new Exception("Failed to detach true branch");
                if(!edges.Remove(f))
                    throw new Exception("Failed to detach false branch");

                Debug.Assert(sequences.ContainsValue(branch));
                Debug.Assert(sequences.ContainsValue(delayInsn));
                Debug.Assert(Graph.Contains(dup));
                
                edges.Add(new FalseEdge(branch, delayInsn));
                edges.Add(new AlwaysEdge(delayInsn, f.To));
                
                edges.Add(new TrueEdge(branch, dup));
                edges.Add(new AlwaysEdge(dup, t.To));
            }
#endif
            
            foreach (var s in sequences.Values)
            {
                Graph.AddNode(s);
            }

            foreach (var e in edges)
            {
                Graph.AddEdge(e);
            }
            
            Debug.Assert(Graph.Validate());
            Graph.MakeUniformBooleanEdges();
            Debug.Assert(Graph.Validate());
        }
    }
}
