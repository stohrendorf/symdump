using System.IO;
using System.Text;

namespace symdump.util
{
    public class IndentedTextWriter : TextWriter
    {
        private readonly TextWriter _inner;
        private bool _indent = true;

        public IndentedTextWriter(TextWriter inner)
        {
            _inner = inner;
        }

        public int Indent { get; set; }

        public override Encoding Encoding => _inner.Encoding;

        public override void Write(char ch)
        {
            if (_indent)
            {
                _indent = false;
                for (var i = 0; i < Indent; ++i)
                    _inner.Write("  ");
            }

            _inner.Write(ch);
            if (ch == '\n')
                _indent = true;
        }
    }
}
