using System.Collections.Generic;

namespace Spard.Transitions
{
    /// <summary>
    /// Intermediate result of table transform
    /// </summary>
    internal sealed class TransitionResult
    {
        /// <summary>
        /// Calculated result
        /// </summary>
        internal IEnumerable<object> Data { get; private set; }
        /// <summary>
        /// The stored values of the variables at the time of calculating the result
        /// </summary>
        internal Dictionary<string, IList<object>> Vars { get; } = new Dictionary<string, IList<object>>();

        public TransitionResult(IEnumerable<object> data)
        {
            Data = data;
        }
    }
}
