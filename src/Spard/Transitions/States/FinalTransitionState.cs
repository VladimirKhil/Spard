using Spard.Common;
using System.Collections;

namespace Spard.Transitions
{
    /// <summary>
    /// Final tranformation state
    /// </summary>
    internal sealed class FinalTransitionState : TransitionStateBase
    {
        protected internal override bool IsFinal
        {
            get { return true; }
        }

        /// <summary>
        /// The expression to form transformation result
        /// </summary>
        internal Expressions.Expression Result { get; }

        internal string ResultString
        {
            get { return Result.ToString(); }
        }

        public FinalTransitionState(Expressions.Expression result)
        {
            this.Result = result;
        }

        protected internal override TransitionStateBase Move(object item, ref TransitionContext context, out IEnumerable result)
        {
            result = null;
            return null;
        }

        protected internal override IEnumerable GetResult(TransitionContext context)
        {
            //context.Results.Clear(); // Remove the garbage so as not to interfere - no longer need it anyway
            return ValueConverter.ConvertToEnumerable(Result.Apply(context.CreateContext()));
        }
    }
}
