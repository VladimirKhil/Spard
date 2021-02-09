using Spard.Core;
using Spard.Expressions;
using System.Collections.Generic;

namespace Spard.Transitions
{
    internal sealed class TransitionSettings
    {
        internal Dictionary<Expression, TransitionTable> TransitionsCache { get; } = new Dictionary<Expression, TransitionTable>();

        internal IExpressionRoot Root { get; private set; }

        internal Directions Direction { get; private set; }

        public TransitionSettings(IExpressionRoot root, Directions direction)
        {
            Root = root;
            Direction = direction;
        }
    }
}
