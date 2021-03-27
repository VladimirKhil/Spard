using System;
using System.IO;

namespace Spard.Executor
{
    /// <summary>
    /// Wraps a <see cref="TextReader" /> allowing to read only the fixed number of characters from it.
    /// </summary>
    internal sealed class SubReader : TextReader
    {
        private readonly TextReader _baseReader;
        private readonly long _length;

        private long _position;

        /// <summary>
        /// Initializes a new instance of <see cref="SubReader" />.
        /// </summary>
        /// <param name="baseReader">Wrapper reader.</param>
        /// <param name="length">Maximum number of characters that can be read from the reader.</param>
        public SubReader(TextReader baseReader, long length)
        {
            _baseReader = baseReader ?? throw new ArgumentNullException(nameof(baseReader));
            _length = length;
        }

        public override int Read()
        {
            if (_position++ < _length)
            {
                return _baseReader.Read();
            }

            return -1;
        }

        public override int Peek()
        {
            if (_position + 1 <= _length)
            {
                return _baseReader.Peek();
            }

            return -1;
        }
    }
}
