using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using symdump.exefile.disasm;
using symdump.exefile.instructions;
using symdump.exefile.operands;
using symdump.exefile.util;
using symdump.symfile;

namespace symdump.exefile
{
    public class ExeFile
    {
        private readonly Queue<uint> m_analysisQueue = new Queue<uint>();
        private readonly byte[] m_data;
        private readonly uint? m_gpBase;

        private readonly Header m_header;

        private readonly SortedDictionary<uint, Instruction> m_instructions = new SortedDictionary<uint, Instruction>();
        private readonly SymFile m_symFile;
        private readonly Dictionary<uint, HashSet<uint>> m_xrefs = new Dictionary<uint, HashSet<uint>>();

        public ExeFile(EndianBinaryReader reader, SymFile symFile)
        {
            m_symFile = symFile;
            reader.baseStream.Seek(0, SeekOrigin.Begin);

            m_header = new Header(reader);
            reader.baseStream.Seek(0x800, SeekOrigin.Begin);
            m_data = reader.readBytes((int) m_header.tSize);

            m_gpBase = m_symFile.labels
                .Where(byOffset => byOffset.Value.Any(lbl => lbl.name.Equals("__SN_GP_BASE")))
                .Select(lbl => lbl.Key)
                .FirstOrDefault();
        }

        private string getSymbolName(uint addr, int rel = 0)
        {
            addr = (uint) (addr + rel);

            List<Label> lbls;
            if (!m_symFile.labels.TryGetValue(addr, out lbls))
                return $"lbl_{addr:X}";

            return lbls.First().name;
        }

        private IEnumerable<string> getSymbolNames(uint addr)
        {
            List<Label> lbls;
            m_symFile.labels.TryGetValue(addr + m_header.tAddr, out lbls);
            return lbls?.Select(l => l.name);
        }

        private void addXref(uint from, uint to)
        {
            HashSet<uint> froms;
            if (!m_xrefs.TryGetValue(to, out froms))
                m_xrefs.Add(to, froms = new HashSet<uint>());

            froms.Add(from);

            if (!m_instructions.ContainsKey(to))
                m_analysisQueue.Enqueue(to);
        }

        private HashSet<uint> getXrefs(uint to)
        {
            HashSet<uint> froms;
            m_xrefs.TryGetValue(to, out froms);
            return froms;
        }

        private uint dataAt(uint ofs)
        {
            uint data;
            data = m_data[ofs++];
            data |= (uint) m_data[ofs++] << 8;
            data |= (uint) m_data[ofs++] << 16;
            data |= (uint) m_data[ofs++] << 24;
            return data;
        }

        private static Opcode extractOpcode(uint data)
        {
            return (Opcode) (data >> 26);
        }

        public void disassemble()
        {
            m_analysisQueue.Clear();
            m_analysisQueue.Enqueue(m_header.pc0 - m_header.tAddr);
            foreach (var addr in m_symFile.functions.Select(f => f.address))
                m_analysisQueue.Enqueue(addr - m_header.tAddr);

            while (m_analysisQueue.Count != 0)
            {
                var index = m_analysisQueue.Dequeue();
                if (m_instructions.ContainsKey(index) || index >= m_data.Length)
                    continue;

                var data = dataAt(index);
                index += 4;
                var insn = m_instructions[index - 4] = decodeInstruction(data, index);

                var branchInsn = insn as SimpleBranchInstruction;
                if (branchInsn != null)
                {
                    data = dataAt(index);
                    index += 4;
                    var insn2 = m_instructions[index - 4] = decodeInstruction(data, index);
                    insn2.isBranchDelaySlot = true;

                    if (!branchInsn.isUnconditional)
                        m_analysisQueue.Enqueue(index);
                }
                else
                {
                    m_analysisQueue.Enqueue(index);
                }
            }

            foreach (var insn in m_instructions)
            {
                if (insn.Value.asReadable().Equals("nop"))
                    continue;

                var f = m_symFile.findFunction(insn.Key + m_header.tAddr);
                if (f != null)
                    Console.WriteLine();

                var xrefsHere = getXrefs(insn.Key);
                if (xrefsHere != null)
                {
                    Console.WriteLine("# XRefs:");
                    foreach (var xref in xrefsHere)
                        Console.WriteLine("# - " + getSymbolName(xref));
                    var names = getSymbolNames(insn.Key);
                    if (names != null)
                        foreach (var name in names)
                            Console.WriteLine(name + ":");
                    else
                        Console.WriteLine(getSymbolName(insn.Key) + ":");
                }

                if (f != null)
                    Console.WriteLine(f.getSignature());

                Console.WriteLine($"  0x{insn.Key:X}  {insn.Value.asReadable()}");
            }
        }

