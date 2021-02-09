using System.Collections.Generic;

namespace Spard.Transitions
{
    /// <summary>
    /// Link to go to another state (edge of state graph)
    /// </summary>
    internal sealed class TransitionLink
    {
        /// <summary>
        /// Target state
        /// </summary>
        public TransitionStateBase State { get; }

        /// <summary>
        /// Actions that you need to perform when you go to the target state
        /// </summary>
        public List<TransitionAction> Actions { get; }

        internal TransitionLink(TransitionStateBase state)
        {
            State = state;
            Actions = new List<TransitionAction>();
        }

        internal TransitionLink(TransitionStateBase state, IEnumerable<TransitionAction> actions)
        {
            State = state;
            Actions = new List<TransitionAction>(actions);
        }
    }
}
