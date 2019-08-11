using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spard.Compilation.CSharp
{
    public sealed class ComplexBlock: StatementBlock
    {
        private readonly string _text;

        public ComplexBlock(string text)
        {
            _text = text;
        }

        public ComplexBlock(string text, params Statement[] statements)
        {
            _text = text;
            AddRange(statements);
        }

        public override string ToString(int indent)
        {
            if (string.IsNullOrEmpty(_text))
                return base.ToString(indent);

            var sb = new StringBuilder();

            var tab = new string(' ', indent * 4);
            sb.Append(base.ToString(indent)).AppendLine();
            sb.Append(base.ToString(indent + 1));

            return sb.ToString();
        }
    }
}
