using System.Collections;
using System.Collections.Generic;

namespace Spard.Results
{
    /// <summary>
    /// Dynamically generated sequence caching its elements
    /// </summary>
    internal sealed class BufferedEnumerable: IEnumerable<object>
    {
        private readonly IEnumerator _source;
        private readonly List<object> _buffer = new List<object>();

        public BufferedEnumerable(IEnumerable source)
        {
            _source = source.GetEnumerator();
        }

        public IEnumerator<object> GetEnumerator()
        {
            foreach (var item in _buffer)
            {
                yield return item;
            }

            while (_source.MoveNext())
            {
                _buffer.Add(_source.Current);
                yield return _source.Current;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
