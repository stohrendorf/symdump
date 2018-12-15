using System;
using System.Collections.Generic;
using System.Linq;
using core;
using core.disasm;
using core.microcode;
using JetBrains.Annotations;
using mips.disasm;
using NLog;

namespace mips.processor
{
    public class PSX
    {
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
            {0x2a, new SyscallInfo(true, "memcpy", 3)},
            {0x2b, new SyscallInfo(true, "memset", 3)},
            {0x2c, new SyscallInfo(true, "memmove", 3)},
            {0x2d, new SyscallInfo(true, "memcmp", 3)},
            {0x2e, new SyscallInfo(true, "memchr", 3)},
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
            {0x41, new SyscallInfo(true, "Format", 1)},
            {0x42, new SyscallInfo(true, "Load", 2)},
            {0x43, new SyscallInfo("Exec", 3)},
            {0x44, new SyscallInfo("FlushCache")},
            {0x45, new SyscallInfo("InstallInterruptHandler")},
            {0x46, new SyscallInfo("GPU_dw", 4)},
            {0x47, new SyscallInfo("mem2vram", 5)},
            {0x48, new SyscallInfo("SendGPU", 1)},
            {0x49, new SyscallInfo("GPU_cw", 1)},
            {0x4a, new SyscallInfo("GPU_cwb", 1)},
            {0x4b, new SyscallInfo("GPU_SendPackets")},
            // TODO syscall A0 0x4b
            // TODO syscall A0 0x4c
            {0x4d, new SyscallInfo(true, "GetGPUStatus")},
            // TODO syscall A0 0x4e
            // TODO syscall A0 0x4f
            // TODO syscall A0 0x50
            {0x51, new SyscallInfo("LoadExec", 3)},
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
            {0x9f, new SyscallInfo("SetMem", 2)},
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
            {0xab, new SyscallInfo(true, "_card_info", 1)},
            {0xac, new SyscallInfo(true, "_card_load", 1)},
            {0xad, new SyscallInfo("_card_auto")},
            {0xae, new SyscallInfo("bufs_cb_4")},
            {0xb2, new SyscallInfo("do_a_long_jmp")}
        };

        private static readonly IReadOnlyDictionary<byte, SyscallInfo> syscallsB0 = new Dictionary<byte, SyscallInfo>
        {
            {0x00, new SyscallInfo("SysMalloc", 1)},
            {0x02, new SyscallInfo("SetRCnt", 3)},
            {0x03, new SyscallInfo(true, "GetRCnt", 1)},
            {0x04, new SyscallInfo(true, "StartRCnt", 1)},
            {0x05, new SyscallInfo("StopRCnt", 1)},
            {0x06, new SyscallInfo("ResetRCnt", 1)},
            {0x07, new SyscallInfo("DeliverEvent", 2)},
            {0x08, new SyscallInfo(true, "OpenEvent", 4)},
            {0x09, new SyscallInfo(true, "CloseEvent", 1)},
            {0x0a, new SyscallInfo(true, "WaitEvent", 1)},
            {0x0b, new SyscallInfo(true, "TestEvent", 1)},
            {0x0c, new SyscallInfo(true, "EnableEvent", 1)},
            {0x0d, new SyscallInfo(true, "DisableEvent", 1)},
            {0x0e, new SyscallInfo(true, "OpenTh", 3)},
            {0x0f, new SyscallInfo(true, "CloseTh", 1)},
            {0x10, new SyscallInfo(true, "ChangeTh", 1)},
            {0x12, new SyscallInfo("InitPad", 4)},
            {0x13, new SyscallInfo("StartPad")},
            {0x14, new SyscallInfo("StopPAD")},
            {0x15, new SyscallInfo("PAD_Init")},
            {0x16, new SyscallInfo(true, "PAD_dr", 2)},
            {0x17, new SyscallInfo("ReturnFromException")},
            {0x18, new SyscallInfo("ResetEntryInt")},
            {0x19, new SyscallInfo("HookEntryInt", 1)},
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
            {0x42, new SyscallInfo(true, "firstfile", 2)},
            {0x43, new SyscallInfo(true, "nextfile", 1)},
            {0x44, new SyscallInfo(true, "rename", 2)},
            {0x45, new SyscallInfo(true, "delete", 1)},
            {0x46, new SyscallInfo("undelete")},
            {0x47, new SyscallInfo("AddDevice")},
            {0x48, new SyscallInfo("RemoveDevice")},
            {0x49, new SyscallInfo("PrintInstalledDevices")},
            {0x4a, new SyscallInfo("InitCARD")},
            {0x4b, new SyscallInfo("StartCARD")},
            {0x4c, new SyscallInfo("StopCARD")},
            {0x4e, new SyscallInfo(true, "_card_write", 2)},
            {0x4f, new SyscallInfo(true, "_card_read", 2)},
            {0x50, new SyscallInfo("_new_card")},
            {0x51, new SyscallInfo(true, "Krom2RawAdd", 1)},
            {0x54, new SyscallInfo(true, "_get_errno")},
            {0x55, new SyscallInfo(true, "_get_error", 1)},
            {0x56, new SyscallInfo(true, "GetC0Table")},
            {0x57, new SyscallInfo(true, "GetB0Table")},
            {0x58, new SyscallInfo(true, "_card_chan")},
            {0x5b, new SyscallInfo("ChangeClearPad", 1)},
            {0x5c, new SyscallInfo(true, "_card_status")},
            {0x5d, new SyscallInfo("_card_wait")}
        };

