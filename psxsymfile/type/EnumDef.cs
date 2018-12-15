using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using core;
using core.util;
using symfile.util;

namespace symfile.type
{
    public class EnumDef : IEquatable<EnumDef>
    {
        private readonly Dictionary<string, int> _members = new Dictionary<string, int>();
        private readonly string _name;
        private readonly uint _size;

        public EnumDef(BinaryReader stream, string name, IDebugSource debugSource)
        {
            _name = name;
            while (true)
            {
                var typedValue = new FileEntry(stream);
                if (typedValue.Type == (0x80 | 20))
                {
                    var ti = stream.ReadTypeDecoration(false, debugSource);
                    var memberName = stream.ReadPascalString();

                    if (ti.ClassType == ClassType.EndOfStruct)
                        break;

                    if (ti.ClassType != ClassType.EnumMember)
                        throw new Exception("Unexpected class");

                    _members.Add(memberName, typedValue.Value);
                }
                else if (typedValue.Type == (0x80 | 22))
                {
                    var ti = stream.ReadTypeDecoration(true, debugSource);
                    if (ti.BaseType != BaseType.Null)
                        throw new Exception($"Expected baseType={BaseType.Null}, but it's {ti.BaseType}");

                    if (ti.Dimensions.Length != 0)
                        throw new Exception($"Expected dims=0, but it's {ti.Dimensions.Length}");

                    if (ti.Tag != name)
                        throw new Exception($"Expected name={name}, but it's {ti.Tag}");

                    var tag = stream.ReadPascalString();
                    if (tag != ".eos")
                        throw new Exception($"Expected tag=.eos, but it's {tag}");

                    if (ti.ClassType != ClassType.EndOfStruct)
                        throw new Exception($"Expected classType={ClassType.EndOfStruct}, but it's {ti.ClassType}");

                    _size = ti.Size;
                    break;
                }
                else
                {
                    throw new Exception("Unexpected entry");
                }
            }
        }

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

        public void Dump(IndentedTextWriter writer)
        {
            string ctype;
            switch (_size)
            {
                case 1:
                    ctype = "char";
                    break;
                case 2:
                    ctype = "short";
                    break;
                case 4:
                    ctype = "int";
                    break;
                default:
                    throw new Exception($"Cannot determine primitive type for size {_size}");
            }

            writer.WriteLine($"enum {_name} : {ctype} {{");
            ++writer.Indent;
            foreach (var kvp in _members)
                writer.WriteLine($"{kvp.Key} = {kvp.Value},");
            --writer.Indent;
            writer.WriteLine("};");
        }
    }
}