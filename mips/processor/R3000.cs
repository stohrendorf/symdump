using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using core;
using core.disasm;
using core.microcode;
using JetBrains.Annotations;
using mips.disasm;
using NLog;

namespace mips.processor
{
    public class R3000 : IDisassembler
    {
        internal const uint SyscallTypeBreak = 0;
        internal const uint SyscallTypeSyscall = 1;
        private static readonly ILogger logger = LogManager.GetCurrentClassLogger();
        private readonly IDebugSource _debugSource;
        private readonly uint? _gpBase;
        private readonly PSX _psx;

        private readonly TextSection _textSection;

        private uint _tmpRegId = 1000;

        public R3000([NotNull] TextSection textSection, uint? gpBase, IDebugSource debugSource)
        {
            _textSection = textSection;
            _gpBase = gpBase;
            _debugSource = debugSource;
            _psx = new PSX(this);
        }

        public void Disassemble(uint entrypoint)
        {
            _tmpRegId = 1000;

            logger.Info("Disassembly started");

            var calls = new Dictionary<ulong, uint>(); // TODO: use this information to re-analyze functions
            var callQueue = new Queue<ulong>();
            callQueue.Enqueue(entrypoint);
            foreach (var addr in _debugSource.Functions.Select(f => f.GlobalAddress))
                callQueue.Enqueue(addr);

            while (callQueue.Count > 0)
            {
                var addr = (uint) callQueue.Dequeue();
                if (_textSection.Instructions.ContainsKey(_textSection.MakeLocal(addr)))
                    continue;

                DisassembleFunction(addr, calls, callQueue);
            }

            logger.Info("Reversing control flow");
            foreach (var (blockAddress, block) in _textSection.Instructions)
            foreach (var (targetAddr, jumpType) in block.Outs)
            {
                if (!_textSection.Instructions.TryGetValue(targetAddr, out var target))
                {
                    logger.Error(
                        $"Target address 0x{targetAddr:x8} of type '{jumpType}' not in disassembled address space; assembly block:\n{block}");
                    continue;
                }

                target.Ins.Add(blockAddress, jumpType);
            }

            logger.Info("Collapsing basic assembly blocks");
            var oldSize = _textSection.Instructions.Count;
            var tmp = new SortedDictionary<uint, MicroAssemblyBlock>(_textSection.Instructions);
            _textSection.Instructions.Clear();
            MicroAssemblyBlock basicBlock = null;
            foreach (var addrAsm in tmp)
            {
                if (basicBlock == null)
                {
                    basicBlock = addrAsm.Value;
                    Debug.Assert(basicBlock.Address == addrAsm.Key);
                    _textSection.Instructions.Add(basicBlock.Address, basicBlock);
                    continue;
                }

                if (addrAsm.Value.Ins.Values.Any(x => x != JumpType.Control))
                {
                    // start a new basic block if we have an incoming edge that is no pure control flow
                    basicBlock = addrAsm.Value;
                    Debug.Assert(basicBlock.Address == addrAsm.Key);
                    _textSection.Instructions.Add(basicBlock.Address, basicBlock);
                    continue;
                }

                // replace the current's outgoing edges, and append the assembly
                basicBlock.Outs = addrAsm.Value.Outs;
                foreach (var insn in addrAsm.Value.Insns)
                    basicBlock.Insns.Add(insn);

                if (basicBlock.Outs.Count == 0 || basicBlock.Outs.Values.Any(x => x != JumpType.Control))
                    basicBlock = null;
            }

            logger.Info($"Collapsed {oldSize} blocks into {_textSection.Instructions.Count} blocks");

            logger.Info("Building function ownerships");
            foreach (var callee in _textSection.CalleesBySource.Values.SelectMany(x => x).ToHashSet())
                _textSection.CollectFunctionBlocks(_textSection.MakeLocal(callee));

            _psx.PeepholeOptimize(_textSection, _debugSource);
        }

        private void DisassembleFunction(uint entrypoint, [NotNull] IDictionary<ulong, uint> calls,
            Queue<ulong> callQueue)
        {
            var analysisQueue = new Queue<uint>();
            analysisQueue.Enqueue(_textSection.MakeLocal(entrypoint));
            while (analysisQueue.Count != 0)
                DisassembleInsn(analysisQueue.Dequeue(), analysisQueue, calls, callQueue);
        }

        private static void DecodeTlb(MicroAssemblyBlock asm, uint nextInsnAddressLocal, uint data,
            DelaySlotMode delaySlotMode)
        {
            switch ((TlbOpcode) (data & 0x1f))
            {
                case TlbOpcode.tlbr:
                    asm.Add(new UnsupportedInsn("tlbr"));
                    if (delaySlotMode != DelaySlotMode.AbortControl)
                        asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                case TlbOpcode.tlbwi:
                    asm.Add(new UnsupportedInsn("tlbwi"));
                    if (delaySlotMode != DelaySlotMode.AbortControl)
                        asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                case TlbOpcode.tlbwr:
                    asm.Add(new UnsupportedInsn("tlbwr"));
                    if (delaySlotMode != DelaySlotMode.AbortControl)
                        asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                case TlbOpcode.tlbp:
                    asm.Add(new UnsupportedInsn("tlbp"));
                    if (delaySlotMode != DelaySlotMode.AbortControl)
                        asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                case TlbOpcode.rfe:
                    asm.Add(new UnsupportedInsn("rfe"));
                    if (delaySlotMode != DelaySlotMode.AbortControl)
                        asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                default:
                    asm.Add(MicroOpcode.Data, new ConstValue(data, 32));
                    break;
            }
        }

