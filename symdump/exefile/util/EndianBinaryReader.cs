using System;
using System.Diagnostics;
using System.IO;

namespace symdump.exefile.util
{
    public class EndianBinaryReader : IDisposable
    {
        private BinaryReader? _stream;

        public EndianBinaryReader(Stream s)
            : this(new BinaryReader(s))
        {
        }

        private EndianBinaryReader(BinaryReader? stream)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));
            if (!stream.BaseStream.CanRead)
                throw new ArgumentException("Stream isn't readable", nameof(stream));
            _stream = stream;
        }

        public Stream? BaseStream => _stream?.BaseStream;

        public void Dispose()
        {
            _stream?.Dispose();
            _stream = null;
        }

        public byte[] ReadBytes(int n)
        {
            Debug.Assert(_stream != null);
            return _stream.ReadBytes(n);
        }

        public byte ReadByte()
        {
            Debug.Assert(_stream != null);
            return _stream.ReadByte();
        }

        public sbyte ReadSByte()
        {
            Debug.Assert(_stream != null);
            return _stream.ReadSByte();
        }

        private short ReadInt16()
        {
            Debug.Assert(_stream != null);
            var tmp = _stream.ReadBytes(2);
            return (short) ((tmp[1] << 8) | tmp[0]);
        }

        private int ReadInt32()
        {
            Debug.Assert(_stream != null);
            var tmp = _stream.ReadBytes(4);
            return (tmp[3] << 24) | (tmp[2] << 16) | (tmp[1] << 8) | tmp[0];
        }

        public ushort ReadUInt16()
        {
            return (ushort) ReadInt16();
        }

        public uint ReadUInt32()
        {
            return (uint) ReadInt32();
        }
    }
}
