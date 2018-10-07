using symdump.exefile.operands;

namespace symdump.exefile.instructions
{
    public class WordData : Instruction
    {
        private readonly uint _data;

        public WordData(uint data)
        {
            _data = data;
        }

        public override IOperand[] Operands { get; } = new IOperand[0];

        public override string ToString()
        {
            return $".word 0x{_data:x}";
        }

        public override string AsReadable()
        {
            return ToString();
        }
    }
}
