using System;
using System.Text;

namespace Spard.Transitions
{
    /// <summary>
    /// State transition table suitable for visual presentation
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
        /// Build simple visual table
        /// </summary>
        /// <returns></returns>
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

            for (int j = 1; j < height; j++)
            {
                for (int i = 1; i < width; i++)
                {
                    var link = Data[j - 1, i - 1];
                    if (link != null)
                    {
                        var value = new StringBuilder(Array.IndexOf(RowHeaders, link.State).ToString());

                        if (link.Actions.Count > 0)
                        {
                            value.Append(" (");
                            var first = true;
                            foreach (var action in link.Actions)
                            {
                                if (!first)
                                    value.Append(", ");

                                if (action is InsertResultAction insertAction)
                                    value.Append(':').Append(insertAction.RemoveLastCount).Append(':').Append(insertAction.Result);
                                else
                                {
                                    if (action is ReturnResultAction returnAction)
                                        value.Append("r").Append(returnAction.LeftResultsCount);
                                }

                                first = false;
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
