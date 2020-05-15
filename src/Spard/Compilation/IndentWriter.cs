using System;
using System.IO;
using System.Text;

namespace Spard.Compilation
{
    internal sealed class IndentWriter: IDisposable
    {
        private readonly TextWriter _writer;
        private readonly bool _disposeWriter;

        public int Indent { get; set; }

        public IndentWriter(TextWriter writer)
        {
            _writer = writer;
        }

        public IndentWriter(StringBuilder sb)
        {
            _writer = new StringWriter(sb);
            _disposeWriter = true;
        }

        public void Write(string s)
        {
            _writer.Write(s);
        }

        public void WriteLine()
        {
            _writer.WriteLine();
        }

        public void WriteLine(string s, bool withIndent = true)
        {
            if (s == "}")
                Indent--;

            var tab = new string(' ', Indent * 4);
            _writer.WriteLine((withIndent ? tab : "") + s);

            if (s == "{")
                Indent++;
        }

        public void WriteLine(string format, params object[] arg)
        {
            _writer.WriteLine(new string(' ', Indent * 4) + format, arg);
        }

        public void Dispose()
        {
            if (_disposeWriter)
                _writer.Dispose();
        }
    }
}
