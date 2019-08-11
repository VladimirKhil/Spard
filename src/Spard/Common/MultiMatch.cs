using Spard.Core;

namespace Spard.Common
{
    /// <summary>
    /// Plural match
    /// </summary>
    internal sealed class MultiMatch
    {
        /// <summary>
        /// Number of matched elements
        /// </summary>
        internal int Count { get; set; }
        /// <summary>
        /// Match context
        /// </summary>
        internal IContext Context { get; set; }
        /// <summary>
        /// Last input positions of the match
        /// </summary>
        internal int Position { get; set; }

        public override string ToString()
        {
            return $"{Count}/{Position}";
        }
    }
}
