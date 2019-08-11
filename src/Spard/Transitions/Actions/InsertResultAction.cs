using Spard.Common;
using System.Collections;

namespace Spard.Transitions
{
    /// <summary>
    /// Saving the intermediate result in context (this result may not be returned at all)
    /// </summary>
    internal sealed class InsertResultAction : TransitionAction
    {
        /// <summary>
        /// Expression that generates the result
        /// </summary>
        internal Expressions.Expression Result { get; }

        /// <summary>
        /// Exclude a number of recent results from the possible results
        /// If -1, then the results are cleared completely
        /// </summary>
        internal int RemoveLastCount { get; private set; }

        public InsertResultAction(Expressions.Expression result, int removeLastCount = 0)
        {
            this.Result = result;
            RemoveLastCount = removeLastCount;
        }

        /// <summary>
        /// Increase the number of excluded results
        /// </summary>
        internal void IncreaseRemoveLastCount()
        {
            RemoveLastCount++;
        }

        internal override IEnumerable Do(object item, ref TransitionContext context)
        {
            if (RemoveLastCount > 0)
            {
                // Remove the last RemoveLastCount results
                context.Results.RemoveRange(context.Results.Count - RemoveLastCount, RemoveLastCount);
            }
            else if (RemoveLastCount == -1)
            {
                // Remove all results
                context.Results.Clear();
            }

            if (ProduceResult())
            {
                // We add a new result (does not set any variable values)
                context.Results.Add(new TransitionResult(ValueConverter.ConvertToEnumerable(Result.Apply(context.CreateContext()))));
            }

            // Return nothing
            return null;
        }

        /// <summary>
        /// Will the result be produced at execution time
        /// </summary>
        /// <returns></returns>
        internal bool ProduceResult()
        {
            return Result != null;
        }

        public override string ToString()
        {
            if (Result == null)
                return "i" + RemoveLastCount;

            return "i" + RemoveLastCount + "," + Result.ToString();
        }

        public override bool Equals(TransitionAction other)
        {
            if (!(other is InsertResultAction other2))
                return false;

            return object.Equals(Result, other2.Result) && RemoveLastCount == other2.RemoveLastCount;
        }

        public override int GetHashCode()
        {
            return Result.GetHashCode() * 31 + RemoveLastCount.GetHashCode();
        }
    }
}
