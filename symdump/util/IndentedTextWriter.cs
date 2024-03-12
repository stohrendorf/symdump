using System;
using System.IO;
using System.Text;

namespace symdump.util
{
    public class IndentedTextWriter(TextWriter inner) : TextWriter
    {
        private bool _indent = true;
        private int _indentSize;

        public int Indent
        {
            get => _indentSize;
            set => _indentSize = Math.Max(value, 0);
        }

        public override Encoding Encoding => inner.Encoding;

        public override void Write(char ch)
        {
            if (_indent)
            {
                _indent = false;
                for (var i = 0; i < Indent; ++i)
                    inner.Write("  ");
            }

            inner.Write(ch);
            if (ch == '\n')
                _indent = true;
        }
    }
}
