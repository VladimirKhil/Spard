using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spard.Compilation.CSharp
{
    /// <summary>
    /// Language statement
    /// </summary>
    public class SimpleStatement: Statement
    {
        protected string _text;

        public SimpleStatement(string text)
        {
            _text = text;
        }

        public override string ToString()
        {
            return _text;
        }

        public override string ToString(int indent)
        {
            var tab = new string(' ', indent * 4);
            return tab + _text;
        }
    }
}
