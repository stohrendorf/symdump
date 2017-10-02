using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using core;
using core.expression;
using core.instruction;
using core.operand;
using core.util;
using JetBrains.Annotations;
using mips.disasm;
using NLog;

namespace exefile.dataflow
{
    public class DataFlowState : IDataFlowState
    {
        private static readonly ILogger logger = LogManager.GetCurrentClassLogger();

        private readonly SortedDictionary<int, IExpressionNode> _registers =
            new SortedDictionary<int, IExpressionNode>();

        private readonly List<IExpressionNode> _stack = new List<IExpressionNode>();

        public IDebugSource DebugSource { get; }

        public DataFlowState([CanBeNull] IDebugSource debugSource)
        {
            DebugSource = debugSource;
        }

        public bool Apply(Instruction insn, Instruction nextInsn)
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
                Apply(nextInsn, null);

            logger.Debug("[eval] " + insn.AsReadable());

            switch (insn)
            {
                case CallPtrInstruction instruction:
                    return Apply(instruction);
                case ArithmeticInstruction instruction:
                    return Apply(instruction);
                case DataCopyInstruction instruction:
                    Apply(instruction);
                    return true;
                case ConditionalBranchInstruction _:
                    Debug.Assert(condition != null);
                    logger.Debug(condition.ToCode());
                    break;
                default:
                    DumpState(new IndentedTextWriter(Console.Out));

                    logger.Warn("[raw] " + insn.AsReadable());
                    _registers.Clear();
                    break;
            }

            return true;
        }

        private void Apply(DataCopyInstruction insn)
        {
            var copyTo = insn.Dst;
            switch (copyTo)
            {
                case RegisterOperand operand:
                    _registers[operand.Register] = insn.Src.ToExpressionNode(this);
                    break;
                case RegisterOffsetOperand registerOffsetOperand when registerOffsetOperand.Register == RegisterUtil.ToInt(Register.sp):
                    var ofs = registerOffsetOperand.Offset;
                    // FIXME: handle non-dword data
                    Debug.Assert(ofs % 4 == 0);
                    // FIXME: If ofs is <0, assume it's a parameter...
                    Debug.Assert(ofs >= 0);
                    if (ofs / 4 >= _stack.Count)
                        _stack.AddRange(Enumerable.Repeat<IExpressionNode>(null, 1 + ofs / 4 - _stack.Count));
                    _stack[ofs / 4] = insn.Src.ToExpressionNode(this);
                    break;
                default:
                    DumpState(new IndentedTextWriter(Console.Out));

                    logger.Debug(insn.ToExpressionNode(this).ToCode());
                    break;
            }
        }

        private bool Apply(ArithmeticInstruction arith)
        {
            var dst = arith.Destination;
            switch (dst)
            {
                case RegisterOperand reg:
                    _registers[reg.Register] = arith.ToExpressionNode(this);
                    if (reg.Register != RegisterUtil.ToInt(Register.sp) || !arith.IsInplace || !(arith.Rhs is ImmediateOperand))
                        return true;

                    // stack frame size change
                    var delta = (int) ((ImmediateOperand) arith.Rhs).Value;
                    Debug.Assert(delta % 4 == 0);
                    delta /= 4;
                    if (arith.Operator == Operator.Sub)
                        delta = -delta;

                    if (delta > 0)
                    {
                        _stack.RemoveRange(0, Math.Min(_stack.Count, delta));
                    }
                    else if (delta < 0)
                    {
                        for (; delta < 0; ++delta)
                            _stack.Insert(0, null);
                    }
                    break;
                case RegisterOffsetOperand registerOffsetOperand when registerOffsetOperand.Register == RegisterUtil.ToInt(Register.sp):
                    var ofs = registerOffsetOperand.Offset;
                    Debug.Assert(ofs % 4 == 0);
                    if (ofs / 4 >= _stack.Count)
                        _stack.AddRange(Enumerable.Repeat<IExpressionNode>(null, 1 + ofs / 4 - _stack.Count));
                    _stack[ofs / 4] = arith.ToExpressionNode(this);
                    break;
                default:
                    DumpState(new IndentedTextWriter(Console.Out));

                    logger.Debug(arith.ToExpressionNode(this).ToCode());
                    break;
            }
            return true;
        }

        private bool Apply(CallPtrInstruction insn)
        {
            if (insn.ReturnAddressTarget != null)
            {
                DumpState(new IndentedTextWriter(Console.Out));

                if (insn.Target is LabelOperand operand)
                {
                    var fn = DebugSource?.FindFunction(operand.Label);
                    if (fn != null)
                    {
                        logger.Debug("// " + fn.GetSignature());
                        // piece together the parameters
                        var parameters = new List<string>();
                        foreach (var p in fn.RegisterParameters)
                        {
                            if (_registers.TryGetValue(p.Key, out var tmp))
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
                            _registers[RegisterUtil.ToInt(Register.v0)] =
                                new NamedMemoryLayout("ret", 0, fn.ReturnType);
                        }
                        else
                        {
                            logger.Debug($"{fn.Name}({string.Join(", ", parameters)})");
                        }
                        return true;
                    }
                }

                logger.Debug(insn.AsReadable());
                _registers.Remove(RegisterUtil.ToInt(Register.v0));
                return true;
            }

            if (insn.Target is RegisterOperand registerOperand && registerOperand.Register == RegisterUtil.ToInt(Register.ra))
            {
                logger.Debug("return");
            }
            else
            {
                DumpState(new IndentedTextWriter(Console.Out));

                logger.Debug("[jmp] " + insn.AsReadable());
            }
            return false;
        }

        public void DumpState(IndentedTextWriter writer)
        {
            if (_registers.Count > 0)
            {
                writer.WriteLine("// Registers:");
                ++writer.Indent;
                foreach (var reg in _registers)
                {
                    writer.WriteLine("// $" + reg.Key + " = " + reg.Value.ToCode());
                }
                --writer.Indent;
            }

            var sel = Enumerable
                .Range(0, _stack.Count)
                .Where(i => _stack[i] != null)
                .ToList();

            if (!sel.Any())
                return;

            writer.WriteLine("// Stack:");
            ++writer.Indent;
            foreach (var s in sel.Select(i => $"// sp[{i * 4}] = {_stack[i].ToCode()}"))
            {
                writer.WriteLine(s);
            }
            --writer.Indent;
        }

        public IExpressionNode GetRegisterExpression(int registerId)
        {
            _registers.TryGetValue(registerId, out var expression);
            return expression;
        }
    }
}
