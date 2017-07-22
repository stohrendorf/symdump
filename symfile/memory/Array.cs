using System;
using core;
using core.util;
using symfile.type;
using Xunit;

namespace symfile.memory
{
    public class Array : IMemoryLayout, IEquatable<Array>
    {
        public int precedence => Operator.Array.getPrecedence(false);

        public string fundamentalType => inner.fundamentalType;

        public uint dataSize => dimension * inner.dataSize;

        public IMemoryLayout inner { get; }

        public IMemoryLayout pointee => null;

        public readonly uint dimension;

        public Array(uint dimension, IMemoryLayout inner)
        {
            this.dimension = dimension;
            this.inner = inner;
        }

        public string asIncompleteDeclaration(string identifier, string argList)
        {
            var innerCode = inner.asIncompleteDeclaration(identifier, argList);
            return inner.precedence >= precedence
                ? $"({innerCode})[{dimension}]"
                : $"{innerCode}[{dimension}]";
        }

        public string getAccessPathTo(uint offset)
        {
            var idx = offset / inner.dataSize;
            var subOfs = offset % inner.dataSize;
            var innerAccess = inner.getAccessPathTo(subOfs);
            return $"[{idx}]{innerAccess}";
        }

        public bool Equals(Array other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return dimension == other.dimension && Equals(inner, other.inner);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Array) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((int) dimension * 397) ^ (inner != null ? inner.GetHashCode() : 0);
            }
        }
    }

    public static class ArrayTest
    {
        [Fact]
        public static void testDeclarationSimple()
        {
            var tmp = new Array(10, new PrimitiveType(BaseType.Char));
            Assert.Equal("foo[10]", tmp.asIncompleteDeclaration("foo", null));
            Assert.Equal("char", tmp.fundamentalType);
            Assert.Equal("[3]", tmp.getAccessPathTo(3));
        }

        [Fact]
        public static void testDeclarationPrecedence()
        {
            var tmp = new Array(10, new Pointer(new PrimitiveType(BaseType.Char)));
            Assert.Equal("(*foo)[10]", tmp.asIncompleteDeclaration("foo", null));
            Assert.Equal("char", tmp.fundamentalType);
            Assert.Equal("[1]", tmp.getAccessPathTo(4));
        }
    }
}
