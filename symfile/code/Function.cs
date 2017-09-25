using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using core;
using core.util;
using mips.disasm;
using symfile.type;
using symfile.util;

namespace symfile.code
{
    public class Function : IFunction
    {
        public class ArgumentInfo : IDeclaration
        {
            public string Name { get; }

            public IMemoryLayout MemoryLayout => TypeDecoration.MemoryLayout;

            public readonly TypeDecoration TypeDecoration;
            public readonly Register StackBase;
            public readonly uint? StackOffset;
            public readonly Register? Register;

            public ArgumentInfo(string name, TypeDecoration typeDecoration, Register stackBase, uint? stackOffset, Register? register)
            {
                Name = name;
                TypeDecoration = typeDecoration;
                StackBase = stackBase;
                StackOffset = stackOffset;
                Register = register;
            }

            public override string ToString()
            {
                if (TypeDecoration.ClassType == ClassType.Argument)
                {
                    Debug.Assert(StackOffset != null);
                    return $"{TypeDecoration.AsDeclaration(Name)} /*${StackBase} {StackOffset}*/";
                }
                else if (TypeDecoration.ClassType == ClassType.RegParam)
                {
                    Debug.Assert(Register != null);
                    return $"{TypeDecoration.AsDeclaration(Name)} /*${Register}*/";
                }
                else
                    throw new Exception("Meh");
            }
        }

        public uint GlobalAddress { get; }
        public IMemoryLayout ReturnType => _returnType.MemoryLayout;
        private readonly Block _body;
        private readonly string _file;
        private readonly uint _line;
        private readonly uint _mask;
        private readonly int _maskOffs;
        public string Name { get; }

        private readonly IDictionary<Register, ArgumentInfo> _registerParameters =
            new SortedDictionary<Register, ArgumentInfo>();

        public IEnumerable<KeyValuePair<int, IDeclaration>> RegisterParameters =>
            _registerParameters.Select(p => new KeyValuePair<int, IDeclaration>((int) p.Key, p.Value));

        private readonly IDictionary<int, ArgumentInfo> _stackParameters = new SortedDictionary<int, ArgumentInfo>();
        
        public IEnumerable<KeyValuePair<int, IDeclaration>> StackParameters =>
            _stackParameters.Select(p => new KeyValuePair<int, IDeclaration>(p.Key, p.Value));
        
        private readonly Register _returnAddressRegister;
        private readonly TypeDecoration _returnType;
        private readonly Register _stackBase;
        private readonly uint _stackFrameSize;

        public Function(BinaryReader reader, uint ofs, SymFile symFile)
        {
            GlobalAddress = ofs;

            _stackBase = (Register) reader.ReadUInt16();
            _stackFrameSize = reader.ReadUInt32();
            _returnAddressRegister = (Register) reader.ReadUInt16();
            _mask = reader.ReadUInt32();
            _maskOffs = reader.ReadInt32();

            _line = reader.ReadUInt32();
            _file = reader.ReadPascalString();
            Name = reader.ReadPascalString();

            _body = new Block(GlobalAddress, _line, this, symFile);

            symFile.FuncTypes.TryGetValue(Name, out _returnType);

            while (true)
            {
                var typedValue = new FileEntry(reader);

                if (reader.SkipSld(typedValue))
                    continue;

                TypeDecoration ti;
                string memberName;
                switch (typedValue.Type & 0x7f)
                {
                    case 14: // end of function
                        reader.ReadUInt32();
                        return;
                    case 16: // begin of block
                        _body.SubBlocks.Add(new Block(reader, (uint) typedValue.Value, reader.ReadUInt32(), this,
                            symFile));
                        continue;
                    case 20:
                        ti = reader.ReadTypeDecoration(false, symFile);
                        memberName = reader.ReadPascalString();
                        break;
                    case 22:
                        ti = reader.ReadTypeDecoration(true, symFile);
                        memberName = reader.ReadPascalString();
                        break;
                    default:
                        throw new Exception("Nope");
                }

                if (ti == null || memberName == null)
                    break;

                switch (ti.ClassType)
                {
                    case ClassType.Argument:
                        //Debug.Assert(m_registerParameters.Count >= 4);
                        _stackParameters[_stackParameters.Count * 4] = new ArgumentInfo(memberName, ti, _stackBase, (uint) (_stackParameters.Count * 4), null);
                        break;
                    case ClassType.RegParam:
                        Debug.Assert(_registerParameters.Count < 4);
                        _registerParameters[Register.a0 + _registerParameters.Count] = new ArgumentInfo(memberName, ti, _stackBase, null, Register.a0 + _registerParameters.Count);
                        break;
                    default:
                        _body.Vars.Add(memberName, new Block.VarInfo(memberName, ti, typedValue));
                        break;
                }
            }

            throw new Exception("Should never reach this");
        }

        private IEnumerable<Register> SavedRegisters => Enumerable.Range(0, 32)
            .Where(i => ((1 << i) & _mask) != 0)
            .Select(i => (Register) i);

        public void Dump(IndentedTextWriter writer)
        {
            writer.WriteLine("/*");
            writer.WriteLine($" * Offset 0x{GlobalAddress:X}");
            writer.WriteLine($" * {_file} (line {_line})");
            writer.WriteLine($" * Stack frame base ${_stackBase}, size {_stackFrameSize}");
            writer.WriteLine($" * Caller return address in ${_returnAddressRegister}");
            if (_mask != 0)
                writer.WriteLine($" * Saved registers at offset {_maskOffs}: {string.Join(" ", SavedRegisters)}");
            writer.WriteLine(" */");

            writer.WriteLine(GetSignature());

            _body.Dump(writer);
        }

        public string GetSignature()
        {
            var parameters = _registerParameters.Values.Concat(_stackParameters.Values);
            Debug.Assert(_returnType != null);
            return _returnType?.AsDeclaration(Name, string.Join(", ", parameters));
        }
    }
}
