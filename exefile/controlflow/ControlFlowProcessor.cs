using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using core;
using core.util;
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

        public readonly SortedDictionary<uint, IBlock> Blocks = new SortedDictionary<uint, IBlock>();

        [NotNull]
        private Block GetBlockForAddress(uint addr)
        {
            IBlock block = null;
            if (!Blocks.TryGetValue(addr, out block))
            {
                // we don't have a block starting at addr, so find the best matching candidate,
                // which then either needs splitting, or we can safely add a new block
                var candidate = Blocks.Keys.LastOrDefault(a => a <= addr);
                if (Blocks.TryGetValue(candidate, out block))
                {
                    if (!block.ContainsAddress(addr))
                        block = null;
                }
            }

            if (block != null && block.Instructions.Count > 0 && block.Start == addr)
                return (Block) block;
            
            if (block == null)
            {
                Blocks.Add(addr, block = new Block());
                return (Block) block;
            }

            if (block.Instructions.Count <= 0 || block.Start == addr)
                return (Block) block;
            
            var typed = (Block) block;
            var split = new Block
            {
                ExitType = typed.ExitType,
                TrueExit = typed.TrueExit,
                FalseExit = typed.FalseExit
            };
            
            typed.TrueExit = split;
            typed.FalseExit = null;
            typed.ExitType = ExitType.Unconditional;

            var toRemove = new HashSet<uint>();
            foreach (var insn in typed.Instructions.Where(kv => kv.Key >= addr))
            {
                split.Instructions.Add(insn.Key, insn.Value);
                toRemove.Add(insn.Key);
            }
            foreach (var rm in toRemove)
            {
                typed.Instructions.Remove(rm);
            }
                
            Blocks.Add(split.Start, split);
            return split;
        }

        public void Process(uint start, [NotNull] IReadOnlyDictionary<uint, Instruction> instructions)
        {
            var entryPoints = new Queue<uint>();
            entryPoints.Enqueue(start);

            while (entryPoints.Count > 0)
            {
                var addr = entryPoints.Dequeue();
                if (addr < start)
                    continue;

                var block = GetBlockForAddress(addr);
                if (block.ContainsAddress(addr))
                {
                    Debug.Assert(addr == block.Start);
                    logger.Debug($"Already processed: 0x{addr:X}");
                    continue;
                }

                logger.Debug($"=== Start analysis of block: 0x{addr:X} ===");

                for (;; addr += 4)
                {
                    if (block.Instructions.Count > 0 && Blocks.ContainsKey(addr))
                    {
                        if (block.ExitType == null)
                        {
                            block.ExitType = ExitType.Unconditional;
                            block.TrueExit = Blocks[addr];
                        }
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
                        block.ExitType = ExitType.Conditional;

                        block.Instructions.Add(addr + 4, instructions[addr + 4]);

                        block.FalseExit = GetBlockForAddress(addr + 8);
                        entryPoints.Enqueue(addr + 8);

                        var target = ((ConditionalBranchInstruction) insn).Target;
                        var targetLabel = target as LabelOperand;
                        if (targetLabel != null)
                        {
                            block.TrueExit = GetBlockForAddress(targetLabel.Address);
                            entryPoints.Enqueue(targetLabel.Address);
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
                            block.ExitType = ExitType.Return;
                            logger.Debug("return");
                        }
                        break;
                    }
                    else if (cpi?.Target is LabelOperand && cpi.ReturnAddressTarget == null)
                    {
                        block.Instructions.Add(addr + 4, instructions[addr + 4]);
                        block.ExitType = ExitType.Unconditional;

                        logger.Debug("jmp " + cpi.Target);

                        var lbl = (LabelOperand) cpi.Target;
                        block.TrueExit = GetBlockForAddress(lbl.Address);
                        entryPoints.Enqueue(lbl.Address);
                        break;
                    }
                }
            }
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
