using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spard.Transitions
{
    internal sealed class ValuesRange : IValues
    {
        private readonly char _lowerBound;
        private readonly char _upperBound;

        public ValuesRange(char leftChar, char rightChar)
        {
            _lowerBound = leftChar;
            _upperBound = rightChar;
        }

        public bool Contains(object item)
        {
            var c = (char)item;
            return c >= _lowerBound && c <= _upperBound;
        }

        public IEnumerator<object> GetEnumerator()
        {
            for (var i = _lowerBound; i <= _upperBound; i++)
            {
                yield return i;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
