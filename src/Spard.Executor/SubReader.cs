using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Spard.Executor
{
    /// <summary>
    /// 
    /// </summary>
    internal sealed class SubReader : TextReader
    {
        private readonly TextReader _baseReader;
        private readonly long _length;

        private long _position;

        public SubReader(TextReader baseStream, long length)
        {
            _baseReader = baseStream ?? throw new ArgumentNullException(nameof(baseStream));
            _length = length;
        }

        public override int Read()
        {
            if (_position++ < _length)
                return _baseReader.Read();

            return -1;
        }

        public override int Peek()
        {
            if (_position + 1 <= _length)
                return _baseReader.Peek();

            return -1;
        }
    }
}
