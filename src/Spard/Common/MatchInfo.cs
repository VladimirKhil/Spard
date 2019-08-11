using System;
using System.Text;
using System.Diagnostics;

namespace Spard.Common
{
    /// <summary>
    /// Matching information. Used to get the best partial match if the overall match fails
    /// </summary>
    public sealed class MatchInfo: IComparable<MatchInfo>
    {
        /// <summary>
        /// The fatherst position in input which belongs to match
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private int _index = -1;

        /// <summary>
        /// Call stack of expressions
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private StackFrame[] _stackTrace = null;

        /// <summary>
        /// Matches themselves
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private object _match = null;

        /// <summary>
        /// The fatherst position in input which belongs to match
        /// </summary>
        public int Index { get { return _index; } }

        /// <summary>
        /// Call stack of expressions
        /// </summary>
        public StackFrame[] StackTrace { get { return _stackTrace; } }

        /// <summary>
        /// Matches themselves
        /// </summary>
        public object Match { get { return _match; } }

        public MatchInfo(int index, StackFrame[] stackTrace, object match = null)
        {
            _index = index;
            _stackTrace = stackTrace;
            _match = match;
        }

        /// <summary>
        /// Get the best match information of the two
        /// </summary>
        /// <param name="info1">First match information</param>
        /// <param name="info2">Second match information</param>
        /// <returns>Best match information</returns>
        public static MatchInfo Best(MatchInfo info1, MatchInfo info2)
        {
            if (info1 == null)
                return info2;

            return info1.CompareTo(info2) >= 0 ? info1 : info2;
        }

        #region IComparable<MatchInfo> Members

        public int CompareTo(MatchInfo other)
        {
            if (other == null)
                return 1;

            var result = _index.CompareTo(other._index);
            if (result != 0)
                return result;

            var ind = _stackTrace.Length - 1;
            var otherInd = other._stackTrace.Length - 1;

            while (ind > 0 && otherInd > 0)
            {
                var thisIndex = 0;
                foreach (var item in _stackTrace[ind].Expression.Operands())
                {
                    if (item == _stackTrace[ind - 1].Expression)
                        break;

                    thisIndex++;
                }

                var otherIndex = 0;
                foreach (var item in other._stackTrace[otherInd].Expression.Operands())
                {
                    if (item == other._stackTrace[otherInd - 1].Expression)
                        break;

                    otherIndex++;
                }

                result = thisIndex.CompareTo(otherIndex);
                if (result != 0)
                    return result;

                ind--;
                otherInd--;
            }

            result = ind.CompareTo(otherInd);
            if (result != 0)
                return result;

            if (_match == null && other._match == null)
                return 0;

            if (_match != null && other._match == null)
                return 1;

            if (_match == null && other._match != null)
                return -1;

            return CompareMatches(_match, other._match);
        }

        private int CompareMatches(object match1, object match2)
        {
            return 0;
        }

        #endregion

        public string PrintStackTrace()
        {
            var result = new StringBuilder();
            foreach (var item in _stackTrace)
            {
                result.AppendFormat("   {0}: {1}", item.InputPosition, item.Expression).AppendLine();
            }

            return result.ToString();
        }
    }
}