        private static readonly IReadOnlyDictionary<byte, SyscallInfo> syscallsC0 = new Dictionary<byte, SyscallInfo>
        {
            {0x00, new SyscallInfo("InitRCnt")},
            {0x01, new SyscallInfo("InitException")},
            {0x02, new SyscallInfo(true, "SysEnqIntRP", 2)},
            {0x03, new SyscallInfo(true, "SysDeqIntRP", 2)},
            {0x04, new SyscallInfo("get_free_EvCB_slot")},
            {0x05, new SyscallInfo("get_free_TCB_slot")},
            {0x06, new SyscallInfo("ExceptionHandler")},
            {0x07, new SyscallInfo("InstallExceptionHandler")},
            {0x08, new SyscallInfo("SysInitMemory")},
            {0x09, new SyscallInfo("SysInitKMem")},
            {0x0a, new SyscallInfo(true, "ChangeClearRCnt", 2)},
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
                // psx syscall implementation
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

        private readonly R3000 _r3000;

        public PSX([NotNull] R3000 r3000)
        {
            _r3000 = r3000;
        }

        private bool PrepareSyscallFromDynamicJmp(IDebugSource debugSource, IList<MicroInsn> insns, MicroInsn insn)
        {
            // psx syscall preparation (attach $t1 and syscall intrinsic address as parameter to call)
            if (insn.Args.Count != 1 || !insn.Is(MicroOpcode.DynamicJmp).Arg<ConstValue>(out var c0) ||
                c0.Value != 0xa0 && c0.Value != 0xb0 && c0.Value != 0xc0)
                return false;

            var tmp = _r3000.GetTmpReg(32);
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
            // psx syscall preparation (attach $t1 and syscall intrinsic address as parameter to call)
            if (insn.Args.Count != 2 ||
                !insn.Is(MicroOpcode.Call).ArgRegIs(Register.ra.ToUInt()).Arg<ConstValue>(out var c0) ||
                c0.Value != 0xa0 && c0.Value != 0xb0 && c0.Value != 0xc0)
                return false;

            var tmp = _r3000.GetTmpReg(32);
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
            // psx syscall preparation (attach $t1 and syscall intrinsic address as parameter to call)
            if (insn.Args.Count != 2 ||
                !insn.Is(MicroOpcode.Syscall).Arg<ConstValue>(out var c0).Arg<ConstValue>(out var c1) ||
                c0.Value != R3000.SyscallTypeBreak && c0.Value != R3000.SyscallTypeSyscall)
                return false;

            var tmp = _r3000.GetTmpReg(32);
            var ra = new RegisterArg(Register.ra.ToUInt(), 32);
            insns.Add(new CopyInsn(tmp, ra));
            if (c0.Value == R3000.SyscallTypeBreak)
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
            else if (c0.Value == R3000.SyscallTypeSyscall)
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

        public void PeepholeOptimize(TextSection textSection, IDebugSource debugSource)
        {
            var ph = peephole1
                .Concat(Enumerable.Repeat<Peephole1Delegate>(PrepareSyscallFromDynamicJmp, 1))
                .Concat(Enumerable.Repeat<Peephole1Delegate>(PrepareSyscallFromCall, 1))
                .Concat(Enumerable.Repeat<Peephole1Delegate>(SyscallFromBreakOrSyscall, 1))
                .ToList();

            logger.Info("Peephole optimization");
            long before = 0, after = 0;
            foreach (var asm in textSection.Instructions.Values)
                asm.Optimize(debugSource, ref before, ref after, ph, null);

            logger.Info($"Reduced instruction count from {before} to {after} ({100 * after / before}%)");
        }
    }

    internal sealed class SyscallInfo
    {
        private static readonly ILogger logger = LogManager.GetCurrentClassLogger();

        private readonly bool _hasReturn;
        private readonly string _name;
        private readonly byte _registerArgs;

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

            _hasReturn = hasReturn;
            _name = name;
            _registerArgs = registerArgs;
        }

        public FunctionProperties ToProperties()
        {
            var fn = new FunctionProperties(_name);
            if (_hasReturn)
                fn.OutRegs.Add(Register.v0.ToUInt());
            if (_registerArgs >= 1)
                fn.InRegs.Add(Register.a0.ToUInt());
            if (_registerArgs >= 2)
                fn.InRegs.Add(Register.a1.ToUInt());
            if (_registerArgs >= 3)
                fn.InRegs.Add(Register.a2.ToUInt());
            if (_registerArgs >= 4)
                fn.InRegs.Add(Register.a3.ToUInt());
            return fn;
        }

        public MicroInsn ToInsn()
        {
            return new MicroInsn(MicroOpcode.Call, new FunctionRefArg(ToProperties()));
        }
    }

