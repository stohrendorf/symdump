using System;
using core;
using symfile.type;
using Xunit;

namespace symfile.memory
{
    public class Array : IMemoryLayout, IEquatable<Array>
    {
        private readonly uint _dimension;

        private readonly IMemoryLayout _inner;

        public Array(uint dimension, IMemoryLayout inner)
        {
            _dimension = dimension;
            _inner = inner;
        }

        public bool Equals(Array other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return _dimension == other._dimension && Equals(_inner, other._inner);
        }

        public int Precedence => 1;

        public string FundamentalType => _inner.FundamentalType;

        public uint DataSize => _dimension * _inner.DataSize;

        public string AsIncompleteDeclaration(string identifier, string argList)
        {
            var innerCode = _inner.AsIncompleteDeclaration(identifier, argList);
            return _inner.Precedence >= Precedence
                ? $"({innerCode})[{_dimension}]"
                : $"{innerCode}[{_dimension}]";
        }

        public string GetAccessPathTo(uint offset)
        {
            var idx = offset / _inner.DataSize;
            var subOfs = offset % _inner.DataSize;
            var innerAccess = _inner.GetAccessPathTo(subOfs);
            if (innerAccess == null)
                return $"[{idx}]";

            if (_inner is Array)
                return $"[{idx}]{innerAccess}";
            return $"[{idx}].{innerAccess}";
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
                return ((int) _dimension * 397) ^ (_inner != null ? _inner.GetHashCode() : 0);
            }
        }
    }

    public static class ArrayTest
    {
        [Fact]
        public static void TestAccessSimple()
        {
            var tmp = new Array(10, new PrimitiveType(BaseType.Char));
            Assert.Equal("[3]", tmp.GetAccessPathTo(3));
        }

        [Fact]
        public static void TestAccessSized()
        {
            var tmp = new Array(10, new PrimitiveType(BaseType.Int));
            Assert.Equal("[1]", tmp.GetAccessPathTo(4));
        }

        [Fact]
        public static void TestAccessSizedUnaligned()
        {
            var tmp = new Array(10, new PrimitiveType(BaseType.Int));
            Assert.Throws<UnalignedAccessException>(() => tmp.GetAccessPathTo(1));
        }

        [Fact]
        public static void TestDeclarationPrecedence()
        {
            var tmp = new Array(10, new Pointer(new PrimitiveType(BaseType.Char)));
            Assert.Equal("(*foo)[10]", tmp.AsIncompleteDeclaration("foo", null));
            Assert.Equal("char", tmp.FundamentalType);
        }

        [Fact]
        public static void TestDeclarationSimple()
        {
            var tmp = new Array(10, new PrimitiveType(BaseType.Char));
            Assert.Equal("foo[10]", tmp.AsIncompleteDeclaration("foo", null));
            Assert.Equal("char", tmp.FundamentalType);
        }
    }
}