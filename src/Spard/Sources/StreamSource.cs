using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Spard.Sources
{
    internal sealed class StreamSource : ISource
    {
        private Stream _source = null;
        private List<char> _buffer = new List<char>();
        private int _index = 0;

        public StreamSource(Stream source)
        {
            _source = source;
        }

        public object Read()
        {
            if (_index == _buffer.Count)
            {
                var buf = new byte[2048];
                int k;
                if ((k = _source.Read(buf, 0, buf.Length)) == 0)
                    return default(char);

                if (k < buf.Length)
                    Array.Resize<byte>(ref buf, k);

                var str = Encoding.UTF8.GetString(buf, 0, buf.Length);
                if (str[0] == 65279 && _buffer.Count == 0)
                    str = str.Substring(1);

                _buffer.AddRange(str);
            }

            return _buffer[_index++];
        }

        public IEnumerable Subarray(int startIndex, int length)
        {
            var end = startIndex + length;
            for (int i = startIndex; i < end; i++)
            {
                yield return _buffer[i];
            }
        }

        public bool EndOfSource
        {
            get
            {
                if (_index < _buffer.Count)
                    return false;

                var buf = new byte[2048];
                int k;
                if ((k = _source.Read(buf, 0, buf.Length)) == 0)
                    return true;

                if (k < buf.Length)
                    Array.Resize<byte>(ref buf, k);

                var str = Encoding.UTF8.GetString(buf, 0, buf.Length);
                if (str[0] == 65279 && _buffer.Count == 0)
                    str = str.Substring(1);

                _buffer.AddRange(str);
                return false;
            }
        }

        public void MoveToEnd()
        {
            var buf = new byte[2048];
            int k;
            while ((k = _source.Read(buf, 0, buf.Length)) > 0)
            {
                if (k < buf.Length)
                    Array.Resize<byte>(ref buf, k);

                var str = Encoding.UTF8.GetString(buf, 0, buf.Length);
                if (str[0] == 65279 && _buffer.Count == 0)
                    str = str.Substring(1);

                _buffer.AddRange(str);
            }
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
    }
}
