using System.Collections.Generic;

namespace Spard.Common
{
    /// <summary>
    /// Transformer state.
    /// Used to detect identical situations in transformations with lef recursion
    /// </summary>
    internal sealed class RecursiveState
    {
        /// <summary>
        /// Formed results for n steps in a chain of recursive calls (at each turn of recursion n increases by 1, starting from 0)
        /// </summary>
        public List<RecursiveTransformState> Results { get; private set; } = new List<RecursiveTransformState>();

        public int Index { get; set; }

        public bool Fired { get; set; }

        public Expressions.Set Top { get; set; }

        public List<int> CurrentDerivation { get; } = new List<int>();

        internal RecursiveState Clone()
        {
            return new RecursiveState { Top = Top, Index = Index, Fired = Fired, Results = new List<RecursiveTransformState>(Results) };
        }
    }
}