        private void DecodeInstruction(MicroAssemblyBlock asm, uint data, uint nextInsnAddressLocal,
            DelaySlotMode delaySlotMode)
        {
            switch (ExtractOpcode(data))
            {
                case Opcode.RegisterFormat:
                    DecodeRegisterFormat(asm, nextInsnAddressLocal, data, delaySlotMode);
                    break;
                case Opcode.PCRelative:
                    if (delaySlotMode != DelaySlotMode.None)
                    {
                        logger.Warn($"Recursive delay slot disassembly at 0x{nextInsnAddressLocal - 4:x8}");
                        Console.WriteLine(asm);
                        break;
                    }

                    DecodePcRelative(asm, nextInsnAddressLocal, data);
                    break;
                case Opcode.j:
                {
                    if (delaySlotMode != DelaySlotMode.None)
                    {
                        logger.Warn($"j: Recursive delay slot disassembly at 0x{nextInsnAddressLocal - 4:x8}");
                        Console.WriteLine(asm);
                        break;
                    }

                    var absoluteAddress = (data & 0x03FFFFFF) * 4;
                    var tgt = new AddressValue(absoluteAddress, _debugSource.GetSymbolName(absoluteAddress));
                    if (_textSection.MakeLocal(absoluteAddress) != nextInsnAddressLocal + 4)
                        asm.Outs.Add(_textSection.MakeLocal(absoluteAddress), JumpType.Jump);
                    DecodeInstruction(asm, _textSection.WordAtLocal(nextInsnAddressLocal), nextInsnAddressLocal + 4,
                        DelaySlotMode.AbortControl);
                    asm.Add(MicroOpcode.Jmp, tgt);
                }
                    break;
                case Opcode.jal:
                {
                    if (delaySlotMode != DelaySlotMode.None)
                    {
                        logger.Warn($"jal: Recursive delay slot disassembly at 0x{nextInsnAddressLocal - 4:x8}");
                        Console.WriteLine(asm);
                        break;
                    }

                    var absoluteAddress = (data & 0x03FFFFFF) * 4;
                    var tgt = new AddressValue(absoluteAddress, _debugSource.GetSymbolName(absoluteAddress));
                    asm.Outs.Add(_textSection.MakeLocal(absoluteAddress), JumpType.Call);
                    if (!_textSection.CalleesBySource.TryGetValue(nextInsnAddressLocal - 1, out var callees))
                        callees = _textSection.CalleesBySource[nextInsnAddressLocal] = new HashSet<uint>();
                    callees.Add(absoluteAddress);
                    DecodeInstruction(asm, _textSection.WordAtLocal(nextInsnAddressLocal), nextInsnAddressLocal + 4,
                        DelaySlotMode.ContinueControl);
                    asm.Add(MicroOpcode.Call, new RegisterArg(Register.ra.ToUInt(), 32), tgt);
                }
                    break;
                case Opcode.beq:
                {
                    if (delaySlotMode != DelaySlotMode.None)
                    {
                        logger.Warn($"beq: Recursive delay slot disassembly at 0x{nextInsnAddressLocal - 4:x8}");
                        Console.WriteLine(asm);
                        break;
                    }

                    var localAddress = RelAddr(nextInsnAddressLocal, (short) data);
                    var tgt = new AddressValue(_textSection.MakeGlobal(localAddress),
                        _debugSource.GetSymbolName(_textSection.MakeGlobal(localAddress)));
                    asm.Outs.Add(localAddress, JumpType.JumpConditional);
                    var r1 = MakeZeroRegisterOperand(data, 21);
                    var r2 = MakeZeroRegisterOperand(data, 16);
                    var tmp = new RegisterArg(Register.CmpResult.ToUInt(), 1);
                    asm.Add(MicroOpcode.SetEq, tmp, r1, r2);
                    DecodeInstruction(asm, _textSection.WordAtLocal(nextInsnAddressLocal), nextInsnAddressLocal + 4,
                        DelaySlotMode.ContinueControl);
                    asm.Add(MicroOpcode.JmpIf, tmp, tgt);
                }
                    break;
                case Opcode.bne:
                {
                    if (delaySlotMode != DelaySlotMode.None)
                    {
                        logger.Warn($"bne: Recursive delay slot disassembly at 0x{nextInsnAddressLocal - 4:x8}");
                        Console.WriteLine(asm);
                        break;
                    }

                    var localAddress = RelAddr(nextInsnAddressLocal, (short) data);
                    var tgt = new AddressValue(_textSection.MakeGlobal(localAddress),
                        _debugSource.GetSymbolName(_textSection.MakeGlobal(localAddress)));
                    asm.Outs.Add(localAddress, JumpType.JumpConditional);
                    var r1 = MakeZeroRegisterOperand(data, 21);
                    var r2 = MakeZeroRegisterOperand(data, 16);
                    var tmp = new RegisterArg(Register.CmpResult.ToUInt(), 1);
                    asm.Add(MicroOpcode.SetNEq, tmp, r1, r2);
                    DecodeInstruction(asm, _textSection.WordAtLocal(nextInsnAddressLocal), nextInsnAddressLocal + 4,
                        DelaySlotMode.ContinueControl);
                    asm.Add(MicroOpcode.JmpIf, tmp, tgt);
                }
                    break;
                case Opcode.blez:
                {
                    if (delaySlotMode != DelaySlotMode.None)
                    {
                        logger.Warn($"blez: Recursive delay slot disassembly at 0x{nextInsnAddressLocal - 4:x8}");
                        Console.WriteLine(asm);
                        break;
                    }

                    var localAddress = RelAddr(nextInsnAddressLocal, (short) data);
                    var tgt = new AddressValue(_textSection.MakeGlobal(localAddress),
                        _debugSource.GetSymbolName(_textSection.MakeGlobal(localAddress)));
                    asm.Outs.Add(localAddress, JumpType.JumpConditional);
                    var r1 = MakeZeroRegisterOperand(data, 21);
                    var tmp = new RegisterArg(Register.CmpResult.ToUInt(), 1);
                    asm.Add(MicroOpcode.SSetLE, tmp, r1, new ConstValue(0, 32));
                    DecodeInstruction(asm, _textSection.WordAtLocal(nextInsnAddressLocal), nextInsnAddressLocal + 4,
                        DelaySlotMode.ContinueControl);
                    asm.Add(MicroOpcode.JmpIf, tmp, tgt);
                }
                    break;
                case Opcode.bgtz:
                {
                    if (delaySlotMode != DelaySlotMode.None)
                    {
                        logger.Warn($"bgtz: Recursive delay slot disassembly at 0x{nextInsnAddressLocal - 4:x8}");
                        Console.WriteLine(asm);
                        break;
                    }

                    var localAddress = RelAddr(nextInsnAddressLocal, (short) data);
                    var tgt = new AddressValue(_textSection.MakeGlobal(localAddress),
                        _debugSource.GetSymbolName(_textSection.MakeGlobal(localAddress)));
                    asm.Outs.Add(localAddress, JumpType.JumpConditional);
                    var r1 = MakeZeroRegisterOperand(data, 21);
                    var tmp = new RegisterArg(Register.CmpResult.ToUInt(), 1);
                    asm.Add(MicroOpcode.SSetL, tmp, new ConstValue(0, 32), r1);
                    DecodeInstruction(asm, _textSection.WordAtLocal(nextInsnAddressLocal), nextInsnAddressLocal + 4,
                        DelaySlotMode.ContinueControl);
                    asm.Add(MicroOpcode.JmpIf, tmp, tgt);
                }
                    break;
                case Opcode.addi:
                    asm.Add(MicroOpcode.Add, MakeZeroRegisterOperand(data, 16), MakeZeroRegisterOperand(data, 21),
                        new ConstValue((ushort) data, 16));
                    if (delaySlotMode != DelaySlotMode.AbortControl)
                        asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                case Opcode.addiu:
                    asm.Add(MicroOpcode.Add, MakeZeroRegisterOperand(data, 16), MakeZeroRegisterOperand(data, 21),
                        new ConstValue((ushort) data, 16));
                    if (delaySlotMode != DelaySlotMode.AbortControl)
                        asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                case Opcode.slti:
                {
                    var tmp = GetTmpReg(32);
                    asm.Add(new SignedCastInsn(tmp, new ConstValue((ushort) data, 16)));
                    asm.Add(MicroOpcode.SSetL, MakeZeroRegisterOperand(data, 16), MakeZeroRegisterOperand(data, 21),
                        tmp);
                    if (delaySlotMode != DelaySlotMode.AbortControl)
                        asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                }
                case Opcode.sltiu:
                {
                    var tmp = GetTmpReg(32);
                    asm.Add(new SignedCastInsn(tmp, new ConstValue((ushort) data, 16)));
                    asm.Add(MicroOpcode.SSetL, MakeZeroRegisterOperand(data, 16), MakeZeroRegisterOperand(data, 21),
                        tmp);
                    if (delaySlotMode != DelaySlotMode.AbortControl)
                        asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                }
                case Opcode.andi:
                    asm.Add(MicroOpcode.And, MakeZeroRegisterOperand(data, 16), MakeZeroRegisterOperand(data, 21),
                        new ConstValue((ushort) data, 16));
                    if (delaySlotMode != DelaySlotMode.AbortControl)
                        asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                case Opcode.ori:
                    asm.Add(MicroOpcode.Or, MakeZeroRegisterOperand(data, 16), MakeZeroRegisterOperand(data, 21),
                        new ConstValue((ushort) data, 16));
                    if (delaySlotMode != DelaySlotMode.AbortControl)
                        asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                case Opcode.xori:
                    asm.Add(MicroOpcode.XOr, MakeZeroRegisterOperand(data, 16), MakeZeroRegisterOperand(data, 21),
                        new ConstValue((ushort) data, 16));
                    if (delaySlotMode != DelaySlotMode.AbortControl)
                        asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                case Opcode.lui:
                    asm.Add(new CopyInsn(MakeZeroRegisterOperand(data, 16),
                        new ConstValue((ulong) ((ushort) data << 16), 32)));
                    if (delaySlotMode != DelaySlotMode.AbortControl)
                        asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                case Opcode.CpuControl:
                    DecodeCpuControl(asm, nextInsnAddressLocal, data, delaySlotMode);
                    break;
                case Opcode.FloatingPoint:
                    asm.Add(MicroOpcode.Data, new ConstValue(data, 32));
                    break;
                case Opcode.lb:
                    asm.Add(
                        new SignedCastInsn(MakeRegisterOperand(data, 16), MakeGpBasedArg(data, 21, (short) data, 8)));
                    if (delaySlotMode != DelaySlotMode.AbortControl)
                        asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                case Opcode.lh:
                    asm.Add(new SignedCastInsn(MakeRegisterOperand(data, 16),
                        MakeGpBasedArg(data, 21, (short) data, 16)));
                    if (delaySlotMode != DelaySlotMode.AbortControl)
                        asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                case Opcode.lwl:
                    asm.Add(new UnsupportedInsn("lwl", MakeZeroRegisterOperand(data, 32),
                        MakeGpBasedArg(data, 21, (short) data, 32)));
                    if (delaySlotMode != DelaySlotMode.AbortControl)
                        asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                case Opcode.lw:
                    asm.Add(new CopyInsn(MakeZeroRegisterOperand(data, 16),
                        MakeGpBasedArg(data, 21, (short) data, 32)));
                    if (delaySlotMode != DelaySlotMode.AbortControl)
                        asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                case Opcode.lbu:
                    asm.Add(new UnsignedCastInsn(MakeRegisterOperand(data, 16),
                        MakeGpBasedArg(data, 21, (short) data, 8)));
                    if (delaySlotMode != DelaySlotMode.AbortControl)
                        asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                case Opcode.lhu:
                    asm.Add(new UnsignedCastInsn(MakeRegisterOperand(data, 16),
                        MakeGpBasedArg(data, 21, (short) data, 16)));
                    if (delaySlotMode != DelaySlotMode.AbortControl)
                        asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                case Opcode.lwr:
                    asm.Add(new UnsupportedInsn("lwr", MakeZeroRegisterOperand(data, 16),
                        MakeGpBasedArg(data, 21, (short) data, 32)));
                    if (delaySlotMode != DelaySlotMode.AbortControl)
                        asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                case Opcode.sb:
                {
                    var op = MakeZeroRegisterOperand(data, 16);
                    if (op is RegisterArg r)
                        asm.Add(new UnsignedCastInsn(MakeGpBasedArg(data, 21, (short) data, 8), r));
                    else
                        asm.Add(new CopyInsn(MakeGpBasedArg(data, 21, (short) data, 8), new ConstValue(0, 8)));

                    if (delaySlotMode != DelaySlotMode.AbortControl)
                        asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                }
                    break;
                case Opcode.sh:
                {
                    var op = MakeZeroRegisterOperand(data, 16);
                    if (op is RegisterArg r)
                        asm.Add(new UnsignedCastInsn(MakeGpBasedArg(data, 21, (short) data, 16), r));
                    else
                        asm.Add(new CopyInsn(MakeGpBasedArg(data, 21, (short) data, 16), new ConstValue(0, 16)));

                    if (delaySlotMode != DelaySlotMode.AbortControl)
                        asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                }
                    break;
                case Opcode.swl:
                    asm.Add(new UnsupportedInsn("swl", MakeZeroRegisterOperand(data, 16),
                        MakeGpBasedArg(data, 21, (short) data, 32)));
                    if (delaySlotMode != DelaySlotMode.AbortControl)
                        asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                case Opcode.sw:
                    asm.Add(new CopyInsn(MakeGpBasedArg(data, 21, (short) data, 32),
                        MakeZeroRegisterOperand(data, 16)));
                    if (delaySlotMode != DelaySlotMode.AbortControl)
                        asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                case Opcode.swr:
                    asm.Add(new UnsupportedInsn("swr", MakeZeroRegisterOperand(data, 16),
                        MakeGpBasedArg(data, 21, (short) data, 32)));
                    if (delaySlotMode != DelaySlotMode.AbortControl)
                        asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                case Opcode.swc1:
                    asm.Add(new UnsupportedInsn("swc1", MakeZeroRegisterOperand(data, 16),
                        new ConstValue((ushort) data, 16), MakeZeroRegisterOperand(data, 21)));
                    if (delaySlotMode != DelaySlotMode.AbortControl)
                        asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                case Opcode.lwc1:
                    asm.Add(new UnsupportedInsn("lwc1", MakeC2RegisterOperand(data, 16),
                        new ConstValue((ushort) data, 16), MakeZeroRegisterOperand(data, 21)));
                    if (delaySlotMode != DelaySlotMode.AbortControl)
                        asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                case Opcode.cop0:
                    asm.Add(new UnsupportedInsn("cop0", new ConstValue(data & ((1 << 26) - 1), 26)));
                    if (delaySlotMode != DelaySlotMode.AbortControl)
                        asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                case Opcode.cop1:
                    asm.Add(new UnsupportedInsn("cop1", new ConstValue(data & ((1 << 26) - 1), 26)));
                    if (delaySlotMode != DelaySlotMode.AbortControl)
                        asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                case Opcode.cop2:
                    DecodeCop2(asm, nextInsnAddressLocal, data, delaySlotMode);
                    break;
                case Opcode.cop3:
                    asm.Add(new UnsupportedInsn("cop3", new ConstValue(data & ((1 << 26) - 1), 26)));
                    if (delaySlotMode != DelaySlotMode.AbortControl)
                        asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                case Opcode.beql:
                {
                    if (delaySlotMode != DelaySlotMode.None)
                    {
                        logger.Warn($"beql: Recursive delay slot disassembly at 0x{nextInsnAddressLocal - 4:x8}");
                        Console.WriteLine(asm);
                        break;
                    }

                    var localAddress = RelAddr(nextInsnAddressLocal, (short) data);
                    var tgt = new AddressValue(_textSection.MakeGlobal(localAddress),
                        _debugSource.GetSymbolName(_textSection.MakeGlobal(localAddress)));
                    asm.Outs.Add(localAddress, JumpType.JumpConditional);
                    var tmp = new RegisterArg(Register.CmpResult.ToUInt(), 1);
                    asm.Add(MicroOpcode.SetEq, tmp, MakeZeroRegisterOperand(data, 21),
                        MakeZeroRegisterOperand(data, 16));
                    DecodeInstruction(asm, _textSection.WordAtLocal(nextInsnAddressLocal), nextInsnAddressLocal + 4,
                        DelaySlotMode.ContinueControl);
                    asm.Add(MicroOpcode.JmpIf, tmp, tgt);
                }
                    break;
                case Opcode.bnel:
                {
                    if (delaySlotMode != DelaySlotMode.None)
                    {
                        logger.Warn($"bnel: Recursive delay slot disassembly at 0x{nextInsnAddressLocal - 4:x8}");
                        Console.WriteLine(asm);
                        break;
                    }

                    var localAddress = RelAddr(nextInsnAddressLocal, (short) data);
                    var tgt = new AddressValue(_textSection.MakeGlobal(localAddress),
                        _debugSource.GetSymbolName(_textSection.MakeGlobal(localAddress)));
                    asm.Outs.Add(localAddress, JumpType.JumpConditional);
                    var tmp = new RegisterArg(Register.CmpResult.ToUInt(), 1);
                    asm.Add(MicroOpcode.SetNEq, tmp, MakeZeroRegisterOperand(data, 21),
                        MakeZeroRegisterOperand(data, 16));
                    DecodeInstruction(asm, _textSection.WordAtLocal(nextInsnAddressLocal), nextInsnAddressLocal + 4,
                        DelaySlotMode.ContinueControl);
                    asm.Add(MicroOpcode.JmpIf, tmp, tgt);
                }
                    break;
                case Opcode.blezl:
                {
                    if (delaySlotMode != DelaySlotMode.None)
                    {
                        logger.Warn($"blezl: Recursive delay slot disassembly at 0x{nextInsnAddressLocal - 4:x8}");
                        Console.WriteLine(asm);
                        break;
                    }

                    var localAddress = RelAddr(nextInsnAddressLocal, (short) data);
                    var tgt = new AddressValue(_textSection.MakeGlobal(localAddress),
                        _debugSource.GetSymbolName(_textSection.MakeGlobal(localAddress)));
                    asm.Outs.Add(localAddress, JumpType.JumpConditional);
                    var tmp = new RegisterArg(Register.CmpResult.ToUInt(), 1);
                    asm.Add(MicroOpcode.SSetLE, tmp, MakeZeroRegisterOperand(data, 21), new ConstValue(0, 32));
                    DecodeInstruction(asm, _textSection.WordAtLocal(nextInsnAddressLocal), nextInsnAddressLocal + 4,
                        DelaySlotMode.ContinueControl);
                    asm.Add(MicroOpcode.JmpIf, tmp, tgt);
                }
                    break;
                case Opcode.bgtzl:
                {
                    if (delaySlotMode != DelaySlotMode.None)
                    {
                        logger.Warn($"bgtzl: Recursive delay slot disassembly at 0x{nextInsnAddressLocal - 4:x8}");
                        Console.WriteLine(asm);
                        break;
                    }

                    var localAddress = RelAddr(nextInsnAddressLocal, (short) data);
                    var tgt = new AddressValue(_textSection.MakeGlobal(localAddress),
                        _debugSource.GetSymbolName(_textSection.MakeGlobal(localAddress)));
                    asm.Outs.Add(localAddress, JumpType.JumpConditional);
                    var tmp = new RegisterArg(Register.CmpResult.ToUInt(), 1);
                    asm.Add(MicroOpcode.SSetL, tmp, new ConstValue(0, 32), MakeZeroRegisterOperand(data, 21));
                    DecodeInstruction(asm, _textSection.WordAtLocal(nextInsnAddressLocal), nextInsnAddressLocal + 4,
                        DelaySlotMode.ContinueControl);
                    asm.Add(MicroOpcode.JmpIf, tmp, tgt);
                }
                    break;
                default:
                    asm.Add(MicroOpcode.Data, new ConstValue(data, 32));
                    break;
            }
        }