    internal sealed class BreakSyscallInfo
    {
        private static readonly ILogger logger = LogManager.GetCurrentClassLogger();

        private readonly bool _hasReturn;
        private readonly string _name;
        private readonly byte _registerArgs;

        public BreakSyscallInfo(string name, byte registerArgs = 0) : this(false, name, registerArgs)
        {
        }

        private BreakSyscallInfo(bool hasReturn, string name, byte registerArgs = 0)
        {
            if (registerArgs >= 4)
            {
                // throw new ArgumentOutOfRangeException(nameof(registerArgs), registerArgs, "Cannot handle more than 4 register args");
                logger.Warn($"Too many parameters ({registerArgs}), limiting to 3");
                registerArgs = 4;
            }

            _hasReturn = hasReturn;
            _name = name;
            _registerArgs = registerArgs;
        }

        public FunctionProperties ToProperties()
        {
            var fn = new FunctionProperties(_name);
            if (_hasReturn)
                fn.OutRegs.Add(Register.v0.ToUInt());
            if (_registerArgs >= 1)
                fn.InRegs.Add(Register.a1.ToUInt());
            if (_registerArgs >= 2)
                fn.InRegs.Add(Register.a2.ToUInt());
            if (_registerArgs >= 3)
                fn.InRegs.Add(Register.a3.ToUInt());
            return fn;
        }

        public MicroInsn ToInsn()
        {
            return new MicroInsn(MicroOpcode.Call, new FunctionRefArg(ToProperties()));
        }
    }
}