        private IOperand makeGpBasedOperand(uint data, int shift, int offset)
        {
            var regofs = new RegisterOffsetOperand(data, shift, offset);
            if (m_gpBase == null)
                return regofs;

            if (regofs.register == Register.gp)
                return new LabelOperand(getSymbolName(m_gpBase.Value, regofs.offset));

            return regofs;
        }

        private Instruction decodeInstruction(uint data, uint index)
        {
            switch (extractOpcode(data))
            {
                case Opcode.RegisterFormat:
                    return decodeRegisterFormat(data);
                case Opcode.PCRelative:
                    return decodePcRelative(index, data);
                case Opcode.j:
                    addXref(index - 4, (data & 0x03FFFFFF) << 2);
                    m_analysisQueue.Enqueue((data & 0x03FFFFFF) << 2);
                    return new SimpleBranchInstruction("j", "goto {0}", true,
                        new LabelOperand(getSymbolName((data & 0x03FFFFFF) << 2)));
                case Opcode.jal:
                    addXref(index - 4, (data & 0x03FFFFFF) << 2);
                    m_analysisQueue.Enqueue((data & 0x03FFFFFF) << 2);
                    return new SimpleBranchInstruction("jal", "{0}()", false,
                        new LabelOperand(getSymbolName((data & 0x03FFFFFF) << 2)));
                case Opcode.beq:
                    addXref(index - 4, (uint) ((index + (short) data) << 2));
                    m_analysisQueue.Enqueue(index + (uint) ((short) data << 2));
                    if (((data >> 16) & 0x1F) == 0)
                        return new SimpleBranchInstruction("beqz", "if({0} == 0) goto {1}", false,
                            new RegisterOperand(data, 21),
                            new LabelOperand(getSymbolName(index, (short) data << 2)));
                    else
                        return new SimpleBranchInstruction("beq", "if({0} == {1}) goto {2}", false,
                            new RegisterOperand(data, 21),
                            new RegisterOperand(data, 16),
                            new LabelOperand(getSymbolName(index, (short) data << 2)));
                case Opcode.bne:
                    addXref(index - 4, (uint) ((index + (short) data) << 2));
                    m_analysisQueue.Enqueue(index + (uint) ((short) data << 2));
                    if (((data >> 16) & 0x1F) == 0)
                        return new SimpleBranchInstruction("bnez", "if({0} != 0) goto {1}", false,
                            new RegisterOperand(data, 21),
                            new LabelOperand(getSymbolName(index, (short) data << 2)));
                    else
                        return new SimpleBranchInstruction("bne", "if({0} != {1}) goto {2}", false,
                            new RegisterOperand(data, 21),
                            new RegisterOperand(data, 16),
                            new LabelOperand(getSymbolName(index, (short) data << 2)));
                case Opcode.blez:
                    addXref(index - 4, (uint) ((index + (short) data) << 2));
                    m_analysisQueue.Enqueue(index + (uint) ((short) data << 2));
                    return new SimpleBranchInstruction("blez", "if({0} <= 0) goto {1}", false,
                        new RegisterOperand(data, 21),
                        new LabelOperand(getSymbolName(index, (short) data << 2)));
                case Opcode.bgtz:
                    addXref(index - 4, (uint) ((index + (short) data) << 2));
                    m_analysisQueue.Enqueue(index + (uint) ((short) data << 2));
                    return new SimpleBranchInstruction("bgtz", "if({0} > 0) goto {1}", false,
                        new RegisterOperand(data, 21),
                        new LabelOperand(getSymbolName(index, (short) data << 2)));
                case Opcode.addi:
                    return new ArithmeticInstruction(ArithmeticInstruction.Operation.Add,
                        new RegisterOperand(data, 16),
                        new RegisterOperand(data, 21),
                        new ImmediateOperand((short) data));
                case Opcode.addiu:
                    if (((data >> 21) & 0x1F) == 0)
                        return new SimpleInstruction("li", "{0} = {1}", new RegisterOperand(data, 16),
                            new ImmediateOperand((short) data));
                    else
                        return new ArithmeticInstruction(ArithmeticInstruction.Operation.Add, 
                            new RegisterOperand(data, 16),
                            new RegisterOperand(data, 21),
                            new ImmediateOperand((ushort) data));
                case Opcode.subi:
                    return new ArithmeticInstruction(ArithmeticInstruction.Operation.Sub, 
                        new RegisterOperand(data, 16),
                        new RegisterOperand(data, 21),
                        new ImmediateOperand((short) data));
                case Opcode.subiu:
                    return new ArithmeticInstruction(ArithmeticInstruction.Operation.Sub,
                        new RegisterOperand(data, 16),
                        new RegisterOperand(data, 21),
                        new ImmediateOperand((ushort) data));
                case Opcode.andi:
                    return new ArithmeticInstruction(ArithmeticInstruction.Operation.BitAnd, 
                        new RegisterOperand(data, 16),
                        new RegisterOperand(data, 21),
                        new ImmediateOperand((short) data));
                case Opcode.ori:
                    return new ArithmeticInstruction(ArithmeticInstruction.Operation.BitOr, 
                        new RegisterOperand(data, 16),
                        new RegisterOperand(data, 21),
                        new ImmediateOperand((short) data));
                case Opcode.xori:
                    return new ArithmeticInstruction(ArithmeticInstruction.Operation.BitXor, 
                        new RegisterOperand(data, 16),
                        new RegisterOperand(data, 21),
                        new ImmediateOperand((short) data));
                case Opcode.lui:
                    return new SimpleInstruction("lui", "{0} = {1}",
                        new RegisterOperand(data, 16),
                        new ImmediateOperand((ushort) data << 16));
                case Opcode.CpuControl:
                    return decodeCpuControl(index, data);
                case Opcode.FloatingPoint:
                    return new WordData(data);
                case Opcode.lb:
                    return new SimpleInstruction("lb", "{0} = (signed char){1}", new RegisterOperand(data, 16),
                        makeGpBasedOperand(data, 21, (short) data));
                case Opcode.lh:
                    return new SimpleInstruction("lh", "{0} = (short){1}", new RegisterOperand(data, 16),
                        makeGpBasedOperand(data, 21, (short) data));
                case Opcode.lwl:
                    return new SimpleInstruction("lwl", null, new RegisterOperand(data, 16),
                        makeGpBasedOperand(data, 21, (short) data));
                case Opcode.lw:
                    return new SimpleInstruction("lw", "{0} = (int){1}", new RegisterOperand(data, 16),
                        makeGpBasedOperand(data, 21, (short) data));
                case Opcode.lbu:
                    return new SimpleInstruction("lbu", "{0} = (unsigned char){1}", new RegisterOperand(data, 16),
                        makeGpBasedOperand(data, 21, (short) data));
                case Opcode.lhu:
                    return new SimpleInstruction("lhu", "{0} = (unsigned short){1}", new RegisterOperand(data, 16),
                        makeGpBasedOperand(data, 21, (short) data));
                case Opcode.lwr:
                    return new SimpleInstruction("lwr", null, new RegisterOperand(data, 16),
                        makeGpBasedOperand(data, 21, (short) data));
                case Opcode.sb:
                    return new SimpleInstruction("sb", "{1} = (char){0}", new RegisterOperand(data, 16),
                        makeGpBasedOperand(data, 21, (short) data));
                case Opcode.sh:
                    return new SimpleInstruction("sh", "{1} = (short){0}", new RegisterOperand(data, 16),
                        makeGpBasedOperand(data, 21, (short) data));
                case Opcode.swl:
                    return new SimpleInstruction("swl", null, new RegisterOperand(data, 16),
                        makeGpBasedOperand(data, 21, (short) data));
                case Opcode.sw:
                    return new SimpleInstruction("sw", "{1} = (int){0}", new RegisterOperand(data, 16),
                        makeGpBasedOperand(data, 21, (short) data));
                case Opcode.swr:
                    return new SimpleInstruction("swr", null, new RegisterOperand(data, 16),
                        makeGpBasedOperand(data, 21, (short) data));
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
                    return decodeCop2(data);
                case Opcode.cop3:
                    return new SimpleInstruction("cop3", null, new ImmediateOperand(data & ((1 << 26) - 1)));
                case Opcode.beql:
                    addXref(index - 4, (uint) ((index + (short) data) << 2));
                    m_analysisQueue.Enqueue(index + (uint) ((short) data << 2));
                    return new SimpleBranchInstruction("beql", "if({0} == {1}) goto {2}", false,
                        new RegisterOperand(data, 21), new RegisterOperand(data, 16),
                        new LabelOperand(getSymbolName(index, (short) data << 2)));
                case Opcode.bnel:
                    addXref(index - 4, (uint) ((index + (short) data) << 2));
                    m_analysisQueue.Enqueue(index + (uint) ((short) data << 2));
                    return new SimpleBranchInstruction("bnel", "if({0} != {1}) goto {2}", false,
                        new RegisterOperand(data, 21), new RegisterOperand(data, 16),
                        new LabelOperand(getSymbolName(index, (short) data << 2)));
                case Opcode.blezl:
                    addXref(index - 4, (uint) ((index + (short) data) << 2));
                    m_analysisQueue.Enqueue(index + (uint) ((short) data << 2));
                    return new SimpleBranchInstruction("blezl", "if((signed){0} <= 0) goto {1}", false,
                        new RegisterOperand(data, 21),
                        new LabelOperand(getSymbolName(index, (short) data << 2)));
                case Opcode.bgtzl:
                    addXref(index - 4, (uint) ((index + (short) data) << 2));
                    m_analysisQueue.Enqueue(index + (uint) ((short) data << 2));
                    return new SimpleBranchInstruction("bgtzl", "if((signed){0} > 0) goto {1}", false,
                        new RegisterOperand(data, 21),
                        new LabelOperand(getSymbolName(index, (short) data << 2)));
                default:
                    return new WordData(data);
            }
        }

