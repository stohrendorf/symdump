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
        private InstructionSequence GetOrCreateSequence(IDictionary<uint, InstructionSequence> sequences,  uint addr)
        {
            InstructionSequence sequence;
            if (!sequences.TryGetValue(addr, out sequence))
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
                return sequence;
            
            var chopped = sequence.Chop(addr);
            sequences.Add(chopped.Start, chopped);
            return chopped;
        }

        public void Process(uint start, [NotNull] IReadOnlyDictionary<uint, Instruction> instructions)
        {
            var entryPoints = new Queue<uint>();
            entryPoints.Enqueue(start);

            var sequences = new Dictionary<uint, InstructionSequence>();
            var edges = new HashSet<IEdge>();
            
            var entry = new EntryNode(Graph);
            Graph.AddNode(entry);
            var exit = new ExitNode(Graph);
            Graph.AddNode(exit);
            
            edges.Add(new AlwaysEdge(entry, GetOrCreateSequence(sequences, start)));
            
            while (entryPoints.Count > 0)
            {
                var addr = entryPoints.Dequeue();
                if (addr < start)
                    continue;

                var block = GetOrCreateSequence(sequences, addr);
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

                    if (insn is ConditionalBranchInstruction)
                    {
                        block.Instructions.Add(addr + 4, instructions[addr + 4]);

                        edges.Add(new FalseEdge(block, GetOrCreateSequence(sequences, addr + 8)));
                        entryPoints.Enqueue(addr + 8);

                        var target = ((ConditionalBranchInstruction) insn).JumpTarget;
                        if (target != null)
                        {
                            edges.Add(new TrueEdge(block, GetOrCreateSequence(sequences, target.Value)));
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

                        logger.Debug("jmp " + cpi.Target);

                        var lbl = cpi.JumpTarget;
                        Debug.Assert(lbl.HasValue);
                        edges.Add(new AlwaysEdge(block, GetOrCreateSequence(sequences, lbl.Value)));
                        entryPoints.Enqueue(lbl.Value);
                        break;
                    }
                }
            }
            
            foreach (var s in sequences.Values)
            {
                Graph.AddNode(s);
            }

            foreach (var e in edges)
            {
                Graph.AddEdge(e);
            }
        }
    }
}
