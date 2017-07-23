#define TRACE_CONTROLFLOW_EVAL

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using core;
using core.util;
using JetBrains.Annotations;
using mips.disasm;
using mips.instructions;
using mips.operands;

namespace exefile.controlflow
{
    public class ControlFlowProcessor
    {
        public SortedDictionary<uint, Block> blocks = new SortedDictionary<uint, Block>();

        [NotNull]
        private Block getBlockForAddress(uint addr)
        {
            Block block = null;
            blocks.TryGetValue(addr, out block);
            if (block == null)
            {
                blocks.Add(addr, block = new Block());
            }
            else
            {
                if (block.instructions.Count > 0 && block.start != addr)
                {
                    Console.WriteLine($"Block 0x{block.start:X} needs split at 0x{addr:X}");
                }
            }

            Debug.Assert(block != null);
            return block;
        }

        public void process(uint start, IDictionary<uint, Instruction> instructions)
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
                    Console.WriteLine($"Already processed: 0x{addr:X}");
                    continue;
                }

                Console.WriteLine($"=== Start analysis: 0x{addr:X} ===");

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

#if TRACE_CONTROLFLOW_EVAL
                    Console.WriteLine($"[eval 0x{addr:X}] {insn.asReadable()}");
#endif

                    if (insn is NopInstruction)
                    {
                        continue;
                    }

                    if (insn is ConditionalBranchInstruction)
                    {
                        block.exitType = ExitType.Conditional;

                        block.instructions.Add(addr + 4, instructions[addr + 4]);

                        block.condition = (ConditionalBranchInstruction) insn;
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
                        var target = (RegisterOperand) cpi.target;
                        if (target.register == Register.ra)
                        {
                            block.exitType = ExitType.Return;
#if TRACE_CONTROLFLOW_EVAL
                            Console.WriteLine("return");
#endif
                        }
                        break;
                    }
                    else if (cpi?.target is LabelOperand && cpi.returnAddressTarget == null)
                    {
                        block.exitType = ExitType.Unconditional;
#if TRACE_CONTROLFLOW_EVAL
                        Console.WriteLine("jmp " + cpi.target);
#endif

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
    }
}
