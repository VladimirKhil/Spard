using Spard.Core;
using System.Collections.Generic;

namespace Spard.Transitions
{
    /// <summary>
    /// Table transformer context.
    /// When moving from state to state, it keeps all necessary information about previous matches.
    /// This information can be used to control branching or build a result.
    /// </summary>
    internal sealed class TransitionContext
    {
        /// <summary>
        /// Saved results (they are consistently accumulated during the transformation)
        /// </summary>
        internal List<TransitionResult> Results { get; } = new List<TransitionResult>();

        /// <summary>
        /// Context variables (they are appended during the transformation), the oldest values
        /// </summary>
        internal Dictionary<string, IList<object>> Vars { get; set; } = new Dictionary<string, IList<object>>();

        /// <summary>
        /// The increase of the result insertion index within recursion 
        /// </summary>
        public int ResultIndexIncrease { get; set; }

        /// <summary>
        /// Get the values of variables at the specified index of the saved list of results.
        /// The index is counted from the initial state
        /// </summary>
        /// <param name="index">0, if initial values are needed; result index otherwise</param>
        /// <returns></returns>
        internal Dictionary<string, IList<object>> GetVarsByIndex(int index)
        {
            return index == 0 ? Vars : Results[index - 1].Vars;
        }

        /// <summary>
        /// Create a classic transformation tree context (fill in the values of variables)
        /// </summary>
        /// <returns></returns>
        public Context CreateContext()
        {
            var context = new Context((IRuntimeInfo)null);

            var actualVars = GetVarsByIndex(Results.Count);

            foreach (var item in actualVars)
            {
                context.Vars[item.Key] = item.Value;
            }

            return context;
        }
    }
}
