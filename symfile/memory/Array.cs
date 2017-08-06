using System;
using core;
using core.util;
using symfile.type;
using Xunit;

namespace symfile.memory
{
    public class Array : IMemoryLayout, IEquatable<Array>
    {
        public int Precedence => Operator.Array.GetPrecedence(false);

        public string FundamentalType => Inner.FundamentalType;

        public uint DataSize => Dimension * Inner.DataSize;

        public IMemoryLayout Inner { get; }

        public IMemoryLayout Pointee => null;

        public readonly uint Dimension;

        public Array(uint dimension, IMemoryLayout inner)
        {
            Dimension = dimension;
            Inner = inner;
        }

        public string AsIncompleteDeclaration(string identifier, string argList)
        {
            var innerCode = Inner.AsIncompleteDeclaration(identifier, argList);
            return Inner.Precedence >= Precedence
                ? $"({innerCode})[{Dimension}]"
                : $"{innerCode}[{Dimension}]";
        }

        public string GetAccessPathTo(uint offset)
        {
            var idx = offset / Inner.DataSize;
            var subOfs = offset % Inner.DataSize;
            var innerAccess = Inner.GetAccessPathTo(subOfs);
            if (innerAccess == null)
                return $"[{idx}]";
            
            if(Inner is Array)
                return $"[{idx}]{innerAccess}";
            else
                return $"[{idx}].{innerAccess}";
        }

        public bool Equals(Array other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Dimension == other.Dimension && Equals(Inner, other.Inner);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((Array) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((int) Dimension * 397) ^ (Inner != null ? Inner.GetHashCode() : 0);
            }
        }
    }

    public static class ArrayTest
    {
        [Fact]
        public static void testDeclarationSimple()
        {
            var tmp = new Array(10, new PrimitiveType(BaseType.Char));
            Assert.Equal("foo[10]", tmp.AsIncompleteDeclaration("foo", null));
            Assert.Equal("char", tmp.FundamentalType);
            Assert.Equal("[3]", tmp.GetAccessPathTo(3));
        }

        [Fact]
        public static void testDeclarationPrecedence()
        {
            var tmp = new Array(10, new Pointer(new PrimitiveType(BaseType.Char)));
            Assert.Equal("(*foo)[10]", tmp.AsIncompleteDeclaration("foo", null));
            Assert.Equal("char", tmp.FundamentalType);
            Assert.Equal("[1]", tmp.GetAccessPathTo(4));
        }
    }
}
