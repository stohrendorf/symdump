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
        private const uint SyscallTypeBreak = 0;
        private const uint SyscallTypeSyscall = 1;
        private static readonly ILogger logger = LogManager.GetCurrentClassLogger();

        private static readonly IReadOnlyDictionary<byte, SyscallInfo> syscallsA0 = new Dictionary<byte, SyscallInfo>
        {
            {0x00, new SyscallInfo(true, "open", 2)},
            {0x01, new SyscallInfo(true, "lseek", 3)},
            {0x02, new SyscallInfo(true, "read", 3)},
            {0x03, new SyscallInfo(true, "write", 3)},
            {0x04, new SyscallInfo("close", 1)},
            {0x05, new SyscallInfo(true, "ioctl", 3)},
            {0x06, new SyscallInfo("exit")},
            {0x07, new SyscallInfo("sys_b0_39")},
            {0x08, new SyscallInfo(true, "getc", 1)},
            {0x09, new SyscallInfo("putc", 2)},
            {0x0a, new SyscallInfo("todigit")},
            {0x0b, new SyscallInfo(true, "atof", 1)},
            {0x0c, new SyscallInfo(true, "strtoul", 3)},
            {0x0d, new SyscallInfo(true, "strtol", 3)},
            {0x0e, new SyscallInfo(true, "abs", 1)},
            {0x0f, new SyscallInfo(true, "labs", 1)},
            {0x10, new SyscallInfo(true, "atoi", 1)},
            {0x11, new SyscallInfo(true, "atol", 1)},
            {0x12, new SyscallInfo("atob")},
            {0x13, new SyscallInfo(true, "setjmp", 1)},
            {0x14, new SyscallInfo("longjmp", 2)},
            {0x15, new SyscallInfo(true, "strcat", 2)},
            {0x16, new SyscallInfo(true, "strncat", 3)},
            {0x17, new SyscallInfo(true, "strcmp", 2)},
            {0x18, new SyscallInfo(true, "strncmp", 3)},
            {0x19, new SyscallInfo(true, "strcpy", 2)},
            {0x1a, new SyscallInfo(true, "strncpy", 3)},
            {0x1b, new SyscallInfo(true, "strlen", 1)},
            {0x1c, new SyscallInfo(true, "index", 2)},
            {0x1d, new SyscallInfo(true, "rindex", 2)},
            {0x1e, new SyscallInfo(true, "strchr", 2)},
            {0x1f, new SyscallInfo(true, "strrchr", 2)},
            {0x20, new SyscallInfo(true, "strpbrk", 2)},
            {0x21, new SyscallInfo(true, "strspn", 2)},
            {0x22, new SyscallInfo(true, "strcspn", 2)},
            {0x23, new SyscallInfo("strtok", 2)},
            {0x24, new SyscallInfo(true, "strstr", 2)},
            {0x25, new SyscallInfo(true, "toupper", 1)},
            {0x26, new SyscallInfo(true, "tolower", 1)},
            {0x27, new SyscallInfo("bcopy", 3)},
            {0x28, new SyscallInfo("bzero", 2)},
            {0x29, new SyscallInfo(true, "bcmp", 3)},
            {0x2a, new SyscallInfo("memcpy", 3)},
            {0x2b, new SyscallInfo("memset", 3)},
            {0x2c, new SyscallInfo("memmove", 3)},
            {0x2d, new SyscallInfo(true, "memcmp", 3)},
            {0x2e, new SyscallInfo("memchr", 3)},
            {0x2f, new SyscallInfo(true, "rand")},
            {0x30, new SyscallInfo("srand", 1)},
            {0x31, new SyscallInfo("qsort", 4)},
            {0x32, new SyscallInfo(true, "strtod", 2)},
            {0x33, new SyscallInfo(true, "malloc", 1)},
            {0x34, new SyscallInfo("free", 1)},
            {0x35, new SyscallInfo(true, "lsearch", 5)},
            {0x36, new SyscallInfo(true, "bsearch", 5)},
            {0x37, new SyscallInfo(true, "calloc", 2)},
            {0x38, new SyscallInfo(true, "realloc", 2)},
            {0x39, new SyscallInfo("InitHeap", 2)},
            {0x3a, new SyscallInfo("_exit")},
            {0x3b, new SyscallInfo(true, "getchar", 1)},
            {0x3c, new SyscallInfo("putchar", 2)},
            {0x3d, new SyscallInfo(true, "gets", 1)},
            {0x3e, new SyscallInfo("puts", 1)},
            {0x3f, new SyscallInfo("printf", 1)},
            // TODO syscall A0 0x40
            {0x41, new SyscallInfo("LoadTest", 2)},
            {0x42, new SyscallInfo("Load", 2)},
            {0x43, new SyscallInfo("Exec", 3)},
            {0x44, new SyscallInfo("FlushCache")},
            {0x45, new SyscallInfo("InstallInterruptHandler")},
            {0x46, new SyscallInfo("GPU_dw")},
            // TODO syscall A0 0x47
            {0x48, new SyscallInfo(true, "SetGPUStatus")},
            {0x49, new SyscallInfo("GPU_cw")}, // TODO ... or GPU_sync?
            {0x4a, new SyscallInfo("GPU_cwb")},
            // TODO syscall A0 0x4b
            // TODO syscall A0 0x4c
            {0x4d, new SyscallInfo(true, "GetGPUStatus")},
            // TODO syscall A0 0x4e
            // TODO syscall A0 0x4f
            // TODO syscall A0 0x50
            {0x51, new SyscallInfo("LoadExec", 2)},
            {0x52, new SyscallInfo("GetSysSP")},
            // TODO syscall A0 0x53
            {0x54, new SyscallInfo("_96_init")},
            {0x55, new SyscallInfo("_bu_init")},
            {0x56, new SyscallInfo("_96_remove")},
            {0x57, new SyscallInfo(true, "__nop_zero_a0_57")},
            {0x58, new SyscallInfo(true, "__nop_zero_a0_58")},
            {0x59, new SyscallInfo(true, "__nop_zero_a0_59")},
            {0x5a, new SyscallInfo(true, "__nop_zero_a0_5a")},
            {0x5b, new SyscallInfo("dev_tty_init")},
            {0x5c, new SyscallInfo("dev_tty_open")},
            // TODO syscall A0 0x5d
            {0x5e, new SyscallInfo("dev_tty_ioctl")},
            {0x5f, new SyscallInfo("dev_cd_open")},
            {0x60, new SyscallInfo("dev_cd_read")},
            {0x61, new SyscallInfo("dev_cd_close")},
            {0x62, new SyscallInfo("dev_cd_firstfile")},
            {0x63, new SyscallInfo("dev_cd_nextfile")},
            {0x64, new SyscallInfo("dev_cd_chdir")},
            {0x65, new SyscallInfo("dev_card_open")},
            {0x66, new SyscallInfo("dev_card_read")},
            {0x67, new SyscallInfo("dev_card_write")},
            {0x68, new SyscallInfo("dev_card_close")},
            {0x69, new SyscallInfo("dev_card_firstfile")},
            {0x6a, new SyscallInfo("dev_card_nextfile")},
            {0x6b, new SyscallInfo("dev_card_erase")},
            {0x6c, new SyscallInfo("dev_card_undelete")},
            {0x6d, new SyscallInfo("dev_card_format")},
            {0x6e, new SyscallInfo("dev_card_rename")},
            // TODO syscall A0 0x6f
            // TODO syscalls A0 0x70 .. 0xff incomplete
            {0x70, new SyscallInfo("_bu_init")},
            {0x71, new SyscallInfo("_96_init")},
            {0x72, new SyscallInfo("_96_remove")},
            {0x78, new SyscallInfo("_96_CdSeekL")},
            {0x7c, new SyscallInfo("_96_CdGetStatus")},
            {0x7e, new SyscallInfo("_96_CdRead")},
            {0x85, new SyscallInfo("_96_CdStop")},
            {0x96, new SyscallInfo("AddCDROMDevice")},
            {0x97, new SyscallInfo("AddMemCardDevice")},
            {0x98, new SyscallInfo("DisableKernelIORedirection")},
            {0x99, new SyscallInfo("EnableKernelIORedirection")},
            {0x9c, new SyscallInfo("GetConf", 3)},
            {0x9d, new SyscallInfo("GetConf", 3)}, // TODO duplicated signature with A0 0x9c
            {0x9f, new SyscallInfo("SetMem")},
            {0xa0, new SyscallInfo("_boot")},
            {0xa1, new SyscallInfo("SystemError")},
            {0xa2, new SyscallInfo("EnqueueCdIntr")},
            {0xa3, new SyscallInfo("DequeueCdIntr")},
            {0xa5, new SyscallInfo("ReadSector", 3)},
            {0xa6, new SyscallInfo("get_cd_status")},
            {0xa7, new SyscallInfo("bufs_cb_0")},
            {0xa8, new SyscallInfo("bufs_cb_1")},
            {0xa9, new SyscallInfo("bufs_cb_2")},
            {0xaa, new SyscallInfo("bufs_cb_3")},
            {0xab, new SyscallInfo("_card_info")},
            {0xac, new SyscallInfo("_card_load")},
            {0xad, new SyscallInfo("_card_auto")},
            {0xae, new SyscallInfo("bufs_cb_4")},
            {0xb2, new SyscallInfo("do_a_long_jmp")}
        };

        private static readonly IReadOnlyDictionary<byte, SyscallInfo> syscallsB0 = new Dictionary<byte, SyscallInfo>
        {
            {0x00, new SyscallInfo("SysMalloc", 1)},
            {0x07, new SyscallInfo("DeliverEvent", 2)},
            {0x08, new SyscallInfo("OpenEvent", 4)},
            {0x09, new SyscallInfo("CloseEvent", 1)},
            {0x0a, new SyscallInfo("WaitEvent", 1)},
            {0x0b, new SyscallInfo("TestEvent", 1)},
            {0x0c, new SyscallInfo("EnableEvent", 1)},
            {0x0d, new SyscallInfo("DisableEvent", 1)},
            {0x0e, new SyscallInfo("OpenTh")},
            {0x0f, new SyscallInfo("CloseTh")},
            {0x10, new SyscallInfo("ChangeTh")},
            {0x12, new SyscallInfo("InitPad")},
            {0x13, new SyscallInfo("StartPad")},
            {0x14, new SyscallInfo("StopPAD")},
            {0x15, new SyscallInfo("PAD_Init")},
            {0x16, new SyscallInfo(true, "PAD_dr")},
            {0x17, new SyscallInfo("ReturnFromException")},
            {0x18, new SyscallInfo("ResetEntryInt")},
            {0x19, new SyscallInfo("HookEntryInt")},
            {0x20, new SyscallInfo("UnDeliverEvent", 2)},
            {0x32, new SyscallInfo(true, "open", 2)},
            {0x33, new SyscallInfo(true, "lseek", 3)},
            {0x34, new SyscallInfo(true, "read", 3)},
            {0x35, new SyscallInfo(true, "write", 3)},
            {0x36, new SyscallInfo("close", 1)},
            {0x37, new SyscallInfo(true, "ioctl", 3)},
            {0x38, new SyscallInfo("exit", 1)},
            {0x3a, new SyscallInfo(true, "getc", 1)},
            {0x3b, new SyscallInfo("putc", 2)},
            {0x3c, new SyscallInfo(true, "getchar")},
            {0x3d, new SyscallInfo("putchar", 1)},
            {0x3e, new SyscallInfo(true, "gets", 1)},
            {0x3f, new SyscallInfo("puts", 1)},
            {0x40, new SyscallInfo("cd")},
            {0x41, new SyscallInfo("format")},
            {0x42, new SyscallInfo("firstfile")},
            {0x43, new SyscallInfo("nextfile")},
            {0x44, new SyscallInfo("rename")},
            {0x45, new SyscallInfo("delete")},
            {0x46, new SyscallInfo("undelete")},
            {0x47, new SyscallInfo("AddDevice")},
            {0x48, new SyscallInfo("RemoveDevice")},
            {0x49, new SyscallInfo("PrintInstalledDevices")},
            {0x4a, new SyscallInfo("InitCARD")},
            {0x4b, new SyscallInfo("StartCARD")},
            {0x4c, new SyscallInfo("StopCARD")},
            {0x4e, new SyscallInfo("_card_write")},
            {0x4f, new SyscallInfo("_card_read")},
            {0x50, new SyscallInfo("_new_card")},
            {0x51, new SyscallInfo("Krom2RawAdd")},
            {0x54, new SyscallInfo(true, "_get_errno")},
            {0x55, new SyscallInfo(true, "_get_error", 1)},
            {0x56, new SyscallInfo("GetC0Table")},
            {0x57, new SyscallInfo("GetB0Table")},
            {0x58, new SyscallInfo("_card_chan")},
            {0x5b, new SyscallInfo("ChangeClearPad", 1)},
            {0x5c, new SyscallInfo("_card_status")},
            {0x5d, new SyscallInfo("_card_wait")}
        };

        private static readonly IReadOnlyDictionary<byte, SyscallInfo> syscallsC0 = new Dictionary<byte, SyscallInfo>
        {
            {0x00, new SyscallInfo("InitRCnt")},
            {0x01, new SyscallInfo("InitException")},
            {0x02, new SyscallInfo("SysEnqIntRP", 2)},
            {0x03, new SyscallInfo("SysDeqIntRP", 2)},
            {0x04, new SyscallInfo("get_free_EvCB_slot")},
            {0x05, new SyscallInfo("get_free_TCB_slot")},
            {0x06, new SyscallInfo("ExceptionHandler")},
            {0x07, new SyscallInfo("InstallExceptionHandler")},
            {0x08, new SyscallInfo("SysInitMemory")},
            {0x09, new SyscallInfo("SysInitKMem")},
            {0x0a, new SyscallInfo("ChangeClearRCnt")},
            {0x0b, new SyscallInfo("SystemError")},
            {0x0c, new SyscallInfo("InitDefInt")},
            {0x12, new SyscallInfo("InstallDevices")},
            {0x13, new SyscallInfo("FlishStdInOutPut")},
            {0x14, new SyscallInfo("_nop_zero_c0_14")},
            {0x15, new SyscallInfo("_cdevinput")},
            {0x16, new SyscallInfo("_cdevscan")},
            {0x17, new SyscallInfo(true, "_circgetc", 1)},
            {0x18, new SyscallInfo("_circputc", 2)},
            {0x19, new SyscallInfo("ioabort", 1)},
            {0x1b, new SyscallInfo("KernelRedirect", 1)},
            {0x1c, new SyscallInfo("PatchA0Table", 1)}
        };

        private static readonly IReadOnlyDictionary<ushort, BreakSyscallInfo> syscallsBreak =
            new Dictionary<ushort, BreakSyscallInfo>
            {
                {0x0101, new BreakSyscallInfo("PCInit")},
                {0x0102, new BreakSyscallInfo("PCCreat", 2)},
                {0x0103, new BreakSyscallInfo("PCOpen", 2)},
                {0x0104, new BreakSyscallInfo("PCClose", 1)},
                {0x0105, new BreakSyscallInfo("PCRead", 3)},
                {0x0106, new BreakSyscallInfo("PCWrite", 3)},
                {0x0107, new BreakSyscallInfo("PClSeek", 3)}
            };

        private static readonly Peephole1Delegate[] peephole1 =
        {
            (debugSource, insns, insn) =>
            {
                // psx systemcall implementation
                if (insn.Args.Count != 3 ||
                    !insn.Is(MicroOpcode.Syscall)
                        .Arg<RegisterArg>(out var callRetAddr)
                        .Arg<AddressValue>(out var addr)
                        .Arg<ConstValue>(out var t1)
                    || addr.Address != 0xa0 && addr.Address != 0xb0 && addr.Address != 0xc0
                    || callRetAddr.Register != Register.ra.ToUInt())
                    return false;

                IReadOnlyDictionary<byte, SyscallInfo> table;
                if (addr.Address == 0xa0)
                    table = syscallsA0;
                else if (addr.Address == 0xb0)
                    table = syscallsB0;
                else if (addr.Address == 0xc0)
                    table = syscallsC0;
                else
                    throw new IndexOutOfRangeException($"Invalid syscall table reference {addr.Address:X}");

                if (table.TryGetValue(checked((byte) t1.Value), out var syscallInfo))
                {
                    insns.Add(syscallInfo.ToInsn());
                    return true;
                }

                logger.Warn($"Unhandled syscall {addr.Address:X} {t1.Value:X}");
                return false;
            }
        };

        private readonly byte[] _data;

        private readonly IDebugSource _debugSource;
        private readonly uint? _gpBase;

        private readonly Header _header;

        public readonly IDictionary<uint, MicroAssemblyBlock> Instructions =
            new SortedDictionary<uint, MicroAssemblyBlock>();

        private uint _tmpRegId = 1000;

        public IDictionary<uint, ISet<uint>> CalleesBySource = new Dictionary<uint, ISet<uint>>();

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

        public IEnumerable<KeyValuePair<uint, MicroAssemblyBlock>> RelocatedInstructions =>
            Instructions.Select(kv => new KeyValuePair<uint, MicroAssemblyBlock>(MakeGlobal(kv.Key), kv.Value));

        public MicroAssemblyBlock BlockAtLocal(uint addr)
        {
            return Instructions.TryGetValue(addr, out var x) ? x : null;
        }

        public uint MakeGlobal(uint addr)
        {
            return addr + _header.tAddr;
        }

        public uint MakeLocal(uint addr)
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

        private uint GetTmpRegId()
        {
            return _tmpRegId++;
        }

        private RegisterArg GetTmpReg(byte bits)
        {
            return new RegisterArg(GetTmpRegId(), bits);
        }

        private void DisassembleInsn(uint localAddress, Queue<uint> analysisQueue)
        {
            if (localAddress >= _header.tSize)
                return;

            if (!Instructions.TryGetValue(localAddress, out var asm))
            {
                asm = new MicroAssemblyBlock(localAddress);
                Instructions[localAddress] = asm;
                DecodeInstruction(asm, WordAtLocal(localAddress), localAddress + 4, DelaySlotMode.None);
            }

            foreach (var addr in asm.Outs)
            {
                if (addr.Key >= _header.tSize) continue;

                if (!Instructions.ContainsKey(addr.Key)) analysisQueue.Enqueue(addr.Key);
            }
        }

        private void DropFnDisassembly(uint localAddress)
        {
            var dropQueue = new Queue<uint>();
            dropQueue.Enqueue(localAddress);

            while (dropQueue.Count > 0)
            {
                localAddress = dropQueue.Dequeue();
                if (!Instructions.TryGetValue(localAddress, out var block))
                    continue;

                Instructions.Remove(localAddress);

                foreach (var addr in block.Outs
                    .Where(addr => addr.Key < _header.tSize)
                    .Where(addr =>
                        addr.Value != JumpType.Call && addr.Value != JumpType.CallConditional &&
                        addr.Value != JumpType.Control)
                    .Select(addr => addr.Key))
                    dropQueue.Enqueue(addr);
            }
        }


        public void Disassemble()
        {
            _tmpRegId = 1000;

            logger.Info("Disassembly started");

            var analysisQueue = new Queue<uint>();
            analysisQueue.Enqueue(MakeLocal(_header.pc0));

            foreach (var addr in _debugSource.Functions.Select(f => MakeLocal(f.GlobalAddress)))
                analysisQueue.Enqueue(addr);

            while (analysisQueue.Count != 0) DisassembleInsn(analysisQueue.Dequeue(), analysisQueue);

            logger.Info("Reversing control flow");
            foreach (var asm in Instructions)
            foreach (var @out in asm.Value.Outs)
            {
                var addr = @out.Key;
                if (!Instructions.TryGetValue(addr, out var target))
                {
                    logger.Warn($"Target address 0x{addr:x8} not in local address space");
                    continue;
                }

                target.Ins.Add(asm.Key, @out.Value);
            }

            logger.Info("Collapsing basic assembly blocks");
            var oldSize = Instructions.Count;
            var tmp = new SortedDictionary<uint, MicroAssemblyBlock>(Instructions);
            Instructions.Clear();
            MicroAssemblyBlock basicBlock = null;
            foreach (var addrAsm in tmp)
            {
                if (basicBlock == null)
                {
                    basicBlock = addrAsm.Value;
                    Debug.Assert(basicBlock.Address == addrAsm.Key);
                    Instructions.Add(basicBlock.Address, basicBlock);
                    continue;
                }

                if (addrAsm.Value.Ins.Values.Any(x => x != JumpType.Control))
                {
                    // start a new basic block if we have an incoming edge that is no pure control flow
                    basicBlock = addrAsm.Value;
                    Debug.Assert(basicBlock.Address == addrAsm.Key);
                    Instructions.Add(basicBlock.Address, basicBlock);
                    continue;
                }

                // replace the current's outgoing edges, and append the assembly
                basicBlock.Outs = addrAsm.Value.Outs;
                foreach (var insn in addrAsm.Value.Insns)
                    basicBlock.Insns.Add(insn);

                if (basicBlock.Outs.Count == 0 || basicBlock.Outs.Values.Any(x => x != JumpType.Control))
                    basicBlock = null;
            }

            logger.Info($"Collapsed {oldSize} blocks into {Instructions.Count} blocks");

            logger.Info("Building function ownerships");
            foreach (var callee in CalleesBySource.Values.SelectMany(x => x).ToHashSet())
                CollectFunctionBlocks(MakeLocal(callee));

            logger.Info("Peephole optimization");
            long before = 0, after = 0;
            foreach (var asm in Instructions.Values)
                asm.Optimize(_debugSource, ref before, ref after,
                    peephole1
                        .Concat(Enumerable.Repeat<Peephole1Delegate>(PrepareSyscallFromDynamicJmp, 1))
                        .Concat(Enumerable.Repeat<Peephole1Delegate>(PrepareSyscallFromCall, 1))
                        .Concat(Enumerable.Repeat<Peephole1Delegate>(SyscallFromBreakOrSyscall, 1)),
                    null);

            logger.Info($"Reduced instruction count from {before} to {after} ({100 * after / before}%)");
        }

        private bool PrepareSyscallFromDynamicJmp(IDebugSource debugSource, IList<MicroInsn> insns, MicroInsn insn)
        {
            // psx systemcall preparation (attach $t1 and syscall intrinsic address as parameter to call)
            if (insn.Args.Count != 1 || !insn.Is(MicroOpcode.DynamicJmp).Arg<ConstValue>(out var c0) ||
                c0.Value != 0xa0 && c0.Value != 0xb0 && c0.Value != 0xc0)
                return false;

            var tmp = GetTmpReg(32);
            var ra = new RegisterArg(Register.ra.ToUInt(), 32);
            insns.Add(new CopyInsn(tmp, ra));
            insns.Add(
                new MicroInsn(MicroOpcode.Syscall,
                    ra,
                    new AddressValue(c0.Value, "syscall!<unknown>"),
                    new RegisterArg(Register.t1.ToUInt(), 32))
            );
            insns.Add(new CopyInsn(ra, tmp));
            insns.Add(new MicroInsn(MicroOpcode.Return, ra));
            return true;
        }

        private bool PrepareSyscallFromCall(IDebugSource debugSource, IList<MicroInsn> insns, MicroInsn insn)
        {
            // psx systemcall preparation (attach $t1 and syscall intrinsic address as parameter to call)
            if (insn.Args.Count != 2 ||
                !insn.Is(MicroOpcode.Call).ArgRegIs(Register.ra.ToUInt()).Arg<ConstValue>(out var c0) ||
                c0.Value != 0xa0 && c0.Value != 0xb0 && c0.Value != 0xc0)
                return false;

            var tmp = GetTmpReg(32);
            var ra = new RegisterArg(Register.ra.ToUInt(), 32);
            insns.Add(new CopyInsn(tmp, ra));
            insns.Add(
                new MicroInsn(MicroOpcode.Syscall,
                    ra,
                    new AddressValue(c0.Value, "syscall!<unknown>"),
                    new RegisterArg(Register.t1.ToUInt(), 32))
            );
            insns.Add(new CopyInsn(ra, tmp));
            insns.Add(new MicroInsn(MicroOpcode.Return, ra));
            return true;
        }

        private bool SyscallFromBreakOrSyscall(IDebugSource debugSource, IList<MicroInsn> insns, MicroInsn insn)
        {
            // psx systemcall preparation (attach $t1 and syscall intrinsic address as parameter to call)
            if (insn.Args.Count != 2 ||
                !insn.Is(MicroOpcode.Syscall).Arg<ConstValue>(out var c0).Arg<ConstValue>(out var c1) ||
                c0.Value != SyscallTypeBreak && c0.Value != SyscallTypeSyscall)
                return false;

            var tmp = GetTmpReg(32);
            var ra = new RegisterArg(Register.ra.ToUInt(), 32);
            insns.Add(new CopyInsn(tmp, ra));
            if (c0.Value == SyscallTypeBreak)
            {
                if (syscallsBreak.TryGetValue(checked((ushort) c1.Value), out var breakCallInfo))
                    insns.Add(breakCallInfo.ToInsn());
                else
                    insns.Add(
                        new MicroInsn(MicroOpcode.Syscall,
                            ra,
                            new AddressValue(c1.Value, "syscall!<unknown/break>")
                        )
                    );
            }
            else if (c0.Value == SyscallTypeSyscall)
            {
                insns.Add(
                    new MicroInsn(MicroOpcode.Syscall,
                        ra,
                        new AddressValue(c0.Value, "syscall!<unknown>"),
                        c1
                    )
                );
            }
            else
            {
                throw new IndexOutOfRangeException();
            }

            insns.Add(new CopyInsn(ra, tmp));
            insns.Add(new MicroInsn(MicroOpcode.Return, ra));
            return true;
        }

        private void CollectFunctionBlocks(uint functionAddr)
        {
            var q = new Queue<uint>();
            q.Enqueue(functionAddr);

            var blocks = new HashSet<uint>();

            while (q.Count > 0)
            {
                var blockAddr = q.Dequeue();
                if (blocks.Contains(blockAddr))
                    continue;

                var block = Instructions.TryGetValue(blockAddr, out var x) ? x : null;
                if (block == null)
                    continue;

                blocks.Add(blockAddr);
                block.OwningFunctions.Add(functionAddr);

                foreach (var o in block.Outs)
                    switch (o.Value)
                    {
                        case JumpType.Call:
                        case JumpType.CallConditional:
                            break;
                        case JumpType.Jump:
                        case JumpType.JumpConditional:
                        case JumpType.Control:
                            q.Enqueue(o.Key);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
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
            var regofs = MakeRegisterOffsetArg(data, shift, offset, bits);
            if (_gpBase == null || regofs.Register != Register.gp.ToUInt())
                return regofs;

            var absoluteAddress = (uint) (_gpBase.Value + regofs.Offset);
            return new AddressValue(absoluteAddress, _debugSource.GetSymbolName(absoluteAddress)).Deref(bits);
        }

        private static uint RelAddr(uint @base, short offset)
        {
            return (uint) (@base + offset * 4);
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
                    if (MakeLocal(absoluteAddress) != nextInsnAddressLocal + 4)
                        asm.Outs.Add(MakeLocal(absoluteAddress), JumpType.Jump);
                    DecodeInstruction(asm, WordAtLocal(nextInsnAddressLocal), nextInsnAddressLocal + 4,
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
                    asm.Outs.Add(MakeLocal(absoluteAddress), JumpType.Call);
                    if (!CalleesBySource.TryGetValue(nextInsnAddressLocal - 1, out var callees))
                        callees = CalleesBySource[nextInsnAddressLocal] = new HashSet<uint>();
                    callees.Add(absoluteAddress);
                    DecodeInstruction(asm, WordAtLocal(nextInsnAddressLocal), nextInsnAddressLocal + 4,
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
                    var tgt = new AddressValue(MakeGlobal(localAddress),
                        _debugSource.GetSymbolName(MakeGlobal(localAddress)));
                    asm.Outs.Add(localAddress, JumpType.JumpConditional);
                    var r1 = MakeZeroRegisterOperand(data, 21);
                    var r2 = MakeZeroRegisterOperand(data, 16);
                    var tmp = new RegisterArg(Register.CmpResult.ToUInt(), 1);
                    asm.Add(MicroOpcode.SetEq, tmp, r1, r2);
                    DecodeInstruction(asm, WordAtLocal(nextInsnAddressLocal), nextInsnAddressLocal + 4,
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
                    var tgt = new AddressValue(MakeGlobal(localAddress),
                        _debugSource.GetSymbolName(MakeGlobal(localAddress)));
                    asm.Outs.Add(localAddress, JumpType.JumpConditional);
                    var r1 = MakeZeroRegisterOperand(data, 21);
                    var r2 = MakeZeroRegisterOperand(data, 16);
                    var tmp = new RegisterArg(Register.CmpResult.ToUInt(), 1);
                    asm.Add(MicroOpcode.SetNEq, tmp, r1, r2);
                    DecodeInstruction(asm, WordAtLocal(nextInsnAddressLocal), nextInsnAddressLocal + 4,
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
                    var tgt = new AddressValue(MakeGlobal(localAddress),
                        _debugSource.GetSymbolName(MakeGlobal(localAddress)));
                    asm.Outs.Add(localAddress, JumpType.JumpConditional);
                    var r1 = MakeZeroRegisterOperand(data, 21);
                    var tmp = new RegisterArg(Register.CmpResult.ToUInt(), 1);
                    asm.Add(MicroOpcode.SSetLE, tmp, r1, new ConstValue(0, 32));
                    DecodeInstruction(asm, WordAtLocal(nextInsnAddressLocal), nextInsnAddressLocal + 4,
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
                    var tgt = new AddressValue(MakeGlobal(localAddress),
                        _debugSource.GetSymbolName(MakeGlobal(localAddress)));
                    asm.Outs.Add(localAddress, JumpType.JumpConditional);
                    var r1 = MakeZeroRegisterOperand(data, 21);
                    var tmp = new RegisterArg(Register.CmpResult.ToUInt(), 1);
                    asm.Add(MicroOpcode.SSetL, tmp, new ConstValue(0, 32), r1);
                    DecodeInstruction(asm, WordAtLocal(nextInsnAddressLocal), nextInsnAddressLocal + 4,
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
                    var tgt = new AddressValue(MakeGlobal(localAddress),
                        _debugSource.GetSymbolName(MakeGlobal(localAddress)));
                    asm.Outs.Add(localAddress, JumpType.JumpConditional);
                    var tmp = new RegisterArg(Register.CmpResult.ToUInt(), 1);
                    asm.Add(MicroOpcode.SetEq, tmp, MakeZeroRegisterOperand(data, 21),
                        MakeZeroRegisterOperand(data, 16));
                    DecodeInstruction(asm, WordAtLocal(nextInsnAddressLocal), nextInsnAddressLocal + 4,
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
                    var tgt = new AddressValue(MakeGlobal(localAddress),
                        _debugSource.GetSymbolName(MakeGlobal(localAddress)));
                    asm.Outs.Add(localAddress, JumpType.JumpConditional);
                    var tmp = new RegisterArg(Register.CmpResult.ToUInt(), 1);
                    asm.Add(MicroOpcode.SetNEq, tmp, MakeZeroRegisterOperand(data, 21),
                        MakeZeroRegisterOperand(data, 16));
                    DecodeInstruction(asm, WordAtLocal(nextInsnAddressLocal), nextInsnAddressLocal + 4,
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
                    var tgt = new AddressValue(MakeGlobal(localAddress),
                        _debugSource.GetSymbolName(MakeGlobal(localAddress)));
                    asm.Outs.Add(localAddress, JumpType.JumpConditional);
                    var tmp = new RegisterArg(Register.CmpResult.ToUInt(), 1);
                    asm.Add(MicroOpcode.SSetLE, tmp, MakeZeroRegisterOperand(data, 21), new ConstValue(0, 32));
                    DecodeInstruction(asm, WordAtLocal(nextInsnAddressLocal), nextInsnAddressLocal + 4,
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
                    var tgt = new AddressValue(MakeGlobal(localAddress),
                        _debugSource.GetSymbolName(MakeGlobal(localAddress)));
                    asm.Outs.Add(localAddress, JumpType.JumpConditional);
                    var tmp = new RegisterArg(Register.CmpResult.ToUInt(), 1);
                    asm.Add(MicroOpcode.SSetL, tmp, new ConstValue(0, 32), MakeZeroRegisterOperand(data, 21));
                    DecodeInstruction(asm, WordAtLocal(nextInsnAddressLocal), nextInsnAddressLocal + 4,
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
                    DecodeInstruction(asm, WordAtLocal(nextInsnAddressLocal), nextInsnAddressLocal + 4,
                        DelaySlotMode.AbortControl);
                    if (rs1 is RegisterArg r && r.Register == Register.ra.ToUInt())
                    {
                        asm.Add(MicroOpcode.Return, rs1);
                    }
                    else
                    {
                        logger.Info($"Possible switch statement at 0x{MakeGlobal(nextInsnAddressLocal - 4):x8}");
                        asm.Add(MicroOpcode.DynamicJmp, rs1);
                    }

                    break;
                case OpcodeFunction.jalr:
                    DecodeInstruction(asm, WordAtLocal(nextInsnAddressLocal), nextInsnAddressLocal + 4,
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
                                new AddressValue(MakeGlobal(localAddress),
                                    _debugSource.GetSymbolName(MakeGlobal(localAddress)))));

                            asm.Outs.Add(localAddress, JumpType.JumpConditional);
                            DecodeInstruction(asm, WordAtLocal(nextInsnAddressLocal), nextInsnAddressLocal + 4,
                                DelaySlotMode.ContinueControl);
                        }
                            break;
                        case 1:
                        {
                            var localAddress = RelAddr(nextInsnAddressLocal, (short) data);
                            asm.Add(new UnsupportedInsn("bc0t",
                                new AddressValue(MakeGlobal(localAddress),
                                    _debugSource.GetSymbolName(MakeGlobal(localAddress)))));

                            asm.Outs.Add(localAddress, JumpType.JumpConditional);
                            DecodeInstruction(asm, WordAtLocal(nextInsnAddressLocal), nextInsnAddressLocal + 4,
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

        private void DecodePcRelative(MicroAssemblyBlock asm, uint nextInsnAddressLocal, uint data)
        {
            var rs = MakeZeroRegisterOperand(data, 21);
            var localAddress = RelAddr(nextInsnAddressLocal, (short) data);
            var offset = new AddressValue(MakeGlobal(localAddress),
                _debugSource.GetSymbolName(MakeGlobal(localAddress)));
            switch ((data >> 16) & 0x1f)
            {
                case 0: // bltz
                {
                    asm.Outs.Add(localAddress, JumpType.JumpConditional);
                    var tmpReg = new RegisterArg(Register.CmpResult.ToUInt(), 1);
                    asm.Add(MicroOpcode.SSetL, tmpReg, rs, new ConstValue(0, 32));
                    DecodeInstruction(asm, WordAtLocal(nextInsnAddressLocal), nextInsnAddressLocal + 4,
                        DelaySlotMode.ContinueControl);
                    asm.Add(MicroOpcode.JmpIf, tmpReg, offset);
                    break;
                }
                case 1: // bgez
                {
                    asm.Outs.Add(localAddress, JumpType.JumpConditional);
                    var tmpReg = new RegisterArg(Register.CmpResult.ToUInt(), 1);
                    asm.Add(MicroOpcode.SSetLE, tmpReg, new ConstValue(0, 32), rs);
                    DecodeInstruction(asm, WordAtLocal(nextInsnAddressLocal), nextInsnAddressLocal + 4,
                        DelaySlotMode.ContinueControl);
                    asm.Add(MicroOpcode.JmpIf, tmpReg, offset);
                    break;
                }
                case 16: // bltzal
                {
                    asm.Outs.Add(localAddress, JumpType.JumpConditional);
                    var tmpReg = new RegisterArg(Register.CmpResult.ToUInt(), 1);
                    asm.Add(MicroOpcode.SSetL, tmpReg, rs, new ConstValue(0, 32));
                    DecodeInstruction(asm, WordAtLocal(nextInsnAddressLocal), nextInsnAddressLocal + 4,
                        DelaySlotMode.ContinueControl);
                    asm.Add(MicroOpcode.JmpIf, tmpReg, offset);
                    break;
                }
                case 17: // bgezal
                {
                    asm.Outs.Add(localAddress, JumpType.JumpConditional);
                    var tmpReg = new RegisterArg(Register.CmpResult.ToUInt(), 1);
                    asm.Add(MicroOpcode.SSetLE, tmpReg, new ConstValue(0, 32), rs);
                    DecodeInstruction(asm, WordAtLocal(nextInsnAddressLocal), nextInsnAddressLocal + 4,
                        DelaySlotMode.ContinueControl);
                    asm.Add(MicroOpcode.JmpIf, tmpReg, offset);
                    break;
                }
                default:
                    asm.Add(MicroOpcode.Data, new ConstValue(data, 32));
                    break;
            }
        }

        private static void DecodeCop2(MicroAssemblyBlock asm, uint nextInsnAddressLocal, uint data,
            DelaySlotMode delaySlotMode)
        {
            var opc = data & ((1 << 26) - 1);
            if (((data >> 25) & 1) != 0)
            {
                DecodeCop2Gte(asm, nextInsnAddressLocal, opc, delaySlotMode);
                return;
            }

            var cf = (opc >> 21) & 0x1F;
            switch (cf)
            {
                case 0: // mfc2
                    asm.Add(new CopyInsn(MakeRegisterOperand(opc, 16), MakeC2RegisterOperand(opc, 11)));
                    if (delaySlotMode != DelaySlotMode.AbortControl)
                        asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                case 2: // cfc2
                    asm.Add(new CopyInsn(MakeRegisterOperand(opc, 16), MakeC2ControlRegisterOperand(opc, 11)));
                    if (delaySlotMode != DelaySlotMode.AbortControl)
                        asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                case 4: // mtc2
                    asm.Add(new CopyInsn(MakeC2RegisterOperand(opc, 11), MakeRegisterOperand(opc, 16)));
                    if (delaySlotMode != DelaySlotMode.AbortControl)
                        asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                case 6: // ctc2
                    asm.Add(new CopyInsn(MakeC2ControlRegisterOperand(opc, 11), MakeRegisterOperand(opc, 16)));
                    if (delaySlotMode != DelaySlotMode.AbortControl)
                        asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                default:
                    asm.Add(MicroOpcode.Data, new ConstValue(data, 32));
                    break;
            }
        }

        private static void DecodeCop2Gte(MicroAssemblyBlock asm, uint nextInsnAddressLocal, uint data,
            DelaySlotMode delaySlotMode)
        {
            switch (data & 0x1F003FF)
            {
                case 0x0400012:
                    asm.Add(new UnsupportedInsn("mvmva",
                        new ConstValue((data >> 19) & 1, 1),
                        new ConstValue((data >> 17) & 3, 2),
                        new ConstValue((data >> 15) & 3, 2),
                        new ConstValue((data >> 13) & 3, 2),
                        new ConstValue((data >> 10) & 1, 1)
                    ));
                    if (delaySlotMode != DelaySlotMode.AbortControl)
                        asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                case 0x0a00428:
                    asm.Add(new UnsupportedInsn("sqr", new ConstValue((data >> 19) & 1, 1)));
                    if (delaySlotMode != DelaySlotMode.AbortControl)
                        asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                case 0x170000C:
                    asm.Add(new UnsupportedInsn("op", new ConstValue((data >> 19) & 1, 1)));
                    if (delaySlotMode != DelaySlotMode.AbortControl)
                        asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                case 0x1400006:
                    asm.Add(new UnsupportedInsn("nclip", new ConstValue((data >> 19) & 1, 1)));
                    if (delaySlotMode != DelaySlotMode.AbortControl)
                        asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                case 0x190003D:
                    asm.Add(new UnsupportedInsn("gpf", new ConstValue((data >> 19) & 1, 1)));
                    if (delaySlotMode != DelaySlotMode.AbortControl)
                        asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                case 0x1A0003E:
                    asm.Add(new UnsupportedInsn("gpl", new ConstValue((data >> 19) & 1, 1)));
                    if (delaySlotMode != DelaySlotMode.AbortControl)
                        asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                    break;
                default:
                    switch (data)
                    {
                        case 0x0180001:
                            asm.Add(new UnsupportedInsn("rtps"));
                            if (delaySlotMode != DelaySlotMode.AbortControl)
                                asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                            break;
                        case 0x0280030:
                            asm.Add(new UnsupportedInsn("rtpt"));
                            if (delaySlotMode != DelaySlotMode.AbortControl)
                                asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                            break;
                        case 0x0680029:
                            asm.Add(new UnsupportedInsn("dcpl"));
                            if (delaySlotMode != DelaySlotMode.AbortControl)
                                asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                            break;
                        case 0x0780010:
                            asm.Add(new UnsupportedInsn("dcps"));
                            if (delaySlotMode != DelaySlotMode.AbortControl)
                                asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                            break;
                        case 0x0980011:
                            asm.Add(new UnsupportedInsn("intpl"));
                            if (delaySlotMode != DelaySlotMode.AbortControl)
                                asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                            break;
                        case 0x0C8041E:
                            asm.Add(new UnsupportedInsn("ncs"));
                            if (delaySlotMode != DelaySlotMode.AbortControl)
                                asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                            break;
                        case 0x0D80420:
                            asm.Add(new UnsupportedInsn("nct"));
                            if (delaySlotMode != DelaySlotMode.AbortControl)
                                asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                            break;
                        case 0x0E80413:
                            asm.Add(new UnsupportedInsn("ncds"));
                            if (delaySlotMode != DelaySlotMode.AbortControl)
                                asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                            break;
                        case 0x0F80416:
                            asm.Add(new UnsupportedInsn("ncdt"));
                            if (delaySlotMode != DelaySlotMode.AbortControl)
                                asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                            break;
                        case 0x0F8002A:
                            asm.Add(new UnsupportedInsn("dpct"));
                            if (delaySlotMode != DelaySlotMode.AbortControl)
                                asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                            break;
                        case 0x108041B:
                            asm.Add(new UnsupportedInsn("nccs"));
                            if (delaySlotMode != DelaySlotMode.AbortControl)
                                asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                            break;
                        case 0x118043F:
                            asm.Add(new UnsupportedInsn("ncct"));
                            if (delaySlotMode != DelaySlotMode.AbortControl)
                                asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                            break;
                        case 0x1280414:
                            asm.Add(new UnsupportedInsn("cdp"));
                            if (delaySlotMode != DelaySlotMode.AbortControl)
                                asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                            break;
                        case 0x138041C:
                            asm.Add(new UnsupportedInsn("cc"));
                            if (delaySlotMode != DelaySlotMode.AbortControl)
                                asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                            break;
                        case 0x1400006:
                            asm.Add(new UnsupportedInsn("nclip"));
                            if (delaySlotMode != DelaySlotMode.AbortControl)
                                asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                            break;
                        case 0x158002D:
                            asm.Add(new UnsupportedInsn("avsz3"));
                            if (delaySlotMode != DelaySlotMode.AbortControl)
                                asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                            break;
                        case 0x168002E:
                            asm.Add(new UnsupportedInsn("avsz4"));
                            if (delaySlotMode != DelaySlotMode.AbortControl)
                                asm.Outs.Add(nextInsnAddressLocal, JumpType.Control);
                            break;
                        default:
                            asm.Add(MicroOpcode.Data, new ConstValue(data, 32));
                            break;
                    }

                    break;
            }
        }

        private sealed class SyscallInfo
        {
            public readonly bool HasReturn;
            public readonly string Name;
            public readonly byte RegisterArgs;

            public SyscallInfo(string name, byte registerArgs = 0) : this(false, name, registerArgs)
            {
            }

            public SyscallInfo(bool hasReturn, string name, byte registerArgs = 0)
            {
                if (registerArgs >= 5)
                {
                    // throw new ArgumentOutOfRangeException(nameof(registerArgs), registerArgs, "Cannot handle more than 4 register args");
                    logger.Warn($"Too many parameters ({registerArgs}), limiting to 4");
                    registerArgs = 4;
                }

                HasReturn = hasReturn;
                Name = name;
                RegisterArgs = registerArgs;
            }

            public MicroInsn ToInsn()
            {
#if false
                var args = new List<IMicroArg>
                {
                    HasReturn
                        ? new RegisterArg(Register.v0.ToUInt(), 32)
                        : new RegisterArg(Register.zero.ToUInt(), 0) // FIXME
                    ,
                    new AddressValue(0, $"syscall!{Name}")
                };
#else
                var args = new List<IMicroArg>
                {
                    new AddressValue(0, $"syscall!{Name}")
                };
#endif

                if (RegisterArgs >= 1)
                    args.Add(new RegisterArg(Register.a0.ToUInt(), 32));
                if (RegisterArgs >= 2)
                    args.Add(new RegisterArg(Register.a1.ToUInt(), 32));
                if (RegisterArgs >= 3)
                    args.Add(new RegisterArg(Register.a2.ToUInt(), 32));
                if (RegisterArgs >= 4)
                    args.Add(new RegisterArg(Register.a3.ToUInt(), 32));

                return new MicroInsn(MicroOpcode.Syscall, args.ToArray());
            }
        }

        private sealed class BreakSyscallInfo
        {
            public readonly bool HasReturn;
            public readonly string Name;
            public readonly byte RegisterArgs;

            public BreakSyscallInfo(string name, byte registerArgs = 0) : this(false, name, registerArgs)
            {
            }

            public BreakSyscallInfo(bool hasReturn, string name, byte registerArgs = 0)
            {
                if (registerArgs >= 4)
                {
                    // throw new ArgumentOutOfRangeException(nameof(registerArgs), registerArgs, "Cannot handle more than 4 register args");
                    logger.Warn($"Too many parameters ({registerArgs}), limiting to 3");
                    registerArgs = 4;
                }

                HasReturn = hasReturn;
                Name = name;
                RegisterArgs = registerArgs;
            }

            public MicroInsn ToInsn()
            {
#if false
                var args = new List<IMicroArg>
                {
                    HasReturn
                        ? new RegisterArg(Register.v0.ToUInt(), 32)
                        : new RegisterArg(Register.zero.ToUInt(), 0) // FIXME
                    ,
                    new AddressValue(0, $"syscall!{Name}")
                };
#else
                var args = new List<IMicroArg>
                {
                    new AddressValue(0, $"syscall!{Name}")
                };
#endif

                if (RegisterArgs >= 1)
                    args.Add(new RegisterArg(Register.a1.ToUInt(), 32));
                if (RegisterArgs >= 2)
                    args.Add(new RegisterArg(Register.a2.ToUInt(), 32));
                if (RegisterArgs >= 3)
                    args.Add(new RegisterArg(Register.a3.ToUInt(), 32));

                return new MicroInsn(MicroOpcode.Syscall, args.ToArray());
            }
        }

        private enum DelaySlotMode
        {
            None,
            ContinueControl,
            AbortControl
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