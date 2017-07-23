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
        private static ILogger logger = LogManager.GetCurrentClassLogger();
        
        public SortedDictionary<uint, Block> blocks = new SortedDictionary<uint, Block>();

        [NotNull]
        private Block getBlockForAddress(uint addr)
        {
            Block block;
            if (!blocks.TryGetValue(addr, out block))
            {
                blocks.Add(addr, block = new Block());
            }
            else
            {
                if (block.instructions.Count > 0 && block.start != addr)
                {
                    logger.Debug($"Block 0x{block.start:X} needs split at 0x{addr:X}");
                }
            }

            Debug.Assert(block != null);
            return block;
        }

        public void process(uint start, [NotNull] IDictionary<uint, Instruction> instructions)
        {
            var entryPoints = new Queue<uint>();
            entryPoints.Enqueue(start);

            while (entryPoints.Count > 0)
            {
                var addr = entryPoints.Dequeue();
                if (addr < start)
                    continue;

                var block = getBlockForAddress(addr);
                if (block.containsAddress(addr))
                {
                    logger.Debug($"Already processed: 0x{addr:X}");
                    continue;
                }

                logger.Debug($"=== Start analysis of block: 0x{addr:X} ===");

                for (;; addr += 4)
                {
                    if (block.instructions.Count > 0 && blocks.ContainsKey(addr))
                    {
                        if (block.exitType == null)
                        {
                            block.exitType = ExitType.Unconditional;
                            block.trueExit = blocks[addr];
                        }
                        break;
                    }

                    var insn = instructions[addr];
                    block.instructions.Add(addr, insn);

                    logger.Debug($"[eval 0x{addr:X}] {insn.asReadable()}");

                    if (insn is NopInstruction)
                    {
                        continue;
                    }

                    if (insn is ConditionalBranchInstruction)
                    {
                        block.exitType = ExitType.Conditional;

                        block.instructions.Add(addr + 4, instructions[addr + 4]);

                        block.falseExit = getBlockForAddress(addr + 8);
                        entryPoints.Enqueue(addr + 8);

                        var target = ((ConditionalBranchInstruction) insn).target;
                        var targetLabel = target as LabelOperand;
                        if (targetLabel != null)
                        {
                            block.trueExit = getBlockForAddress(targetLabel.address);
                            entryPoints.Enqueue(targetLabel.address);
                        }

                        break;
                    }

                    var cpi = insn as CallPtrInstruction;
                    if (cpi?.target is RegisterOperand)
                    {
                        block.instructions.Add(addr + 4, instructions[addr + 4]);
                        var target = (RegisterOperand) cpi.target;
                        if (target.register == Register.ra)
                        {
                            block.exitType = ExitType.Return;
                            logger.Debug("return");
                        }
                        break;
                    }
                    else if (cpi?.target is LabelOperand && cpi.returnAddressTarget == null)
                    {
                        block.instructions.Add(addr + 4, instructions[addr + 4]);
                        block.exitType = ExitType.Unconditional;

                        logger.Debug("jmp " + cpi.target);

                        var lbl = (LabelOperand) cpi.target;
                        block.trueExit = getBlockForAddress(lbl.address);
                        entryPoints.Enqueue(lbl.address);
                    }
                }
            }
        }

        public void dump(IndentedTextWriter writer)
        {
            foreach (var block in blocks.Values)
            {
                block.dump(writer);
                writer.WriteLine();
            }
        }

        public void dumpPlanUml(TextWriter writer)
        {
            writer.WriteLine("skinparam stateFontName Lucida Console");
            writer.WriteLine("skinparam stateAttributeFontName Lucida Console");

            writer.WriteLine();
            writer.WriteLine($"[*] --> {blocks.Values.First().plantUmlName}");
            
            writer.WriteLine();
            foreach (var block in blocks.Values)
            {
                block.dumpPlantUml(writer);
                writer.WriteLine();
            }
        }
    }
}
