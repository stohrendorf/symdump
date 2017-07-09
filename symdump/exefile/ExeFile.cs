using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using symdump.exefile.disasm;
using symdump.exefile.util;
using symfile;

namespace symdump.exefile
{
    public class ExeFile
    {
        [SuppressMessage("ReSharper", "NotAccessedField.Local")]
        [SuppressMessage("ReSharper", "MemberCanBePrivate.Local")]
        private class Header
        {
            public readonly char[] id;
            public readonly uint text;
            public readonly uint data;
            public readonly uint pc0;
            public readonly uint gp0;
            public readonly uint tAddr;
            public readonly uint tSize;
            public readonly uint dAddr;
            public readonly uint dSize;
            public readonly uint bAddr;
            public readonly uint bSize;
            public readonly uint sAddr;
            public readonly uint sSize;
            public readonly uint savedSp;
            public readonly uint savedFp;
            public readonly uint savedGp;
            public readonly uint savedRa;
            public readonly uint savedS0;

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

        private readonly Header m_header;
        private readonly byte[] m_data;
        private readonly SymFile m_symFile;

        public ExeFile(EndianBinaryReader reader, SymFile symFile)
        {
            m_symFile = symFile;
            reader.baseStream.Seek(0, SeekOrigin.Begin);

            m_header = new Header(reader);
            reader.baseStream.Seek(0x800, SeekOrigin.Begin);
            m_data = reader.readBytes((int) m_header.tSize);
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

        private readonly SortedDictionary<uint, Instruction> m_instructions = new SortedDictionary<uint, Instruction>();
        private readonly Dictionary<uint, HashSet<uint>> m_xrefs = new Dictionary<uint, HashSet<uint>>();

        private readonly Queue<uint> m_analysisQueue = new Queue<uint>();

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
            data = (uint) m_data[ofs++];
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
            {
                m_analysisQueue.Enqueue(addr - m_header.tAddr);
            }

            while (m_analysisQueue.Count != 0)
            {
                var index = m_analysisQueue.Dequeue();
                if (m_instructions.ContainsKey(index) || index >= m_data.Length)
                    continue;

                var data = dataAt(index);
                index += 4;

                Instruction insn;

                switch (extractOpcode(data))
                {
                    case Opcode.RegisterFormat:
                        insn = decodeRegisterFormat(data);
                        break;
                    case Opcode.PCRelative:
                        insn = decodePcRelative(index, data);
                        break;
                    case Opcode.j:
                        addXref(index - 4, (data & 0x03FFFFFF) << 2);
                        insn = new SimpleInstruction("j", "goto {0}",
                            new LabelOperand(getSymbolName((data & 0x03FFFFFF) << 2)));
                        break;
                    case Opcode.jal:
                        addXref(index - 4, (data & 0x03FFFFFF) << 2);
                        insn = new SimpleInstruction("jal", "{0}()",
                            new LabelOperand(getSymbolName((data & 0x03FFFFFF) << 2)));
                        break;
                    case Opcode.beq:
                        addXref(index - 4, (uint) (index + (short) data << 2));
                        if (((data >> 16) & 0x1F) == 0)
                        {
                            insn = new SimpleInstruction("beqz", "if({0} == 0) goto {1}", new RegisterOperand(data, 21),
                                new LabelOperand(getSymbolName(index, ((short) data) << 2)));
                        }
                        else
                        {
                            insn = new SimpleInstruction("beq", "if({0} == {1}) goto {2}",
                                new RegisterOperand(data, 21),
                                new RegisterOperand(data, 16),
                                new LabelOperand(getSymbolName(index, ((short) data) << 2)));
                        }
                        break;
                    case Opcode.bne:
                        addXref(index - 4, (uint) (index + (short) data << 2));
                        if (((data >> 16) & 0x1F) == 0)
                        {
                            insn = new SimpleInstruction("bnez", "if({0} != 0) goto {1}", new RegisterOperand(data, 21),
                                new LabelOperand(getSymbolName(index, ((short) data) << 2)));
                        }
                        else
                        {
                            insn = new SimpleInstruction("bne", "if({0} != {1}) goto {2}",
                                new RegisterOperand(data, 21),
                                new RegisterOperand(data, 16),
                                new LabelOperand(getSymbolName(index, ((short) data) << 2)));
                        }
                        break;
                    case Opcode.blez:
                        addXref(index - 4, (uint) (index + (short) data << 2));
                        insn = new SimpleInstruction("blez", "if({0} <= 0) goto {1}", new RegisterOperand(data, 21),
                            new LabelOperand(getSymbolName(index, ((short) data) << 2)));
                        break;
                    case Opcode.bgtz:
                        addXref(index - 4, (uint) (index + (short) data << 2));
                        insn = new SimpleInstruction("bgtz", "if({0} > 0) goto {1}", new RegisterOperand(data, 21),
                            new LabelOperand(getSymbolName(index, ((short) data) << 2)));
                        break;
                    case Opcode.addi:
                        insn = new SimpleInstruction("addi", "{0} = {1} + {2}", new RegisterOperand(data, 16),
                            new RegisterOperand(data, 21), new ImmediateOperand((short) data));
                        break;
                    case Opcode.addiu:
                        if (((data >> 21) & 0x1F) == 0)
                            insn = new SimpleInstruction("li", "{0} = {1}", new RegisterOperand(data, 16),
                                new ImmediateOperand((short) data));
                        else
                            insn = new SimpleInstruction("addiu", "{0} = {1} + {2}", new RegisterOperand(data, 16),
                                new RegisterOperand(data, 21), new ImmediateOperand((ushort) data));
                        break;
                    case Opcode.subi:
                        insn = new SimpleInstruction("subi", "{0} = (signed)({1} - {2})", new RegisterOperand(data, 16),
                            new RegisterOperand(data, 21), new ImmediateOperand((short) data));
                        break;
                    case Opcode.subiu:
                        insn = new SimpleInstruction("subiu", "{0} = (unsigned)({1} - {2})",
                            new RegisterOperand(data, 16),
                            new RegisterOperand(data, 21), new ImmediateOperand((ushort) data));
                        break;
                    case Opcode.andi:
                        insn = new SimpleInstruction("andi", "{0} = {1} & {2}", new RegisterOperand(data, 16),
                            new RegisterOperand(data, 21), new ImmediateOperand((short) data));
                        break;
                    case Opcode.ori:
                        insn = new SimpleInstruction("ori", "{0} = {1} | {2}", new RegisterOperand(data, 16),
                            new RegisterOperand(data, 21), new ImmediateOperand((short) data));
                        break;
                    case Opcode.xori:
                        insn = new SimpleInstruction("xori", "{0} = {1} ^ {2}", new RegisterOperand(data, 16),
                            new RegisterOperand(data, 21), new ImmediateOperand((short) data));
                        break;
                    case Opcode.lui:
                        insn = new SimpleInstruction("lui", "{0} = {1}",
                            new RegisterOperand(data, 16),
                            new ImmediateOperand(((ushort) data) << 16));
                        break;
                    case Opcode.CpuControl:
                        insn = decodeCpuControl(index, data);
                        break;
                    case Opcode.FloatingPoint:
                        insn = new WordData(data);
                        break;
                    case Opcode.lb:
                        insn = new SimpleInstruction("lb", "{0} = (signed char){1}", new RegisterOperand(data, 16),
                            new RegisterOffsetOperand(data, 21, (short) data));
                        break;
                    case Opcode.lh:
                        insn = new SimpleInstruction("lh", "{0} = (short){1}", new RegisterOperand(data, 16),
                            new RegisterOffsetOperand(data, 21, (short) data));
                        break;
                    case Opcode.lwl:
                        insn = new SimpleInstruction("lwl", null, new RegisterOperand(data, 16),
                            new RegisterOffsetOperand(data, 21, (short) data));
                        break;
                    case Opcode.lw:
                        insn = new SimpleInstruction("lw", "{0} = (int){1}", new RegisterOperand(data, 16),
                            new RegisterOffsetOperand(data, 21, (short) data));
                        break;
                    case Opcode.lbu:
                        insn = new SimpleInstruction("lbu", "{0} = (unsigned char){1}", new RegisterOperand(data, 16),
                            new RegisterOffsetOperand(data, 21, (short) data));
                        break;
                    case Opcode.lhu:
                        insn = new SimpleInstruction("lhu", "{0} = (unsigned short){1}", new RegisterOperand(data, 16),
                            new RegisterOffsetOperand(data, 21, (short) data));
                        break;
                    case Opcode.lwr:
                        insn = new SimpleInstruction("lwr", null, new RegisterOperand(data, 16),
                            new RegisterOffsetOperand(data, 21, (short) data));
                        break;
                    case Opcode.sb:
                        insn = new SimpleInstruction("sb", "{1} = (char){0}", new RegisterOperand(data, 16),
                            new RegisterOffsetOperand(data, 21, (short) data));
                        break;
                    case Opcode.sh:
                        insn = new SimpleInstruction("sh", "{1} = (short){0}", new RegisterOperand(data, 16),
                            new RegisterOffsetOperand(data, 21, (short) data));
                        break;
                    case Opcode.swl:
                        insn = new SimpleInstruction("swl", null, new RegisterOperand(data, 16),
                            new RegisterOffsetOperand(data, 21, (short) data));
                        break;
                    case Opcode.sw:
                        insn = new SimpleInstruction("sw", "{1} = (int){0}", new RegisterOperand(data, 16),
                            new RegisterOffsetOperand(data, 21, (short) data));
                        break;
                    case Opcode.swr:
                        insn = new SimpleInstruction("swr", null, new RegisterOperand(data, 16),
                            new RegisterOffsetOperand(data, 21, (short) data));
                        break;
                    case Opcode.swc1:
                        insn = new SimpleInstruction("swc1", null, new RegisterOperand(data, 16),
                            new ImmediateOperand((short) data), new RegisterOperand(data, 21));
                        break;
                    case Opcode.lwc1:
                        insn = new SimpleInstruction("lwc1", null, new C2RegisterOperand(data, 16),
                            new ImmediateOperand((short) data), new RegisterOperand(data, 21));
                        break;
                    case Opcode.cop0:
                        insn = new SimpleInstruction("cop0", null, new ImmediateOperand(data & ((1 << 26) - 1)));
                        break;
                    case Opcode.cop1:
                        insn = new SimpleInstruction("cop1", null, new ImmediateOperand(data & ((1 << 26) - 1)));
                        break;
                    case Opcode.cop2:
                        insn = decodeCop2(data);
                        break;
                    case Opcode.cop3:
                        insn = new SimpleInstruction("cop3", null, new ImmediateOperand(data & ((1 << 26) - 1)));
                        break;
                    case Opcode.beql:
                        addXref(index - 4, (uint) (index + (short) data << 2));
                        insn = new SimpleInstruction("beql", "if({0} == {1}) goto {2}",
                            new RegisterOperand(data, 21), new RegisterOperand(data, 16),
                            new LabelOperand(getSymbolName(index, (short) data << 2)));
                        break;
                    case Opcode.bnel:
                        addXref(index - 4, (uint) (index + (short) data << 2));
                        insn = new SimpleInstruction("bnel", "if({0} != {1}) goto {2}",
                            new RegisterOperand(data, 21), new RegisterOperand(data, 16),
                            new LabelOperand(getSymbolName(index, (short) data << 2)));
                        break;
                    case Opcode.blezl:
                        addXref(index - 4, (uint) (index + (short) data << 2));
                        insn = new SimpleInstruction("blezl", "if((signed){0} <= 0) goto {1}",
                            new RegisterOperand(data, 21),
                            new LabelOperand(getSymbolName(index, (short) data << 2)));
                        break;
                    case Opcode.bgtzl:
                        addXref(index - 4, (uint) (index + (short) data << 2));
                        insn = new SimpleInstruction("bgtzl", "if((signed){0} > 0) goto {1}",
                            new RegisterOperand(data, 21),
                            new LabelOperand(getSymbolName(index, (short) data << 2)));
                        break;
                    default:
                        insn = new WordData(data);
                        break;
                }

                m_instructions[index - 4] = insn;
                if (!insn.asReadable().StartsWith("j"))
                    m_analysisQueue.Enqueue(index);
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
                    {
                        Console.WriteLine("# - " + getSymbolName(xref));
                    }
                    var names = getSymbolNames(insn.Key);
                    if (names != null)
                    {
                        foreach (var name in names)
                        {
                            Console.WriteLine(name + ":");
                        }
                    }
                    else
                    {
                        Console.WriteLine(getSymbolName(insn.Key) + ":");
                    }
                }

                if (f != null)
                    Console.WriteLine(f.getSignature());

                Console.WriteLine($"  0x{insn.Key:X}  {insn.Value.asReadable()}");
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
                    {
                        return new SimpleInstruction("sll", "{0} = (signed){1} >> {2}",
                            rd, rs2,
                            new ImmediateOperand((int) (data >> 6) & 0x1F));
                    }
                case OpcodeFunction.srl:
                    return new SimpleInstruction("srl", "{0} = (unsigned){1} >> {2}",
                        rd, rs2,
                        new ImmediateOperand((int) (data >> 6) & 0x1F));
                case OpcodeFunction.sra:
                    return new SimpleInstruction("sra", "{0} = (signed){1} << {2}",
                        rd, rs2,
                        new ImmediateOperand((int) (data >> 6) & 0x1F));
                case OpcodeFunction.sllv:
                    return new SimpleInstruction("sllv", "{0} = (signed){1} << {2}",
                        rd, rs2,
                        rs1);
                case OpcodeFunction.srlv:
                    return new SimpleInstruction("srlv", "{0} = (unsigned){1} >> {2}",
                        rd, rs2,
                        rs1);
                case OpcodeFunction.srav:
                    return new SimpleInstruction("srav", "{0} = (signed){1} >> {2}",
                        rd, rs2,
                        rs1);
                case OpcodeFunction.jr:
                    return new SimpleInstruction("jr", "goto *{0}", rs1);
                case OpcodeFunction.jalr:
                    return new SimpleInstruction("jalr", "{0} = __RET_ADDR; (*{1})()",
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
                    return new SimpleInstruction("add", "{0} = {1} + {2}", rd,
                        rs1, rs2);
                case OpcodeFunction.addu:
                    if (((data >> 16) & 0x1F) == 0)
                        return new SimpleInstruction("move", "{0} = {1}", rd,
                            rs1);
                    else
                        return new SimpleInstruction("addu", "{0} = {1} + {2}", rd,
                            rs1, rs2);
                case OpcodeFunction.sub:
                    return new SimpleInstruction("sub", "{0} = {1} - {2}", rd,
                        rs1, rs2);
                case OpcodeFunction.subu:
                    return new SimpleInstruction("subu", "{0} = {1} - {2}", rd,
                        rs1, rs2);
                case OpcodeFunction.and:
                    return new SimpleInstruction("and", "{0} = {1} & {2}", rd,
                        rs1, rs2);
                case OpcodeFunction.or:
                    return new SimpleInstruction("or", "{0} = {1} | {2}", rd,
                        rs1, rs2);
                case OpcodeFunction.xor:
                    return new SimpleInstruction("xor", "{0} = {1} ^ {2}", rd,
                        rs1, rs2);
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
                            addXref(index - 4, (uint) (index + (short) data << 2));
                            return new SimpleInstruction("bc0f", null,
                                new LabelOperand(getSymbolName(index, ((ushort) data) << 2)));
                        case 1:
                            addXref(index - 4, (uint) (index + (short) data << 2));
                            return new SimpleInstruction("bc0t", null,
                                new LabelOperand(getSymbolName(index, ((ushort) data) << 2)));
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
            var offset = new LabelOperand(getSymbolName(index, ((ushort) data) << 2));
            switch ((data >> 16) & 0x1f)
            {
                case 0:
                    addXref(index - 4, (uint) (index + (short) data << 2));
                    return new SimpleInstruction("bltz", "if((signed){0} < 0) goto {1}",
                        rs,
                        offset);
                case 1:
                    addXref(index - 4, (uint) (index + (short) data << 2));
                    return new SimpleInstruction("bgez", "if((signed){0} >= 0) goto {1}",
                        rs,
                        offset);
                case 16:
                    addXref(index - 4, (uint) (index + (short) data << 2));
                    return new SimpleInstruction("bltzal", "if((signed){0} < 0) {1}()",
                        rs,
                        offset);
                case 17:
                    addXref(index - 4, (uint) (index + (short) data << 2));
                    return new SimpleInstruction("bgezal", "if((signed){0} >= 0) {1}()",
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
    }
}