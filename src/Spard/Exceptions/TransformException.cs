using Spard.Common;
using Spard.Core;
using Spard.Sources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Spard.Exceptions
{
    /// <summary>
    /// Transformation error
    /// </summary>
    public sealed class TransformException: SpardException
    {
        /// <summary>
        /// Transformation input
        /// </summary>
        internal ISource TransformSource { get; set; }

        /// <summary>
        /// Transformation info
        /// </summary>
        internal RuntimeInfo Runtime { get; set; }

        public Tuple<int, int> ErrorPosition
        {
            get
            {
                if (Runtime.BestTry == null || Source == null)
                    return Tuple.Create(-1, -1);

                return ConvertToLineColumnPosition(Runtime.BestTry.Index);
            }
        }

        public MatchInfo BestTry { get { return Runtime.BestTry; } }
        public Stack<StackFrame> TransformStackTrace { get { return Runtime.StackTrace; } }

        /// <summary>
        /// Current position in data source
        /// </summary>
        public Tuple<int, int> InputPosition
        {
            get
            {
                return TransformSource == null ? Tuple.Create(-1, -1) : ConvertToLineColumnPosition(TransformSource.Position);
            }
        }

        public TransformException()
        {
            
        }

        public TransformException(string message)
            : base(message)
        {

        }

        public TransformException(string message, Exception innerException)
            : base(message, innerException)
        {

        }

        private Tuple<int, int> ConvertToLineColumnPosition(int position)
        {
            var text = TransformSource.Subarray(0, position).Cast<char>();

            int lineNum = 1, columnNum = 1;

            foreach (var item in text)
            {
                if (item == '\n')
                {
                    lineNum++;
                    columnNum = 1;
                }
                else if (item != '\r')
                    columnNum++;
            }

            return Tuple.Create(lineNum, columnNum);
        }

        public string PrintRuntimeStackTrace()
        {
            return PrintStackTrace(TransformStackTrace);
        }

        public string PrintBestTryStackTrace()
        {
            return PrintStackTrace(BestTry.StackTrace);
        }

        private string PrintStackTrace(IEnumerable<StackFrame> stack)
        {
            var result = new StringBuilder();
            foreach (var item in stack)
            {
                var position = ConvertToLineColumnPosition(item.InputPosition);
                result.AppendFormat("   ({0},{1}): {2}", position.Item1, position.Item2, item.Expression).AppendLine();
            }

            return result.ToString();
        }
    }
}