        private void DecodeRegisterFormat(MicroAssemblyBlock asm, uint nextInsnAddressLocal, uint data,
            DelaySlotMode delaySlotMode)
        {
            var rd = MakeZeroRegisterOperand(data, 11);
            var rs2 = MakeZeroRegisterOperand(data, 16);
            var rs1 = MakeZeroRegisterOperand(data, 21);
            switch ((OpcodeFunction) (data & 0x3f))
            {
                case OpcodeFunction.sll:
                    if (data == 0)
                        asm.Add(MicroOpcode.Nop);
                    else
                        asm.Add(MicroOpcode.SHL, rd, rs2, new ConstValue((data >> 6) & 0x1F, 5));
                    if (delaySlotMode != DelaySlotMode.AbortControl)
                        asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                case OpcodeFunction.srl:
                    if (data == 0)
                        asm.Add(MicroOpcode.Nop);
                    else
                        asm.Add(MicroOpcode.SRL, rd, rs2, new ConstValue((data >> 6) & 0x1F, 5));
                    if (delaySlotMode != DelaySlotMode.AbortControl)
                        asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                case OpcodeFunction.sra:
                    if (data == 0)
                        asm.Add(MicroOpcode.Nop);
                    else
                        asm.Add(MicroOpcode.SRA, rd, rs2, new ConstValue((data >> 6) & 0x1F, 5));
                    if (delaySlotMode != DelaySlotMode.AbortControl)
                        asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                case OpcodeFunction.sllv:
                    asm.Add(MicroOpcode.SHL, rd, rs2, rs1);
                    if (delaySlotMode != DelaySlotMode.AbortControl)
                        asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                case OpcodeFunction.srlv:
                    asm.Add(MicroOpcode.SRL, rd, rs2, rs1);
                    if (delaySlotMode != DelaySlotMode.AbortControl)
                        asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                case OpcodeFunction.srav:
                    asm.Add(MicroOpcode.SRA, rd, rs2, rs1);
                    if (delaySlotMode != DelaySlotMode.AbortControl)
                        asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                case OpcodeFunction.jr:
                    DecodeInstruction(asm, _textSection.WordAtLocal(nextInsnAddressLocal), nextInsnAddressLocal + 4,
                        DelaySlotMode.AbortControl);
                    if (rs1 is RegisterArg r && r.Register == Register.ra.ToUInt())
                    {
                        asm.Add(MicroOpcode.Return, rs1);
                    }
                    else
                    {
                        logger.Info(
                            $"Possible switch statement at 0x{_textSection.MakeGlobal(nextInsnAddressLocal - 4):x8}");
                        asm.Add(MicroOpcode.DynamicJmp, rs1);
                    }

                    break;
                case OpcodeFunction.jalr:
                    DecodeInstruction(asm, _textSection.WordAtLocal(nextInsnAddressLocal), nextInsnAddressLocal + 4,
                        DelaySlotMode.AbortControl);
                    asm.Add(MicroOpcode.Call, rd, rs1);
                    break;
                case OpcodeFunction.syscall:
                    asm.Add(MicroOpcode.Syscall, new ConstValue(SyscallTypeSyscall, 32),
                        new ConstValue((data >> 6) & 0xFFFFF, 20));
                    break;
                case OpcodeFunction.break_:
                    asm.Add(MicroOpcode.Syscall, new ConstValue(SyscallTypeBreak, 32),
                        new ConstValue((data >> 6) & 0xFFFFF, 20));
                    break;
                case OpcodeFunction.mfhi:
                {
                    var tmp = GetTmpReg(64);
                    asm.Add(new CopyInsn(tmp, new RegisterArg(Register.DivMulResult.ToUInt(), 64)));
                    asm.Add(MicroOpcode.SRA, tmp, tmp, new ConstValue(32, 6));
                    asm.Add(new UnsignedCastInsn(rd, tmp));
                    if (delaySlotMode != DelaySlotMode.AbortControl)
                        asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                }
                    break;
                case OpcodeFunction.mthi:
                    asm.Add(new UnsupportedInsn("mthi", rd));
                    if (delaySlotMode != DelaySlotMode.AbortControl)
                        asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                case OpcodeFunction.mflo:
                    asm.Add(new UnsignedCastInsn(rd, new RegisterArg(Register.DivMulResult.ToUInt(), 64)));
                    if (delaySlotMode != DelaySlotMode.AbortControl)
                        asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                case OpcodeFunction.mtlo:
                    asm.Add(new UnsupportedInsn("mtlo", rd));
                    if (delaySlotMode != DelaySlotMode.AbortControl)
                        asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                case OpcodeFunction.mult:
                    asm.Add(MicroOpcode.SMul, new RegisterArg(Register.DivMulResult.ToUInt(), 64), rs1, rs2);
                    if (delaySlotMode != DelaySlotMode.AbortControl)
                        asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                case OpcodeFunction.multu:
                    asm.Add(MicroOpcode.UMul, new RegisterArg(Register.DivMulResult.ToUInt(), 64), rs1, rs2);
                    if (delaySlotMode != DelaySlotMode.AbortControl)
                        asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                case OpcodeFunction.div:
                {
                    var tmpD = GetTmpReg(32);
                    asm.Add(MicroOpcode.SDiv, tmpD, rs1, rs2);
                    var tmpM = GetTmpReg(32);
                    asm.Add(MicroOpcode.SMod, tmpM, rs1, rs2);
                    var dm = new RegisterArg(Register.DivMulResult.ToUInt(), 64);
                    asm.Add(new UnsignedCastInsn(dm, tmpM));
                    asm.Add(MicroOpcode.SHL, dm, dm, new ConstValue(32, 6));
                    asm.Add(MicroOpcode.Or, dm, dm, tmpD);
                    if (delaySlotMode != DelaySlotMode.AbortControl)
                        asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                }
                    break;
                case OpcodeFunction.divu:
                {
                    var tmpD = GetTmpReg(32);
                    asm.Add(MicroOpcode.UDiv, tmpD, rs1, rs2);
                    var tmpM = GetTmpReg(32);
                    asm.Add(MicroOpcode.UMod, tmpM, rs1, rs2);
                    var dm = new RegisterArg(Register.DivMulResult.ToUInt(), 64);
                    asm.Add(new UnsignedCastInsn(dm, tmpM));
                    asm.Add(MicroOpcode.SHL, dm, dm, new ConstValue(32, 6));
                    asm.Add(MicroOpcode.Or, dm, dm, tmpD);
                    if (delaySlotMode != DelaySlotMode.AbortControl)
                        asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                }
                    break;
                case OpcodeFunction.add:
                    asm.Add(MicroOpcode.Add, rd, rs1, rs2);
                    if (delaySlotMode != DelaySlotMode.AbortControl)
                        asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    return;
                case OpcodeFunction.addu:
                    asm.Add(MicroOpcode.Add, rd, rs1, rs2);
                    if (delaySlotMode != DelaySlotMode.AbortControl)
                        asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    return;
                case OpcodeFunction.sub:
                    asm.Add(MicroOpcode.Sub, rd, rs1, rs2);
                    if (delaySlotMode != DelaySlotMode.AbortControl)
                        asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    return;
                case OpcodeFunction.subu:
                    asm.Add(MicroOpcode.Sub, rd, rs1, rs2);
                    if (delaySlotMode != DelaySlotMode.AbortControl)
                        asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    return;
                case OpcodeFunction.and:
                    asm.Add(MicroOpcode.And, rd, rs1, rs2);
                    if (delaySlotMode != DelaySlotMode.AbortControl)
                        asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    return;
                case OpcodeFunction.or:
                    asm.Add(MicroOpcode.Or, rd, rs1, rs2);
                    if (delaySlotMode != DelaySlotMode.AbortControl)
                        asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    return;
                case OpcodeFunction.xor:
                    asm.Add(MicroOpcode.XOr, rd, rs1, rs2);
                    if (delaySlotMode != DelaySlotMode.AbortControl)
                        asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    return;
                case OpcodeFunction.nor:
                {
                    var tmp = GetTmpReg(32);
                    asm.Add(MicroOpcode.Or, tmp, rs1, rs2);
                    asm.Add(MicroOpcode.Not, tmp);
                    asm.Add(MicroOpcode.Copy, rd, tmp);
                    if (delaySlotMode != DelaySlotMode.AbortControl)
                        asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                }
                    break;
                case OpcodeFunction.slt:
                    asm.Add(MicroOpcode.SSetL, rd, rs1, rs2);
                    if (delaySlotMode != DelaySlotMode.AbortControl)
                        asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                case OpcodeFunction.sltu:
                    asm.Add(MicroOpcode.USetL, rd, rs1, rs2);
                    if (delaySlotMode != DelaySlotMode.AbortControl)
                        asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                default:
                    asm.Add(MicroOpcode.Data, new ConstValue(data, 32));
                    break;
            }
        }

