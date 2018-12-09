using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using core;
using core.microcode;
using core.util;
using mips.disasm;
using NLog;

namespace exefile
{
    public class ExeFile
    {
        private static readonly ILogger logger = LogManager.GetCurrentClassLogger();

        private readonly byte[] _data;
        private readonly uint? _gpBase;

        private readonly Header _header;

        private readonly IDebugSource _debugSource;

        private readonly IDictionary<uint, MicroAssemblyBlock> _decoded =
            new SortedDictionary<uint, MicroAssemblyBlock>();

        public ISet<uint> Callees = new SortedSet<uint>();

        public IEnumerable<KeyValuePair<uint, MicroAssemblyBlock>> RelocatedInstructions =>
            _decoded.Select(kv => new KeyValuePair<uint, MicroAssemblyBlock>(MakeGlobal(kv.Key), kv.Value));

        public ExeFile(EndianBinaryReader reader, IDebugSource debugSource)
        {
            _debugSource = debugSource;
            reader.BaseStream.Seek(0, SeekOrigin.Begin);

            _header = new Header(reader);
            reader.BaseStream.Seek(0x800, SeekOrigin.Begin);
            _data = reader.ReadBytes((int) (reader.BaseStream.Length - reader.BaseStream.Position));

            _gpBase = _debugSource.Labels
                .Where(byOffset => byOffset.Value.Any(lbl => lbl.Name.Equals("__SN_GP_BASE")))
                .Select(lbl => lbl.Key)
                .Cast<uint?>()
                .FirstOrDefault();
        }

        private uint MakeGlobal(uint addr)
        {
            return addr + _header.tAddr;
        }

        private uint MakeLocal(uint addr)
        {
            if (addr < _header.tAddr /*TODO || addr >= _header.tAddr + _header.tSize*/)
                throw new ArgumentOutOfRangeException(nameof(addr), "Address out of range to make local");

            return addr - _header.tAddr;
        }

        private uint WordAtLocal(uint address)
        {
            uint data;
            data = _data[address++];
            data |= (uint) _data[address++] << 8;
            data |= (uint) _data[address++] << 16;
            // ReSharper disable once RedundantAssignment
            data |= (uint) _data[address++] << 24;
            return data;
        }

        private static Opcode ExtractOpcode(uint data)
        {
            return (Opcode) (data >> 26);
        }

        private uint _tmpRegId = 1000;

        private uint GetTmpRegId()
        {
            return _tmpRegId++;
        }

        private RegisterArg GetTmpReg(byte bits)
        {
            return new RegisterArg(GetTmpRegId(), bits);
        }


