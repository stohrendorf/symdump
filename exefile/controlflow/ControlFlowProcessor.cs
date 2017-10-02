using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using core;
using core.instruction;
using core.operand;
using exefile.controlflow.cfg;
using JetBrains.Annotations;
using mips.disasm;
using NLog;

namespace exefile.controlflow
{
    public class ControlFlowProcessor
    {
        private static readonly ILogger logger = LogManager.GetCurrentClassLogger();

        public readonly Graph Graph = new Graph();

        [NotNull]
        private InstructionSequence GetOrCreateSequence(SortedDictionary<uint, InstructionSequence> sequences,
            ICollection<IEdge> edges, uint addr)
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

            if (sequence != null && sequence.InstructionList.Count > 0 && sequence.InstructionList.Keys.First() == addr)
                return sequence;

            if (sequence == null)
            {
                sequences.Add(addr, sequence = new InstructionSequence(Graph));
                return sequence;
            }

            if (sequence.InstructionList.Count <= 0 || sequence.InstructionList.Keys.First() == addr)
            {
                return sequence;
            }

            logger.Debug($"Splitting {sequence.Id} at 0x{addr:x8}");

            if (sequence.InstructionList[addr].IsBranchDelaySlot)
            {
                throw new Exception("Cannot split branch delay slots");
            }

            var chopped = sequence.Chop(addr);
            Debug.Assert(chopped.InstructionList.Count > 0);
            sequences.Add(chopped.InstructionList.Keys.First(), chopped);

            foreach (var e in edges.ToList())
            {
                if (!ReferenceEquals(e.From, sequence))
                    continue;

                edges.Remove(e);
                edges.Add(e.CloneTyped(chopped, e.To));
            }

            edges.Add(new AlwaysEdge(sequence, chopped));

            return chopped;
        }