        private void DecodeCpuControl(MicroAssemblyBlock asm, uint nextInsnAddressLocal, uint data,
            DelaySlotMode delaySlotMode)
        {
            switch ((CpuControlOpcode) ((data >> 21) & 0x1f))
            {
                case CpuControlOpcode.mtc0:
                    asm.Add(new UnsupportedInsn("mtc0", MakeZeroRegisterOperand(data, 16),
                        MakeC0RegisterOperand(data, 11)));
                    break;
                case CpuControlOpcode.bc0:
                    switch ((data >> 16) & 0x1f)
                    {
                        case 0:
                        {
                            var localAddress = RelAddr(nextInsnAddressLocal, (short) data);
                            asm.Add(new UnsupportedInsn("bc0f",
                                new AddressValue(_textSection.MakeGlobal(localAddress),
                                    _debugSource.GetSymbolName(_textSection.MakeGlobal(localAddress)))));

                            asm.Outs.Add(localAddress, JumpType.JumpConditional);
                            DecodeInstruction(asm, _textSection.WordAtLocal(nextInsnAddressLocal),
                                nextInsnAddressLocal + 4,
                                DelaySlotMode.ContinueControl);
                        }
                            break;
                        case 1:
                        {
                            var localAddress = RelAddr(nextInsnAddressLocal, (short) data);
                            asm.Add(new UnsupportedInsn("bc0t",
                                new AddressValue(_textSection.MakeGlobal(localAddress),
                                    _debugSource.GetSymbolName(_textSection.MakeGlobal(localAddress)))));

                            asm.Outs.Add(localAddress, JumpType.JumpConditional);
                            DecodeInstruction(asm, _textSection.WordAtLocal(nextInsnAddressLocal),
                                nextInsnAddressLocal + 4,
                                DelaySlotMode.ContinueControl);
                        }
                            break;
                        default:
                            asm.Add(MicroOpcode.Data, new ConstValue(data, 32));
                            break;
                    }

                    break;
                case CpuControlOpcode.tlb:
                    DecodeTlb(asm, nextInsnAddressLocal, data, delaySlotMode);
                    break;
                case CpuControlOpcode.mfc0:
                    asm.Add(new UnsupportedInsn("mfc0", MakeZeroRegisterOperand(data, 16),
                        MakeC0RegisterOperand(data, 11)));
                    if (delaySlotMode != DelaySlotMode.AbortControl)
                        asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                default:
                    asm.Add(MicroOpcode.Data, new ConstValue(data, 32));
                    break;
            }
        }

