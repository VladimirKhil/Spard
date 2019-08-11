using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spard.Compilation.CSharp
{
    public sealed class ComplexStatement: SimpleStatement
    {
        private readonly Statement _child;

        public ComplexStatement(string text, Statement child)
            : base(text)
        {
            _child = child;
        }

        public override string ToString(int indent)
        {
            if (string.IsNullOrEmpty(_text))
                return _child.ToString(indent);

            var sb = new StringBuilder();

            var tab = new string(' ', indent * 4);
            sb.Append(base.ToString(indent)).AppendLine();
            sb.Append(_child.ToString(indent + 1));

            return sb.ToString();
        }
    }
}
