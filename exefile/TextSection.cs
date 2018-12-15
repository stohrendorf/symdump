using System;
using System.Collections.Generic;
using System.Linq;
using core.microcode;

namespace exefile
{
    public class TextSection
    {
        private readonly uint _baseAddress;
        private readonly byte[] _data;

        public readonly IDictionary<uint, ISet<uint>> CalleesBySource = new Dictionary<uint, ISet<uint>>();

        public readonly IDictionary<uint, MicroAssemblyBlock> Instructions =
            new SortedDictionary<uint, MicroAssemblyBlock>();

        public TextSection(byte[] data, uint baseAddress)
        {
            _data = data;
            _baseAddress = baseAddress;
        }

        public int Size => _data.Length;

        public uint MakeGlobal(uint addr)
        {
            return addr + _baseAddress;
        }

        public uint MakeLocal(uint addr)
        {
            if (addr < _baseAddress /*TODO || addr >= BaseAddress + _header.tSize*/)
                throw new ArgumentOutOfRangeException(nameof(addr), "Address out of range to make local");

            return addr - _baseAddress;
        }

        public uint WordAtLocal(uint address)
        {
            uint data;
            data = _data[address++];
            data |= (uint) _data[address++] << 8;
            data |= (uint) _data[address++] << 16;
            // ReSharper disable once RedundantAssignment
            data |= (uint) _data[address++] << 24;
            return data;
        }

        private void DropFnDisassembly(uint localAddress)
        {
            var dropQueue = new Queue<uint>();
            dropQueue.Enqueue(localAddress);

            while (dropQueue.Count > 0)
            {
                localAddress = dropQueue.Dequeue();
                if (!Instructions.TryGetValue(localAddress, out var block))
                    continue;

                Instructions.Remove(localAddress);

                foreach (var addr in block.Outs
                    .Where(addr => addr.Key < Size)
                    .Where(addr =>
                        addr.Value != JumpType.Call && addr.Value != JumpType.CallConditional &&
                        addr.Value != JumpType.Control)
                    .Select(addr => addr.Key))
                    dropQueue.Enqueue(addr);
            }
        }

        public void CollectFunctionBlocks(uint functionAddr)
        {
            var q = new Queue<uint>();
            q.Enqueue(functionAddr);

            var blocks = new HashSet<uint>();

            while (q.Count > 0)
            {
                var blockAddr = q.Dequeue();
                if (blocks.Contains(blockAddr))
                    continue;

                var block = Instructions.TryGetValue(blockAddr, out var x) ? x : null;
                if (block == null)
                    continue;

                blocks.Add(blockAddr);
                block.OwningFunctions.Add(functionAddr);

                foreach (var o in block.Outs)
                    switch (o.Value)
                    {
                        case JumpType.Call:
                        case JumpType.CallConditional:
                            break;
                        case JumpType.Jump:
                        case JumpType.JumpConditional:
                        case JumpType.Control:
                            q.Enqueue(o.Key);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
            }
        }
    }
}