        private void DecodePcRelative(MicroAssemblyBlock asm, uint nextInsnAddressLocal, uint data)
        {
            var rs = MakeZeroRegisterOperand(data, 21);
            var localAddress = RelAddr(nextInsnAddressLocal, (short) data);
            var offset = new AddressValue(_textSection.MakeGlobal(localAddress),
                _debugSource.GetSymbolName(_textSection.MakeGlobal(localAddress)));
            switch ((data >> 16) & 0x1f)
            {
                case 0: // bltz
                {
                    asm.Outs.Add(localAddress, JumpType.JumpConditional);
                    var tmpReg = new RegisterArg(Register.CmpResult.ToUInt(), 1);
                    asm.Add(MicroOpcode.SSetL, tmpReg, rs, new ConstValue(0, 32));
                    DecodeInstruction(asm, _textSection.WordAtLocal(nextInsnAddressLocal), nextInsnAddressLocal + 4,
                        DelaySlotMode.ContinueControl);
                    asm.Add(MicroOpcode.JmpIf, tmpReg, offset);
                    break;
                }
                case 1: // bgez
                {
                    asm.Outs.Add(localAddress, JumpType.JumpConditional);
                    var tmpReg = new RegisterArg(Register.CmpResult.ToUInt(), 1);
                    asm.Add(MicroOpcode.SSetLE, tmpReg, new ConstValue(0, 32), rs);
                    DecodeInstruction(asm, _textSection.WordAtLocal(nextInsnAddressLocal), nextInsnAddressLocal + 4,
                        DelaySlotMode.ContinueControl);
                    asm.Add(MicroOpcode.JmpIf, tmpReg, offset);
                    break;
                }
                case 16: // bltzal
                {
                    asm.Outs.Add(localAddress, JumpType.JumpConditional);
                    var tmpReg = new RegisterArg(Register.CmpResult.ToUInt(), 1);
                    asm.Add(MicroOpcode.SSetL, tmpReg, rs, new ConstValue(0, 32));
                    DecodeInstruction(asm, _textSection.WordAtLocal(nextInsnAddressLocal), nextInsnAddressLocal + 4,
                        DelaySlotMode.ContinueControl);
                    asm.Add(MicroOpcode.JmpIf, tmpReg, offset);
                    break;
                }
                case 17: // bgezal
                {
                    asm.Outs.Add(localAddress, JumpType.JumpConditional);
                    var tmpReg = new RegisterArg(Register.CmpResult.ToUInt(), 1);
                    asm.Add(MicroOpcode.SSetLE, tmpReg, new ConstValue(0, 32), rs);
                    DecodeInstruction(asm, _textSection.WordAtLocal(nextInsnAddressLocal), nextInsnAddressLocal + 4,
                        DelaySlotMode.ContinueControl);
                    asm.Add(MicroOpcode.JmpIf, tmpReg, offset);
                    break;
                }
                default:
                    asm.Add(MicroOpcode.Data, new ConstValue(data, 32));
                    break;
            }
        }

