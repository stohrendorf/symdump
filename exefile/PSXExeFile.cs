using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using core;
using core.microcode;
using core.util;
using exefile.processor;

namespace exefile
{
    public class PSXExeFile
    {
        private readonly uint _entrypoint;
        private readonly R3000 _r3000;

        public PSXExeFile(EndianBinaryReader reader, IDebugSource debugSource)
        {
            reader.BaseStream.Seek(0, SeekOrigin.Begin);

            var header = new Header(reader);
            _entrypoint = header.pc0;
            reader.BaseStream.Seek(0x800, SeekOrigin.Begin);
            var data = reader.ReadBytes((int) (reader.BaseStream.Length - reader.BaseStream.Position));

            var gpBase = debugSource.Labels
                .Where(byOffset => byOffset.Value.Any(lbl => lbl.Name.Equals("__SN_GP_BASE")))
                .Select(lbl => lbl.Key)
                .Cast<uint?>()
                .FirstOrDefault();

            TextSection = new TextSection(data, header.tAddr);

            _r3000 = new R3000(TextSection, gpBase, debugSource);
        }

        public TextSection TextSection { get; }

        public MicroAssemblyBlock BlockAtLocal(uint addr)
        {
            return TextSection.Instructions.TryGetValue(addr, out var x) ? x : null;
        }

        public void Disassemble()
        {
            _r3000.Disassemble(_entrypoint);
        }

        [SuppressMessage("ReSharper", "NotAccessedField.Local")]
        [SuppressMessage("ReSharper", "MemberCanBePrivate.Local")]
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        private class Header
        {
            public readonly uint bAddr;
            public readonly uint bSize;
            public readonly uint dAddr;
            public readonly uint data;
            public readonly uint dSize;
            public readonly uint gp0;
            public readonly char[] id;
            public readonly uint pc0;
            public readonly uint sAddr;
            public readonly uint savedFp;
            public readonly uint savedGp;
            public readonly uint savedRa;
            public readonly uint savedS0;
            public readonly uint savedSp;
            public readonly uint sSize;
            public readonly uint tAddr;
            public readonly uint text;
            public readonly uint tSize;

            public Header(EndianBinaryReader reader)
            {
                id = reader.ReadBytes(8).Select(b => (char) b).ToArray();

                if (!"PS-X EXE".Equals(new string(id)))
                    throw new Exception("Header ID mismatch");

                text = reader.ReadUInt32();
                data = reader.ReadUInt32();
                pc0 = reader.ReadUInt32();
                gp0 = reader.ReadUInt32();
                tAddr = reader.ReadUInt32();
                tSize = reader.ReadUInt32();
                dAddr = reader.ReadUInt32();
                dSize = reader.ReadUInt32();
                bAddr = reader.ReadUInt32();
                bSize = reader.ReadUInt32();
                sAddr = reader.ReadUInt32();
                sSize = reader.ReadUInt32();
                savedSp = reader.ReadUInt32();
                savedFp = reader.ReadUInt32();
                savedGp = reader.ReadUInt32();
                savedRa = reader.ReadUInt32();
                savedS0 = reader.ReadUInt32();
            }
        }
    }
}