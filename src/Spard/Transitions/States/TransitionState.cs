using System;
using System.Collections;
using System.Collections.Generic;

namespace Spard.Transitions
{
    /// <summary>
    /// Table transformer state
    /// </summary>
    internal class TransitionState : TransitionStateBase
    {
        /// <summary>
        /// Simple transition table of type "item - edge"
        /// </summary>
        internal Dictionary<object, TransitionLink> Table { get; } = new Dictionary<object, TransitionLink>();
        /// <summary>
        /// Complex transition table of type "item check - edge"
        /// </summary>
        internal List<Tuple<InputSet, TransitionLink>> SecondTable { get; } = new List<Tuple<InputSet, TransitionLink>>();

        /// <summary>
        /// Is it a final state
        /// </summary>
        protected internal override bool IsFinal => false;

        protected internal override TransitionStateBase Move(object item, ref TransitionContext context, out IEnumerable result)
        {
            if (!Table.TryGetValue(item, out TransitionLink next))
            {
                foreach (var tuple in SecondTable)
                {
                    if (tuple.Item1.Contains(item))
                    {
                        next = tuple.Item2;
                        break;
                    }
                }
            }

            result = null;

            if (next == null)
                return null;

            // next.Actions affect context
            foreach (var action in next.Actions)
            {
                var tempResult = action.Do(item, ref context);
                if (tempResult != null)
                    result = tempResult;
            }

            return next.State;
        }

        protected internal override IEnumerable GetResult(TransitionContext context) => null;
    }
}
