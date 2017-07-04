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

				if(!"PS-X EXE".Equals(new string(id)))
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
			m_data = reader.readBytes((int)m_header.tSize);
		}

		private string getSymbolName(uint addr, int rel = 0)
		{
			addr = (uint)(addr + rel);

			List<Label> lbls;
			if(!m_symFile.labels.TryGetValue(addr, out lbls))
				return $"lbl_{addr:X}";

			return lbls.First().name;
		}

		private readonly SortedDictionary<uint, IInstruction> instructions = new SortedDictionary<uint, IInstruction>();

		private uint dataAt(uint ofs)
		{
			uint data;
			data = (uint)m_data[ofs++];
			data |= (uint)m_data[ofs++] << 8;
			data |= (uint)m_data[ofs++] << 16;
			data |= (uint)m_data[ofs++] << 24;
			return data;
		}

		private static byte extractOp(uint data)
		{
			return (byte)(data >> 29);
		}
		
		public void disassemble()
		{
			uint index = 0;

			while(index < m_data.Length) {
				{
					var f = m_symFile.findFunction(index);
					if(f != null) {
						Console.WriteLine();
						Console.WriteLine(f.getSignature());
					}

					List<Label> lbls;
					if(m_symFile.labels.TryGetValue(index, out lbls)) {
						foreach(var lbl in lbls) {
							Console.WriteLine($"{lbl.name}:");
						}
					}
				}


				uint data = dataAt(index);
				index += 4;

				IInstruction insn = null;

				var code2 = (data >> 26) & 0x7;

				switch(extractOp(data)) {
				case 0:
					insn = decodeBranching(index, data);
					break;
				case 1:
					insn = decodeArithmetic(data);
					break;
				case 2:
					switch(code2) {
					case 0:
						{
							var code3 = (data >> 21) & 0x1f;

							switch(code3) {
							case 0:
								insn = new SimpleInstruction("mfc0", null, new RegisterOperand(data, 16), new C0RegisterOperand(data, 11));
								break;
							case 4:
								insn = new SimpleInstruction("mtc0", null, new RegisterOperand(data, 16), new C0RegisterOperand(data, 11));
								break;
							case 16:
								if((data & 0x1f) == 16)
									insn = new SimpleInstruction("rfe", "__RETURN_FROM_EXCEPTION()");
								else
									insn = new WordData(data);
								break;
							default:
								insn = new WordData(data);
								break;
							}
						}
						break;
					case 2:
						{
							var code3 = (data >> 25) & 0x1;

							if(code3 == 1) {
								data &= 0x1FFFFFF;

								switch((data & 0x1F003FF)) {
								case 0x0400012:
									insn = new SimpleInstruction("mvmva",
										null,
										new ImmediateOperand((int)(data >> 19) & 1),
										new ImmediateOperand((int)(data >> 17) & 3),
										new ImmediateOperand((int)(data >> 15) & 3),
										new ImmediateOperand((int)(data >> 13) & 3),
										new ImmediateOperand((int)(data >> 10) & 1)
									);
									break;
								case 0x0a00428:
									insn = new SimpleInstruction("sqr", null, new ImmediateOperand((int)(data >> 19) & 1));
									break;
								case 0x170000C:
									insn = new SimpleInstruction("op", null, new ImmediateOperand((int)(data >> 19) & 1));
									break;
								case 0x190003D:
									insn = new SimpleInstruction("gpf", null, new ImmediateOperand((int)(data >> 19) & 1));
									break;
								case 0x1A0003E:
									insn = new SimpleInstruction("gpl", null, new ImmediateOperand((int)(data >> 19) & 1));
									break;
								default:
									switch(data) {
									case 0x0180001:
										insn = new SimpleInstruction("rtps", null);
										break;
									case 0x0280030:
										insn = new SimpleInstruction("rtpt", null);
										break;
									case 0x0680029:
										insn = new SimpleInstruction("dcpl", null);
										break;
									case 0x0780010:
										insn = new SimpleInstruction("dcps", null);
										break;
									case 0x0980011:
										insn = new SimpleInstruction("intpl", null);
										break;
									case 0x0C8041E:
										insn = new SimpleInstruction("ncs", null);
										break;
									case 0x0D80420:
										insn = new SimpleInstruction("nct", null);
										break;
									case 0x0E80413:
										insn = new SimpleInstruction("ncds", null);
										break;
									case 0x0F80416:
										insn = new SimpleInstruction("ncdt", null);
										break;
									case 0x0F8002A:
										insn = new SimpleInstruction("dpct", null);
										break;
									case 0x108041B:
										insn = new SimpleInstruction("nccs", null);
										break;
									case 0x118043F:
										insn = new SimpleInstruction("ncct", null);
										break;
									case 0x1280414:
										insn = new SimpleInstruction("cdp", null);
										break;
									case 0x138041C:
										insn = new SimpleInstruction("cc", null);
										break;
									case 0x1400006:
										insn = new SimpleInstruction("nclip", null);
										break;
									case 0x158002D:
										insn = new SimpleInstruction("avsz3", null);
										break;
									case 0x168002E:
										insn = new SimpleInstruction("avsz4", null);
										break;
									default:
										insn = new SimpleInstruction("cop2", null, new ImmediateOperand((int)data & 0x1ffffff));
										break;
									}
									break;
								}
							} else {
								code3 = (data >> 21) & 0x1F;
								switch(code3) {
								case 0:
									insn = new SimpleInstruction("mfc2", null, new RegisterOperand(data, 16), new ImmediateOperand((short)data), new C2RegisterOperand(data, 21));
									break;
								case 2:
									insn = new SimpleInstruction("cfc2", null, new RegisterOperand(data, 16), new ImmediateOperand((short)data), new C2RegisterOperand(data, 21));
									break;
								case 4:
									insn = new SimpleInstruction("mtc2", null, new RegisterOperand(data, 16), new ImmediateOperand((short)data), new C2RegisterOperand(data, 21));
									break;
								case 6:
									insn = new SimpleInstruction("ctc2", null, new RegisterOperand(data, 16), new ImmediateOperand((short)data), new C2RegisterOperand(data, 21));
									break;
								default:
									insn = new WordData(data);
									break;
								}
							}
						}
						break;
					case 4:
						insn = new SimpleInstruction("beql", "if({0} == {1}) goto {2}", new RegisterOperand(data, 21), new RegisterOperand(data, 16), new LabelOperand(getSymbolName(index, (short)data << 2)));
						break;
					case 5:
						insn = new SimpleInstruction("bnel", "if({0} != {1}) goto {2}", new RegisterOperand(data, 21), new RegisterOperand(data, 16), new LabelOperand(getSymbolName(index, (short)data << 2)));
						break;
					case 6:
						insn = new SimpleInstruction("blezl", "if((signed){0} <= 0) goto {1}", new RegisterOperand(data, 21), new LabelOperand(getSymbolName(index, (short)data << 2)));
						break;
					case 7:
						insn = new SimpleInstruction("bgtzl", "if((signed){0} > 0) goto {1}", new RegisterOperand(data, 21), new LabelOperand(getSymbolName(index, (short)data << 2)));
						break;
					default:
						insn = new WordData(data);
						break;
					}
					break;
				case 4:
					insn = decodeLoad(data);
					break;
				case 5:
					insn = decodeStore(data);
					break;
				case 6:
					switch(code2) {
					case 0:
						insn = new SimpleInstruction("ll", null, new RegisterOperand(data, 16), new ImmediateOperand((short)data), new RegisterOperand(data, 21));
						break;
					case 2:
						insn = new SimpleInstruction("lwc2", null, new C2RegisterOperand(data, 16), new ImmediateOperand((short)data), new RegisterOperand(data, 21));
						break;
					default:
						insn = new WordData(data);
						break;
					}
					break;
				case 7:
					switch(code2) {
					case 2:
						insn = new SimpleInstruction("swc2", null, new C2RegisterOperand(data, 16), new ImmediateOperand((short)data), new RegisterOperand(data, 21));
						break;
					default:
						insn = new WordData(data);
						break;
					}
					break;
				default:
					insn = new WordData(data);
					break;
				}
				Console.WriteLine(insn.asReadable());
			}
		}

		private static IInstruction decodeSpecial(uint data)
		{
			var code4 = (data >> 0) & 0x7;

			switch((data >> 3) & 0x7) {
			case 0:
				switch(code4) {
				case 0:
					if(data == 0)
						return new SimpleInstruction("nop", null);
					else
						return new SimpleInstruction("sll", "{0} = (signed){1} << {2}", new RegisterOperand(data, 11), new RegisterOperand(data, 16), new ImmediateOperand((int)(data >> 6) & 0x1F));
				case 2:
					return new SimpleInstruction("srl", "{0} = (unsigned){1} >> {2}", new RegisterOperand(data, 11), new RegisterOperand(data, 16), new ImmediateOperand((int)(data >> 6) & 0x1F));
				case 3:
					return new SimpleInstruction("sra", "{0} = (signed){1} >> {2}", new RegisterOperand(data, 11), new RegisterOperand(data, 16), new ImmediateOperand((int)(data >> 6) & 0x1F));
				case 4:
					return new SimpleInstruction("sllv", "{0} = (signed){1} << {2}", new RegisterOperand(data, 11), new RegisterOperand(data, 16), new RegisterOperand(data, 21));
				case 6:
					return new SimpleInstruction("srlv", "{0} = (unsigned){1} >> {2}", new RegisterOperand(data, 11), new RegisterOperand(data, 16), new RegisterOperand(data, 21));
				case 7:
					return new SimpleInstruction("srav", "{0} = (signed){1} >> {2}", new RegisterOperand(data, 11), new RegisterOperand(data, 16), new RegisterOperand(data, 21));
				default:
					return new WordData(data);
				}
			case 1:
				switch(code4) {
				case 0:
					return new SimpleInstruction("jr", "goto *{0}", new RegisterOperand(data, 21));
				case 1:
					return new SimpleInstruction("jalr", "{0} = __RET_ADDR; (*{1})()", new RegisterOperand(data, 11), new RegisterOperand(data, 21));
				case 4:
					return new SimpleInstruction("syscall", "trap(SYSCALL, {0})", new ImmediateOperand((int)(data >> 6) & 0xFFFFF));
				case 5:
					return new SimpleInstruction("break", "trap(BREAK, {0})", new ImmediateOperand((int)(data >> 6) & 0xFFFFF));
				default:
					return new WordData(data);
				}
			case 2:
				switch(code4) {
				case 0:
					return new SimpleInstruction("mfhi", "{0} = __DIV_REMAINDER_OR_MULT_HI()", new RegisterOperand(data, 11));
				case 1:
					return new SimpleInstruction("mthi", "__LOAD_DIV_REMAINDER_OR_MULT_HI({0})", new RegisterOperand(data, 11));
				case 2:
					return new SimpleInstruction("mflo", "{0} = __DIV_OR_MULT_LO()", new RegisterOperand(data, 11));
				case 3:
					return new SimpleInstruction("mtlo", "__LOAD_DIV_OR_MULT_LO({0})", new RegisterOperand(data, 11));
				default:
					return new WordData(data);
				}
			case 3:
				switch(code4) {
				case 0:
					return new SimpleInstruction("mult", "__MULT((signed){0}, (signed){1})", new RegisterOperand(data, 21), new RegisterOperand(data, 16));
				case 1:
					return new SimpleInstruction("multu", "__MULT((unsigned){0}, (unsigned){1})", new RegisterOperand(data, 21), new RegisterOperand(data, 16));
				case 2:
					return new SimpleInstruction("div", "__DIV((signed){0}, (signed){1})", new RegisterOperand(data, 21), new RegisterOperand(data, 16));
				case 3:
					return new SimpleInstruction("divu", "__DIV((unsigned){0}, (unsigned){1})", new RegisterOperand(data, 21), new RegisterOperand(data, 16));
				default:
					return new WordData(data);
				}
			case 4:
				switch(code4) {
				case 0:
					return new SimpleInstruction("add", "{0} = {1} + {2}", new RegisterOperand(data, 11), new RegisterOperand(data, 21), new RegisterOperand(data, 16));
				case 1:
					if(((data >> 16) & 0x1F) == 0)
						return new SimpleInstruction("move", "{0} = {1}", new RegisterOperand(data, 11), new RegisterOperand(data, 21));
					else
						return new SimpleInstruction("addu", "{0} = {1} + {2}", new RegisterOperand(data, 11), new RegisterOperand(data, 21), new RegisterOperand(data, 16));
				case 2:
					return new SimpleInstruction("sub", "{0} = {1} - {2}", new RegisterOperand(data, 11), new RegisterOperand(data, 21), new RegisterOperand(data, 16));
				case 3:
					return new SimpleInstruction("subu", "{0} = {1} - {2}", new RegisterOperand(data, 11), new RegisterOperand(data, 21), new RegisterOperand(data, 16));
				case 4:
					return new SimpleInstruction("and", "{0} = {1} & {2}", new RegisterOperand(data, 11), new RegisterOperand(data, 21), new RegisterOperand(data, 16));
				case 5:
					return new SimpleInstruction("or", "{0} = {1} | {2}", new RegisterOperand(data, 11), new RegisterOperand(data, 21), new RegisterOperand(data, 16));
				case 6:
					return new SimpleInstruction("xor", "{0} = {1} ^ {2}", new RegisterOperand(data, 11), new RegisterOperand(data, 21), new RegisterOperand(data, 16));
				case 7:
					return new SimpleInstruction("nor", "{0} = ~({1} | {2})", new RegisterOperand(data, 11), new RegisterOperand(data, 21), new RegisterOperand(data, 16));
				default:
					return new WordData(data);
				}
			case 5:
				switch(code4) {
				case 2:
					return new SimpleInstruction("slt", "{0} = {1} < {2} ? 1 : 0", new RegisterOperand(data, 11), new RegisterOperand(data, 21), new RegisterOperand(data, 16));
				case 3:
					return new SimpleInstruction("sltu", "{0} = {1} < {2} ? 1 : 0", new RegisterOperand(data, 11), new RegisterOperand(data, 21), new RegisterOperand(data, 16));
				default:
					return new WordData(data);
				}
			case 6:
				switch(code4) {
				case 0:
					return new SimpleInstruction("tge", null, new RegisterOperand(data, 21), new RegisterOperand(data, 16));
				case 1:
					return new SimpleInstruction("tgeu", null, new RegisterOperand(data, 21), new RegisterOperand(data, 16));
				case 2:
					return new SimpleInstruction("tlt", null, new RegisterOperand(data, 21), new RegisterOperand(data, 16));
				case 3:
					return new SimpleInstruction("tltu", null, new RegisterOperand(data, 21), new RegisterOperand(data, 16));
				case 4:
					return new SimpleInstruction("teq", null, new RegisterOperand(data, 21), new RegisterOperand(data, 16));
				case 6:
					return new SimpleInstruction("tne", null, new RegisterOperand(data, 21), new RegisterOperand(data, 16));
				default:
					return new WordData(data);
				}
			default:
				return new WordData(data);
			}
		}

		private IInstruction decodeRegImm(uint index, uint data)
		{
			switch((data >> 19) & 0x3) {
			case 0:
				switch((data >> 16) & 0x7) {
				case 0:
					return new SimpleInstruction("bltz", "if((signed){0} < 0) goto {1}", new RegisterOperand(data, 21), new LabelOperand(getSymbolName(index, ((ushort)data) << 2)));
				case 1:
					return new SimpleInstruction("bgez", "if((signed){0} >= 0) goto {1}", new RegisterOperand(data, 21), new LabelOperand(getSymbolName(index, ((ushort)data) << 2)));
				case 2:
					return new SimpleInstruction("bltzl", "if((signed){0} < 0) goto {1}", new RegisterOperand(data, 21), new LabelOperand(getSymbolName(index, ((ushort)data) << 2)));
				case 3:
					return new SimpleInstruction("bgezl", "if((signed){0} >= 0) goto {1}", new RegisterOperand(data, 21), new LabelOperand(getSymbolName(index, ((ushort)data) << 2)));
				default:
					return new WordData(data);
				}
			default:
				return new WordData(data);
			}
		}

		private static IInstruction decodeLoad(uint data)
		{
			switch((data >> 26) & 0x7) {
			case 0:
				return new SimpleInstruction("lb", "{0} = (signed char){1}", new RegisterOperand(data, 16), new RegisterOffsetOperand(data, 21, (short)data));
			case 1:
				return new SimpleInstruction("lh", "{0} = (short){1}", new RegisterOperand(data, 16), new RegisterOffsetOperand(data, 21, (short)data));
			case 2:
				return new SimpleInstruction("lwl", null, new RegisterOperand(data, 16), new RegisterOffsetOperand(data, 21, (short)data));
			case 3:
				return new SimpleInstruction("lw", "{0} = (int){1}", new RegisterOperand(data, 16), new RegisterOffsetOperand(data, 21, (short)data));
			case 4:
				return new SimpleInstruction("lbu", "{0} = (unsigned char){1}", new RegisterOperand(data, 16), new RegisterOffsetOperand(data, 21, (short)data));
			case 5:
				return new SimpleInstruction("lhu", "{0} = (unsigned short){1}", new RegisterOperand(data, 16), new RegisterOffsetOperand(data, 21, (short)data));
			case 6:
				return new SimpleInstruction("lwr", null, new RegisterOperand(data, 16), new RegisterOffsetOperand(data, 21, (short)data));
			default:
				return new WordData(data);
			}
		}

		private static IInstruction decodeStore(uint data)
		{
			switch((data >> 26) & 0x7) {
			case 0:
				return new SimpleInstruction("sb", "{1} = (char){0}", new RegisterOperand(data, 16), new RegisterOffsetOperand(data, 21, (short)data));
			case 1:
				return new SimpleInstruction("sh", "{1} = (short){0}", new RegisterOperand(data, 16), new RegisterOffsetOperand(data, 21, (short)data));
			case 2:
				return new SimpleInstruction("swl", null, new RegisterOperand(data, 16), new RegisterOffsetOperand(data, 21, (short)data));
			case 3:
				return new SimpleInstruction("sw", "{1} = (int){0}", new RegisterOperand(data, 16), new RegisterOffsetOperand(data, 21, (short)data));
			case 6:
				return new SimpleInstruction("swr", null, new RegisterOperand(data, 16), new RegisterOffsetOperand(data, 21, (short)data));
			default:
				return new WordData(data);
			}
		}

		private IInstruction decodeBranching(uint index, uint data)
		{
			switch((data >> 26) & 0x7) {
			case 0:
				return decodeSpecial(data);
			case 1:
				return decodeRegImm(index, data);
			case 2:
				return new SimpleInstruction("j", "goto {0}", new LabelOperand(getSymbolName((data & 0x03FFFFFF) << 2)));
			case 3:
				return new SimpleInstruction("jal", "{0}()", new LabelOperand(getSymbolName((data & 0x03FFFFFF) << 2)));
			case 4:
				if(((data >> 16) & 0x1F) == 0)
					return new SimpleInstruction("beqz", "if({0} == 0) goto {1}", new RegisterOperand(data, 21), new LabelOperand(getSymbolName(index, ((short)data) << 2)));
				else
					return new SimpleInstruction("beq", "if({0} == {1}) goto {2}", new RegisterOperand(data, 21), new RegisterOperand(data, 16), new LabelOperand(getSymbolName(index, ((short)data) << 2)));
			case 5:
				if(((data >> 16) & 0x1F) == 0)
					return new SimpleInstruction("bnez", "if({0} != 0) goto {1}", new RegisterOperand(data, 21), new LabelOperand(getSymbolName(index, ((short)data) << 2)));
				else
					return new SimpleInstruction("bne", "if({0} != {1}) goto {2}", new RegisterOperand(data, 21), new RegisterOperand(data, 16), new LabelOperand(getSymbolName(index, ((short)data) << 2)));
			case 6:
				return new SimpleInstruction("blez", "if({0} < 0) goto {1}", new RegisterOperand(data, 21), new LabelOperand(getSymbolName(index, ((short)data) << 2)));
			case 7:
				return new SimpleInstruction("bgtz", "if({0} > 0) goto {1}", new RegisterOperand(data, 21), new LabelOperand(getSymbolName(index, ((short)data) << 2)));
			default:
				return new WordData(data);
			}
		}

		private static IInstruction decodeArithmetic(uint data)
		{
			switch((data >> 26) & 0x7) {
			case 0:
				return new SimpleInstruction("addi", "{0} = {1} + {2}", new RegisterOperand(data, 16), new RegisterOperand(data, 21), new ImmediateOperand((short)data));
			case 1:
				if(((data >> 21) & 0x1F) == 0)
					return new SimpleInstruction("li", "{0} = {1}", new RegisterOperand(data, 16), new ImmediateOperand((short)data));
				else
					return new SimpleInstruction("addiu", "{0} = {1} + {2}", new RegisterOperand(data, 16), new RegisterOperand(data, 21), new ImmediateOperand((ushort)data));
			case 2:
				return new SimpleInstruction("slti", "{0} = {1} < {2} ? 1 : 0", new RegisterOperand(data, 16), new RegisterOperand(data, 21), new ImmediateOperand((short)data));
			case 3:
				return new SimpleInstruction("sltiu", "{0} = {1} < {2} ? 1 : 0", new RegisterOperand(data, 16), new RegisterOperand(data, 21), new ImmediateOperand((ushort)data));
			case 4:
				return new SimpleInstruction("andi", "{0} = {1} & {2}", new RegisterOperand(data, 16), new RegisterOperand(data, 21), new ImmediateOperand((short)data));
			case 5:
				return new SimpleInstruction("ori", "{0} = {1} | {2}", new RegisterOperand(data, 16), new RegisterOperand(data, 21), new ImmediateOperand((short)data));
			case 6:
				return new SimpleInstruction("xori", "{0} = {1} ^ {2}", new RegisterOperand(data, 16), new RegisterOperand(data, 21), new ImmediateOperand((short)data));
			case 7:
				return new SimpleInstruction("lui", "{0} = {1}", new RegisterOperand(data, 16), new ImmediateOperand((ushort)data));
			default:
				return new WordData(data);
			}
		}
	}
}