﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using symdump.util;

namespace symdump.symfile.type
{
    public class StructDef : ICompoundType, IEquatable<StructDef>
    {
        public readonly List<StructMember> members = new List<StructMember>();
        public readonly string name;

        public StructDef(BinaryReader stream, string name, SymFile symFile)
        {
            this.name = name;
            while (true)
            {
                var typedValue = new TypedValue(stream);
                if (typedValue.type == (0x80 | 20))
                {
                    var m = new StructMember(typedValue, stream, false, symFile);

                    if (m.typeInfo.classType == ClassType.EndOfStruct)
                        break;

                    members.Add(m);
                }
                else if (typedValue.type == (0x80 | 22))
                {
                    var m = new StructMember(typedValue, stream, true, symFile);

                    if (m.typeInfo.classType == ClassType.EndOfStruct)
                        break;

                    members.Add(m);
                }
                else
                {
                    throw new Exception("Unexpected entry");
                }
            }
        }

        public bool isFake => new Regex(@"^\.\d+fake$").IsMatch(name);

        public bool Equals(StructDef other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return members.SequenceEqual(other.members) && string.Equals(name, other.name);
        }

        public override string ToString()
        {
            return name;
        }

        public void dump(IndentedTextWriter writer)
        {
            writer.WriteLine($"struct {name} {{");
            ++writer.indent;
            foreach (var m in members)
                writer.WriteLine(m);
            --writer.indent;
            writer.WriteLine("};");
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((StructDef) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((members != null ? members.GetHashCode() : 0) * 397) ^ (name != null ? name.GetHashCode() : 0);
            }
        }

        public StructMember forOffset(uint ofs)
        {
            return members
                .LastOrDefault(m => m.typeInfo.classType != ClassType.Bitfield && m.typedValue.value <= ofs);
        }

        public string tryDeref(uint ofs)
        {
            var member = forOffset(ofs);

            if (member == null)
                return null;

            if (!(member.compoundType is StructDef))
                return member.name;

            var sdef = (StructDef) member.compoundType;
            
            ofs -= (uint) member.typedValue.value;
            var subMember = sdef.forOffset(ofs);
            if (subMember == null)
                return null;

            return member.name + "." + sdef.tryDeref(ofs);
        }
    }
}
