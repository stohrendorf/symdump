using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using symdump.symfile.util;
using symdump.util;

namespace symdump.symfile
{
    public class EnumDef : IEquatable<EnumDef>
    {
        private readonly Dictionary<string, int> _members = new Dictionary<string, int>();
        private readonly string _name;
        private readonly uint _size;

        public EnumDef(BinaryReader stream, string name)
        {
            _name = name;
            while (true)
            {
                var typedValue = new TypedValue(stream);
                if (typedValue.Type == (0x80 | TypedValue.Definition))
                {
                    var taggedSymbol = stream.ReadTaggedSymbol(false);
                    var memberName = stream.ReadPascalString();

                    if (taggedSymbol.Type == SymbolType.EndOfStruct)
                        break;

                    if (taggedSymbol.Type != SymbolType.EnumMember)
                        throw new Exception($"Unexpected {nameof(SymbolType)}");

                    _members.Add(memberName, typedValue.Value);
                }
                else if (typedValue.Type == (0x80 | TypedValue.ArrayDefinition))
                {
                    var taggedSymbol = stream.ReadTaggedSymbol(true);
                    if (taggedSymbol.DerivedTypeDef.Type != PrimitiveType.Null)
                        throw new Exception(
                            $"Expected baseType={PrimitiveType.Null}, but it's {taggedSymbol.DerivedTypeDef.Type}");

                    if (taggedSymbol.Extents.Length != 0)
                        throw new Exception($"Expected dims=0, but it's {taggedSymbol.Extents.Length}");

                    if (taggedSymbol.Tag != name)
                        throw new Exception($"Expected name={name}, but it's {taggedSymbol.Tag}");

                    var tag = stream.ReadPascalString();
                    if (tag != ".eos")
                        throw new Exception($"Expected tag=.eos, but it's {tag}");

                    if (taggedSymbol.Type != SymbolType.EndOfStruct)
                        throw new Exception(
                            $"Expected {nameof(SymbolType)}={SymbolType.EndOfStruct}, but it's {taggedSymbol.Type}");

                    _size = taggedSymbol.Size;
                    break;
                }
                else
                {
                    throw new Exception("Unexpected entry");
                }
            }
        }

        public bool IsFake => _name.IsFake();

        public bool Equals(EnumDef other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return _members.SequenceEqual(other._members) && string.Equals(_name, other._name);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((EnumDef) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (_members.GetHashCode() * 397) ^ _name.GetHashCode();
            }
        }

        public void Dump(IndentedTextWriter writer, bool forInline)
        {
            string cType;
            switch (_size)
            {
                case 1:
                    cType = "char";
                    break;
                case 2:
                    cType = "short";
                    break;
                case 4:
                    cType = "int";
                    break;
                default:
                    throw new Exception($"Cannot determine primitive type for size {_size}");
            }

            writer.WriteLine(forInline ? $"enum : {cType} {{" : $"enum {_name} : {cType} {{");
            ++writer.Indent;
            foreach (var (key, value) in _members)
                writer.WriteLine($"{key} = {value},");
            --writer.Indent;
            if (forInline)
                writer.Write("}");
            else
                writer.WriteLine("};");
        }
    }
}
