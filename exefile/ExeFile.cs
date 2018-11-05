using System;
using System.Collections.Generic;
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

        private readonly Queue<uint> _analysisQueue = new Queue<uint>();
        private readonly byte[] _data;
        private readonly uint? _gpBase;

        private readonly Header _header;

        private readonly IDebugSource _debugSource;
        private readonly IDictionary<uint, MicroAssembly> _decoded = new Dictionary<uint, MicroAssembly>();
        public ISet<uint> Callees = new SortedSet<uint>();

        public IEnumerable<KeyValuePair<uint, MicroAssembly>> RelocatedInstructions =>
            _decoded.Select(kv => new KeyValuePair<uint, MicroAssembly>(MakeGlobal(kv.Key), kv.Value));

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
                .FirstOrDefault();
        }

        private void EnqueueForAnalysis(uint localAddress)
        {
            if (!_decoded.ContainsKey(localAddress))
                _analysisQueue.Enqueue(localAddress);
        }

        public uint MakeGlobal(uint addr)
        {
            return addr + _header.tAddr;
        }

        public uint MakeLocal(uint addr)
        {
            if (addr < _header.tAddr)
                throw new ArgumentOutOfRangeException(nameof(addr), "Address out of range to make local");

            return addr - _header.tAddr;
        }

        public uint WordAtGlobal(uint address)
        {
            return WordAtLocal(MakeLocal(address));
        }

        public uint WordAtLocal(uint address)
        {
            uint data;
            data = _data[address++];
            data |= (uint) _data[address++] << 8;
            data |= (uint) _data[address++] << 16;
            // ReSharper disable once RedundantAssignment
            data |= (uint) _data[address++] << 24;
            return data;
        }

        public bool ContainsGlobal(uint address, bool onlyCode = true)
        {
            try
            {
                return ContainsLocal(MakeLocal(address), onlyCode);
            }
            catch (ArgumentOutOfRangeException)
            {
                return false;
            }
        }

        public bool ContainsLocal(uint address, bool onlyCode = true)
        {
            if (onlyCode)
                return address < _header.tSize;
            else
                return address < _data.Length;
        }

        private static Opcode ExtractOpcode(uint data)
        {
            return (Opcode) (data >> 26);
        }

        public void Disassemble(uint localStart)
        {
            _analysisQueue.Clear();
            _analysisQueue.Enqueue(localStart);
            DisassembleImpl();
        }

        public void Disassemble()
        {
            _analysisQueue.Clear();
            _analysisQueue.Enqueue(MakeLocal(_header.pc0));
            foreach (var addr in _debugSource.Functions.Select(f => f.GlobalAddress).Select(MakeLocal))
                _analysisQueue.Enqueue(addr);
            DisassembleImpl();
        }

        private static IMicroArg MakeRegisterOperand(uint data, int offset)
        {
            var r = (Register) ((data >> offset) & 0x1f);
            return new RegisterArg(RegisterUtil.ToUInt(r));
        }

        private static RegisterArg MakeC0RegisterOperand(uint data, int offset)
        {
            return new RegisterArg(RegisterUtil.ToUInt((C0Register) ((data >> offset) & 0x1f)));
        }

        private static RegisterArg MakeC2RegisterOperand(uint data, int offset)
        {
            return new RegisterArg(RegisterUtil.ToUInt((C2Register) ((data >> offset) & 0x1f)));
        }

        private static RegisterMemArg MakeRegisterOffsetOperand(uint data, int shift, int offset)
        {
            return new RegisterMemArg(RegisterUtil.ToUInt((Register) ((data >> shift) & 0x1f)), offset);
        }

        private void DisassembleImpl()
        {
            logger.Info("Disassembly started");

            while (_analysisQueue.Count != 0)
            {
                var localAddress = _analysisQueue.Dequeue();
                if (_decoded.ContainsKey(localAddress) || localAddress >= _header.tSize)
                    continue;

                var asm = new MicroAssembly();
                _decoded[localAddress] = asm;

                var data = WordAtLocal(localAddress);
                localAddress += 4;
                DecodeInstruction(asm, data, ref localAddress, false);
                EnqueueForAnalysis(localAddress);
            }
        }

        private IMicroArg MakeGpBasedOperand(uint data, int shift, int offset)
        {
            var regofs = MakeRegisterOffsetOperand(data, shift, offset);
            if (_gpBase == null)
                return regofs;

            if (regofs.Register == RegisterUtil.ToUInt(Register.gp))
                return new AddressValue((uint) (_gpBase.Value + regofs.Offset),
                    _debugSource.GetSymbolName(_gpBase.Value, regofs.Offset));

            return regofs;
        }

        private void DecodeInstruction(MicroAssembly asm, uint data, ref uint nextInsnAddressLocal, bool inDelaySlot)
        {
            switch (ExtractOpcode(data))
            {
                case Opcode.RegisterFormat:
                    DecodeRegisterFormat(asm, data);
                    break;
                case Opcode.PCRelative:
                    if (inDelaySlot)
                    {
                        logger.Warn($"Recursive delay slot disassembly at 0x{nextInsnAddressLocal - 4}");
                        break;
                    }

                    DecodePcRelative(asm, ref nextInsnAddressLocal, data);
                    break;
                case Opcode.j:
                {
                    if (inDelaySlot)
                    {
                        logger.Warn($"Recursive delay slot disassembly at 0x{nextInsnAddressLocal - 4}");
                        break;
                    }

                    var addr = (data & 0x03FFFFFF) << 2;
                    var tgt = new AddressValue(addr, _debugSource.GetSymbolName(addr));
                    EnqueueForAnalysis(MakeLocal(addr));
                    nextInsnAddressLocal += 4;
                    DecodeInstruction(asm, WordAtLocal(nextInsnAddressLocal - 4), ref nextInsnAddressLocal, true);
                    asm.Add(MicroOpcode.Jmp, tgt);
                }
                    break;
                case Opcode.jal:
                {
                    if (inDelaySlot)
                    {
                        logger.Warn($"Recursive delay slot disassembly at 0x{nextInsnAddressLocal - 4}");
                        break;
                    }

                    var addr = (data & 0x03FFFFFF) << 2;
                    var tgt = new AddressValue(addr, _debugSource.GetSymbolName(addr));
                    EnqueueForAnalysis(MakeLocal(addr));
                    Callees.Add(addr);
                    nextInsnAddressLocal += 4;
                    DecodeInstruction(asm, WordAtLocal(nextInsnAddressLocal - 4), ref nextInsnAddressLocal, true);
                    asm.Add(MicroOpcode.Call, new RegisterArg(RegisterUtil.ToUInt(Register.ra)), tgt);
                }
                    break;
                case Opcode.beq:
                {
                    if (inDelaySlot)
                    {
                        logger.Warn($"Recursive delay slot disassembly at 0x{nextInsnAddressLocal - 4}");
                        break;
                    }

                    var addr = (uint) ((nextInsnAddressLocal + (short) data) << 2);
                    var tgt = new AddressValue(addr,
                        _debugSource.GetSymbolName(nextInsnAddressLocal, (short) data << 2));
                    EnqueueForAnalysis(addr);
                    var r1 = MakeRegisterOperand(data, 21);
                    var r2 = MakeRegisterOperand(data, 16);
                    var tmp = asm.GetTmpReg();
                    asm.Add(MicroOpcode.Cmp, r1, r2);
                    asm.Add(MicroOpcode.SetEq, tmp);
                    nextInsnAddressLocal += 4;
                    DecodeInstruction(asm, WordAtLocal(nextInsnAddressLocal - 4), ref nextInsnAddressLocal, true);
                    asm.Add(MicroOpcode.JmpIf, tmp, tgt);
                }
                    break;
                case Opcode.bne:
                {
                    if (inDelaySlot)
                    {
                        logger.Warn($"Recursive delay slot disassembly at 0x{nextInsnAddressLocal - 4}");
                        break;
                    }

                    var addr = (uint) ((nextInsnAddressLocal + (short) data) << 2);
                    var tgt = new AddressValue(addr,
                        _debugSource.GetSymbolName(nextInsnAddressLocal, (short) data << 2));
                    EnqueueForAnalysis(addr);
                    var r1 = MakeRegisterOperand(data, 21);
                    var r2 = MakeRegisterOperand(data, 16);
                    var tmp = asm.GetTmpReg();
                    asm.Add(MicroOpcode.Cmp, r1, r2);
                    asm.Add(MicroOpcode.SetEq, tmp);
                    asm.Add(MicroOpcode.LogicalNot, tmp);
                    nextInsnAddressLocal += 4;
                    DecodeInstruction(asm, WordAtLocal(nextInsnAddressLocal - 4), ref nextInsnAddressLocal, true);
                    asm.Add(MicroOpcode.JmpIf, tmp, tgt);
                }
                    break;
                case Opcode.blez:
                {
                    if (inDelaySlot)
                    {
                        logger.Warn($"Recursive delay slot disassembly at 0x{nextInsnAddressLocal - 4}");
                        break;
                    }

                    var addr = (uint) ((nextInsnAddressLocal + (short) data) << 2);
                    var tgt = new AddressValue(addr,
                        _debugSource.GetSymbolName(nextInsnAddressLocal, (short) data << 2));
                    EnqueueForAnalysis(addr);
                    var r1 = MakeRegisterOperand(data, 21);
                    var tmp = asm.GetTmpReg();
                    asm.Add(MicroOpcode.Cmp, r1, new ConstValue(0, 32));
                    asm.Add(MicroOpcode.SSetLE, tmp);
                    nextInsnAddressLocal += 4;
                    DecodeInstruction(asm, WordAtLocal(nextInsnAddressLocal - 4), ref nextInsnAddressLocal, true);
                    asm.Add(MicroOpcode.JmpIf, tmp, tgt);
                }
                    break;
                case Opcode.bgtz:
                {
                    if (inDelaySlot)
                    {
                        logger.Warn($"Recursive delay slot disassembly at 0x{nextInsnAddressLocal - 4}");
                        break;
                    }

                    var addr = (uint) ((nextInsnAddressLocal + (short) data) << 2);
                    var tgt = new AddressValue(addr,
                        _debugSource.GetSymbolName(nextInsnAddressLocal, (short) data << 2));
                    EnqueueForAnalysis(addr);
                    var r1 = MakeRegisterOperand(data, 21);
                    var tmp = asm.GetTmpReg();
                    asm.Add(MicroOpcode.Cmp, r1, new ConstValue(0, 32));
                    asm.Add(MicroOpcode.SSetLE, tmp);
                    asm.Add(MicroOpcode.LogicalNot, tmp);
                    nextInsnAddressLocal += 4;
                    DecodeInstruction(asm, WordAtLocal(nextInsnAddressLocal - 4), ref nextInsnAddressLocal, true);
                    asm.Add(MicroOpcode.JmpIf, tmp, tgt);
                }
                    break;
                case Opcode.addi:
                    asm.Add(MicroOpcode.Add, MakeRegisterOperand(data, 16), MakeRegisterOperand(data, 21),
                        new ConstValue((ushort) data, 16));
                    break;
                case Opcode.addiu:
                    asm.Add(MicroOpcode.Add, MakeRegisterOperand(data, 16), MakeRegisterOperand(data, 21),
                        new ConstValue((ushort) data, 16));
                    break;
                case Opcode.subi:
                    asm.Add(MicroOpcode.Sub, MakeRegisterOperand(data, 16), MakeRegisterOperand(data, 21),
                        new ConstValue((ushort) data, 16));
                    break;
                case Opcode.subiu:
                    asm.Add(MicroOpcode.Sub, MakeRegisterOperand(data, 16), MakeRegisterOperand(data, 21),
                        new ConstValue((ushort) data, 16));
                    break;
                case Opcode.andi:
                    asm.Add(MicroOpcode.And, MakeRegisterOperand(data, 16), MakeRegisterOperand(data, 21),
                        new ConstValue((ushort) data, 16));
                    break;
                case Opcode.ori:
                    asm.Add(MicroOpcode.Or, MakeRegisterOperand(data, 16), MakeRegisterOperand(data, 21),
                        new ConstValue((ushort) data, 16));
                    break;
                case Opcode.xori:
                    asm.Add(MicroOpcode.XOr, MakeRegisterOperand(data, 16), MakeRegisterOperand(data, 21),
                        new ConstValue((ushort) data, 16));
                    break;
                case Opcode.lui:
                    asm.Add(new CopyInsn(MakeRegisterOperand(data, 16),
                        new ConstValue((ulong) ((ushort) data << 16), 32), 32));
                    break;
                case Opcode.CpuControl:
                    DecodeCpuControl(asm, nextInsnAddressLocal, data);
                    break;
                case Opcode.FloatingPoint:
                    asm.Add(MicroOpcode.Data, new ConstValue(data, 32));
                    break;
                case Opcode.lb:
                {
                    var tmp = asm.GetTmpReg();
                    asm.Add(new CopyInsn(tmp, MakeGpBasedOperand(data, 21, (short) data), 8));
                    asm.Add(new SignedCastInsn(tmp, 8, 32));
                    asm.Add(new CopyInsn(MakeRegisterOperand(data, 16), tmp, 32));
                }
                    break;
                case Opcode.lh:
                {
                    var tmp = asm.GetTmpReg();
                    asm.Add(new CopyInsn(tmp, MakeGpBasedOperand(data, 21, (short) data), 16));
                    asm.Add(new SignedCastInsn(tmp, 16, 32));
                    asm.Add(new CopyInsn(MakeRegisterOperand(data, 16), tmp, 32));
                }
                    break;
                case Opcode.lwl:
                    asm.Add(new UnsupportedInsn("lwl", MakeRegisterOperand(data, 16),
                        MakeGpBasedOperand(data, 21, (short) data)));
                    break;
                case Opcode.lw:
                    asm.Add(new CopyInsn(MakeRegisterOperand(data, 16), MakeGpBasedOperand(data, 21, (short) data),
                        32));
                    break;
                case Opcode.lbu:
                {
                    var tmp = asm.GetTmpReg();
                    asm.Add(new CopyInsn(tmp, MakeGpBasedOperand(data, 21, (short) data), 8));
                    asm.Add(new UnsignedCastInsn(tmp, 8, 32));
                    asm.Add(new CopyInsn(MakeRegisterOperand(data, 16), tmp, 32));
                }
                    break;
                case Opcode.lhu:
                {
                    var tmp = asm.GetTmpReg();
                    asm.Add(new CopyInsn(tmp, MakeGpBasedOperand(data, 21, (short) data), 16));
                    asm.Add(new UnsignedCastInsn(tmp, 16, 32));
                    asm.Add(new CopyInsn(MakeRegisterOperand(data, 16), tmp, 32));
                }
                    break;
                case Opcode.lwr:
                    asm.Add(new UnsupportedInsn("lwr", MakeRegisterOperand(data, 16),
                        MakeGpBasedOperand(data, 21, (short) data)));
                    break;
                case Opcode.sb:
                {
                    var tmp = asm.GetTmpReg();
                    asm.Add(new CopyInsn(tmp, MakeRegisterOperand(data, 16), 32));
                    asm.Add(new UnsignedCastInsn(tmp, 32, 8));
                    asm.Add(new CopyInsn(MakeGpBasedOperand(data, 21, (short) data), tmp, 8));
                }
                    break;
                case Opcode.sh:
                {
                    var tmp = asm.GetTmpReg();
                    asm.Add(new CopyInsn(tmp, MakeRegisterOperand(data, 16), 32));
                    asm.Add(new UnsignedCastInsn(tmp, 32, 16));
                    asm.Add(new CopyInsn(MakeGpBasedOperand(data, 21, (short) data), tmp, 16));
                }
                    break;
                case Opcode.swl:
                    asm.Add(new UnsupportedInsn("swl", MakeRegisterOperand(data, 16),
                        MakeGpBasedOperand(data, 21, (short) data)));
                    break;
                case Opcode.sw:
                    asm.Add(new CopyInsn(MakeGpBasedOperand(data, 21, (short) data), MakeRegisterOperand(data, 16),
                        32));
                    break;
                case Opcode.swr:
                    asm.Add(new UnsupportedInsn("swr", MakeRegisterOperand(data, 16),
                        MakeGpBasedOperand(data, 21, (short) data)));
                    break;
                case Opcode.swc1:
                    asm.Add(new UnsupportedInsn("swc1", MakeRegisterOperand(data, 16),
                        new ConstValue((ushort) data, 16), MakeRegisterOperand(data, 21)));
                    break;
                case Opcode.lwc1:
                    asm.Add(new UnsupportedInsn("lwc1", MakeC2RegisterOperand(data, 16),
                        new ConstValue((ushort) data, 16), MakeRegisterOperand(data, 21)));
                    break;
                case Opcode.cop0:
                    asm.Add(new UnsupportedInsn("cop0", new ConstValue(data & ((1 << 26) - 1), 26)));
                    break;
                case Opcode.cop1:
                    asm.Add(new UnsupportedInsn("cop1", new ConstValue(data & ((1 << 26) - 1), 26)));
                    break;
                case Opcode.cop2:
                    DecodeCop2(asm, data);
                    break;
                case Opcode.cop3:
                    asm.Add(new UnsupportedInsn("cop3", new ConstValue(data & ((1 << 26) - 1), 26)));
                    break;
                case Opcode.beql:
                {
                    if (inDelaySlot)
                    {
                        logger.Warn($"Recursive delay slot disassembly at 0x{nextInsnAddressLocal - 4}");
                        break;
                    }

                    var addr = (uint) ((nextInsnAddressLocal + (short) data) << 2);
                    var tgt = new AddressValue(addr,
                        _debugSource.GetSymbolName(nextInsnAddressLocal, (short) data << 2));
                    EnqueueForAnalysis(addr);
                    asm.Add(MicroOpcode.Cmp, MakeRegisterOperand(data, 21), MakeRegisterOperand(data, 16));
                    var tmp = asm.GetTmpReg();
                    asm.Add(MicroOpcode.SetEq, tmp);
                    nextInsnAddressLocal += 4;
                    DecodeInstruction(asm, WordAtLocal(nextInsnAddressLocal - 4), ref nextInsnAddressLocal, true);
                    asm.Add(MicroOpcode.JmpIf, tmp, tgt);
                }
                    break;
                case Opcode.bnel:
                {
                    if (inDelaySlot)
                    {
                        logger.Warn($"Recursive delay slot disassembly at 0x{nextInsnAddressLocal - 4}");
                        break;
                    }

                    var addr = (uint) ((nextInsnAddressLocal + (short) data) << 2);
                    var tgt = new AddressValue(addr,
                        _debugSource.GetSymbolName(nextInsnAddressLocal, (short) data << 2));
                    EnqueueForAnalysis(addr);
                    asm.Add(MicroOpcode.Cmp, MakeRegisterOperand(data, 21), MakeRegisterOperand(data, 16));
                    var tmp = asm.GetTmpReg();
                    asm.Add(MicroOpcode.SetEq, tmp);
                    asm.Add(MicroOpcode.LogicalNot, tmp);
                    nextInsnAddressLocal += 4;
                    DecodeInstruction(asm, WordAtLocal(nextInsnAddressLocal - 4), ref nextInsnAddressLocal, true);
                    asm.Add(MicroOpcode.JmpIf, tmp, tgt);
                }
                    break;
                case Opcode.blezl:
                {
                    if (inDelaySlot)
                    {
                        logger.Warn($"Recursive delay slot disassembly at 0x{nextInsnAddressLocal - 4}");
                        break;
                    }

                    var addr = (uint) ((nextInsnAddressLocal + (short) data) << 2);
                    var tgt = new AddressValue(addr,
                        _debugSource.GetSymbolName(nextInsnAddressLocal, (short) data << 2));
                    EnqueueForAnalysis(addr);
                    asm.Add(MicroOpcode.Cmp, MakeRegisterOperand(data, 21), new ConstValue(0, 32));
                    var tmp = asm.GetTmpReg();
                    asm.Add(MicroOpcode.SSetLE, tmp);
                    nextInsnAddressLocal += 4;
                    DecodeInstruction(asm, WordAtLocal(nextInsnAddressLocal - 4), ref nextInsnAddressLocal, true);
                    asm.Add(MicroOpcode.JmpIf, tmp, tgt);
                }
                    break;
                case Opcode.bgtzl:
                {
                    if (inDelaySlot)
                    {
                        logger.Warn($"Recursive delay slot disassembly at 0x{nextInsnAddressLocal - 4}");
                        break;
                    }

                    var addr = (uint) ((nextInsnAddressLocal + (short) data) << 2);
                    var tgt = new AddressValue(addr,
                        _debugSource.GetSymbolName(nextInsnAddressLocal, (short) data << 2));
                    EnqueueForAnalysis(addr);
                    asm.Add(MicroOpcode.Cmp, MakeRegisterOperand(data, 21), new ConstValue(0, 32));
                    var tmp = asm.GetTmpReg();
                    asm.Add(MicroOpcode.SSetLE, tmp);
                    asm.Add(MicroOpcode.LogicalNot, tmp);
                    nextInsnAddressLocal += 4;
                    DecodeInstruction(asm, WordAtLocal(nextInsnAddressLocal - 4), ref nextInsnAddressLocal, true);
                    asm.Add(MicroOpcode.JmpIf, tmp, tgt);
                }
                    break;
                default:
                    asm.Add(MicroOpcode.Data, new ConstValue(data, 32));
                    break;
            }
        }

        private static void DecodeRegisterFormat(MicroAssembly asm, uint data)
        {
            var rd = MakeRegisterOperand(data, 11);
            var rs2 = MakeRegisterOperand(data, 16);
            var rs1 = MakeRegisterOperand(data, 21);
            switch ((OpcodeFunction) (data & 0x3f))
            {
                case OpcodeFunction.sll:
                    if (data == 0)
                        asm.Add(MicroOpcode.Nop);
                    else
                        asm.Add(MicroOpcode.SHL, rd, rs2, new ConstValue(data >> 6 & 0x1F, 5));
                    break;
                case OpcodeFunction.srl:
                    if (data == 0)
                        asm.Add(MicroOpcode.Nop);
                    else
                        asm.Add(MicroOpcode.SRL, rd, rs2, new ConstValue(data >> 6 & 0x1F, 5));
                    break;
                case OpcodeFunction.sra:
                    if (data == 0)
                        asm.Add(MicroOpcode.Nop);
                    else
                        asm.Add(MicroOpcode.SRA, rd, rs2, new ConstValue(data >> 6 & 0x1F, 5));
                    break;
                case OpcodeFunction.sllv:
                    asm.Add(MicroOpcode.SHL, rd, rs2, rs1);
                    break;
                case OpcodeFunction.srlv:
                    asm.Add(MicroOpcode.SRL, rd, rs2, rs1);
                    break;
                case OpcodeFunction.srav:
                    asm.Add(MicroOpcode.SRA, rd, rs2, rs1);
                    break;
                case OpcodeFunction.jr:
                    asm.Add(MicroOpcode.Jmp, rs1);
                    break;
                case OpcodeFunction.jalr:
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
                    break;
                case OpcodeFunction.mthi:
                    asm.Add(new UnsupportedInsn("mthi", rd));
                    break;
                case OpcodeFunction.mflo:
                    asm.Add(new UnsupportedInsn("mflo", rd));
                    break;
                case OpcodeFunction.mtlo:
                    asm.Add(new UnsupportedInsn("mtlo", rd));
                    break;
                case OpcodeFunction.mult:
                    asm.Add(new UnsupportedInsn("mult", rs1, rs2));
                    break;
                case OpcodeFunction.multu:
                    asm.Add(new UnsupportedInsn("multu", rs1, rs2));
                    break;
                case OpcodeFunction.div:
                    asm.Add(new UnsupportedInsn("div", rs1, rs2));
                    break;
                case OpcodeFunction.divu:
                    asm.Add(new UnsupportedInsn("divu", rs1, rs2));
                    break;
                case OpcodeFunction.add:
                    asm.Add(MicroOpcode.Add, rd, rs1, rs2);
                    return;
                case OpcodeFunction.addu:
                    asm.Add(MicroOpcode.Add, rd, rs1, rs2);
                    return;
                case OpcodeFunction.sub:
                    asm.Add(MicroOpcode.Sub, rd, rs1, rs2);
                    return;
                case OpcodeFunction.subu:
                    asm.Add(MicroOpcode.Sub, rd, rs1, rs2);
                    return;
                case OpcodeFunction.and:
                    asm.Add(MicroOpcode.And, rd, rs1, rs2);
                    return;
                case OpcodeFunction.or:
                    asm.Add(MicroOpcode.Or, rd, rs1, rs2);
                    return;
                case OpcodeFunction.xor:
                    asm.Add(MicroOpcode.XOr, rd, rs1, rs2);
                    return;
                case OpcodeFunction.nor:
                {
                    var tmp = asm.GetTmpReg();
                    asm.Add(MicroOpcode.Copy, tmp, rs1);
                    asm.Add(MicroOpcode.Or, tmp, rs2);
                    asm.Add(MicroOpcode.Not, tmp);
                    asm.Add(MicroOpcode.Copy, rd, tmp);
                }
                    break;
                case OpcodeFunction.slt:
                    asm.Add(MicroOpcode.Cmp, rs1, rs2);
                    asm.Add(MicroOpcode.SSetL, rd);
                    break;
                case OpcodeFunction.sltu:
                    asm.Add(MicroOpcode.Cmp, rs1, rs2);
                    asm.Add(MicroOpcode.USetL, rd);
                    break;
                default:
                    asm.Add(MicroOpcode.Data, new ConstValue(data, 32));
                    break;
            }
        }

        private void DecodeCpuControl(MicroAssembly asm, uint nextInsnAddressLocal, uint data)
        {
            switch ((CpuControlOpcode) ((data >> 21) & 0x1f))
            {
                case CpuControlOpcode.mtc0:
                    asm.Add(new UnsupportedInsn("mtc0", MakeRegisterOperand(data, 16),
                        MakeC0RegisterOperand(data, 11)));
                    break;
                case CpuControlOpcode.bc0:
                    switch ((data >> 16) & 0x1f)
                    {
                        case 0:
                            EnqueueForAnalysis((uint) ((nextInsnAddressLocal + (short) data) << 2));
                            asm.Add(new UnsupportedInsn("bc0f",
                                new AddressValue((uint) (nextInsnAddressLocal + ((short) data << 2)),
                                    _debugSource.GetSymbolName(nextInsnAddressLocal, (ushort) data << 2))));
                            break;
                        case 1:
                            EnqueueForAnalysis((uint) ((nextInsnAddressLocal + (short) data) << 2));
                            asm.Add(new UnsupportedInsn("bc0t",
                                new AddressValue((uint) (nextInsnAddressLocal + ((short) data << 2)),
                                    _debugSource.GetSymbolName(nextInsnAddressLocal, (ushort) data << 2))));
                            break;
                        default:
                            asm.Add(MicroOpcode.Data, new ConstValue(data, 32));
                            break;
                    }

                    break;
                case CpuControlOpcode.tlb:
                    DecodeTlb(asm, data);
                    break;
                case CpuControlOpcode.mfc0:
                    asm.Add(new UnsupportedInsn("mfc0", MakeRegisterOperand(data, 16),
                        MakeC0RegisterOperand(data, 11)));
                    break;
                default:
                    asm.Add(MicroOpcode.Data, new ConstValue(data, 32));
                    break;
            }
        }

        private static void DecodeTlb(MicroAssembly asm, uint data)
        {
            switch ((TlbOpcode) (data & 0x1f))
            {
                case TlbOpcode.tlbr:
                    asm.Add(new UnsupportedInsn("tlbr"));
                    break;
                case TlbOpcode.tlbwi:
                    asm.Add(new UnsupportedInsn("tlbwi"));
                    break;
                case TlbOpcode.tlbwr:
                    asm.Add(new UnsupportedInsn("tlbwr"));
                    break;
                case TlbOpcode.tlbp:
                    asm.Add(new UnsupportedInsn("tlbp"));
                    break;
                case TlbOpcode.rfe:
                    asm.Add(new UnsupportedInsn("rfe"));
                    break;
                default:
                    asm.Add(MicroOpcode.Data, new ConstValue(data, 32));
                    break;
            }
        }

        private void DecodePcRelative(MicroAssembly asm, ref uint nextInsnAddressLocal, uint data)
        {
            var rs = MakeRegisterOperand(data, 21);
            var addr = (short) data << 2;
            var offset = new AddressValue((ulong) (nextInsnAddressLocal + addr),
                _debugSource.GetSymbolName(nextInsnAddressLocal, addr));
            switch ((data >> 16) & 0x1f)
            {
                case 0: // bltz
                {
                    EnqueueForAnalysis((uint) ((nextInsnAddressLocal + (short) data) << 2));
                    asm.Add(MicroOpcode.Cmp, rs, new ConstValue(0, 32));
                    var tmpReg = asm.GetTmpReg();
                    asm.Add(MicroOpcode.SSetL, tmpReg);
                    nextInsnAddressLocal += 4;
                    DecodeInstruction(asm, WordAtLocal(nextInsnAddressLocal - 4), ref nextInsnAddressLocal, true);
                    asm.Add(MicroOpcode.JmpIf, tmpReg, offset);
                    break;
                }
                case 1: // bgez
                {
                    EnqueueForAnalysis((uint) (nextInsnAddressLocal + addr));
                    asm.Add(MicroOpcode.Cmp, rs, new ConstValue(0, 32));
                    var tmpReg = asm.GetTmpReg();
                    asm.Add(MicroOpcode.SSetL, tmpReg);
                    asm.Add(MicroOpcode.LogicalNot, tmpReg);
                    nextInsnAddressLocal += 4;
                    DecodeInstruction(asm, WordAtLocal(nextInsnAddressLocal - 4), ref nextInsnAddressLocal, true);
                    asm.Add(MicroOpcode.JmpIf, tmpReg, offset);
                    break;
                }
                case 16: // bltzal
                {
                    EnqueueForAnalysis((uint) (nextInsnAddressLocal + addr));
                    asm.Add(MicroOpcode.Cmp, rs, new ConstValue(0, 32));
                    var tmpReg = asm.GetTmpReg();
                    asm.Add(MicroOpcode.SSetL, tmpReg);
                    nextInsnAddressLocal += 4;
                    DecodeInstruction(asm, WordAtLocal(nextInsnAddressLocal - 4), ref nextInsnAddressLocal, true);
                    asm.Add(MicroOpcode.JmpIf, tmpReg, offset);
                    break;
                }
                case 17: // bgezal
                {
                    EnqueueForAnalysis((uint) (nextInsnAddressLocal + addr));
                    asm.Add(MicroOpcode.Cmp, rs, new ConstValue(0, 32));
                    var tmpReg = asm.GetTmpReg();
                    asm.Add(MicroOpcode.SSetL, tmpReg);
                    asm.Add(MicroOpcode.LogicalNot, tmpReg);
                    nextInsnAddressLocal += 4;
                    DecodeInstruction(asm, WordAtLocal(nextInsnAddressLocal - 4), ref nextInsnAddressLocal, true);
                    asm.Add(MicroOpcode.JmpIf, tmpReg, offset);
                    break;
                }
                default:
                    asm.Add(MicroOpcode.Data, new ConstValue(data, 32));
                    break;
            }
        }

        private static void DecodeCop2(MicroAssembly asm, uint data)
        {
            var opc = data & ((1 << 26) - 1);
            if (((data >> 25) & 1) != 0)
            {
                DecodeCop2Gte(asm, opc);
                return;
            }

            var cf = (opc >> 21) & 0x1F;
            switch (cf)
            {
                case 0:
                    asm.Add(new UnsupportedInsn("mfc2", MakeRegisterOperand(opc, 16),
                        new ConstValue((ushort) opc, 16), MakeC2RegisterOperand(opc, 21)));
                    break;
                case 2:
                    asm.Add(new UnsupportedInsn("cfc2", MakeRegisterOperand(opc, 16),
                        new ConstValue((ushort) opc, 16), MakeC2RegisterOperand(opc, 21)));
                    break;
                case 4:
                    asm.Add(new UnsupportedInsn("mtc2", MakeRegisterOperand(opc, 16),
                        new ConstValue((ushort) opc, 16), MakeC2RegisterOperand(opc, 21)));
                    break;
                case 6:
                    asm.Add(new UnsupportedInsn("ctc2", MakeRegisterOperand(opc, 16),
                        new ConstValue((ushort) opc, 16), MakeC2RegisterOperand(opc, 21)));
                    break;
                default:
                    asm.Add(MicroOpcode.Data, new ConstValue(data, 32));
                    break;
            }
        }

        private static void DecodeCop2Gte(MicroAssembly asm, uint data)
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
                    break;
                case 0x0a00428:
                    asm.Add(new UnsupportedInsn("sqr",
                        new ConstValue(data >> 19 & 1, 1)));
                    break;
                case 0x170000C:
                    asm.Add(new UnsupportedInsn("op",
                        new ConstValue(data >> 19 & 1, 1)));
                    break;
                case 0x190003D:
                    asm.Add(new UnsupportedInsn("gpf",
                        new ConstValue(data >> 19 & 1, 1)));
                    break;
                case 0x1A0003E:
                    asm.Add(new UnsupportedInsn("gpl",
                        new ConstValue(data >> 19 & 1, 1)));
                    break;
                default:
                    switch (data)
                    {
                        case 0x0180001:
                            asm.Add(new UnsupportedInsn("rtps"));
                            break;
                        case 0x0280030:
                            asm.Add(new UnsupportedInsn("rtpt"));
                            break;
                        case 0x0680029:
                            asm.Add(new UnsupportedInsn("dcpl"));
                            break;
                        case 0x0780010:
                            asm.Add(new UnsupportedInsn("dcps"));
                            break;
                        case 0x0980011:
                            asm.Add(new UnsupportedInsn("intpl"));
                            break;
                        case 0x0C8041E:
                            asm.Add(new UnsupportedInsn("ncs"));
                            break;
                        case 0x0D80420:
                            asm.Add(new UnsupportedInsn("nct"));
                            break;
                        case 0x0E80413:
                            asm.Add(new UnsupportedInsn("ncds"));
                            break;
                        case 0x0F80416:
                            asm.Add(new UnsupportedInsn("ncdt"));
                            break;
                        case 0x0F8002A:
                            asm.Add(new UnsupportedInsn("dpct"));
                            break;
                        case 0x108041B:
                            asm.Add(new UnsupportedInsn("nccs"));
                            break;
                        case 0x118043F:
                            asm.Add(new UnsupportedInsn("ncct"));
                            break;
                        case 0x1280414:
                            asm.Add(new UnsupportedInsn("cdp"));
                            break;
                        case 0x138041C:
                            asm.Add(new UnsupportedInsn("cc"));
                            break;
                        case 0x1400006:
                            asm.Add(new UnsupportedInsn("nclip"));
                            break;
                        case 0x158002D:
                            asm.Add(new UnsupportedInsn("avsz3"));
                            break;
                        case 0x168002E:
                            asm.Add(new UnsupportedInsn("avsz4"));
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