        private static Instruction decodeRegisterFormat(uint data)
        {
            var rd = new RegisterOperand(data, 11);
            var rs2 = new RegisterOperand(data, 16);
            var rs1 = new RegisterOperand(data, 21);
            switch ((OpcodeFunction) (data & 0x3f))
            {
                case OpcodeFunction.sll:
                    if (data == 0)
                        return new SimpleInstruction("nop", null);
                    else
                        return new ArithmeticInstruction(ArithmeticInstruction.Operation.Shl,
                            rd, rs2,
                            new ImmediateOperand((int) (data >> 6) & 0x1F));
                case OpcodeFunction.srl:
                    return new ArithmeticInstruction(ArithmeticInstruction.Operation.Shr,
                        rd, rs2,
                        new ImmediateOperand((int) (data >> 6) & 0x1F));
                case OpcodeFunction.sra:
                    return new ArithmeticInstruction(ArithmeticInstruction.Operation.Sar,
                        rd, rs2,
                        new ImmediateOperand((int) (data >> 6) & 0x1F));
                case OpcodeFunction.sllv:
                    return new ArithmeticInstruction(ArithmeticInstruction.Operation.Shl,
                        rd, rs2,
                        rs1);
                case OpcodeFunction.srlv:
                    return new ArithmeticInstruction(ArithmeticInstruction.Operation.Shr,
                        rd, rs2,
                        rs1);
                case OpcodeFunction.srav:
                    return new ArithmeticInstruction(ArithmeticInstruction.Operation.Sar,
                        rd, rs2,
                        rs1);
                case OpcodeFunction.jr:
                    return new SimpleBranchInstruction("jr", "goto *{0}", true, rs1);
                case OpcodeFunction.jalr:
                    return new SimpleBranchInstruction("jalr", "{0} = __RET_ADDR; (*{1})()",
                        false, // marked as non-conditional so that analysis continues after call return
                        rd, rs1);
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
                    return new ArithmeticInstruction(ArithmeticInstruction.Operation.Add,
                        rd, rs1, rs2);
                case OpcodeFunction.addu:
                    if (((data >> 16) & 0x1F) == 0)
                        return new SimpleInstruction("move", "{0} = {1}", rd,
                            rs1);
                    else
                        return new ArithmeticInstruction(ArithmeticInstruction.Operation.Add, 
                            rd, rs1, rs2);
                case OpcodeFunction.sub:
                    return new ArithmeticInstruction(ArithmeticInstruction.Operation.Sub, 
                        rd, rs1, rs2);
                case OpcodeFunction.subu:
                    return new ArithmeticInstruction(ArithmeticInstruction.Operation.Sub,
                        rd, rs1, rs2);
                case OpcodeFunction.and:
                    return new ArithmeticInstruction(ArithmeticInstruction.Operation.BitAnd,
                        rd, rs1, rs2);
                case OpcodeFunction.or:
                    return new ArithmeticInstruction(ArithmeticInstruction.Operation.BitOr, 
                        rd, rs1, rs2);
                case OpcodeFunction.xor:
                    return new ArithmeticInstruction(ArithmeticInstruction.Operation.BitXor, 
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

        private Instruction decodeCpuControl(uint index, uint data)
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
                            addXref(index - 4, (uint) ((index + (short) data) << 2));
                            return new SimpleInstruction("bc0f", null,
                                new LabelOperand(getSymbolName(index, (ushort) data << 2)));
                        case 1:
                            addXref(index - 4, (uint) ((index + (short) data) << 2));
                            return new SimpleInstruction("bc0t", null,
                                new LabelOperand(getSymbolName(index, (ushort) data << 2)));
                        default:
                            return new WordData(data);
                    }
                case CpuControlOpcode.tlb:
                    return decodeTlb(data);
                case CpuControlOpcode.mfc0:
                    return new SimpleInstruction("mfc0", null, new RegisterOperand(data, 16),
                        new C0RegisterOperand(data, 11));
                default:
                    return new WordData(data);
            }
        }