        private static IMicroArg MakeZeroRegisterOperand(uint data, int offset)
        {
            var r = (Register) ((data >> offset) & 0x1f);
            if (r == Register.zero)
                return new ConstValue(0, 32);
            return new RegisterArg(r.ToUInt(), 32);
        }

        private static RegisterArg MakeRegisterOperand(uint data, int offset)
        {
            var r = (Register) ((data >> offset) & 0x1f);
            return new RegisterArg(r.ToUInt(), 32);
        }

        private static RegisterArg MakeC0RegisterOperand(uint data, int offset)
        {
            return new RegisterArg(((C0Register) ((data >> offset) & 0x1f)).ToUInt(), 32);
        }

        private static RegisterArg MakeC2RegisterOperand(uint data, int offset)
        {
            return new RegisterArg(((C2Register) ((data >> offset) & 0x1f)).ToUInt(), 32);
        }

        private static RegisterArg MakeC2ControlRegisterOperand(uint data, int offset)
        {
            return new RegisterArg(((C2ControlRegister) ((data >> offset) & 0x1f)).ToUInt(), 32);
        }

        private static RegisterMemArg MakeRegisterOffsetArg(uint data, int shift, int offset, byte bits)
        {
            return new RegisterMemArg(((Register) ((data >> shift) & 0x1f)).ToUInt(), offset, bits);
        }

        private IMicroArg MakeGpBasedArg(uint data, int shift, int offset, byte bits)
        {
            var regOfs = MakeRegisterOffsetArg(data, shift, offset, bits);
            if (_gpBase == null || regOfs.Register != Register.gp.ToUInt())
                return regOfs;

            var absoluteAddress = (uint) (_gpBase.Value + regOfs.Offset);
            return new AddressValue(absoluteAddress, _debugSource.GetSymbolName(absoluteAddress)).Deref(bits);
        }

        private static Opcode ExtractOpcode(uint data)
        {
            return (Opcode) (data >> 26);
        }

        private static uint RelAddr(uint @base, short offset)
        {
            return (uint) (@base + offset * 4);
        }

        private static void DecodeCop2(MicroAssemblyBlock asm, uint nextInsnAddressLocal, uint data,
            DelaySlotMode delaySlotMode)
        {
            var coFun = data & ((1 << 25) - 1);
            if (((data >> 25) & 1) != 0)
            {
                DecodeCop2Gte(asm, nextInsnAddressLocal, coFun, delaySlotMode);
                return;
            }

            var cf = (coFun >> 21) & 0x1F;
            switch (cf)
            {
                case 0: // mfc2
                    asm.Add(new CopyInsn(MakeRegisterOperand(coFun, 16),
                        MakeC2RegisterOperand(coFun, 11)));
                    if (delaySlotMode != DelaySlotMode.AbortControl)
                        asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                case 2: // cfc2
                    asm.Add(new CopyInsn(MakeRegisterOperand(coFun, 16),
                        MakeC2ControlRegisterOperand(coFun, 11)));
                    if (delaySlotMode != DelaySlotMode.AbortControl)
                        asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                case 4: // mtc2
                    asm.Add(new CopyInsn(MakeC2RegisterOperand(coFun, 11),
                        MakeRegisterOperand(coFun, 16)));
                    if (delaySlotMode != DelaySlotMode.AbortControl)
                        asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                case 6: // ctc2
                    asm.Add(new CopyInsn(MakeC2ControlRegisterOperand(coFun, 11),
                        MakeRegisterOperand(coFun, 16)));
                    if (delaySlotMode != DelaySlotMode.AbortControl)
                        asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                default:
                    asm.Add(MicroOpcode.Data, new ConstValue(data, 32));
                    break;
            }
        }

