using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spard.Compilation.CSharp
{
    public class StatementBlock: Statement
    {
        protected List<Statement> _statements = new List<Statement>();

        public int Count { get { return _statements.Count; } }

        public StatementBlock()
        {

        }

        public void Add(string text)
        {
            _statements.Add(new SimpleStatement(text));
        }

        public void AddRange(Statement[] statements)
        {
            _statements.AddRange(statements);
        }

        public void Add(string format, params object[] args)
        {
            _statements.Add(new SimpleStatement(string.Format(format, args)));
        }

        public void Add(Statement statement)
        {
            _statements.Add(statement);
        }

        public override string ToString()
        {
            if (_statements.Count == 0)
                return ";";

            if (_statements.Count == 1)
                return _statements[0].ToString();

            var result = new StringBuilder();
            result.Append('{');
            foreach (var statement in _statements)
            {
                result.Append(statement).AppendLine();
            }

            result.Append('}');
            return result.ToString();
        }

        public override string ToString(int indent)
        {
            if (_statements.Count == 0)
                return new string(' ', indent * 4) + ";";

            if (_statements.Count == 1)
                return _statements[0].ToString(indent);

            var tab = new string(' ', (indent - 1) * 4);

            var result = new StringBuilder();
            result.Append(tab).Append('{').AppendLine();
            foreach (var statement in _statements)
            {
                result.Append(statement.ToString(indent)).AppendLine();
            }

            result.Append(tab).Append('}');
            return result.ToString();
        }
    }
}