        public void Disassemble()
        {
            _tmpRegId = 1000;
            
            logger.Info("Disassembly started");

            Queue<uint> analysisQueue = new Queue<uint>();
            analysisQueue.Enqueue(MakeLocal(_header.pc0));
            foreach (var addr in _debugSource.Functions.Select(f => MakeLocal(f.GlobalAddress)))
                analysisQueue.Enqueue(addr);

            while (analysisQueue.Count != 0)
            {
                var localAddress = analysisQueue.Dequeue();
                if (localAddress >= _header.tSize)
                    continue;

                if (!_decoded.TryGetValue(localAddress, out var asm))
                {
                    asm = new MicroAssemblyBlock(localAddress);
                    _decoded[localAddress] = asm;
                    DecodeInstruction(asm, WordAtLocal(localAddress), localAddress + 4, false);
                }

                foreach (var addr in asm.Outs)
                {
                    if (addr.Key >= _header.tSize)
                    {
                        //logger.Warn($"Outgoing address 0x{addr.Key:x8} out of bounds");
                        continue;
                    }

                    if (!_decoded.ContainsKey(addr.Key))
                    {
                        analysisQueue.Enqueue(addr.Key);
                    }
                }
            }

            logger.Info("Reversing control flow");
            foreach (var asm in _decoded)
            {
                foreach (var @out in asm.Value.Outs)
                {
                    var addr = @out.Key;
                    if (!_decoded.TryGetValue(addr, out var target))
                    {
                        logger.Warn($"Target address 0x${addr:x8} not in local address space");
                        continue;
                    }

                    target.Ins.Add(asm.Key, @out.Value);
                }
            }

            logger.Info("Collapsing basic assembly blocks");
            var tmp = new SortedDictionary<uint, MicroAssemblyBlock>(_decoded);
            _decoded.Clear();
            MicroAssemblyBlock basicBlock = null;
            foreach (var addrAsm in tmp)
            {
                if (basicBlock == null)
                {
                    basicBlock = addrAsm.Value;
                    Debug.Assert(basicBlock.Address == addrAsm.Key);
                    _decoded.Add(basicBlock.Address, basicBlock);
                    continue;
                }

                if (addrAsm.Value.Ins.Values.Any(x => x != JumpType.Control))
                {
                    // start a new basic block if we have an incoming edge that is no pure control flow
                    basicBlock = addrAsm.Value;
                    Debug.Assert(basicBlock.Address == addrAsm.Key);
                    _decoded.Add(basicBlock.Address, basicBlock);
                    continue;
                }

                // replace the current's outgoing edges, and append the assembly
                basicBlock.Outs = addrAsm.Value.Outs;
                foreach (var insn in addrAsm.Value.Insns)
                    basicBlock.Insns.Add(insn);
                
                if (basicBlock.Outs.Count == 0 || basicBlock.Outs.Values.Any(x => x != JumpType.Control))
                {
                    // stop the current block if we have no pure outgoing control flow
                    basicBlock = null;
                }
            }

            logger.Info("Peephole optimization");
            foreach (var asm in _decoded.Values)
            {
                asm.Optimize(_debugSource);
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

        private static RegisterMemArg MakeRegisterOffsetArg(uint data, int shift, int offset, byte bits)
        {
            return new RegisterMemArg(((Register) ((data >> shift) & 0x1f)).ToUInt(), offset, bits);
        }

        private IMicroArg MakeGpBasedArg(uint data, int shift, int offset, byte bits)
        {
            var regofs = MakeRegisterOffsetArg(data, shift, offset, bits);
            if (_gpBase == null)
                return regofs;

            if (regofs.Register == Register.gp.ToUInt())
            {
                var absoluteAddress = (uint) (_gpBase.Value + regofs.Offset);
                return new AddressValue(absoluteAddress, _debugSource.GetSymbolName(absoluteAddress), bits);
            }

            return regofs;
        }

        private static uint RelAddr(uint @base, short offset)
        {
            return (uint) (@base + offset * 4);
        }

        private void DecodeInstruction(MicroAssemblyBlock asm, uint data, uint nextInsnAddressLocal, bool inDelaySlot)
        {
            switch (ExtractOpcode(data))
            {
                case Opcode.RegisterFormat:
                    DecodeRegisterFormat(asm, nextInsnAddressLocal, data);
                    break;
                case Opcode.PCRelative:
                    if (inDelaySlot)
                    {
                        logger.Warn($"Recursive delay slot disassembly at 0x{nextInsnAddressLocal - 4:x8}");
                        Console.WriteLine(asm);
                        break;
                    }

                    DecodePcRelative(asm, nextInsnAddressLocal, data);
                    break;
                case Opcode.j:
                {
                    if (inDelaySlot)
                    {
                        logger.Warn($"j: Recursive delay slot disassembly at 0x{nextInsnAddressLocal - 4:x8}");
                        Console.WriteLine(asm);
                        break;
                    }

                    var absoluteAddress = (data & 0x03FFFFFF) * 4;
                    var tgt = new AddressValue(absoluteAddress, _debugSource.GetSymbolName(absoluteAddress), 0);
                    if (MakeLocal(absoluteAddress) != nextInsnAddressLocal + 4)
                        asm.Outs.Add(MakeLocal(absoluteAddress), JumpType.Jump);
                    DecodeInstruction(asm, WordAtLocal(nextInsnAddressLocal), nextInsnAddressLocal + 4, true);
                    asm.Add(MicroOpcode.Jmp, tgt);
                }
                    break;
                case Opcode.jal:
                {
                    if (inDelaySlot)
                    {
                        logger.Warn($"jal: Recursive delay slot disassembly at 0x{nextInsnAddressLocal - 4:x8}");
                        Console.WriteLine(asm);
                        break;
                    }

                    var absoluteAddress = (data & 0x03FFFFFF) * 4;
                    var tgt = new AddressValue(absoluteAddress, _debugSource.GetSymbolName(absoluteAddress), 0);
                    asm.Outs.Add(MakeLocal(absoluteAddress), JumpType.Call);
                    Callees.Add(absoluteAddress);
                    DecodeInstruction(asm, WordAtLocal(nextInsnAddressLocal), nextInsnAddressLocal + 4, true);
                    asm.Add(MicroOpcode.Call, new RegisterArg(Register.ra.ToUInt(), 32), tgt);
                }
                    break;
                case Opcode.beq:
                {
                    if (inDelaySlot)
                    {
                        logger.Warn($"beq: Recursive delay slot disassembly at 0x{nextInsnAddressLocal - 4:x8}");
                        Console.WriteLine(asm);
                        break;
                    }

                    var localAddress = RelAddr(nextInsnAddressLocal, (short) data);
                    var tgt = new AddressValue(MakeGlobal(localAddress),
                        _debugSource.GetSymbolName(MakeGlobal(localAddress)), 0);
                    asm.Outs.Add(localAddress, JumpType.JumpConditional);
                    var r1 = MakeZeroRegisterOperand(data, 21);
                    var r2 = MakeZeroRegisterOperand(data, 16);
                    var tmp = GetTmpReg(1);
                    asm.Add(MicroOpcode.Cmp, r1, r2);
                    asm.Add(MicroOpcode.SetEq, tmp);
                    DecodeInstruction(asm, WordAtLocal(nextInsnAddressLocal), nextInsnAddressLocal + 4, true);
                    asm.Add(MicroOpcode.JmpIf, tmp, tgt);
                }
                    break;
                case Opcode.bne:
                {
                    if (inDelaySlot)
                    {
                        logger.Warn($"bne: Recursive delay slot disassembly at 0x{nextInsnAddressLocal - 4:x8}");
                        Console.WriteLine(asm);
                        break;
                    }

                    var localAddress = RelAddr(nextInsnAddressLocal, (short) data);
                    var tgt = new AddressValue(MakeGlobal(localAddress),
                        _debugSource.GetSymbolName(MakeGlobal(localAddress)), 0);
                    asm.Outs.Add(localAddress, JumpType.JumpConditional);
                    var r1 = MakeZeroRegisterOperand(data, 21);
                    var r2 = MakeZeroRegisterOperand(data, 16);
                    var tmp = GetTmpReg(1);
                    asm.Add(MicroOpcode.Cmp, r1, r2);
                    asm.Add(MicroOpcode.SetEq, tmp);
                    asm.Add(MicroOpcode.LogicalNot, tmp);
                    DecodeInstruction(asm, WordAtLocal(nextInsnAddressLocal), nextInsnAddressLocal + 4, true);
                    asm.Add(MicroOpcode.JmpIf, tmp, tgt);
                }
                    break;
                case Opcode.blez:
                {
                    if (inDelaySlot)
                    {
                        logger.Warn($"blez: Recursive delay slot disassembly at 0x{nextInsnAddressLocal - 4:x8}");
                        Console.WriteLine(asm);
                        break;
                    }

                    var localAddress = RelAddr(nextInsnAddressLocal, (short) data);
                    var tgt = new AddressValue(MakeGlobal(localAddress),
                        _debugSource.GetSymbolName(MakeGlobal(localAddress)), 0);
                    asm.Outs.Add(localAddress, JumpType.JumpConditional);
                    var r1 = MakeZeroRegisterOperand(data, 21);
                    var tmp = GetTmpReg(1);
                    asm.Add(MicroOpcode.Cmp, r1, new ConstValue(0, 32));
                    asm.Add(MicroOpcode.SSetLE, tmp);
                    DecodeInstruction(asm, WordAtLocal(nextInsnAddressLocal), nextInsnAddressLocal + 4, true);
                    asm.Add(MicroOpcode.JmpIf, tmp, tgt);
                }
                    break;
                case Opcode.bgtz:
                {
                    if (inDelaySlot)
                    {
                        logger.Warn($"bgtz: Recursive delay slot disassembly at 0x{nextInsnAddressLocal - 4:x8}");
                        Console.WriteLine(asm);
                        break;
                    }

                    var localAddress = RelAddr(nextInsnAddressLocal, (short) data);
                    var tgt = new AddressValue(MakeGlobal(localAddress),
                        _debugSource.GetSymbolName(MakeGlobal(localAddress)), 0);
                    asm.Outs.Add(localAddress, JumpType.JumpConditional);
                    var r1 = MakeZeroRegisterOperand(data, 21);
                    var tmp = GetTmpReg(1);
                    asm.Add(MicroOpcode.Cmp, r1, new ConstValue(0, 32));
                    asm.Add(MicroOpcode.SSetLE, tmp);
                    asm.Add(MicroOpcode.LogicalNot, tmp);
                    DecodeInstruction(asm, WordAtLocal(nextInsnAddressLocal), nextInsnAddressLocal + 4, true);
                    asm.Add(MicroOpcode.JmpIf, tmp, tgt);
                }
                    break;
                case Opcode.addi:
                    asm.Add(MicroOpcode.Add, MakeZeroRegisterOperand(data, 16), MakeZeroRegisterOperand(data, 21),
                        new ConstValue((ushort) data, 16));
                    asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                case Opcode.addiu:
                    asm.Add(MicroOpcode.Add, MakeZeroRegisterOperand(data, 16), MakeZeroRegisterOperand(data, 21),
                        new ConstValue((ushort) data, 16));
                    asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                case Opcode.subi:
                    asm.Add(MicroOpcode.Sub, MakeZeroRegisterOperand(data, 16), MakeZeroRegisterOperand(data, 21),
                        new ConstValue((ushort) data, 16));
                    asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                case Opcode.subiu:
                    asm.Add(MicroOpcode.Sub, MakeZeroRegisterOperand(data, 16), MakeZeroRegisterOperand(data, 21),
                        new ConstValue((ushort) data, 16));
                    asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                case Opcode.andi:
                    asm.Add(MicroOpcode.And, MakeZeroRegisterOperand(data, 16), MakeZeroRegisterOperand(data, 21),
                        new ConstValue((ushort) data, 16));
                    asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                case Opcode.ori:
                    asm.Add(MicroOpcode.Or, MakeZeroRegisterOperand(data, 16), MakeZeroRegisterOperand(data, 21),
                        new ConstValue((ushort) data, 16));
                    asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                case Opcode.xori:
                    asm.Add(MicroOpcode.XOr, MakeZeroRegisterOperand(data, 16), MakeZeroRegisterOperand(data, 21),
                        new ConstValue((ushort) data, 16));
                    asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                case Opcode.lui:
                    asm.Add(new CopyInsn(MakeZeroRegisterOperand(data, 16),
                        new ConstValue((ulong) ((ushort) data << 16), 32)));
                    asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                case Opcode.CpuControl:
                    DecodeCpuControl(asm, nextInsnAddressLocal, data);
                    break;
                case Opcode.FloatingPoint:
                    asm.Add(MicroOpcode.Data, new ConstValue(data, 32));
                    break;
                case Opcode.lb:
                {
                    var tmp = GetTmpReg(8);
                    asm.Add(new CopyInsn(tmp, MakeGpBasedArg(data, 21, (short) data, 8)));
                    asm.Add(tmp.SCastTo(MakeRegisterOperand(data, 8)));
                    asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                }
                    break;
                case Opcode.lh:
                {
                    var tmp = GetTmpReg(16);
                    asm.Add(new CopyInsn(tmp, MakeGpBasedArg(data, 21, (short) data, 16)));
                    asm.Add(tmp.SCastTo(MakeRegisterOperand(data, 16)));
                    asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                }
                    break;
                case Opcode.lwl:
                    asm.Add(new UnsupportedInsn("lwl", MakeZeroRegisterOperand(data, 32),
                        MakeGpBasedArg(data, 21, (short) data, 32)));
                    asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                case Opcode.lw:
                    asm.Add(new CopyInsn(MakeZeroRegisterOperand(data, 16), MakeGpBasedArg(data, 21, (short) data, 32)));
                    asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                case Opcode.lbu:
                {
                    var tmp = GetTmpReg(8);
                    asm.Add(new CopyInsn(tmp, MakeGpBasedArg(data, 21, (short) data, 8)));
                    asm.Add(tmp.UCastTo(MakeRegisterOperand(data, 16)));
                    asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                }
                    break;
                case Opcode.lhu:
                {
                    var tmp = GetTmpReg(16);
                    asm.Add(new CopyInsn(tmp, MakeGpBasedArg(data, 21, (short) data, 16)));
                    asm.Add(tmp.UCastTo(MakeRegisterOperand(data, 16)));
                    asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                }
                    break;
                case Opcode.lwr:
                    asm.Add(new UnsupportedInsn("lwr", MakeZeroRegisterOperand(data, 16),
                        MakeGpBasedArg(data, 21, (short) data, 32)));
                    asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                case Opcode.sb:
                {
                    var op = MakeZeroRegisterOperand(data, 16);
                    if (op is RegisterArg r)
                    {
                        var tmp = GetTmpReg(8);
                        asm.Add(r.UCastTo(tmp));
                        asm.Add(new CopyInsn(MakeGpBasedArg(data, 21, (short) data, 8), tmp));
                    }
                    else
                    {
                        asm.Add(new CopyInsn(MakeGpBasedArg(data, 21, (short) data, 8), new ConstValue(0, 8)));
                    }

                    asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                }
                    break;
                case Opcode.sh:
                {
                    var op = MakeZeroRegisterOperand(data, 16);
                    if (op is RegisterArg r)
                    {
                        var tmp = GetTmpReg(16);
                        asm.Add(r.UCastTo(tmp));
                        asm.Add(new CopyInsn(MakeGpBasedArg(data, 21, (short) data, 16), tmp));
                    }
                    else
                    {
                        asm.Add(new CopyInsn(MakeGpBasedArg(data, 21, (short) data, 16), new ConstValue(0, 16)));
                    }
                    
                    asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                }
                    break;
                case Opcode.swl:
                    asm.Add(new UnsupportedInsn("swl", MakeZeroRegisterOperand(data, 16),
                        MakeGpBasedArg(data, 21, (short) data, 32)));
                    asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                case Opcode.sw:
                    asm.Add(new CopyInsn(MakeGpBasedArg(data, 21, (short) data, 32), MakeZeroRegisterOperand(data, 16)));
                    asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                case Opcode.swr:
                    asm.Add(new UnsupportedInsn("swr", MakeZeroRegisterOperand(data, 16),
                        MakeGpBasedArg(data, 21, (short) data, 32)));
                    asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                case Opcode.swc1:
                    asm.Add(new UnsupportedInsn("swc1", MakeZeroRegisterOperand(data, 16),
                        new ConstValue((ushort) data, 16), MakeZeroRegisterOperand(data, 21)));
                    asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                case Opcode.lwc1:
                    asm.Add(new UnsupportedInsn("lwc1", MakeC2RegisterOperand(data, 16),
                        new ConstValue((ushort) data, 16), MakeZeroRegisterOperand(data, 21)));
                    asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                case Opcode.cop0:
                    asm.Add(new UnsupportedInsn("cop0", new ConstValue(data & ((1 << 26) - 1), 26)));
                    asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                case Opcode.cop1:
                    asm.Add(new UnsupportedInsn("cop1", new ConstValue(data & ((1 << 26) - 1), 26)));
                    asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                case Opcode.cop2:
                    DecodeCop2(asm, nextInsnAddressLocal, data);
                    break;
                case Opcode.cop3:
                    asm.Add(new UnsupportedInsn("cop3", new ConstValue(data & ((1 << 26) - 1), 26)));
                    asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                case Opcode.beql:
                {
                    if (inDelaySlot)
                    {
                        logger.Warn($"beql: Recursive delay slot disassembly at 0x{nextInsnAddressLocal - 4:x8}");
                        Console.WriteLine(asm);
                        break;
                    }

                    var localAddress = RelAddr(nextInsnAddressLocal, (short) data);
                    var tgt = new AddressValue(MakeGlobal(localAddress),
                        _debugSource.GetSymbolName(MakeGlobal(localAddress)), 0);
                    asm.Outs.Add(localAddress, JumpType.JumpConditional);
                    asm.Add(MicroOpcode.Cmp, MakeZeroRegisterOperand(data, 21), MakeZeroRegisterOperand(data, 16));
                    var tmp = GetTmpReg(1);
                    asm.Add(MicroOpcode.SetEq, tmp);
                    DecodeInstruction(asm, WordAtLocal(nextInsnAddressLocal), nextInsnAddressLocal + 4, true);
                    asm.Add(MicroOpcode.JmpIf, tmp, tgt);
                }
                    break;
                case Opcode.bnel:
                {
                    if (inDelaySlot)
                    {
                        logger.Warn($"bnel: Recursive delay slot disassembly at 0x{nextInsnAddressLocal - 4:x8}");
                        Console.WriteLine(asm);
                        break;
                    }

                    var localAddress = RelAddr(nextInsnAddressLocal, (short) data);
                    var tgt = new AddressValue(MakeGlobal(localAddress),
                        _debugSource.GetSymbolName(MakeGlobal(localAddress)), 0);
                    asm.Outs.Add(localAddress, JumpType.JumpConditional);
                    asm.Add(MicroOpcode.Cmp, MakeZeroRegisterOperand(data, 21), MakeZeroRegisterOperand(data, 16));
                    var tmp = GetTmpReg(1);
                    asm.Add(MicroOpcode.SetEq, tmp);
                    asm.Add(MicroOpcode.LogicalNot, tmp);
                    DecodeInstruction(asm, WordAtLocal(nextInsnAddressLocal), nextInsnAddressLocal + 4, true);
                    asm.Add(MicroOpcode.JmpIf, tmp, tgt);
                }
                    break;
                case Opcode.blezl:
                {
                    if (inDelaySlot)
                    {
                        logger.Warn($"blezl: Recursive delay slot disassembly at 0x{nextInsnAddressLocal - 4:x8}");
                        Console.WriteLine(asm);
                        break;
                    }

                    var localAddress = RelAddr(nextInsnAddressLocal, (short) data);
                    var tgt = new AddressValue(MakeGlobal(localAddress),
                        _debugSource.GetSymbolName(MakeGlobal(localAddress)), 0);
                    asm.Outs.Add(localAddress, JumpType.JumpConditional);
                    asm.Add(MicroOpcode.Cmp, MakeZeroRegisterOperand(data, 21), new ConstValue(0, 32));
                    var tmp = GetTmpReg(1);
                    asm.Add(MicroOpcode.SSetLE, tmp);
                    DecodeInstruction(asm, WordAtLocal(nextInsnAddressLocal), nextInsnAddressLocal + 4, true);
                    asm.Add(MicroOpcode.JmpIf, tmp, tgt);
                }
                    break;
                case Opcode.bgtzl:
                {
                    if (inDelaySlot)
                    {
                        logger.Warn($"bgtzl: Recursive delay slot disassembly at 0x{nextInsnAddressLocal - 4:x8}");
                        Console.WriteLine(asm);
                        break;
                    }

                    var localAddress = RelAddr(nextInsnAddressLocal, (short) data);
                    var tgt = new AddressValue(MakeGlobal(localAddress),
                        _debugSource.GetSymbolName(MakeGlobal(localAddress)), 0);
                    asm.Outs.Add(localAddress, JumpType.JumpConditional);
                    asm.Add(MicroOpcode.Cmp, MakeZeroRegisterOperand(data, 21), new ConstValue(0, 32));
                    var tmp = GetTmpReg(1);
                    asm.Add(MicroOpcode.SSetLE, tmp);
                    asm.Add(MicroOpcode.LogicalNot, tmp);
                    DecodeInstruction(asm, WordAtLocal(nextInsnAddressLocal), nextInsnAddressLocal + 4, true);
                    asm.Add(MicroOpcode.JmpIf, tmp, tgt);
                }
                    break;
                default:
                    asm.Add(MicroOpcode.Data, new ConstValue(data, 32));
                    break;
            }
        }

        private void DecodeRegisterFormat(MicroAssemblyBlock asm, uint nextInsnAddressLocal, uint data)
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
                        asm.Add(MicroOpcode.SHL, rd, rs2, new ConstValue(data >> 6 & 0x1F, 5));
                    asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                case OpcodeFunction.srl:
                    if (data == 0)
                        asm.Add(MicroOpcode.Nop);
                    else
                        asm.Add(MicroOpcode.SRL, rd, rs2, new ConstValue(data >> 6 & 0x1F, 5));
                    asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                case OpcodeFunction.sra:
                    if (data == 0)
                        asm.Add(MicroOpcode.Nop);
                    else
                        asm.Add(MicroOpcode.SRA, rd, rs2, new ConstValue(data >> 6 & 0x1F, 5));
                    asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                case OpcodeFunction.sllv:
                    asm.Add(MicroOpcode.SHL, rd, rs2, rs1);
                    asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                case OpcodeFunction.srlv:
                    asm.Add(MicroOpcode.SRL, rd, rs2, rs1);
                    asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                case OpcodeFunction.srav:
                    asm.Add(MicroOpcode.SRA, rd, rs2, rs1);
                    asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                case OpcodeFunction.jr:
                    DecodeInstruction(asm, WordAtLocal(nextInsnAddressLocal), nextInsnAddressLocal + 4, true);
                    if (rs1 is RegisterArg r && r.Register == Register.ra.ToUInt())
                        asm.Add(MicroOpcode.Return, rs1);
                    else
                        asm.Add(MicroOpcode.Jmp, rs1);
                    break;
                case OpcodeFunction.jalr:
                    DecodeInstruction(asm, WordAtLocal(nextInsnAddressLocal), nextInsnAddressLocal + 4, true);
                    asm.Add(MicroOpcode.Call, rd, rs1);
                    break;
                case OpcodeFunction.syscall:
                    asm.Add(new UnsupportedInsn("syscall", new ConstValue(data >> 6 & 0xFFFFF, 20)));
                    break;
                case OpcodeFunction.break_:
                    asm.Add(new UnsupportedInsn("break", new ConstValue(data >> 6 & 0xFFFFF, 20)));
                    break;
                case OpcodeFunction.mfhi:
                    asm.Add(new UnsupportedInsn("mfhi", rd));
                    asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                case OpcodeFunction.mthi:
                    asm.Add(new UnsupportedInsn("mthi", rd));
                    asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                case OpcodeFunction.mflo:
                    asm.Add(new UnsupportedInsn("mflo", rd));
                    asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                case OpcodeFunction.mtlo:
                    asm.Add(new UnsupportedInsn("mtlo", rd));
                    asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                case OpcodeFunction.mult:
                    asm.Add(new UnsupportedInsn("mult", rs1, rs2));
                    asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                case OpcodeFunction.multu:
                    asm.Add(new UnsupportedInsn("multu", rs1, rs2));
                    asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                case OpcodeFunction.div:
                    asm.Add(new UnsupportedInsn("div", rs1, rs2));
                    asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                case OpcodeFunction.divu:
                    asm.Add(new UnsupportedInsn("divu", rs1, rs2));
                    asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                case OpcodeFunction.add:
                    asm.Add(MicroOpcode.Add, rd, rs1, rs2);
                    asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    return;
                case OpcodeFunction.addu:
                    asm.Add(MicroOpcode.Add, rd, rs1, rs2);
                    asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    return;
                case OpcodeFunction.sub:
                    asm.Add(MicroOpcode.Sub, rd, rs1, rs2);
                    asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    return;
                case OpcodeFunction.subu:
                    asm.Add(MicroOpcode.Sub, rd, rs1, rs2);
                    asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    return;
                case OpcodeFunction.and:
                    asm.Add(MicroOpcode.And, rd, rs1, rs2);
                    asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    return;
                case OpcodeFunction.or:
                    asm.Add(MicroOpcode.Or, rd, rs1, rs2);
                    asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    return;
                case OpcodeFunction.xor:
                    asm.Add(MicroOpcode.XOr, rd, rs1, rs2);
                    asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    return;
                case OpcodeFunction.nor:
                {
                    var tmp = GetTmpReg(32);
                    asm.Add(MicroOpcode.Or, tmp, rs1, rs2);
                    asm.Add(MicroOpcode.Not, tmp);
                    asm.Add(MicroOpcode.Copy, rd, tmp);
                    asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                }
                    break;
                case OpcodeFunction.slt:
                    asm.Add(MicroOpcode.Cmp, rs1, rs2);
                    asm.Add(MicroOpcode.SSetL, rd);
                    asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                case OpcodeFunction.sltu:
                    asm.Add(MicroOpcode.Cmp, rs1, rs2);
                    asm.Add(MicroOpcode.USetL, rd);
                    asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                default:
                    asm.Add(MicroOpcode.Data, new ConstValue(data, 32));
                    break;
            }
        }