        private static Instruction decodeTlb(uint data)
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

        private Instruction decodePcRelative(uint index, uint data)
        {
            var rs = new RegisterOperand(data, 21);
            var offset = new LabelOperand(getSymbolName(index, (ushort) data << 2));
            switch ((data >> 16) & 0x1f)
            {
                case 0:
                    addXref(index - 4, (uint) ((index + (short) data) << 2));
                    m_analysisQueue.Enqueue(index + (uint) ((short) data << 2));
                    return new SimpleBranchInstruction("bltz", "if((signed){0} < 0) goto {1}", false,
                        rs,
                        offset);
                case 1:
                    addXref(index - 4, (uint) ((index + (short) data) << 2));
                    m_analysisQueue.Enqueue(index + (uint) ((short) data << 2));
                    return new SimpleBranchInstruction("bgez", "if((signed){0} >= 0) goto {1}", false,
                        rs,
                        offset);
                case 16:
                    addXref(index - 4, (uint) ((index + (short) data) << 2));
                    m_analysisQueue.Enqueue(index + (uint) ((short) data << 2));
                    return new SimpleBranchInstruction("bltzal", "if((signed){0} < 0) {1}()", false,
                        rs,
                        offset);
                case 17:
                    addXref(index - 4, (uint) ((index + (short) data) << 2));
                    m_analysisQueue.Enqueue(index + (uint) ((short) data << 2));
                    return new SimpleBranchInstruction("bgezal", "if((signed){0} >= 0) {1}()", false,
                        rs,
                        offset);
                default:
                    return new WordData(data);
            }
        }

        private static Instruction decodeCop2(uint data)
        {
            var opc = data & ((1 << 26) - 1);
            if (((data >> 25) & 1) != 0)
                return decodeCop2Gte(opc);

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

        private static Instruction decodeCop2Gte(uint data)
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
                id = reader.readBytes(8).Select(b => (char) b).ToArray();

                if (!"PS-X EXE".Equals(new string(id)))
                    throw new Exception("Header ID mismatch");

                text = reader.readUInt32();
                data = reader.readUInt32();
                pc0 = reader.readUInt32();
                gp0 = reader.readUInt32();
                tAddr = reader.readUInt32();
                tSize = reader.readUInt32();
                dAddr = reader.readUInt32();
                dSize = reader.readUInt32();
                bAddr = reader.readUInt32();
                bSize = reader.readUInt32();
                sAddr = reader.readUInt32();
                sSize = reader.readUInt32();
                savedSp = reader.readUInt32();
                savedFp = reader.readUInt32();
                savedGp = reader.readUInt32();
                savedRa = reader.readUInt32();
                savedS0 = reader.readUInt32();
            }
        }
    }
}