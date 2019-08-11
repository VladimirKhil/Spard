using System;
using System.Collections;

namespace Spard.Sources
{
    /// <summary>
    /// A string wrapper that allows it to act as a data source for a pipeline
    /// </summary>
    internal sealed class StringSource : ISource
    {
        private readonly string _buffer = null;
        private int _index = 0;

        public StringSource(object source)
        {
            _buffer = source.ToString();
        }

        public StringSource(char[] source)
        {
            _buffer = new string(source);
        }

        /// <summary>
        /// Created string data source
        /// </summary>
        /// <param name="source">Wrapped string</param>
        public StringSource(string source)
        {
            _buffer = source;
        }

        public override string ToString()
        {
            return _buffer.Substring(_index);
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
            return _buffer.Substring(startIndex, length);
        }

        public object Read()
        {
            if (_index == _buffer.Length)
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
