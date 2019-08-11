using System.Collections.Generic;
using System.IO;

namespace Spard.Sources
{
    /// <summary>
    /// A class that wraps a TextReader and provides simple text access via the IEnumerable interface
    /// </summary>
    internal sealed class TextReaderProxy: IEnumerable<char>
    {
        private readonly TextReader _reader;

        public TextReaderProxy(TextReader reader)
        {
            _reader = reader;
        }

        #region Члены IEnumerable<char>

        public IEnumerator<char> GetEnumerator()
        {
            int sym = -1;

            while ((sym = _reader.Read()) != -1)
            {
                yield return (char)sym;
            }
        }

        #endregion

        #region Члены IEnumerable

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion
    }
}