        private static void DecodeCop2Gte(MicroAssemblyBlock asm, uint nextInsnAddressLocal, uint coFun,
            DelaySlotMode delaySlotMode)
        {
            switch (coFun)
            {
                case 0x0180001:
                    asm.Add(MicroOpcode.Syscall, new AddressValue(0, "syscall!cop2_rtps"));
                    if (delaySlotMode != DelaySlotMode.AbortControl)
                        asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                case 0x0280030:
                    asm.Add(MicroOpcode.Syscall, new AddressValue(0, "syscall!cop2_rtpt"));
                    if (delaySlotMode != DelaySlotMode.AbortControl)
                        asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                case 0x0400012:
                    asm.Add(MicroOpcode.Syscall, new AddressValue(0, "syscall!cop2_mvmva"));
                    if (delaySlotMode != DelaySlotMode.AbortControl)
                        asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                case 0x0680029:
                    asm.Add(MicroOpcode.Syscall, new AddressValue(0, "syscall!cop2_dpcl"));
                    if (delaySlotMode != DelaySlotMode.AbortControl)
                        asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                case 0x0780010:
                    asm.Add(MicroOpcode.Syscall, new AddressValue(0, "syscall!cop2_dpcs"));
                    if (delaySlotMode != DelaySlotMode.AbortControl)
                        asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                case 0x0F8002A:
                    asm.Add(MicroOpcode.Syscall, new AddressValue(0, "syscall!cop2_dpct"));
                    if (delaySlotMode != DelaySlotMode.AbortControl)
                        asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                case 0x0980011:
                    asm.Add(MicroOpcode.Syscall, new AddressValue(0, "syscall!cop2_intpl"));
                    if (delaySlotMode != DelaySlotMode.AbortControl)
                        asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                case 0x0A00428:
                    asm.Add(MicroOpcode.Syscall, new AddressValue(0, "syscall!cop2_sqr"));
                    if (delaySlotMode != DelaySlotMode.AbortControl)
                        asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                case 0x0C8041E:
                    asm.Add(MicroOpcode.Syscall, new AddressValue(0, "syscall!cop2_ncs"));
                    if (delaySlotMode != DelaySlotMode.AbortControl)
                        asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                case 0x0D80420:
                    asm.Add(MicroOpcode.Syscall, new AddressValue(0, "syscall!cop2_nct"));
                    if (delaySlotMode != DelaySlotMode.AbortControl)
                        asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                case 0x0E80413:
                    asm.Add(MicroOpcode.Syscall, new AddressValue(0, "syscall!cop2_ncds"));
                    if (delaySlotMode != DelaySlotMode.AbortControl)
                        asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                case 0x0F80416:
                    asm.Add(MicroOpcode.Syscall, new AddressValue(0, "syscall!cop2_ncdt"));
                    if (delaySlotMode != DelaySlotMode.AbortControl)
                        asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                case 0x108041B:
                    asm.Add(MicroOpcode.Syscall, new AddressValue(0, "syscall!cop2_nccs"));
                    if (delaySlotMode != DelaySlotMode.AbortControl)
                        asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                case 0x118043F:
                    asm.Add(MicroOpcode.Syscall, new AddressValue(0, "syscall!cop2_ncct"));
                    if (delaySlotMode != DelaySlotMode.AbortControl)
                        asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                case 0x1280414:
                    asm.Add(MicroOpcode.Syscall, new AddressValue(0, "syscall!cop2_cdp"));
                    if (delaySlotMode != DelaySlotMode.AbortControl)
                        asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                case 0x138041C:
                    asm.Add(MicroOpcode.Syscall, new AddressValue(0, "syscall!cop2_cc"));
                    if (delaySlotMode != DelaySlotMode.AbortControl)
                        asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                case 0x1400006:
                    asm.Add(MicroOpcode.Syscall, new AddressValue(0, "syscall!cop2_nclip"));
                    if (delaySlotMode != DelaySlotMode.AbortControl)
                        asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                case 0x158002D:
                    asm.Add(MicroOpcode.Syscall, new AddressValue(0, "syscall!cop2_avsz3"));
                    if (delaySlotMode != DelaySlotMode.AbortControl)
                        asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                case 0x168002E:
                    asm.Add(MicroOpcode.Syscall, new AddressValue(0, "syscall!cop2_avsz4"));
                    if (delaySlotMode != DelaySlotMode.AbortControl)
                        asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                case 0x170000C:
                    asm.Add(MicroOpcode.Syscall, new AddressValue(0, "syscall!cop2_op"));
                    if (delaySlotMode != DelaySlotMode.AbortControl)
                        asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                case 0x190003D:
                    asm.Add(MicroOpcode.Syscall, new AddressValue(0, "syscall!cop2_gpf"));
                    if (delaySlotMode != DelaySlotMode.AbortControl)
                        asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                case 0x1A0003E:
                    asm.Add(MicroOpcode.Syscall, new AddressValue(0, "syscall!cop2_gpl"));
                    if (delaySlotMode != DelaySlotMode.AbortControl)
                        asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                case 0x0486012:
                    asm.Add(MicroOpcode.Syscall, new AddressValue(0, "syscall!cop2_rtv0"));
                    if (delaySlotMode != DelaySlotMode.AbortControl)
                        asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                case 0x048E012:
                    asm.Add(MicroOpcode.Syscall, new AddressValue(0, "syscall!cop2_rtv1"));
                    if (delaySlotMode != DelaySlotMode.AbortControl)
                        asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                case 0x0496012:
                    asm.Add(MicroOpcode.Syscall, new AddressValue(0, "syscall!cop2_rtv2"));
                    if (delaySlotMode != DelaySlotMode.AbortControl)
                        asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                case 0x049E012:
                    asm.Add(MicroOpcode.Syscall, new AddressValue(0, "syscall!cop2_rtir12"));
                    if (delaySlotMode != DelaySlotMode.AbortControl)
                        asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                case 0x041E012:
                    asm.Add(MicroOpcode.Syscall, new AddressValue(0, "syscall!cop2_rtir0"));
                    if (delaySlotMode != DelaySlotMode.AbortControl)
                        asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                case 0x0480012:
                    asm.Add(MicroOpcode.Syscall, new AddressValue(0, "syscall!cop2_rtv0tr"));
                    if (delaySlotMode != DelaySlotMode.AbortControl)
                        asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                case 0x0488012:
                    asm.Add(MicroOpcode.Syscall, new AddressValue(0, "syscall!cop2_rtv1tr"));
                    if (delaySlotMode != DelaySlotMode.AbortControl)
                        asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                case 0x0490012:
                    asm.Add(MicroOpcode.Syscall, new AddressValue(0, "syscall!cop2_rtv2tr"));
                    if (delaySlotMode != DelaySlotMode.AbortControl)
                        asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                case 0x0498012:
                    asm.Add(MicroOpcode.Syscall, new AddressValue(0, "syscall!cop2_rtirtr"));
                    if (delaySlotMode != DelaySlotMode.AbortControl)
                        asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                case 0x0482012:
                    asm.Add(MicroOpcode.Syscall, new AddressValue(0, "syscall!cop2_rtv0bk"));
                    if (delaySlotMode != DelaySlotMode.AbortControl)
                        asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                case 0x048A012:
                    asm.Add(MicroOpcode.Syscall, new AddressValue(0, "syscall!cop2_rtv1bk"));
                    if (delaySlotMode != DelaySlotMode.AbortControl)
                        asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                case 0x0492012:
                    asm.Add(MicroOpcode.Syscall, new AddressValue(0, "syscall!cop2_rtv2bk"));
                    if (delaySlotMode != DelaySlotMode.AbortControl)
                        asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                case 0x049A012:
                    asm.Add(MicroOpcode.Syscall, new AddressValue(0, "syscall!cop2_rtirkb"));
                    if (delaySlotMode != DelaySlotMode.AbortControl)
                        asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                case 0x04A6412:
                    asm.Add(MicroOpcode.Syscall, new AddressValue(0, "syscall!cop2_ll"));
                    if (delaySlotMode != DelaySlotMode.AbortControl)
                        asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                case 0x04A6012:
                    asm.Add(MicroOpcode.Syscall, new AddressValue(0, "syscall!cop2_llv0"));
                    if (delaySlotMode != DelaySlotMode.AbortControl)
                        asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                case 0x04AE012:
                    asm.Add(MicroOpcode.Syscall, new AddressValue(0, "syscall!cop2_llv1"));
                    if (delaySlotMode != DelaySlotMode.AbortControl)
                        asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                case 0x04B6012:
                    asm.Add(MicroOpcode.Syscall, new AddressValue(0, "syscall!cop2_llv2"));
                    if (delaySlotMode != DelaySlotMode.AbortControl)
                        asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                case 0x04BE012:
                    asm.Add(MicroOpcode.Syscall, new AddressValue(0, "syscall!cop2_llvir"));
                    if (delaySlotMode != DelaySlotMode.AbortControl)
                        asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                case 0x04A0012:
                    asm.Add(MicroOpcode.Syscall, new AddressValue(0, "syscall!cop2_llv0tr"));
                    if (delaySlotMode != DelaySlotMode.AbortControl)
                        asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                case 0x04A8012:
                    asm.Add(MicroOpcode.Syscall, new AddressValue(0, "syscall!cop2_llv1tr"));
                    if (delaySlotMode != DelaySlotMode.AbortControl)
                        asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                case 0x04B0012:
                    asm.Add(MicroOpcode.Syscall, new AddressValue(0, "syscall!cop2_llv2tr"));
                    if (delaySlotMode != DelaySlotMode.AbortControl)
                        asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                case 0x04B8012:
                    asm.Add(MicroOpcode.Syscall, new AddressValue(0, "syscall!cop2_llirtr"));
                    if (delaySlotMode != DelaySlotMode.AbortControl)
                        asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                case 0x04A2012:
                    asm.Add(MicroOpcode.Syscall, new AddressValue(0, "syscall!cop2_llv0bk"));
                    if (delaySlotMode != DelaySlotMode.AbortControl)
                        asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                case 0x04AA012:
                    asm.Add(MicroOpcode.Syscall, new AddressValue(0, "syscall!cop2_llv1bk"));
                    if (delaySlotMode != DelaySlotMode.AbortControl)
                        asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                case 0x04B2012:
                    asm.Add(MicroOpcode.Syscall, new AddressValue(0, "syscall!cop2_llv2bk"));
                    if (delaySlotMode != DelaySlotMode.AbortControl)
                        asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                case 0x04BA012:
                    asm.Add(MicroOpcode.Syscall, new AddressValue(0, "syscall!cop2_llirbk"));
                    if (delaySlotMode != DelaySlotMode.AbortControl)
                        asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                case 0x04DA412:
                    asm.Add(MicroOpcode.Syscall, new AddressValue(0, "syscall!cop2_lc"));
                    if (delaySlotMode != DelaySlotMode.AbortControl)
                        asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                case 0x04C6012:
                    asm.Add(MicroOpcode.Syscall, new AddressValue(0, "syscall!cop2_lcv0"));
                    if (delaySlotMode != DelaySlotMode.AbortControl)
                        asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                case 0x04CE012:
                    asm.Add(MicroOpcode.Syscall, new AddressValue(0, "syscall!cop2_lcv1"));
                    if (delaySlotMode != DelaySlotMode.AbortControl)
                        asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                case 0x04D6012:
                    asm.Add(MicroOpcode.Syscall, new AddressValue(0, "syscall!cop2_lcv2"));
                    if (delaySlotMode != DelaySlotMode.AbortControl)
                        asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                case 0x04DE012:
                    asm.Add(MicroOpcode.Syscall, new AddressValue(0, "syscall!cop2_lcvir"));
                    if (delaySlotMode != DelaySlotMode.AbortControl)
                        asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                case 0x04C0012:
                    asm.Add(MicroOpcode.Syscall, new AddressValue(0, "syscall!cop2_lcv0tr"));
                    if (delaySlotMode != DelaySlotMode.AbortControl)
                        asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                case 0x04C8012:
                    asm.Add(MicroOpcode.Syscall, new AddressValue(0, "syscall!cop2_lcv1tr"));
                    if (delaySlotMode != DelaySlotMode.AbortControl)
                        asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                case 0x04D0012:
                    asm.Add(MicroOpcode.Syscall, new AddressValue(0, "syscall!cop2_lcv2tr"));
                    if (delaySlotMode != DelaySlotMode.AbortControl)
                        asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                case 0x04D8012:
                    asm.Add(MicroOpcode.Syscall, new AddressValue(0, "syscall!cop2_lcirtr"));
                    if (delaySlotMode != DelaySlotMode.AbortControl)
                        asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                case 0x04C2012:
                    asm.Add(MicroOpcode.Syscall, new AddressValue(0, "syscall!cop2_lev0bk"));
                    if (delaySlotMode != DelaySlotMode.AbortControl)
                        asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                case 0x04CA012:
                    asm.Add(MicroOpcode.Syscall, new AddressValue(0, "syscall!cop2_lev1bk"));
                    if (delaySlotMode != DelaySlotMode.AbortControl)
                        asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                case 0x04D2012:
                    asm.Add(MicroOpcode.Syscall, new AddressValue(0, "syscall!cop2_lev2bk"));
                    if (delaySlotMode != DelaySlotMode.AbortControl)
                        asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                case 0x04DA012:
                    asm.Add(MicroOpcode.Syscall, new AddressValue(0, "syscall!cop2_leirbk"));
                    if (delaySlotMode != DelaySlotMode.AbortControl)
                        asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                case 0x178000C:
                    asm.Add(MicroOpcode.Syscall, new AddressValue(0, "syscall!cop2_op_12"));
                    if (delaySlotMode != DelaySlotMode.AbortControl)
                        asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                case 0x198003D:
                    asm.Add(MicroOpcode.Syscall, new AddressValue(0, "syscall!cop2_gpf_12"));
                    if (delaySlotMode != DelaySlotMode.AbortControl)
                        asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                case 0x1A8003E:
                    asm.Add(MicroOpcode.Syscall, new AddressValue(0, "syscall!cop2_gpl_12"));
                    if (delaySlotMode != DelaySlotMode.AbortControl)
                        asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;

                default:
                    asm.Add(MicroOpcode.Syscall, new AddressValue(0, $"syscall!cop2_0x{coFun:X}"));
                    if (delaySlotMode != DelaySlotMode.AbortControl)
                        asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
            }
        }

