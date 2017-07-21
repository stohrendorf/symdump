using System.IO;
using System.Text;
using JetBrains.Annotations;

namespace core.util
{
    public class IndentedTextWriter : TextWriter
    {
        [NotNull] private readonly TextWriter m_inner;
        private bool m_indent = true;

        public IndentedTextWriter(TextWriter inner)
        {
            m_inner = inner;
        }

        public int indent { get; set; }

        public override Encoding Encoding => m_inner.Encoding;

        public override void Write(char ch)
        {
            if (m_indent)
            {
                m_indent = false;
                for (var i = 0; i < indent; ++i)
                    m_inner.Write("  ");
            }
            m_inner.Write(ch);
            if (ch == '\n')
                m_indent = true;
        }
    }
}