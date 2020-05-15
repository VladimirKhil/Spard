using System;
using System.Collections.Generic;
using System.Collections;

namespace Spard.Sources
{
    /// <summary>
    /// A data source that buffers the output of its own source. Is is a wrapper around IEnumerable
    /// </summary>
    internal class BufferedSource: ISource
    {
        private IEnumerator _source = null;
        private readonly List<object> _buffer = new List<object>();
        private int _index = 0;

        protected BufferedSource()
        {

        }

        internal BufferedSource(IEnumerator source)
        {
            _source = source;
        }

        internal BufferedSource(IEnumerable source)
        {
            SetSource(source);
        }

        protected void SetSource(IEnumerable source)
        {
            _source = source.GetEnumerator();
        }

        public bool EndOfSource
        {
            get
            {
                if (_index < _buffer.Count)
                    return false;

                if (!_source.MoveNext())
                    return true;

                _buffer.Add(_source.Current);
                return false;
            }
        }

        public object Read()
        {
            if (_index == _buffer.Count)
            {
                if (!_source.MoveNext())
                    return null;

                _buffer.Add(_source.Current);
            }
            return _buffer[_index++];
        }

        public int Position
        {
            get
            {
                return _index;
            }
            set
            {
                _index = Math.Max(0, Math.Min(value, _buffer.Count));
            }
        }

        public IEnumerable Subarray(int startIndex, int length)
        {
            var end = startIndex + length;
            for (int i = startIndex; i < end; i++)
            {
                yield return _buffer[i];
            }
        }
        
        public void MoveToEnd()
        {
            while (_source.MoveNext())
                _buffer.Add(_source.Current);

            _index = _buffer.Count;
        }
    }
}
