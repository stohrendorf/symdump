using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using core;
using core.util;
using exefile.controlflow;
using exefile.controlflow.cfg;
using mips.disasm;
using mips.instructions;
using mips.operands;
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

        public IEnumerable<KeyValuePair<uint, Instruction>> RelocatedInstructions => _instructions
            .Select(kv => new KeyValuePair<uint, Instruction>(MakeGlobal(kv.Key), kv.Value));

        public IReadOnlyDictionary<uint, Instruction> Instructions => _instructions;

        public IReadOnlyCollection<uint> Callees => _callees;

        private readonly IDebugSource _debugSource;
        private readonly Dictionary<uint, HashSet<uint>> _xrefs = new Dictionary<uint, HashSet<uint>>();
        private readonly SortedSet<uint> _callees = new SortedSet<uint>();
        private readonly SortedDictionary<uint, Instruction> _instructions = new SortedDictionary<uint, Instruction>();

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

        private IEnumerable<string> GetLocalSymbolNames(uint localAddress)
        {
            _debugSource.Labels.TryGetValue(MakeGlobal(localAddress), out var lbls);
            return lbls?.Select(l => l.Name);
        }

        private void AddLocalCall(uint from, uint to)
        {
            AddLocalXref(from, to);
            _callees.Add(to);
        }

        private void AddLocalXref(uint from, uint to)
        {
            if (!_xrefs.TryGetValue(to, out var froms))
                _xrefs.Add(to, froms = new HashSet<uint>());

            froms.Add(from);

            if (!_instructions.ContainsKey(to))
                _analysisQueue.Enqueue(to);
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

        private HashSet<uint> GetGlobalXrefs(uint to)
        {
            _xrefs.TryGetValue(to, out var froms);
            return froms;
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

        public Graph AnalyzeControlFlow(uint globalAddress)
        {
            logger.Info($"Started control flow analysis of address 0x{globalAddress:x8}");

            var func = _debugSource.FindFunction(globalAddress);
            if (func != null)
                logger.Debug(func.GetSignature());

            //var flowState = new DataFlowState(_debugSource);

            var control = new ControlFlowProcessor();
            control.Process(MakeLocal(globalAddress), this);

            var reducer = new Reducer(control.Graph);
            reducer.Reduce();
            //reducer.Dump(new IndentedTextWriter(Console.Out));

            return reducer.Graph;

#if false
            {
                Console.WriteLine();
                var itw = new IndentedTextWriter(Console.Out);
                control.Dump(itw);
            }

            foreach (var insnPair in _instructions.Where(i => i.Key >= addr))
            {
                var xrefs = GetXrefs(insnPair.Key);
                if (xrefs != null)
                {
                    flowState.DumpState();
                    logger.Debug(_debugSource.GetSymbolName(insnPair.Key) + ":");
                }

                var insn = insnPair.Value;
                if (insn is NopInstruction || insn.IsBranchDelaySlot)
                {
                    continue;
                }

                //Console.WriteLine($"??? 0x{insnPair.Key:X}  " + insn.asReadable());

                var nextInsn = _instructions[insnPair.Key + 4];
                if (!flowState.Process(insn, nextInsn))
                    break;
            }
#endif
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
            foreach (var addr in _debugSource.Functions.Select(f => f.GlobalAddress))
                _analysisQueue.Enqueue(MakeLocal(addr));
            DisassembleImpl();
        }

        private void DisassembleImpl()
        {
            logger.Info("Disassembly started");

            bool needsJoin = false;
            
            while (_analysisQueue.Count != 0)
            {
                var localAddress = _analysisQueue.Dequeue();
                if (_instructions.ContainsKey(localAddress) || localAddress >= _header.tSize)
                    continue;

                needsJoin = true;

                var data = WordAtLocal(localAddress);
                localAddress += 4;
                var insn = _instructions[localAddress - 4] = DecodeInstruction(data, localAddress);

                if (insn is ConditionalBranchInstruction)
                {
                    data = WordAtLocal(localAddress);
                    localAddress += 4;
                    var insn2 = _instructions[localAddress - 4] = DecodeInstruction(data, localAddress);
                    insn2.IsBranchDelaySlot = true;

                    _analysisQueue.Enqueue(localAddress);

                    continue;
                }

                if (insn is CallPtrInstruction callInsn)
                {
                    data = WordAtLocal(localAddress);
                    localAddress += 4;
                    var insn2 = _instructions[localAddress - 4] = DecodeInstruction(data, localAddress);
                    insn2.IsBranchDelaySlot = true;

                    if (callInsn.ReturnAddressTarget?.Register == Register.ra)
                        _analysisQueue.Enqueue(localAddress);

                    continue;
                }

                _analysisQueue.Enqueue(localAddress);
            }

            logger.Info($"Disassembled {_instructions.Count} instructions, detected {_callees.Count} callees");

            if(needsJoin)
                JoinLoadStoreInstructions();
        }

        private void JoinLoadStoreInstructions()
        {
            uint localAddress = _instructions.Keys.First();
            Debug.Assert(localAddress % 4 == 0);
            uint last = _instructions.Keys.Last();
            Debug.Assert(last % 4 == 0);
            Debug.Assert(last + 4 != 0);

            logger.Info("Joining split load/store instructions");

            uint joinCount = 0;

            for (; localAddress <= last; localAddress += 4)
            {
                if (!_instructions.ContainsKey(localAddress) || !_instructions.ContainsKey(localAddress + 4))
                    continue;

                // only join if the second instruction is not referenced
                if (_callees.Contains(localAddress + 4) || _xrefs.ContainsKey(localAddress + 4))
                    continue;

                var a = _instructions[localAddress];
                var b = _instructions[localAddress + 4];

                // la $x, addr  <==>  lui $at, addr>>16; addiu $x, $at, addr&0xffff
                if (a is DataCopyInstruction && (a.Operands[0] as RegisterOperand)?.Register == Register.at &&
                    a.Operands[1] is ImmediateOperand)
                {
                    var b2 = b as ArithmeticInstruction;
                    if (b2?.Operands[0] is RegisterOperand &&
                        (b2.Operands[1] as RegisterOperand)?.Register == Register.at &&
                        b2.Operands[2] is ImmediateOperand)
                    {
                        uint offs = (uint) (((ImmediateOperand) a.Operands[1]).Value +
                                            ((ImmediateOperand) b2.Operands[2]).Value);
                        _instructions[localAddress] = new DataCopyInstruction(b2.Operands[0], 4, new ImmediateOperand(offs), 4);
                        _instructions[localAddress + 4] = new NopInstruction();
                        localAddress += 4;
                        ++joinCount;
                        continue;
                    }
                }

                // a variation: use in-place register instead of $at
                // la $x, addr  <==>  lui $x, addr>>16; addiu $x, $x, addr&0xffff
                if (a is DataCopyInstruction && b is ArithmeticInstruction
                    && (a.Operands[0] as RegisterOperand)?.Register == (b.Operands[1] as RegisterOperand)?.Register
                    && a.Operands[1] is ImmediateOperand)
                {
                    var b2 = b as ArithmeticInstruction;
                    if (b2.Operands[2] is ImmediateOperand operand && (b2.Operands[0] as RegisterOperand)?.Register ==
                        (b2.Operands[1] as RegisterOperand)?.Register)
                    {
                        uint offs = (uint) (((ImmediateOperand) a.Operands[1]).Value +
                                            operand.Value);
                        _instructions[localAddress] = new DataCopyInstruction(b2.Operands[0], 4, new ImmediateOperand(offs), 4);
                        _instructions[localAddress + 4] = new NopInstruction();
                        localAddress += 4;
                        ++joinCount;
                        continue;
                    }
                }

                // a variation: use temp register instead of $at
                // la $x, addr  <==>  lui $x2, addr>>16; addiu $x, $x2, addr&0xffff
                if (a is DataCopyInstruction && b is ArithmeticInstruction
                    && (a.Operands[0] as RegisterOperand)?.Register == (b.Operands[1] as RegisterOperand)?.Register
                    && a.Operands[1] is ImmediateOperand)
                {
                    var b2 = b as ArithmeticInstruction;
                    if (b2.Operands[2] is ImmediateOperand operand)
                    {
                        uint offs = (uint) (((ImmediateOperand) a.Operands[1]).Value +
                                            operand.Value);
                        _instructions[localAddress + 4] =
                            new DataCopyInstruction(b2.Operands[0], 4, new ImmediateOperand(offs), 4);
                        localAddress += 4;
                        ++joinCount;
                        continue;
                    }
                }

                // lw $x, addr  <==>  lui $at, addr>>16; lw $x, (addr&0xffff)($at)
                if (a is DataCopyInstruction && b is DataCopyInstruction
                    && (a.Operands[0] as RegisterOperand)?.Register == Register.at
                    && a.Operands[1] is ImmediateOperand)
                {
                    var b2 = b as DataCopyInstruction;
                    if (b2.Operands[0] is RegisterOperand &&
                        (b2.Operands[1] as RegisterOffsetOperand)?.Register == Register.at)
                    {
                        uint offs = (uint) (((ImmediateOperand) a.Operands[1]).Value +
                                            ((RegisterOffsetOperand) b.Operands[1]).Offset);

                        var src = new LabelOperand(_debugSource.GetSymbolName(offs), offs);
                        var srcSize = b2.SrcSize;
                        var dstSize = b2.DstSize;

                        _instructions[localAddress] = new DataCopyInstruction(b2.Operands[0], dstSize, src, srcSize);
                        _instructions[localAddress + 4] = new NopInstruction();
                        localAddress += 4;
                        ++joinCount;
                        continue;
                    }
                }

                // a variation: use in-place register instead of $at
                // lw $x, addr  <==>  lui $x, addr>>16; lw $x, (addr&0xffff)($x)
                if (a is DataCopyInstruction && b is DataCopyInstruction
                    && (a.Operands[0] as RegisterOperand)?.Register ==
                    (b.Operands[1] as RegisterOffsetOperand)?.Register
                    && a.Operands[1] is ImmediateOperand)
                {
                    var b2 = b as DataCopyInstruction;
                    if (b2.Operands[0] is RegisterOperand)
                    {
                        uint offs = (uint) (((ImmediateOperand) a.Operands[1]).Value +
                                            ((RegisterOffsetOperand) b.Operands[1]).Offset);

                        var src = new LabelOperand(_debugSource.GetSymbolName(offs), offs);
                        var srcSize = b2.SrcSize;
                        var dstSize = b2.DstSize;

                        _instructions[localAddress] = new DataCopyInstruction(b2.Operands[0], dstSize, src, srcSize);
                        _instructions[localAddress + 4] = new NopInstruction();
                        localAddress += 4;
                        ++joinCount;
                    }
                }
            }

            logger.Info($"Joined {joinCount} load/store instructions");
        }

        public void Dump()
        {
            foreach (var insn in _instructions)
            {
                if (_callees.Contains(insn.Key))
                    logger.Debug("### FUNCTION");
                if (insn.Value is NopInstruction)
                    continue;

                var f = _debugSource.FindFunction(MakeGlobal(insn.Key));

                var xrefsHere = GetGlobalXrefs(MakeGlobal(insn.Key));
                if (xrefsHere != null)
                {
                    logger.Debug("# XRefs:");
                    foreach (var xref in xrefsHere)
                        logger.Debug("# - " + _debugSource.GetSymbolName(xref));
                    var names = GetLocalSymbolNames(insn.Key);
                    if (names != null)
                        foreach (var name in names)
                            logger.Debug(name + ":");
                    else
                        logger.Debug(_debugSource.GetSymbolName(insn.Key) + ":");
                }

                if (f != null)
                    logger.Debug(f.GetSignature());

                logger.Debug($"  0x{insn.Key:X}  {insn.Value.AsReadable()}");
            }
        }

        private IOperand MakeGpBasedOperand(uint data, int shift, int offset)
        {
            var regofs = new RegisterOffsetOperand(data, shift, offset);
            if (_gpBase == null)
                return regofs;

            if (regofs.Register == Register.gp)
                return new LabelOperand(_debugSource.GetSymbolName(_gpBase.Value, regofs.Offset),
                    (uint) (_gpBase.Value + regofs.Offset));

            return regofs;
        }

        private Instruction DecodeInstruction(uint data, uint localAddress)
        {
            switch (ExtractOpcode(data))
            {
                case Opcode.RegisterFormat:
                    return DecodeRegisterFormat(data);
                case Opcode.PCRelative:
                    return DecodePcRelative(localAddress, data);
                case Opcode.j:
                    AddLocalXref(localAddress - 4, (data & 0x03FFFFFF) << 2);
                    _analysisQueue.Enqueue((data & 0x03FFFFFF) << 2);
                    return new CallPtrInstruction(
                        new LabelOperand(_debugSource.GetSymbolName((data & 0x03FFFFFF) << 2),
                            (data & 0x03FFFFFF) << 2),
                        null);
                case Opcode.jal:
                    AddLocalCall(localAddress - 4, (data & 0x03FFFFFF) << 2);
                    _analysisQueue.Enqueue((data & 0x03FFFFFF) << 2);
                    return new CallPtrInstruction(
                        new LabelOperand(_debugSource.GetSymbolName((data & 0x03FFFFFF) << 2),
                            (data & 0x03FFFFFF) << 2),
                        new RegisterOperand(Register.ra));
                case Opcode.beq:
                    AddLocalXref(localAddress - 4, (uint) ((localAddress + (short) data) << 2));
                    _analysisQueue.Enqueue(localAddress + (uint) ((short) data << 2));
                    if (((data >> 16) & 0x1F) == 0)
                        return new ConditionalBranchInstruction(Operator.Equal,
                            new RegisterOperand(data, 21),
                            new ImmediateOperand(0),
                            new LabelOperand(_debugSource.GetSymbolName(localAddress, (short) data << 2),
                                (uint) (localAddress + ((short) data << 2))));
                    else
                        return new ConditionalBranchInstruction(Operator.Equal,
                            new RegisterOperand(data, 21),
                            new RegisterOperand(data, 16),
                            new LabelOperand(_debugSource.GetSymbolName(localAddress, (short) data << 2),
                                (uint) (localAddress + ((short) data << 2))));
                case Opcode.bne:
                    AddLocalXref(localAddress - 4, (uint) ((localAddress + (short) data) << 2));
                    _analysisQueue.Enqueue(localAddress + (uint) ((short) data << 2));
                    if (((data >> 16) & 0x1F) == 0)
                        return new ConditionalBranchInstruction(Operator.NotEqual,
                            new RegisterOperand(data, 21),
                            new ImmediateOperand(0),
                            new LabelOperand(_debugSource.GetSymbolName(localAddress, (short) data << 2),
                                (uint) (localAddress + ((short) data << 2))));
                    else
                        return new ConditionalBranchInstruction(Operator.NotEqual,
                            new RegisterOperand(data, 21),
                            new RegisterOperand(data, 16),
                            new LabelOperand(_debugSource.GetSymbolName(localAddress, (short) data << 2),
                                (uint) (localAddress + ((short) data << 2))));
                case Opcode.blez:
                    AddLocalXref(localAddress - 4, (uint) ((localAddress + (short) data) << 2));
                    _analysisQueue.Enqueue(localAddress + (uint) ((short) data << 2));
                    return new ConditionalBranchInstruction(Operator.LessEqual,
                        new RegisterOperand(data, 21),
                        new ImmediateOperand(0),
                        new LabelOperand(_debugSource.GetSymbolName(localAddress, (short) data << 2),
                            (uint) (localAddress + ((short) data << 2))));
                case Opcode.bgtz:
                    AddLocalXref(localAddress - 4, (uint) ((localAddress + (short) data) << 2));
                    _analysisQueue.Enqueue(localAddress + (uint) ((short) data << 2));
                    return new ConditionalBranchInstruction(Operator.Greater,
                        new RegisterOperand(data, 21),
                        new ImmediateOperand(0),
                        new LabelOperand(_debugSource.GetSymbolName(localAddress, (short) data << 2),
                            (uint) (localAddress + ((short) data << 2))));
                case Opcode.addi:
                    return new ArithmeticInstruction(Operator.Add,
                        new RegisterOperand(data, 16),
                        new RegisterOperand(data, 21),
                        new ImmediateOperand((short) data));
                case Opcode.addiu:
                    if (((data >> 21) & 0x1F) == 0)
                        return new DataCopyInstruction(
                            new RegisterOperand(data, 16), 4,
                            new ImmediateOperand((short) data), 4);
                    else
                        return new ArithmeticInstruction(Operator.Add,
                            new RegisterOperand(data, 16),
                            new RegisterOperand(data, 21),
                            new ImmediateOperand((short) data));
                case Opcode.subi:
                    return new ArithmeticInstruction(Operator.Sub,
                        new RegisterOperand(data, 16),
                        new RegisterOperand(data, 21),
                        new ImmediateOperand((short) data));
                case Opcode.subiu:
                    return new ArithmeticInstruction(Operator.Sub,
                        new RegisterOperand(data, 16),
                        new RegisterOperand(data, 21),
                        new ImmediateOperand((short) data));
                case Opcode.andi:
                    return new ArithmeticInstruction(Operator.BitAnd,
                        new RegisterOperand(data, 16),
                        new RegisterOperand(data, 21),
                        new ImmediateOperand((short) data));
                case Opcode.ori:
                    return new ArithmeticInstruction(Operator.BitOr,
                        new RegisterOperand(data, 16),
                        new RegisterOperand(data, 21),
                        new ImmediateOperand((short) data));
                case Opcode.xori:
                    return new ArithmeticInstruction(Operator.BitXor,
                        new RegisterOperand(data, 16),
                        new RegisterOperand(data, 21),
                        new ImmediateOperand((short) data));
                case Opcode.lui:
                    return new DataCopyInstruction(
                        new RegisterOperand(data, 16), 4,
                        new ImmediateOperand((ushort) data << 16), 4);
                case Opcode.CpuControl:
                    return DecodeCpuControl(localAddress, data);
                case Opcode.FloatingPoint:
                    return new WordData(data);
                case Opcode.lb:
                    return new DataCopyInstruction(
                        new RegisterOperand(data, 16), 4,
                        MakeGpBasedOperand(data, 21, (short) data), 1
                    );
                case Opcode.lh:
                    return new DataCopyInstruction(
                        new RegisterOperand(data, 16), 4,
                        MakeGpBasedOperand(data, 21, (short) data), 2
                    );
                case Opcode.lwl:
                    return new SimpleInstruction("lwl", null, new RegisterOperand(data, 16),
                        MakeGpBasedOperand(data, 21, (short) data));
                case Opcode.lw:
                    return new DataCopyInstruction(
                        new RegisterOperand(data, 16), 4,
                        MakeGpBasedOperand(data, 21, (short) data), 4);
                case Opcode.lbu:
                    return new DataCopyInstruction(
                        new RegisterOperand(data, 16), 4,
                        MakeGpBasedOperand(data, 21, (short) data), 1
                    );
                case Opcode.lhu:
                    return new DataCopyInstruction(
                        new RegisterOperand(data, 16), 4,
                        MakeGpBasedOperand(data, 21, (short) data), 2
                    );
                case Opcode.lwr:
                    return new SimpleInstruction("lwr", null, new RegisterOperand(data, 16),
                        MakeGpBasedOperand(data, 21, (short) data));
                case Opcode.sb:
                    return new DataCopyInstruction(
                        MakeGpBasedOperand(data, 21, (short) data), 1,
                        new RegisterOperand(data, 16), 4
                    );
                case Opcode.sh:
                    return new DataCopyInstruction(
                        MakeGpBasedOperand(data, 21, (short) data), 2,
                        new RegisterOperand(data, 16), 4
                    );
                case Opcode.swl:
                    return new SimpleInstruction("swl", null, new RegisterOperand(data, 16),
                        MakeGpBasedOperand(data, 21, (short) data));
                case Opcode.sw:
                    return new DataCopyInstruction(
                        MakeGpBasedOperand(data, 21, (short) data), 4,
                        new RegisterOperand(data, 16), 4);
                case Opcode.swr:
                    return new SimpleInstruction("swr", null, new RegisterOperand(data, 16),
                        MakeGpBasedOperand(data, 21, (short) data));
                case Opcode.swc1:
                    return new SimpleInstruction("swc1", null, new RegisterOperand(data, 16),
                        new ImmediateOperand((short) data), new RegisterOperand(data, 21));
                case Opcode.lwc1:
                    return new SimpleInstruction("lwc1", null, new C2RegisterOperand(data, 16),
                        new ImmediateOperand((short) data), new RegisterOperand(data, 21));
                case Opcode.cop0:
                    return new SimpleInstruction("cop0", null, new ImmediateOperand(data & ((1 << 26) - 1)));
                case Opcode.cop1:
                    return new SimpleInstruction("cop1", null, new ImmediateOperand(data & ((1 << 26) - 1)));
                case Opcode.cop2:
                    return DecodeCop2(data);
                case Opcode.cop3:
                    return new SimpleInstruction("cop3", null, new ImmediateOperand(data & ((1 << 26) - 1)));
                case Opcode.beql:
                    AddLocalXref(localAddress - 4, (uint) ((localAddress + (short) data) << 2));
                    _analysisQueue.Enqueue(localAddress + (uint) ((short) data << 2));
                    return new ConditionalBranchInstruction(Operator.Equal,
                        new RegisterOperand(data, 21),
                        new RegisterOperand(data, 16),
                        new LabelOperand(_debugSource.GetSymbolName(localAddress, (short) data << 2),
                            (uint) (localAddress + ((short) data << 2))));
                case Opcode.bnel:
                    AddLocalXref(localAddress - 4, (uint) ((localAddress + (short) data) << 2));
                    _analysisQueue.Enqueue(localAddress + (uint) ((short) data << 2));
                    return new ConditionalBranchInstruction(Operator.NotEqual,
                        new RegisterOperand(data, 21),
                        new RegisterOperand(data, 16),
                        new LabelOperand(_debugSource.GetSymbolName(localAddress, (short) data << 2),
                            (uint) (localAddress + ((short) data << 2))));
                case Opcode.blezl:
                    AddLocalXref(localAddress - 4, (uint) ((localAddress + (short) data) << 2));
                    _analysisQueue.Enqueue(localAddress + (uint) ((short) data << 2));
                    return new ConditionalBranchInstruction(Operator.SignedLessEqual,
                        new RegisterOperand(data, 21),
                        new ImmediateOperand(0),
                        new LabelOperand(_debugSource.GetSymbolName(localAddress, (short) data << 2),
                            (uint) (localAddress + ((short) data << 2))));
                case Opcode.bgtzl:
                    AddLocalXref(localAddress - 4, (uint) ((localAddress + (short) data) << 2));
                    _analysisQueue.Enqueue(localAddress + (uint) ((short) data << 2));
                    return new ConditionalBranchInstruction(Operator.Greater,
                        new RegisterOperand(data, 21),
                        new ImmediateOperand(0),
                        new LabelOperand(_debugSource.GetSymbolName(localAddress, (short) data << 2),
                            (uint) (localAddress + ((short) data << 2))));
                default:
                    return new WordData(data);
            }
        }

        private static Instruction DecodeRegisterFormat(uint data)
        {
            var rd = new RegisterOperand(data, 11);
            var rs2 = new RegisterOperand(data, 16);
            var rs1 = new RegisterOperand(data, 21);
            switch ((OpcodeFunction) (data & 0x3f))
            {
                case OpcodeFunction.sll:
                    if (data == 0)
                        return new NopInstruction();
                    else
                        return new ArithmeticInstruction(Operator.Shl,
                            rd, rs2,
                            new ImmediateOperand((int) (data >> 6) & 0x1F));
                case OpcodeFunction.srl:
                    return new ArithmeticInstruction(Operator.Shr,
                        rd, rs2,
                        new ImmediateOperand((int) (data >> 6) & 0x1F));
                case OpcodeFunction.sra:
                    return new ArithmeticInstruction(Operator.Sar,
                        rd, rs2,
                        new ImmediateOperand((int) (data >> 6) & 0x1F));
                case OpcodeFunction.sllv:
                    return new ArithmeticInstruction(Operator.Shl,
                        rd, rs2,
                        rs1);
                case OpcodeFunction.srlv:
                    return new ArithmeticInstruction(Operator.Shr,
                        rd, rs2,
                        rs1);
                case OpcodeFunction.srav:
                    return new ArithmeticInstruction(Operator.Sar,
                        rd, rs2,
                        rs1);
                case OpcodeFunction.jr:
                    return new CallPtrInstruction(rs1, null);
                case OpcodeFunction.jalr:
                    return new CallPtrInstruction(rs1, rd);
                case OpcodeFunction.syscall:
                    return new SimpleInstruction("syscall", "trap(SYSCALL, {0})",
                        new ImmediateOperand((int) (data >> 6) & 0xFFFFF));
                case OpcodeFunction.break_:
                    return new SimpleInstruction("break", "trap(BREAK, {0})",
                        new ImmediateOperand((int) (data >> 6) & 0xFFFFF));
                case OpcodeFunction.mfhi:
                    return new SimpleInstruction("mfhi", "{0} = __DIV_REMAINDER_OR_MULT_HI()",
                        rd);
                case OpcodeFunction.mthi:
                    return new SimpleInstruction("mthi", "__LOAD_DIV_REMAINDER_OR_MULT_HI({0})",
                        rd);
                case OpcodeFunction.mflo:
                    return new SimpleInstruction("mflo", "{0} = __DIV_OR_MULT_LO()",
                        rd);
                case OpcodeFunction.mtlo:
                    return new SimpleInstruction("mtlo", "__LOAD_DIV_OR_MULT_LO({0})",
                        rd);
                case OpcodeFunction.mult:
                    return new SimpleInstruction("mult", "__MULT((signed){0}, (signed){1})",
                        rs1, rs2);
                case OpcodeFunction.multu:
                    return new SimpleInstruction("multu", "__MULT((unsigned){0}, (unsigned){1})",
                        rs1, rs2);
                case OpcodeFunction.div:
                    return new SimpleInstruction("div", "__DIV((signed){0}, (signed){1})",
                        rs1, rs2);
                case OpcodeFunction.divu:
                    return new SimpleInstruction("divu", "__DIV((unsigned){0}, (unsigned){1})",
                        rs1, rs2);
                case OpcodeFunction.add:
                    return new ArithmeticInstruction(Operator.Add,
                        rd, rs1, rs2);
                case OpcodeFunction.addu:
                    if (rs2.Register == Register.zero)
                        return new DataCopyInstruction(rd, 4, rs1, 4);
                    else
                        return new ArithmeticInstruction(Operator.Add, rd, rs1, rs2);
                case OpcodeFunction.sub:
                    return new ArithmeticInstruction(Operator.Sub,
                        rd, rs1, rs2);
                case OpcodeFunction.subu:
                    return new ArithmeticInstruction(Operator.Sub,
                        rd, rs1, rs2);
                case OpcodeFunction.and:
                    return new ArithmeticInstruction(Operator.BitAnd,
                        rd, rs1, rs2);
                case OpcodeFunction.or:
                    return new ArithmeticInstruction(Operator.BitOr,
                        rd, rs1, rs2);
                case OpcodeFunction.xor:
                    return new ArithmeticInstruction(Operator.BitXor,
                        rd, rs1, rs2);
                case OpcodeFunction.nor:
                    return new SimpleInstruction("nor", "{0} = ~({1} | {2})", rd,
                        rs1, rs2);
                case OpcodeFunction.slt:
                    return new SimpleInstruction("slt", "{0} = {1} < {2} ? 1 : 0",
                        rd, rs1,
                        rs2);
                case OpcodeFunction.sltu:
                    return new SimpleInstruction("sltu", "{0} = {1} < {2} ? 1 : 0",
                        rd, rs1,
                        rs2);
                default:
                    return new WordData(data);
            }
        }

        private Instruction DecodeCpuControl(uint localAddress, uint data)
        {
            switch ((CpuControlOpcode) ((data >> 21) & 0x1f))
            {
                case CpuControlOpcode.mtc0:
                    return new SimpleInstruction("mtc0", null, new RegisterOperand(data, 16),
                        new C0RegisterOperand(data, 11));
                case CpuControlOpcode.bc0:
                    switch ((data >> 16) & 0x1f)
                    {
                        case 0:
                            AddLocalXref(localAddress - 4, (uint) ((localAddress + (short) data) << 2));
                            return new SimpleInstruction("bc0f", null,
                                new LabelOperand(_debugSource.GetSymbolName(localAddress, (ushort) data << 2),
                                    (uint) (localAddress + ((short) data << 2))));
                        case 1:
                            AddLocalXref(localAddress - 4, (uint) ((localAddress + (short) data) << 2));
                            return new SimpleInstruction("bc0t", null,
                                new LabelOperand(_debugSource.GetSymbolName(localAddress, (ushort) data << 2),
                                    (uint) (localAddress + ((short) data << 2))));
                        default:
                            return new WordData(data);
                    }
                case CpuControlOpcode.tlb:
                    return DecodeTlb(data);
                case CpuControlOpcode.mfc0:
                    return new SimpleInstruction("mfc0", null, new RegisterOperand(data, 16),
                        new C0RegisterOperand(data, 11));
                default:
                    return new WordData(data);
            }
        }

        private static Instruction DecodeTlb(uint data)
        {
            switch ((TlbOpcode) (data & 0x1f))
            {
                case TlbOpcode.tlbr:
                    return new SimpleInstruction("tlbr", null);
                case TlbOpcode.tlbwi:
                    return new SimpleInstruction("tlbwi", null);
                case TlbOpcode.tlbwr:
                    return new SimpleInstruction("tlbwr", null);
                case TlbOpcode.tlbp:
                    return new SimpleInstruction("tlbp", null);
                case TlbOpcode.rfe:
                    return new SimpleInstruction("rfe", "__RETURN_FROM_EXCEPTION()");
                default:
                    return new WordData(data);
            }
        }

        private Instruction DecodePcRelative(uint localAddress, uint data)
        {
            var rs = new RegisterOperand(data, 21);
            var offset = new LabelOperand(_debugSource.GetSymbolName(localAddress, (ushort) data << 2),
                (uint) (localAddress + ((short) data << 2)));
            switch ((data >> 16) & 0x1f)
            {
                case 0: // bltz
                    AddLocalXref(localAddress - 4, (uint) ((localAddress + (short) data) << 2));
                    _analysisQueue.Enqueue(localAddress + (uint) ((short) data << 2));
                    return new ConditionalBranchInstruction(Operator.SignedLess,
                        rs,
                        new ImmediateOperand(0),
                        offset);
                case 1: // bgez
                    AddLocalXref(localAddress - 4, (uint) ((localAddress + (short) data) << 2));
                    _analysisQueue.Enqueue(localAddress + (uint) ((short) data << 2));
                    return new ConditionalBranchInstruction(Operator.SignedGreaterEqual,
                        rs,
                        new ImmediateOperand(0),
                        offset);
                case 16: // bltzal
                    AddLocalCall(localAddress - 4, (uint) ((localAddress + (short) data) << 2));
                    _analysisQueue.Enqueue(localAddress + (uint) ((short) data << 2));
                    return new ConditionalCallInstruction(Operator.SignedLess,
                        rs,
                        new ImmediateOperand(0),
                        offset);
                case 17: // bgezal
                    AddLocalCall(localAddress - 4, (uint) ((localAddress + (short) data) << 2));
                    _analysisQueue.Enqueue(localAddress + (uint) ((short) data << 2));
                    return new ConditionalCallInstruction(Operator.SignedGreaterEqual,
                        rs,
                        new ImmediateOperand(0),
                        offset);
                default:
                    return new WordData(data);
            }
        }

        private static Instruction DecodeCop2(uint data)
        {
            var opc = data & ((1 << 26) - 1);
            if (((data >> 25) & 1) != 0)
                return DecodeCop2Gte(opc);

            var cf = (opc >> 21) & 0x1F;
            switch (cf)
            {
                case 0:
                    return new SimpleInstruction("mfc2", null, new RegisterOperand(opc, 16),
                        new ImmediateOperand((short) opc), new C2RegisterOperand(opc, 21));
                case 2:
                    return new SimpleInstruction("cfc2", null, new RegisterOperand(opc, 16),
                        new ImmediateOperand((short) opc), new C2RegisterOperand(opc, 21));
                case 4:
                    return new SimpleInstruction("mtc2", null, new RegisterOperand(opc, 16),
                        new ImmediateOperand((short) opc), new C2RegisterOperand(opc, 21));
                case 6:
                    return new SimpleInstruction("ctc2", null, new RegisterOperand(opc, 16),
                        new ImmediateOperand((short) opc), new C2RegisterOperand(opc, 21));
                default:
                    return new WordData(data);
            }
        }

        private static Instruction DecodeCop2Gte(uint data)
        {
            switch (data & 0x1F003FF)
            {
                case 0x0400012:
                    return new SimpleInstruction("mvmva",
                        null,
                        new ImmediateOperand((int) (data >> 19) & 1),
                        new ImmediateOperand((int) (data >> 17) & 3),
                        new ImmediateOperand((int) (data >> 15) & 3),
                        new ImmediateOperand((int) (data >> 13) & 3),
                        new ImmediateOperand((int) (data >> 10) & 1)
                    );
                case 0x0a00428:
                    return new SimpleInstruction("sqr", null,
                        new ImmediateOperand((int) (data >> 19) & 1));
                case 0x170000C:
                    return new SimpleInstruction("op", null,
                        new ImmediateOperand((int) (data >> 19) & 1));
                case 0x190003D:
                    return new SimpleInstruction("gpf", null,
                        new ImmediateOperand((int) (data >> 19) & 1));
                case 0x1A0003E:
                    return new SimpleInstruction("gpl", null,
                        new ImmediateOperand((int) (data >> 19) & 1));
                default:
                    switch (data)
                    {
                        case 0x0180001:
                            return new SimpleInstruction("rtps", null);
                        case 0x0280030:
                            return new SimpleInstruction("rtpt", null);
                        case 0x0680029:
                            return new SimpleInstruction("dcpl", null);
                        case 0x0780010:
                            return new SimpleInstruction("dcps", null);
                        case 0x0980011:
                            return new SimpleInstruction("intpl", null);
                        case 0x0C8041E:
                            return new SimpleInstruction("ncs", null);
                        case 0x0D80420:
                            return new SimpleInstruction("nct", null);
                        case 0x0E80413:
                            return new SimpleInstruction("ncds", null);
                        case 0x0F80416:
                            return new SimpleInstruction("ncdt", null);
                        case 0x0F8002A:
                            return new SimpleInstruction("dpct", null);
                        case 0x108041B:
                            return new SimpleInstruction("nccs", null);
                        case 0x118043F:
                            return new SimpleInstruction("ncct", null);
                        case 0x1280414:
                            return new SimpleInstruction("cdp", null);
                        case 0x138041C:
                            return new SimpleInstruction("cc", null);
                        case 0x1400006:
                            return new SimpleInstruction("nclip", null);
                        case 0x158002D:
                            return new SimpleInstruction("avsz3", null);
                        case 0x168002E:
                            return new SimpleInstruction("avsz4", null);
                        default:
                            return new SimpleInstruction("cop2", null,
                                new ImmediateOperand(data));
                    }
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
