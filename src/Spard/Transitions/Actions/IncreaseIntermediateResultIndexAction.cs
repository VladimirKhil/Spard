using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spard.Transitions.Actions
{
    /// <summary>
    /// Increasing the insertion index of the result at recursive step
    /// </summary>
    internal sealed class IncreaseIntermediateResultIndexAction : TransitionAction
    {
        internal int Increase { get; private set; }

        public IncreaseIntermediateResultIndexAction(int increase)
        {
            Increase = increase;
        }

        public override bool Equals(TransitionAction other)
        {
            if (other is IncreaseIntermediateResultIndexAction action)
                return Increase == action.Increase;

            return false;
        }

        internal override IEnumerable Do(object item, ref TransitionContext context)
        {
            context.ResultIndexIncrease += Increase;
            return null;
        }
    }
}
