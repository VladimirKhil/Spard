using System;
using System.Collections;
using System.Linq;

namespace Spard.Sources
{
    internal sealed class TupleSource: ISource
    {
        public ISource[] Sources { get; set; }

        public bool EndOfSource
        {
            get
            {
                return Sources.All(source => source.EndOfSource);
            }
        }

        public int Position
        {
            get
            {
                return Sources.Sum(source => source.Position);
            }
            set
            {

            }
        }

        public void MoveToEnd()
        {
            throw new NotImplementedException();
        }

        public object Read()
        {
            if (EndOfSource)
                return null;

            return this;
        }

        public IEnumerable Subarray(int startIndex, int length)
        {
            throw new NotImplementedException();
        }
    }
}