        public void Process(uint localStart, [NotNull] ExeFile exeFile)
        {
            var entryPoints = new Queue<uint>();
            entryPoints.Enqueue(localStart);

            var sequences = new SortedDictionary<uint, InstructionSequence>();
            var edges = new List<IEdge>();

            var entry = new EntryNode(Graph);
            Graph.AddNode(entry);
            var exit = new ExitNode(Graph);
            Graph.AddNode(exit);

            edges.Add(new AlwaysEdge(entry, GetOrCreateSequence(sequences, edges, localStart)));

            while (entryPoints.Count > 0)
            {
                var localAddress = entryPoints.Dequeue();
                if (localAddress < localStart)
                    continue;

                var block = GetOrCreateSequence(sequences, edges, localAddress);
                if (block.ContainsAddress(localAddress))
                {
                    Debug.Assert(localAddress == block.InstructionList.Keys.First());
                    logger.Debug($"Already processed: 0x{localAddress:X}");
                    continue;
                }

                logger.Debug($"=== Start analysis of block: 0x{localAddress:X} ===");
                exeFile.Disassemble(localAddress);

                for (;; localAddress += 4)
                {
                    if (block.InstructionList.Count > 0 && sequences.ContainsKey(localAddress))
                    {
                        edges.Add(new AlwaysEdge(block, sequences[localAddress]));
                        break;
                    }

                    var insn = exeFile.Instructions[localAddress];
                    block.InstructionList.Add(localAddress, insn);

                    logger.Debug($"[eval 0x{localAddress:X}] {insn.AsReadable()}");

                    if (insn is NopInstruction)
                    {
                        continue;
                    }

                    if (insn is ConditionalBranchInstruction instruction)
                    {
                        block.InstructionList.Add(localAddress + 4, exeFile.Instructions[localAddress + 4]);

                        var condition = GetOrCreateSequence(sequences, edges, localAddress);
                        
                        edges.Add(new FalseEdge(condition, GetOrCreateSequence(sequences, edges, localAddress + 8)));
                        entryPoints.Enqueue(localAddress + 8);

                        var target = instruction.JumpTarget;
                        if (target != null)
                        {
                            edges.Add(new TrueEdge(condition, GetOrCreateSequence(sequences, edges, target.Value)));
                            entryPoints.Enqueue(target.Value);
                        }

                        break;
                    }

                    var cpi = insn as CallPtrInstruction;
                    if (cpi?.Target is RegisterOperand)
                    {
                        block.InstructionList.Add(localAddress + 4, exeFile.Instructions[localAddress + 4]);
                        var target = (RegisterOperand) cpi.Target;
                        if (target.Register == RegisterUtil.ToInt(Register.ra))
                        {
                            edges.Add(new AlwaysEdge(block, exit));
                            logger.Debug("return");
                        }
                        else if (cpi.ReturnAddressTarget == null)
                        {
                            logger.Debug($"Goto register {target.Register} (switch-case?)");

                            var jmpReg = target.Register;
                            int? tablePtrRegister = null;
                            int? baseTableRegister = null;
                            uint? tableOffset = null;
                            bool failed = false;
                            foreach (var revInsn in block.InstructionList.Reverse().Skip(1))
                            {
                                logger.Debug($"[swich analysis] 0x{revInsn.Key:x8} {revInsn.Value.AsReadable()}");
                                if (revInsn.Value is NopInstruction)
                                    continue;

                                var dci = revInsn.Value as DataCopyInstruction;
                                if (dci?.Dst is RegisterOperand)
                                {
                                    if (((RegisterOperand) dci.Dst).Register == jmpReg)
                                    {
                                        if ((dci.Src as RegisterOffsetOperand)?.Offset == 0)
                                        {
                                            if (tablePtrRegister == null)
                                            {
                                                tablePtrRegister = ((RegisterOffsetOperand) dci.Src).Register;
                                                logger.Debug($"Table pointer register is {tablePtrRegister}");
                                            }
                                            else
                                            {
                                                logger.Debug("Table pointer register is already set");
                                                failed = true;
                                                break;
                                            }
                                        }
                                        else
                                        {
                                            logger.Debug("Non-zero offset dereference of possible table pointer");
                                            failed = true;
                                            break;
                                        }
                                    }
                                    else if (((RegisterOperand) dci.Dst).Register == baseTableRegister)
                                    {
                                        if (!(dci.Src is ImmediateOperand))
                                        {
                                            logger.Debug("Assignment to base table register from non-immediate");
                                            failed = true;
                                            break;
                                        }

                                        tableOffset = (uint)((ImmediateOperand) dci.Src).Value;
                                        break;
                                    }
                                }

                                var ai = revInsn.Value as ArithmeticInstruction;
                                if (ai?.Operator == Operator.Add && ai.IsInplace &&
                                    (ai.Destination as RegisterOperand)?.Register == tablePtrRegister &&
                                    ai.Rhs is RegisterOperand)
                                {
                                    baseTableRegister = ((RegisterOperand) ai.Rhs).Register;
                                    logger.Debug($"Base table register is {baseTableRegister}");
                                }
                            }

                            if (!failed && tableOffset == null)
                            {
                                logger.Debug("analyze predecessors");
                                // probably because it's set in a branch delay instruction
                                var pred = edges.Where(e => ReferenceEquals(e.To, block)).Select(e => e.From).ToList();
                                if (pred.Count == 1 && pred[0] is InstructionSequence)
                                {
                                    var insns = ((InstructionSequence) pred[0]).InstructionList;
                                    if (insns.Count > 0 && insns.Values.Last() is DataCopyInstruction)
                                    {
                                        var dci = (DataCopyInstruction) insns.Values.Last();
                                        if (dci.Dst is RegisterOperand operand && operand.Register == baseTableRegister)
                                        {
                                            if (!(dci.Src is ImmediateOperand))
                                            {
                                                logger.Debug("Assignment to base table register from non-immediate");
                                            }
                                            else
                                            {
                                                tableOffset = (uint) ((ImmediateOperand) dci.Src).Value;
                                            }
                                        }
                                    }
                                }
                            }
                            
                            if (tableOffset != null)
                            {
                                logger.Debug($"Switch-case table probably at 0x{tableOffset:x8}");
                                var first = sequences.Keys.First();
                                var lastKeys = sequences.Values.Last().InstructionList.Keys;
                                uint last = lastKeys.Count > 0 ? lastKeys.Last() : sequences.Keys.Last();
                                
                                logger.Debug($"Function bounds: 0x{first:x8} .. 0x{last:x8}");
                                uint caseIndex = 0;
                                for(uint offset = tableOffset.Value; exeFile.ContainsGlobal(offset, false); offset += 4)
                                {
                                    uint lblOffset = exeFile.WordAtGlobal(offset);
                                    if(!exeFile.ContainsGlobal(lblOffset))
                                        break;
                                    
                                    uint dst = exeFile.MakeLocal(lblOffset);
                                    logger.Debug($"Possible case label: 0x{dst:x8}");
                                    if(dst < first || dst > last)
                                        break;
                                    
                                    entryPoints.Enqueue(dst);
                                    edges.Add(new CaseEdge(block, GetOrCreateSequence(sequences, edges, dst), caseIndex++));
                                }
                            }
                        }
                        break;
                    }
                    else if (cpi?.Target is LabelOperand && cpi.ReturnAddressTarget == null)
                    {
                        block.InstructionList.Add(localAddress + 4, exeFile.Instructions[localAddress + 4]);
                        var lbl = cpi.JumpTarget;
                        Debug.Assert(lbl.HasValue);
                        if (!exeFile.Callees.Contains(lbl.Value))
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

            // split branching instructions for better analysis,
            // and duplicate the branch-delay instructions.
            var branches = sequences
                .Where(s => edges.Count(e => ReferenceEquals(e.From, s.Value)) == 2)
                .Select(s => s.Value)
                .ToList();
            foreach (var branch in branches)
            {
                // the situation we have:  if+delay -T-> true;       if+delay -F-> false
                // what we want is:        if -T-> delay -A-> true;  if -F-> delay(dup) -A-> false
                logger.Debug("xxxx " + branch.Id);
                foreach (var e in edges.Where(e => ReferenceEquals(e.From, branch)))
                {
                    logger.Debug(e);
                }
                
                // see if we can simply swap the branch delay slot with the condition,
                // because that simplifies the control flow graph.
                {
                    var delaySlot = branch.InstructionList.Last().Value;
                    Debug.Assert(delaySlot.IsBranchDelaySlot);
                    var condition = branch.InstructionList.Reverse().Skip(1).First().Value;
                    Debug.Assert(!condition.OutputRegisters.Any());

                    if (!delaySlot.OutputRegisters.Intersect(condition.InputRegisters).Any() && !condition.InputRegisters.Intersect(delaySlot.OutputRegisters).Any())
                    {
                        logger.Debug($"Reordering {branch.Id}");
                        
                        var tmp = new InstructionCollection(branch);
                        var cnd = tmp.InstructionList[tmp.InstructionList.Count - 2];
                        tmp.InstructionList.Remove(cnd);
                        tmp.InstructionList.Add(cnd);
                        Graph.AddNode(tmp);

                        foreach (var e in edges.Where(e => ReferenceEquals(e.From, branch)).ToList())
                        {
                            edges.Remove(e);
                            edges.Add(e.CloneTyped(tmp, e.To));
                        }
                        
                        foreach (var e in edges.Where(e => ReferenceEquals(e.To, branch)).ToList())
                        {
                            edges.Remove(e);
                            edges.Add(e.CloneTyped(e.From, tmp));
                        }

                        sequences.Remove(branch.InstructionList.Keys.First());
                        continue;
                    }
                }

                var delayInsn = branch.Chop(branch.InstructionList.Keys.Last());
                Debug.Assert(delayInsn.InstructionList.Count == 1);
                Debug.Assert(branch.InstructionList.Count > 0);

                sequences.Add(delayInsn.InstructionList.Keys.First(), delayInsn);

                logger.Debug($"Duplicating: {branch.Id} | {delayInsn.Id}");
                foreach (var e in edges.Where(e => ReferenceEquals(e.From, delayInsn)))
                {
                    logger.Debug(e);
                }

                Debug.Assert(edges.Count(e => ReferenceEquals(e.From, branch) && e is FalseEdge) == 1);
                var f = edges.First(e => ReferenceEquals(e.From, branch) && e is FalseEdge);
                Debug.Assert(edges.Count(e => ReferenceEquals(e.From, branch) && e is TrueEdge) == 1);
                var t = edges.First(e => ReferenceEquals(e.From, branch) && e is TrueEdge);

                var dup = new DuplicatedNode<InstructionSequence>(delayInsn);
                Graph.AddNode(dup);

                Debug.Assert(edges.Count(e => ReferenceEquals(e.From, t.From)) == 2);
                Debug.Assert(edges.Count(e => ReferenceEquals(e.From, f.From)) == 2);
                if (!edges.Remove(t))
                    throw new Exception("Failed to detach true branch");
                if (!edges.Remove(f))
                    throw new Exception("Failed to detach false branch");

                Debug.Assert(sequences.ContainsValue(branch));
                Debug.Assert(sequences.ContainsValue(delayInsn));
                Debug.Assert(Graph.Contains(dup));

                edges.Add(new FalseEdge(branch, delayInsn));
                edges.Add(new AlwaysEdge(delayInsn, f.To));

                edges.Add(new TrueEdge(branch, dup));
                edges.Add(new AlwaysEdge(dup, t.To));
            }

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
