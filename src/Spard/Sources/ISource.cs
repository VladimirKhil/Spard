using System.Collections;

namespace Spard.Sources
{
    /// <summary>
    /// Untyped streaming data source with positioning support
    /// </summary>
    internal interface ISource
    {
        /// <summary>
        /// Is the end of the source reached
        /// </summary>
        bool EndOfSource { get; }

        /// <summary>
        /// Get next item from data source
        /// </summary>
        /// <returns>Next item</returns>
        object Read();

        /// <summary>
        /// Move to the end without returning read items
        /// </summary>
        void MoveToEnd();

        /// <summary>
        /// Current read position in the data stream
        /// </summary>
        int Position { get; set; }

        /// <summary>
        /// Get data span
        /// </summary>
        /// <param name="startIndex">First span element index</param>
        /// <param name="length">Span length</param>
        /// <returns>Selected data span</returns>
        IEnumerable Subarray(int startIndex, int length);
    }
}
