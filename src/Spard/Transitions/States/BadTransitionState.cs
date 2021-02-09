using System.Collections;

namespace Spard.Transitions
{
    internal sealed class BadTransitionState : TransitionState
    {
        /// <summary>
        /// The distance in the input that you want to roll back (a valid chain ended there)
        /// </summary>
        internal int BadLength { get; private set; }

        public BadTransitionState(int badLength)
        {
            BadLength = badLength;
        }

        protected internal override bool IsFinal => false;

        protected internal override TransitionStateBase Move(object item, ref TransitionContext context, out IEnumerable result)
        {
            result = null;
            return null;
        }
    }
}