        private void DisassembleInsn(uint localAddress, [NotNull] Queue<uint> analysisQueue,
            [NotNull] IDictionary<ulong, uint> calls, Queue<ulong> callQueue)
        {
            if (localAddress >= _textSection.Size)
                return;

            if (!_textSection.Instructions.TryGetValue(localAddress, out var asm))
            {
                asm = new MicroAssemblyBlock(localAddress);
                _textSection.Instructions[localAddress] = asm;
                DecodeInstruction(asm, _textSection.WordAtLocal(localAddress), localAddress + 4, DelaySlotMode.None);
            }

            foreach (var (addr, type) in asm.Outs)
            {
                if (addr >= _textSection.Size)
                    continue;

                if (_textSection.Instructions.ContainsKey(addr))
                    continue;

                if (type != JumpType.Call)
                    analysisQueue.Enqueue(addr);
                else
                    callQueue.Enqueue(_textSection.MakeGlobal(addr));
            }

            foreach (var insn in asm.Insns)
            {
                if (insn.Opcode != MicroOpcode.Call)
                    continue;

                if (!(insn.Args[0] is RegisterArg retReg))
                    continue;

                if (!(insn.Args[1] is AddressValue callTarget))
                    continue;

                if (calls.TryGetValue(callTarget.Address, out var existing) && existing != retReg.Register)
                {
                    logger.Error(
                        $"Mismatching return address registers for call to {callTarget} ($r{existing} != $r{retReg.Register})");
                    continue;
                }

                calls[callTarget.Address] = retReg.Register;
            }
        }

        internal RegisterArg GetTmpReg(byte bits)
        {
            return new RegisterArg(_tmpRegId++, bits);
        }

        private enum DelaySlotMode
        {
            None,
            ContinueControl,
            AbortControl
        }
    }
}