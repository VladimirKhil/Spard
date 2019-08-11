using System.Collections;
using System.Linq;

namespace Spard.Transitions
{
    /// <summary>
    /// Result returning
    /// </summary>
    internal sealed class ReturnResultAction : TransitionAction
    {
        /// <summary>
        /// The number of last result items to keep in context
        /// </summary>
        internal int LeftResultsCount { get; private set; }

        public ReturnResultAction(int leftResultsCount = 0)
        {
            this.LeftResultsCount = leftResultsCount;
        }

        /// <summary>
        /// Increase the number of last keeped results
        /// </summary>
        internal void IncreaseLeftResultsCount()
        {
            LeftResultsCount++;
        }

        internal override IEnumerable Do(object item, ref TransitionContext context)
        {
            IEnumerable result;
            var count = context.Results.Count;
            var take = count - LeftResultsCount; // How much do we take

            // ToArray is needed otherwise not working
            result = context.Results.Take(take).SelectMany(r => r.Data).ToArray();

            if (take > 0)
            {
                // Move the pointer to the oldest variable values
                context.Vars = context.Results[take - 1].Vars;
                // We delete the returned results
                context.Results.RemoveRange(0, take);
            }

            return result;
        }

        public override bool Equals(TransitionAction other)
        {
            if (!(other is ReturnResultAction other2))
                return false;

            return LeftResultsCount == other2.LeftResultsCount;
        }

        public override int GetHashCode()
        {
            return LeftResultsCount.GetHashCode();
        }

        public override string ToString()
        {
            return "r" + LeftResultsCount;
        }
    }
}
