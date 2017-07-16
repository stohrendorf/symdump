using System;
using System.IO;

namespace core.util
{
    public class EndianBinaryReader : IDisposable
    {
        private BinaryReader m_stream;

        public EndianBinaryReader(Stream s)
            : this(new BinaryReader(s))
        {
        }

        public EndianBinaryReader(BinaryReader stream)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));
            if (!stream.BaseStream.CanRead)
                throw new ArgumentException("Stream isn't readable", nameof(stream));
            m_stream = stream;
        }

        public Stream baseStream => m_stream.BaseStream;

        public void Dispose()
        {
            m_stream.Dispose();
            m_stream = null;
        }

        public byte[] readBytes(int n)
        {
            return m_stream.ReadBytes(n);
        }

        public byte readByte()
        {
            return m_stream.ReadByte();
        }

        public sbyte readSByte()
        {
            return m_stream.ReadSByte();
        }

        public short readInt16()
        {
            var tmp = m_stream.ReadBytes(2);
            return (short) ((tmp[1] << 8) | tmp[0]);
        }

        public int readInt32()
        {
            var tmp = m_stream.ReadBytes(4);
            return (tmp[3] << 24) | (tmp[2] << 16) | (tmp[1] << 8) | tmp[0];
        }

        public ushort readUInt16()
        {
            return (ushort) readInt16();
        }

        public uint readUInt32()
        {
            return (uint) readInt32();
        }
    }
}
