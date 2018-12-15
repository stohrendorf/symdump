using System;
using System.IO;
using JetBrains.Annotations;

namespace core.util
{
    public class EndianBinaryReader : IDisposable
    {
        [NotNull] private readonly BinaryReader _stream;

        public EndianBinaryReader(Stream s)
            : this(new BinaryReader(s))
        {
        }

        private EndianBinaryReader(BinaryReader stream)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));
            if (!stream.BaseStream.CanRead)
                throw new ArgumentException("Stream isn't readable", nameof(stream));
            _stream = stream;
        }

        public Stream BaseStream => _stream.BaseStream;

        public void Dispose()
        {
            _stream.Dispose();
        }

        public byte[] ReadBytes(int n)
        {
            return _stream.ReadBytes(n);
        }

        private int ReadInt32()
        {
            var tmp = _stream.ReadBytes(4);
            return (tmp[3] << 24) | (tmp[2] << 16) | (tmp[1] << 8) | tmp[0];
        }

        public uint ReadUInt32()
        {
            return (uint) ReadInt32();
        }
    }
}