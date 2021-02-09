using System;
using System.Text;

namespace Spard.Transitions
{
    /// <summary>
    /// Defines state transition table suitable for visual presentation.
    /// </summary>
    internal sealed class VisualTable
    {
        internal InputSet[] ColumnHeaders { get; }
        internal TransitionStateBase[] RowHeaders { get; }
        internal TransitionLink[,] Data { get; }

        public VisualTable(InputSet[] columnHeaders, TransitionStateBase[] rowHeaders)
        {
            ColumnHeaders = columnHeaders;
            RowHeaders = rowHeaders;
            Data = new TransitionLink[rowHeaders.Length, columnHeaders.Length];
        }

        /// <summary>
        /// Builds simple visual table.
        /// </summary>
        public string[,] ToSimpleTable()
        {
            var width = ColumnHeaders.Length + 1;
            var height = RowHeaders.Length + 1;
            var result = new string[height, width];

            for (int i = 1; i < width; i++)
            {
                var header = ColumnHeaders[i - 1];
                result[0, i] = header.IsFinishing ? "EOF" : header.ToString();
            }

            for (int j = 1; j < height; j++)
            {
                var value = new StringBuilder((j - 1).ToString());
                if (RowHeaders[j - 1].IsFinal)
                {
                    value.AppendFormat(" ({0})", ((FinalTransitionState)RowHeaders[j - 1]).ResultString);
                }

                result[j, 0] = value.ToString();
            }

            for (var j = 1; j < height; j++)
            {
                for (var i = 1; i < width; i++)
                {
                    var link = Data[j - 1, i - 1];
                    if (link != null)
                    {
                        var value = new StringBuilder(Array.IndexOf(RowHeaders, link.State).ToString());

                        if (link.Actions.Count > 0)
                        {
                            value.Append(" (");
                            var isFirst = true;
                            foreach (var action in link.Actions)
                            {
                                if (!isFirst)
                                {
                                    value.Append(", ");
                                }

                                if (action is InsertResultAction insertAction)
                                {
                                    value.Append(':').Append(insertAction.RemoveLastCount).Append(':').Append(insertAction.Result);
                                }
                                else
                                {
                                    if (action is ReturnResultAction returnAction)
                                    {
                                        value.Append('r').Append(returnAction.LeftResultsCount);
                                    }
                                }

                                isFirst = false;
                            }

                            value.Append(')');
                        }

                        result[j, i] = value.ToString();
                    }
                }
            }

            return result;
        }
    }
}
