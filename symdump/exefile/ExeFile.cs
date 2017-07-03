using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using symdump.exefile.disasm;
using symdump.exefile.util;
using symfile;

namespace symdump.exefile
{
    public class ExeFile
    {
        class Header
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
                id = reader.readBytes(8).Select(b => (char)b).ToArray();

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
        private readonly Dictionary<int, List<Label>> m_labels;

        public ExeFile(EndianBinaryReader reader, Dictionary<int, List<Label>> labels)
        {
            this.m_labels = labels;
            reader.baseStream.Seek(0, SeekOrigin.Begin);

            m_header = new Header(reader);
            m_data = reader.readBytes((int) m_header.tSize);
        }

        private string getSymbolName(int addr, int rel = 0)
        {
            addr += rel;
            
            List<Label> lbls;
            if (!m_labels.TryGetValue(addr, out lbls))
                return $"lbl_{addr:X}";

            return lbls.First().name;
        }
        
        public void disassemble()
        {
            var size = m_data.Length;
            var index = 0;

            while (size > 0)
            {
                {
                    List<Label> lbls;
                    if (m_labels.TryGetValue(index, out lbls))
                        foreach (var lbl in lbls)
                        {
                            Console.WriteLine($"{lbl.name}:");
                        }
                }
                
                
                uint data;
                data = (uint) m_data[index++];
                data |= (uint) m_data[index++] << 8;
                data |= (uint) m_data[index++] << 16;
                data |= (uint) m_data[index++] << 24;
                size -= 4;

                var code1 = (data >> 29) & 0x7;
                var code2 = (data >> 26) & 0x7;

                switch (code1)
                {
                    case 0:
                        switch (code2)
                        {
                            case 0:
                                //	SPECIAL function
                            {
                                var code3 = (data >> 3) & 0x7;
                                var code4 = (data >> 0) & 0x7;

                                switch (code3)
                                {
                                    case 0:
                                        switch (code4)
                                        {
                                            case 0:
                                                if (data == 0)
                                                    Console.Write("nop");
                                                else
                                                    Console.Write(
                                                        $"sll\t{(Register) ((data >> 11) & 0x1F)}, {(Register) ((data >> 16) & 0x1F)}, {(data >> 6) & 0x1F}");
                                                break;
                                            case 2:
                                                Console.Write(
                                                    $"srl\t{(Register) ((data >> 11) & 0x1F)}, {(Register) ((data >> 16) & 0x1F)}, {(data >> 6) & 0x1F}");
                                                break;
                                            case 3:
                                                Console.Write(
                                                    $"sra\t{(Register) ((data >> 11) & 0x1F)}, {(Register) ((data >> 16) & 0x1F)}, {(data >> 6) & 0x1F}");
                                                break;
                                            case 4:
                                                Console.Write(
                                                    $"sllv\t{(Register) ((data >> 11) & 0x1F)}, {(Register) ((data >> 16) & 0x1F)}, {(Register) ((data >> 21) & 0x1F)}");
                                                break;
                                            case 6:
                                                Console.Write(
                                                    $"srlv\t{(Register) ((data >> 11) & 0x1F)}, {(Register) ((data >> 16) & 0x1F)}, {(Register) ((data >> 21) & 0x1F)}");
                                                break;
                                            case 7:
                                                Console.Write(
                                                    $"srav\t{(Register) ((data >> 11) & 0x1F)}, {(Register) ((data >> 16) & 0x1F)}, {(Register) ((data >> 21) & 0x1F)}");
                                                break;
                                            default:
                                                Console.Write($"DW\t${data:X}");
                                                break;
                                        }
                                        break;
                                    case 1:
                                        switch (code4)
                                        {
                                            case 0:
                                                Console.Write($"jr\t{(Register) ((data >> 21) & 0x1F)}");
                                                break;
                                            case 1:
                                                Console.Write(
                                                    $"jalr\t{(Register) ((data >> 11) & 0x1F)},{(Register) ((data >> 21) & 0x1F)}");
                                                break;
                                            case 4:
                                                Console.Write("syscall\t${0:X}", (data >> 6) & 0xFFFFF);
                                                break;
                                            case 5:
                                                Console.Write("break\t${0:X}", (data >> 6) & 0xFFFFF);
                                                break;
                                            default:
                                                Console.Write($"DW\t${data:X}");
                                                break;
                                        }
                                        break;
                                    case 2:
                                        switch (code4)
                                        {
                                            case 0:
                                                Console.Write($"mfhi\t{(Register)((data >> 11) & 0x1F)}");
                                                break;
                                            case 1:
                                                Console.Write($"mthi\t{(Register)((data >> 11) & 0x1F)}");
                                                break;
                                            case 2:
                                                Console.Write($"mflo\t{(Register)((data >> 11) & 0x1F)}");
                                                break;
                                            case 3:
                                                Console.Write($"mtlo\t{(Register)((data >> 11) & 0x1F)}");
                                                break;
                                            default:
                                                Console.Write($"DW\t${data:X}");
                                                break;
                                        }
                                        break;
                                    case 3:
                                        switch (code4)
                                        {
                                            case 0:
                                                Console.Write("mult\t,{0}", (Register)((data >> 21) & 0x1F));
                                                break;
                                            case 1:
                                                Console.Write("multu\t{0},{1}", (Register)((data >> 21) & 0x1F),
                                                    (Register)((data >> 16) & 0x1F));
                                                break;
                                            case 2:
                                                Console.Write("div\t{0},{1}", (Register)((data >> 21) & 0x1F),
                                                    (Register)((data >> 16) & 0x1F));
                                                break;
                                            case 3:
                                                Console.Write("divu\t{0},{1}", (Register)((data >> 21) & 0x1F),
                                                    (Register)((data >> 16) & 0x1F));
                                                break;
                                            default:
                                                Console.Write($"DW\t${data:X}");
                                                break;
                                        }
                                        break;
                                    case 4:
                                        switch (code4)
                                        {
                                            case 0:
                                                Console.Write($"add\t{(Register)((data >> 11) & 0x1F)}, {(Register)((data >> 21) & 0x1F)}, {(Register)((data >> 16) & 0x1F)}");
                                                break;
                                            case 1:
                                                if (((data >> 16) & 0x1F) == 0)
                                                    Console.Write($"move\t{(Register)((data >> 11) & 0x1F)}, {(Register)((data >> 21) & 0x1F)}");
                                                else
                                                    Console.Write($"addu\t{(Register)((data >> 11) & 0x1F)}, {(Register)((data >> 21) & 0x1F)}, {(Register)((data >> 16) & 0x1F)}");
                                                break;
                                            case 2:
                                                Console.Write($"sub\t{(Register)((data >> 11) & 0x1F)}, {(Register)((data >> 21) & 0x1F)}, {(Register)((data >> 16) & 0x1F)}");
                                                break;
                                            case 3:
                                                Console.Write($"subu\t{(Register)((data >> 11) & 0x1F)}, {(Register)((data >> 21) & 0x1F)}, {(Register)((data >> 16) & 0x1F)}");
                                                break;
                                            case 4:
                                                Console.Write($"and\t{(Register)((data >> 11) & 0x1F)}, {(Register)((data >> 21) & 0x1F)}, {(Register)((data >> 16) & 0x1F)}");
                                                break;
                                            case 5:
                                                Console.Write($"or\t{(Register)((data >> 11) & 0x1F)}, {(Register)((data >> 21) & 0x1F)}, {(Register)((data >> 16) & 0x1F)}");
                                                break;
                                            case 6:
                                                Console.Write($"xor\t{(Register)((data >> 11) & 0x1F)}, {(Register)((data >> 21) & 0x1F)}, {(Register)((data >> 16) & 0x1F)}");
                                                break;
                                            case 7:
                                                Console.Write($"nor\t{(Register)((data >> 11) & 0x1F)}, {(Register)((data >> 21) & 0x1F)}, {(Register)((data >> 16) & 0x1F)}");
                                                break;
                                        }
                                        break;
                                    case 5:
                                        switch (code4)
                                        {
                                            case 2:
                                                Console.Write($"slt\t{(Register)((data >> 11) & 0x1F)}, {(Register)((data >> 21) & 0x1F)}, {(Register)((data >> 16) & 0x1F)}");
                                                break;
                                            case 3:
                                                Console.Write($"sltu\t{(Register)((data >> 11) & 0x1F)}, {(Register)((data >> 21) & 0x1F)}, {(Register)((data >> 16) & 0x1F)}");
                                                break;
                                            default:
                                                Console.Write($"DW\t${data:X}");
                                                break;
                                        }
                                        break;
                                    case 6:
                                        switch (code4)
                                        {
                                            case 0:
                                                Console.Write($"tge\t{(Register)((data >> 21) & 0x1F)}, {(Register)((data >> 16) & 0x1F)}");
                                                break;
                                            case 1:
                                                Console.Write($"tgeu\t{(Register)((data >> 21) & 0x1F)}, {(Register)((data >> 16) & 0x1F)}");
                                                break;
                                            case 2:
                                                Console.Write($"tlt\t{(Register)((data >> 21) & 0x1F)}, {(Register)((data >> 16) & 0x1F)}");
                                                break;
                                            case 3:
                                                Console.Write($"tltu\t{(Register)((data >> 21) & 0x1F)}, {(Register)((data >> 16) & 0x1F)}");
                                                break;
                                            case 4:
                                                Console.Write($"teq\t{(Register)((data >> 21) & 0x1F)}, {(Register)((data >> 16) & 0x1F)}");
                                                break;
                                            case 6:
                                                Console.Write($"tne\t{(Register)((data >> 21) & 0x1F)}, {(Register)((data >> 16) & 0x1F)}");
                                                break;
                                            default:
                                                Console.Write($"DW\t${data:X}");
                                                break;
                                        }
                                        break;
                                    default:
                                        Console.Write($"DW\t${data:X}");
                                        break;
                                }
                            }
                                break;
                            case 1:
                                //	REGIMM function
                            {
                                var code3 = (data >> 19) & 0x3;
                                var code4 = (data >> 16) & 0x7;

                                switch (code3)
                                {
                                    case 0:
                                        switch (code4)
                                        {
                                            case 0:
                                                Console.Write("bltz\t{0},{1}", (Register)((data >> 21) & 0x1F),
                                                    getSymbolName(index, ((short) data) << 2));
                                                break;
                                            case 1:
                                                Console.Write("bgez\t{0},{1}", (Register)((data >> 21) & 0x1F),
                                                    getSymbolName(index, ((short) data) << 2));
                                                break;
                                            case 2:
                                                Console.Write("bltzl\t{0},{1}", (Register)((data >> 21) & 0x1F),
                                                    getSymbolName(index, ((short) data) << 2));
                                                break;
                                            case 3:
                                                Console.Write("bgezl\t{0},{1}", (Register)((data >> 21) & 0x1F),
                                                    getSymbolName(index, ((short) data) << 2));
                                                break;
                                            default:
                                                Console.Write($"DW\t${data:X}");
                                                break;
                                        }
                                        break;
                                    default:
                                        Console.Write($"DW\t${data:X}");
                                        break;
                                }
                                break;
                            }
                            case 2:
                                Console.Write("j\t{0}", ((data & 0x03FFFFFF) << 2));
                                break;
                            case 3:
                                Console.Write("jal\t{0}", getSymbolName((int)(data & 0x03FFFFFF) << 2));
                                break;
                            case 4:
                                if (((data >> 16) & 0x1F) == 0)
                                    Console.Write("beqz\t{0},{1}", (Register)((data >> 21) & 0x1F),
                                        getSymbolName(index, ((short) data) << 2));
                                else
                                    Console.Write("beq\t{0},{1},{2}", (Register)((data >> 21) & 0x1F), (Register)((data >> 16) & 0x1F),
                                        getSymbolName(index, ((short) data) << 2));
                                break;
                            case 5:
                                if (((data >> 16) & 0x1F) == 0)
                                    Console.Write("bnez\t{0},{1}", (Register)((data >> 21) & 0x1F),
                                        getSymbolName(index, ((short) data) << 2));
                                else
                                    Console.Write("bne\t{0},{1},{2}", (Register)((data >> 21) & 0x1F), (Register)((data >> 16) & 0x1F),
                                        getSymbolName(index, ((short) data) << 2));
                                break;
                            case 6:
                                Console.Write("blez\t{0},{1}", (Register)((data >> 21) & 0x1F),
                                    getSymbolName(index, ((short) data) << 2));
                                break;
                            case 7:
                                Console.Write("bgtz\t{0},{1}", (Register)((data >> 21) & 0x1F),
                                    getSymbolName(index, ((short) data) << 2));
                                break;
                        }
                        break;
                    case 1:
                        switch (code2)
                        {
                            case 0:
                                Console.Write("addi\t{0},{1},{2}", (Register)((data >> 16) & 0x1F), (Register)((data >> 21) & 0x1F),
                                    (short) data);
                                break;
                            case 1:
                                if (((data >> 21) & 0x1F) == 0)
                                    Console.Write("li\t{0},{1}", (Register)((data >> 16) & 0x1F),
                                        (short) data);
                                else
                                    Console.Write("addiu\t{0},{1},{2}", (Register)((data >> 16) & 0x1F), (Register)((data >> 21) & 0x1F),
                                        (short) data);
                                break;
                            case 2:
                                Console.Write("slti\t{0},{1},{2}", (Register)((data >> 16) & 0x1F), (Register)((data >> 21) & 0x1F),
                                    (short) data);
                                break;
                            case 3:
                                Console.Write("sltiu\t{0},{1},{2}", (Register)((data >> 16) & 0x1F), (Register)((data >> 21) & 0x1F),
                                    (short) data);
                                break;
                            case 4:
                                Console.Write("andi\t{0},{1},{2}", (Register)((data >> 16) & 0x1F), (Register)((data >> 21) & 0x1F),
                                    (ushort) data);
                                break;
                            case 5:
                                Console.Write("ori\t{0},{1},{2}", (Register)((data >> 16) & 0x1F), (Register)((data >> 21) & 0x1F),
                                    (ushort) data);
                                break;
                            case 6:
                                Console.Write("xori\t{0},{1},{2}", (Register)((data >> 16) & 0x1F), (Register)((data >> 21) & 0x1F),
                                    (ushort) data);
                                break;
                            case 7:
                                Console.Write("lui\t{0},{1}", (Register)((data >> 16) & 0x1F),
                                    (ushort) data);
                                break;
                        }
                        break;
                    case 2:
                        switch (code2)
                        {
                            case 0:
                            {
                                var code3 = (data >> 21) & 0x1f;

                                switch (code3)
                                {
                                    case 0:
                                        Console.Write("mfc0\t{0},{1}", (Register)((data >> 16) & 0x1F),
                                            (C0Register)((data >> 11) & 0x1F));
                                        break;
                                    case 4:
                                        Console.Write("mtc0\t{0},{1}", (Register)((data >> 16) & 0x1F),
                                            (C0Register)((data >> 11) & 0x1F));
                                        break;
                                    case 16:
                                        if ((data & 0x1f) == 16)
                                            Console.Write("rfe");
                                        else
                                            Console.Write($"DW\t${data:X}");
                                        break;
                                    default:
                                        Console.Write($"DW\t${data:X}");
                                        break;
                                }
                            }
                                break;
                            case 2:
                            {
                                var code3 = (data >> 25) & 0x1;

                                if (code3 == 1)
                                {
                                    data &= 0x1FFFFFF;

                                    switch ((data & 0x1F003FF))
                                    {
                                        case 0x0400012:
                                            Console.Write("mvmva\t{0},{1},{2},{3},{4}", (data >> 19) & 1, (data >> 17) & 3,
                                                (data >> 15) & 3, (data >> 13) & 3, (data >> 10) & 1); //	sf,mx,v,cv,lm
                                            break;
                                        case 0x0a00428:
                                            Console.Write("sqr\t{0}", (data >> 19) & 1);
                                            break;
                                        case 0x170000C:
                                            Console.Write("op\t{0}", (data >> 19) & 1);
                                            break;
                                        case 0x190003D:
                                            Console.Write("gpf\t{0}", (data >> 19) & 1);
                                            break;
                                        case 0x1A0003E:
                                            Console.Write("gpl\t{0}", (data >> 19) & 1);
                                            break;
                                        default:
                                            switch (data)
                                            {
                                                case 0x0180001:
                                                    Console.Write("rtps");
                                                    break;
                                                case 0x0280030:
                                                    Console.Write("rtpt");
                                                    break;
                                                case 0x0680029:
                                                    Console.Write("dcpl");
                                                    break;
                                                case 0x0780010:
                                                    Console.Write("dcps");
                                                    break;
                                                case 0x0980011:
                                                    Console.Write("intpl");
                                                    break;
                                                case 0x0C8041E:
                                                    Console.Write("ncs");
                                                    break;
                                                case 0x0D80420:
                                                    Console.Write("nct");
                                                    break;
                                                case 0x0E80413:
                                                    Console.Write("ncds");
                                                    break;
                                                case 0x0F80416:
                                                    Console.Write("ncdt");
                                                    break;
                                                case 0x0F8002A:
                                                    Console.Write("dpct");
                                                    break;
                                                case 0x108041B:
                                                    Console.Write("nccs");
                                                    break;
                                                case 0x118043F:
                                                    Console.Write("ncct");
                                                    break;
                                                case 0x1280414:
                                                    Console.Write("cdp");
                                                    break;
                                                case 0x138041C:
                                                    Console.Write("cc");
                                                    break;
                                                case 0x1400006:
                                                    Console.Write("nclip");
                                                    break;
                                                case 0x158002D:
                                                    Console.Write("avsz3");
                                                    break;
                                                case 0x168002E:
                                                    Console.Write("avsz4");
                                                    break;
                                                default:
                                                    Console.Write("cop2\t${0:X}", data & 0x1FFFFFF);
                                                    break;
                                            }
                                            break;
                                    }
                                }
                                else
                                {
                                    code3 = (data >> 21) & 0x1F;
                                    switch (code3)
                                    {
                                        case 0:
                                            Console.Write("mfc2\t{0},{1}", (Register)((data >> 16) & 0x1F),
                                                (C2DataRegister)((data >> 11) & 0x1F));
                                            break;
                                        case 2:
                                            Console.Write("cfc2\t{0},{1}", (Register)((data >> 16) & 0x1F),
                                                (C2DataRegister)((data >> 11) & 0x1F));
                                            break;
                                        case 4:
                                            Console.Write("mtc2\t{0},{1}", (Register)((data >> 16) & 0x1F),
                                                (C2DataRegister)((data >> 11) & 0x1F));
                                            break;
                                        case 6:
                                            Console.Write("ctc2\t{0},{1}", (Register)((data >> 16) & 0x1F),
                                                (C2DataRegister)((data >> 11) & 0x1F));
                                            break;
                                        default:
                                            Console.Write($"DW\t${data:X}");
                                            break;
                                    }
                                }
                            }
                                break;
                            case 4:
                                Console.Write("beql\t{0},{1},{2}", (Register)((data >> 21) & 0x1F), (Register)((data >> 16) & 0x1F),
                                    getSymbolName(index, (short) data << 2));
                                break;
                            case 5:
                                Console.Write("bnel\t{0},{1},{2}", (Register)((data >> 21) & 0x1F), (Register)((data >> 16) & 0x1F),
                                    getSymbolName(index, (short) data << 2));
                                break;
                            case 6:
                                Console.Write("blezl\t{0},{1}", (Register)((data >> 21) & 0x1F),
                                    getSymbolName(index, (short) data << 2));
                                break;
                            case 7:
                                Console.Write("bgtzl\t{0},{1}", (Register)((data >> 21) & 0x1F),
                                    getSymbolName(index, (short) data << 2));
                                break;
                            default:
                                Console.Write($"DW\t${data:X}");
                                break;
                        }
                        break;
                    case 4:
                        switch (code2)
                        {
                            case 0:
                                Console.Write("lb\t{0},{1}({2})", (Register)((data >> 16) & 0x1F),
                                    (short) data,
                                    (Register)((data >> 21) & 0x1F));
                                break;
                            case 1:
                                Console.Write("lh\t{0},{1}({2})", (Register)((data >> 16) & 0x1F),
                                    (short) data,
                                    (Register)((data >> 21) & 0x1F));
                                break;
                            case 2:
                                Console.Write("lwl\t{0},{1}({2})", (Register)((data >> 16) & 0x1F),
                                    (short) data, (Register)((data >> 21) & 0x1F));
                                break;
                            case 3:
                                Console.Write("lw\t{0},{1}({2})", (Register)((data >> 16) & 0x1F),
                                    (short) data,
                                    (Register)((data >> 21) & 0x1F));
                                break;
                            case 4:
                                Console.Write("lbu\t{0},{1}({2})", (Register)((data >> 16) & 0x1F),
                                    (short) data, (Register)((data >> 21) & 0x1F));
                                break;
                            case 5:
                                Console.Write("lhu\t{0},{1}({2})", (Register)((data >> 16) & 0x1F),
                                    (short) data, (Register)((data >> 21) & 0x1F));
                                break;
                            case 6:
                                Console.Write("lwr\t{0},{1}({2})", (Register)((data >> 16) & 0x1F),
                                    (short) data, (Register)((data >> 21) & 0x1F));
                                break;
                            default:
                                Console.Write($"DW\t${data:X}");
                                break;
                        }
                        break;
                    case 5:
                        switch (code2)
                        {
                            case 0:
                                Console.Write("sb\t{0},{1}({2})", (Register)((data >> 16) & 0x1F),
                                    (short) data,
                                    (Register)((data >> 21) & 0x1F));
                                break;
                            case 1:
                                Console.Write("sh\t{0},{1}({2})", (Register)((data >> 16) & 0x1F),
                                    (short) data,
                                    (Register)((data >> 21) & 0x1F));
                                break;
                            case 2:
                                Console.Write("swl\t{0},{1}({2})", (Register)((data >> 16) & 0x1F),
                                    (short) data, (Register)((data >> 21) & 0x1F));
                                break;
                            case 3:
                                Console.Write("sw\t{0},{1}({2})", (Register)((data >> 16) & 0x1F),
                                    (short) data,
                                    (Register)((data >> 21) & 0x1F));
                                break;
                            case 6:
                                Console.Write("swr\t{0},{1}({2})", (Register)((data >> 16) & 0x1F),
                                    (short) data, (Register)((data >> 21) & 0x1F));
                                break;
                            default:
                                Console.Write($"DW\t${data:X}");
                                break;
                        }
                        break;
                    case 6:
                        switch (code2)
                        {
                            case 0:
                                Console.Write("ll\t{0},{1}({2})", (Register)((data >> 16) & 0x1F),
                                    (short) data,
                                    (Register)((data >> 21) & 0x1F));
                                break;
                            case 2:
                                Console.Write("lwc2\t{0},{1}({2})", (C2DataRegister)((data >> 16) & 0x1F),
                                    (short) data, (Register)((data >> 21) & 0x1F));
                                break;

                            default:
                                Console.Write($"DW\t${data:X}");
                                break;
                        }
                        break;
                    case 7:
                        switch (code2)
                        {
                            case 2:
                                Console.Write("swc2\t{0},{1}({2})", (C2DataRegister)((data >> 16) & 0x1F),
                                    (short) data, (Register)((data >> 21) & 0x1F));
                                break;

                            default:
                                Console.Write($"DW\t${data:X}");
                                break;
                        }
                        break;
                    default:
                        Console.Write($"DW\t${data:X}");
                        break;
                }
                Console.WriteLine();
            }
        }
    }
}