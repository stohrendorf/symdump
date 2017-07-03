using System;
using System.IO;

namespace symdump.exefile.util
{
    public class EndianBinaryReader : IDisposable
    {
        public EndianBinaryReader(Stream s)
            : this(new BinaryReader(s))
        {
        }

        public EndianBinaryReader(BinaryReader stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException("stream");
            }
            if (!stream.BaseStream.CanRead)
            {
                throw new ArgumentException("Stream isn't writable", "stream");
            }
            m_stream = stream;
        }

        BinaryReader m_stream;

        public Stream baseStream => m_stream.BaseStream;

        public byte[] readBytes(int n) => m_stream.ReadBytes(n);

        public byte readByte() => m_stream.ReadByte();

        public sbyte readSByte() => m_stream.ReadSByte();

        public short readInt16()
        {
            var tmp = m_stream.ReadBytes(2);
            return (short) ((tmp[0] << 8) | tmp[1]);
        }

        public int readInt32()
        {
            var tmp = m_stream.ReadBytes(4);
            return (tmp[0] << 8) | (tmp[1] << 16) | (tmp[2] << 24) | tmp[3];
        }

        public ushort readUInt16() => (ushort) readInt16();
        public uint readUInt32() => (uint) readInt32();

        public void Dispose()
        {
            m_stream.Dispose();
            m_stream = null;
        }
    }
}