using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using core;
using core.expression;
using mips.disasm;
using mips.instructions;
using mips.operands;
using NLog;

namespace exefile.dataflow
{
    public class DataFlowState : IDataFlowState
    {
        private static readonly ILogger logger = LogManager.GetCurrentClassLogger();

        private readonly SortedDictionary<Register, IExpressionNode> _registers =
            new SortedDictionary<Register, IExpressionNode>();

        private readonly List<IExpressionNode> _stack = new List<IExpressionNode>();

        public IDebugSource DebugSource { get; }

        public DataFlowState(IDebugSource debugSource, IFunction func)
        {
            DebugSource = debugSource;

            if (func == null)
                return;

            foreach (var param in func.RegisterParameters)
            {
                var p = param.Value;
                _registers[(Register) param.Key] = new NamedMemoryLayout(p.Name, 0, p.MemoryLayout);
            }

            DumpState();
        }

        public bool Process(Instruction insn, Instruction nextInsn)
        {
            if (insn is NopInstruction)
            {
                logger.Debug("[eval] " + insn.AsReadable());
                return true;
            }

            IExpressionNode condition = null;
            if (insn is ConditionalBranchInstruction cbi)
            {
                condition = (ConditionalBranchNode) cbi.ToExpressionNode(this);
            }

            if (nextInsn != null && nextInsn.IsBranchDelaySlot)
                Process(nextInsn, null);

            logger.Debug("[eval] " + insn.AsReadable());

            if (insn is CallPtrInstruction instruction)
            {
                return Pocess(instruction);
            }
            else if (insn is ArithmeticInstruction)
            {
                return Process((ArithmeticInstruction) insn);
            }
            else if (insn is DataCopyInstruction)
            {
                var copy = (DataCopyInstruction) insn;
                Process(copy);
                return true;
            }
            else if (insn is ConditionalBranchInstruction)
            {
                Debug.Assert(condition != null);
                logger.Debug(condition.ToCode());
            }
            else
            {
                DumpState();

                logger.Warn("[raw] " + insn.AsReadable());
                _registers.Clear();
            }

            return true;
        }

        private void Process(DataCopyInstruction insn)
        {
            var copyTo = insn.Dst;
            if (copyTo is RegisterOperand operand)
            {
                _registers[operand.Register] = insn.Src.ToExpressionNode(this);
            }
            else if (copyTo is RegisterOffsetOperand && ((RegisterOffsetOperand) copyTo).Register == Register.sp)
            {
                var ofs = ((RegisterOffsetOperand) copyTo).Offset;
                // FIXME: handle non-dword data
                Debug.Assert(ofs % 4 == 0);
                // FIXME: If ofs is <0, assume it's a parameter...
                Debug.Assert(ofs >= 0 && ofs / 4 < _stack.Count);
                _stack[ofs / 4] = insn.Src.ToExpressionNode(this);
            }
            else
            {
                DumpState();

                logger.Debug(insn.ToExpressionNode(this).ToCode());
            }
        }

        private bool Process(ArithmeticInstruction arith)
        {
            var dst = arith.Destination;
            if (dst is RegisterOperand reg)
            {
                _registers[reg.Register] = arith.ToExpressionNode(this);
                if (reg.Register != Register.sp || !arith.IsInplace || !(arith.Rhs is ImmediateOperand))
                    return true;

                // stack frame size change
                var delta = (int) ((ImmediateOperand) arith.Rhs).Value;
                Debug.Assert(delta % 4 == 0);
                delta /= 4;
                if (arith.Operator == Operator.Sub)
                    delta = -delta;

                if (delta > 0)
                {
                    _stack.RemoveRange(0, delta);
                }
                else if (delta < 0)
                {
                    for (; delta < 0; ++delta)
                        _stack.Insert(0, null);
                }
            }
            else if (dst is RegisterOffsetOperand && ((RegisterOffsetOperand) dst).Register == Register.sp)
            {
                var ofs = ((RegisterOffsetOperand) dst).Offset;
                Debug.Assert(ofs % 4 == 0);
                _stack[ofs / 4] = arith.ToExpressionNode(this);
            }
            else
            {
                DumpState();

                logger.Debug(arith.ToExpressionNode(this).ToCode());
            }
            return true;
        }

        private bool Pocess(CallPtrInstruction insn)
        {
            if (insn.ReturnAddressTarget != null)
            {
                DumpState();

                if (insn.Target is LabelOperand operand)
                {
                    var fn = DebugSource.FindFunction(operand.Label);
                    if (fn != null)
                    {
                        logger.Debug("// " + fn.GetSignature());
                        // piece together the parameters
                        var parameters = new List<string>();
                        foreach (var p in fn.RegisterParameters)
                        {
                            if (_registers.TryGetValue((Register) p.Key, out var tmp))
                                parameters.Add(tmp.ToCode());
                            else
                                parameters.Add("__UNKNOWN__");
                        }
                        foreach (var p in fn.StackParameters)
                        {
                            // TODO check
                            Debug.Assert(p.Key % 4 == 0);
                            parameters.Add(_stack[p.Key / 4].ToCode());
                        }

                        if (!fn.GetSignature().StartsWith("void ")) // TODO this is ugly
                        {
                            logger.Debug($"ret = {fn.Name}({string.Join(", ", parameters)})");
                            _registers[Register.v0] = new NamedMemoryLayout("ret", 0, fn.ReturnType);
                        }
                        else
                        {
                            logger.Debug($"{fn.Name}({string.Join(", ", parameters)})");
                        }
                        return true;
                    }
                }

                logger.Debug(insn.AsReadable());
                _registers.Remove(Register.v0);
                return true;
            }

            if (insn.Target is RegisterOperand registerOperand && registerOperand.Register == Register.ra)
            {
                logger.Debug("return");
            }
            else
            {
                DumpState();

                logger.Debug("[jmp] " + insn.AsReadable());
            }
            return false;
        }

        public void DumpState()
        {
            var regDump = string.Join("; ", _registers.Select(reg => reg.Key + " = " + reg.Value.ToCode()));
            var sel = Enumerable
                .Range(0, _stack.Count)
                .Where(i => _stack[i] != null)
                .Select(i => "sp[" + (i * 4) + "] = " + _stack[i].ToCode());
            var stackDump = string.Join("; ", sel);
            logger.Debug("    # Registers: " + regDump);
            logger.Debug("    # Stack: " + stackDump);
        }

        public IExpressionNode GetRegisterExpression(int registerId)
        {
            _registers.TryGetValue((Register) registerId, out var expression);
            return expression;
        }
    }
}
