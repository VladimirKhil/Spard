using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

namespace Spard.Sources
{
    /// <summary>
    /// Object collections wrapper
    /// </summary>
    internal sealed class ObjectSource : ISource
    {
        private readonly object[] _buffer = null;
        private int _index = 0;

        public ObjectSource(object source)
        {
            _buffer = new object[] { source };
        }

        public ObjectSource(IEnumerable<object> source)
        {
            _buffer = source.ToArray();
        }

        public int Position
        {
            get
            {
                return _index;
            }
            set
            {
                _index = Math.Max(0, Math.Min(value, _buffer.Length));
            }
        }

        public IEnumerable Subarray(int startIndex, int length)
        {
            var top = startIndex + length;
            for (int i = startIndex; i < top; i++)
            {
                yield return _buffer[i];
            }
        }

        public object Read()
        {
            if (_index >= _buffer.Length)
                return null;

            return _buffer[_index++];
        }

        public bool EndOfSource
        {
            get { return _index == _buffer.Length; }
        }
        
        public void MoveToEnd()
        {
            _index = _buffer.Length;
        }
    }
}
