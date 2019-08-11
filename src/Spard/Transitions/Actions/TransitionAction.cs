using System;
using System.Collections;

namespace Spard.Transitions
{
    /// <summary>
    /// Additional action performed during the transition between states
    /// </summary>
    internal abstract class TransitionAction: IEquatable<TransitionAction>
    {
        /// <summary>
        /// Permorm action
        /// </summary>
        /// <param name="item">Current processed source item</param>
        /// <param name="context">transition context</param>
        /// <returns>The result of the action (if any)</returns>
        internal abstract IEnumerable Do(object item, ref TransitionContext context);

        public abstract bool Equals(TransitionAction other);
    }
}