        private void DecodeCpuControl(MicroAssemblyBlock asm, uint nextInsnAddressLocal, uint data)
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
                                new AddressValue(MakeGlobal(localAddress),
                                    _debugSource.GetSymbolName(MakeGlobal(localAddress)), 0)));

                            asm.Outs.Add(localAddress, JumpType.JumpConditional);
                            DecodeInstruction(asm, WordAtLocal(nextInsnAddressLocal), nextInsnAddressLocal + 4,
                                true);
                        }
                            break;
                        case 1:
                        {
                            var localAddress = RelAddr(nextInsnAddressLocal, (short) data);
                            asm.Add(new UnsupportedInsn("bc0t",
                                new AddressValue(MakeGlobal(localAddress),
                                    _debugSource.GetSymbolName(MakeGlobal(localAddress)), 0)));

                            asm.Outs.Add(localAddress, JumpType.JumpConditional);
                            DecodeInstruction(asm, WordAtLocal(nextInsnAddressLocal), nextInsnAddressLocal + 4,
                                true);
                        }
                            break;
                        default:
                            asm.Add(MicroOpcode.Data, new ConstValue(data, 32));
                            break;
                    }

                    break;
                case CpuControlOpcode.tlb:
                    DecodeTlb(asm, nextInsnAddressLocal, data);
                    break;
                case CpuControlOpcode.mfc0:
                    asm.Add(new UnsupportedInsn("mfc0", MakeZeroRegisterOperand(data, 16),
                        MakeC0RegisterOperand(data, 11)));
                    asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                default:
                    asm.Add(MicroOpcode.Data, new ConstValue(data, 32));
                    break;
            }
        }

        private static void DecodeTlb(MicroAssemblyBlock asm, uint nextInsnAddressLocal, uint data)
        {
            switch ((TlbOpcode) (data & 0x1f))
            {
                case TlbOpcode.tlbr:
                    asm.Add(new UnsupportedInsn("tlbr"));
                    asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                case TlbOpcode.tlbwi:
                    asm.Add(new UnsupportedInsn("tlbwi"));
                    asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                case TlbOpcode.tlbwr:
                    asm.Add(new UnsupportedInsn("tlbwr"));
                    asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                case TlbOpcode.tlbp:
                    asm.Add(new UnsupportedInsn("tlbp"));
                    asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                case TlbOpcode.rfe:
                    asm.Add(new UnsupportedInsn("rfe"));
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
            var offset = new AddressValue(MakeGlobal(localAddress),
                _debugSource.GetSymbolName(MakeGlobal(localAddress)), 0);
            switch ((data >> 16) & 0x1f)
            {
                case 0: // bltz
                {
                    asm.Outs.Add(localAddress, JumpType.JumpConditional);
                    asm.Add(MicroOpcode.Cmp, rs, new ConstValue(0, 32));
                    var tmpReg = GetTmpReg(1);
                    asm.Add(MicroOpcode.SSetL, tmpReg);
                    DecodeInstruction(asm, WordAtLocal(nextInsnAddressLocal), nextInsnAddressLocal + 4, true);
                    asm.Add(MicroOpcode.JmpIf, tmpReg, offset);
                    break;
                }
                case 1: // bgez
                {
                    asm.Outs.Add(localAddress, JumpType.JumpConditional);
                    asm.Add(MicroOpcode.Cmp, rs, new ConstValue(0, 32));
                    var tmpReg = GetTmpReg(1);
                    asm.Add(MicroOpcode.SSetL, tmpReg);
                    asm.Add(MicroOpcode.LogicalNot, tmpReg);
                    DecodeInstruction(asm, WordAtLocal(nextInsnAddressLocal), nextInsnAddressLocal + 4, true);
                    asm.Add(MicroOpcode.JmpIf, tmpReg, offset);
                    break;
                }
                case 16: // bltzal
                {
                    asm.Outs.Add(localAddress, JumpType.JumpConditional);
                    asm.Add(MicroOpcode.Cmp, rs, new ConstValue(0, 32));
                    var tmpReg = GetTmpReg(1);
                    asm.Add(MicroOpcode.SSetL, tmpReg);
                    DecodeInstruction(asm, WordAtLocal(nextInsnAddressLocal), nextInsnAddressLocal + 4, true);
                    asm.Add(MicroOpcode.JmpIf, tmpReg, offset);
                    break;
                }
                case 17: // bgezal
                {
                    asm.Outs.Add(localAddress, JumpType.JumpConditional);
                    asm.Add(MicroOpcode.Cmp, rs, new ConstValue(0, 32));
                    var tmpReg = GetTmpReg(1);
                    asm.Add(MicroOpcode.SSetL, tmpReg);
                    asm.Add(MicroOpcode.LogicalNot, tmpReg);
                    DecodeInstruction(asm, WordAtLocal(nextInsnAddressLocal), nextInsnAddressLocal + 4, true);
                    asm.Add(MicroOpcode.JmpIf, tmpReg, offset);
                    break;
                }
                default:
                    asm.Add(MicroOpcode.Data, new ConstValue(data, 32));
                    break;
            }
        }

        private static void DecodeCop2(MicroAssemblyBlock asm, uint nextInsnAddressLocal, uint data)
        {
            var opc = data & ((1 << 26) - 1);
            if (((data >> 25) & 1) != 0)
            {
                DecodeCop2Gte(asm, nextInsnAddressLocal, opc);
                return;
            }

            var cf = (opc >> 21) & 0x1F;
            switch (cf)
            {
                case 0:
                    asm.Add(new UnsupportedInsn("mfc2", MakeZeroRegisterOperand(opc, 16),
                        new ConstValue((ushort) opc, 16), MakeC2RegisterOperand(opc, 21)));
                    asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                case 2:
                    asm.Add(new UnsupportedInsn("cfc2", MakeZeroRegisterOperand(opc, 16),
                        new ConstValue((ushort) opc, 16), MakeC2RegisterOperand(opc, 21)));
                    asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                case 4:
                    asm.Add(new UnsupportedInsn("mtc2", MakeZeroRegisterOperand(opc, 16),
                        new ConstValue((ushort) opc, 16), MakeC2RegisterOperand(opc, 21)));
                    asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                case 6:
                    asm.Add(new UnsupportedInsn("ctc2", MakeZeroRegisterOperand(opc, 16),
                        new ConstValue((ushort) opc, 16), MakeC2RegisterOperand(opc, 21)));
                    asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                default:
                    asm.Add(MicroOpcode.Data, new ConstValue(data, 32));
                    break;
            }
        }

        private static void DecodeCop2Gte(MicroAssemblyBlock asm, uint nextInsnAddressLocal, uint data)
        {
            switch (data & 0x1F003FF)
            {
                case 0x0400012:
                    asm.Add(new UnsupportedInsn("mvmva",
                        new ConstValue(data >> 19 & 1, 1),
                        new ConstValue(data >> 17 & 3, 2),
                        new ConstValue(data >> 15 & 3, 2),
                        new ConstValue(data >> 13 & 3, 2),
                        new ConstValue(data >> 10 & 1, 1)
                    ));
                    asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                case 0x0a00428:
                    asm.Add(new UnsupportedInsn("sqr", new ConstValue(data >> 19 & 1, 1)));
                    asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                case 0x170000C:
                    asm.Add(new UnsupportedInsn("op", new ConstValue(data >> 19 & 1, 1)));
                    asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                case 0x190003D:
                    asm.Add(new UnsupportedInsn("gpf", new ConstValue(data >> 19 & 1, 1)));
                    asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                case 0x1A0003E:
                    asm.Add(new UnsupportedInsn("gpl", new ConstValue(data >> 19 & 1, 1)));
                    asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                default:
                    switch (data)
                    {
                        case 0x0180001:
                            asm.Add(new UnsupportedInsn("rtps"));
                            asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                            break;
                        case 0x0280030:
                            asm.Add(new UnsupportedInsn("rtpt"));
                            asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                            break;
                        case 0x0680029:
                            asm.Add(new UnsupportedInsn("dcpl"));
                            asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                            break;
                        case 0x0780010:
                            asm.Add(new UnsupportedInsn("dcps"));
                            asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                            break;
                        case 0x0980011:
                            asm.Add(new UnsupportedInsn("intpl"));
                            asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                            break;
                        case 0x0C8041E:
                            asm.Add(new UnsupportedInsn("ncs"));
                            asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                            break;
                        case 0x0D80420:
                            asm.Add(new UnsupportedInsn("nct"));
                            asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                            break;
                        case 0x0E80413:
                            asm.Add(new UnsupportedInsn("ncds"));
                            asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                            break;
                        case 0x0F80416:
                            asm.Add(new UnsupportedInsn("ncdt"));
                            asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                            break;
                        case 0x0F8002A:
                            asm.Add(new UnsupportedInsn("dpct"));
                            asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                            break;
                        case 0x108041B:
                            asm.Add(new UnsupportedInsn("nccs"));
                            asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                            break;
                        case 0x118043F:
                            asm.Add(new UnsupportedInsn("ncct"));
                            asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                            break;
                        case 0x1280414:
                            asm.Add(new UnsupportedInsn("cdp"));
                            asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                            break;
                        case 0x138041C:
                            asm.Add(new UnsupportedInsn("cc"));
                            asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                            break;
                        case 0x1400006:
                            asm.Add(new UnsupportedInsn("nclip"));
                            asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                            break;
                        case 0x158002D:
                            asm.Add(new UnsupportedInsn("avsz3"));
                            asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                            break;
                        case 0x168002E:
                            asm.Add(new UnsupportedInsn("avsz4"));
                            asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                            break;
                        default:
                            asm.Add(MicroOpcode.Data, new ConstValue(data, 32));
                            break;
                    }

                    break;
            }
        }

        [SuppressMessage("ReSharper", "NotAccessedField.Local")]
        [SuppressMessage("ReSharper", "MemberCanBePrivate.Local")]
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        private class Header
        {
            public readonly uint bAddr;
            public readonly uint bSize;
            public readonly uint dAddr;
            public readonly uint data;
            public readonly uint dSize;
            public readonly uint gp0;
            public readonly char[] id;
            public readonly uint pc0;
            public readonly uint sAddr;
            public readonly uint savedFp;
            public readonly uint savedGp;
            public readonly uint savedRa;
            public readonly uint savedS0;
            public readonly uint savedSp;
            public readonly uint sSize;
            public readonly uint tAddr;
            public readonly uint text;
            public readonly uint tSize;

            public Header(EndianBinaryReader reader)
            {
                id = reader.ReadBytes(8).Select(b => (char) b).ToArray();

                if (!"PS-X EXE".Equals(new string(id)))
                    throw new Exception("Header ID mismatch");

                text = reader.ReadUInt32();
                data = reader.ReadUInt32();
                pc0 = reader.ReadUInt32();
                gp0 = reader.ReadUInt32();
                tAddr = reader.ReadUInt32();
                tSize = reader.ReadUInt32();
                dAddr = reader.ReadUInt32();
                dSize = reader.ReadUInt32();
                bAddr = reader.ReadUInt32();
                bSize = reader.ReadUInt32();
                sAddr = reader.ReadUInt32();
                sSize = reader.ReadUInt32();
                savedSp = reader.ReadUInt32();
                savedFp = reader.ReadUInt32();
                savedGp = reader.ReadUInt32();
                savedRa = reader.ReadUInt32();
                savedS0 = reader.ReadUInt32();
            }
        }
    }
}
