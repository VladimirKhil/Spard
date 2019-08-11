using Spard.Transitions;
using Spard.Sources;
using Spard.Core;

namespace Spard.Expressions
{
    /// <summary>
    /// An expression that matches either the entire input chain or match nothing
    /// </summary>
    public sealed class Anything: Primitive
    {
        protected internal override string Sign
        {
            get { return "_"; }
        }

        public static Anything Instance = new Anything();

        private Anything()
        {

        }

        internal override bool MatchCore(ISource input, ref IContext context, bool next)
        {
            if (next)
                return false;

            input.MoveToEnd();

            return true;
        }

        internal override object Apply(IContext context)
        {
            return null;
        }

        internal override TransitionTable BuildTransitionTableCore(TransitionSettings settings, bool isLast)
        {
            var table = new TransitionTable
            {
                [InputSet.ExcludeEOS] = TransitionTableResultCollection.Create(this),
                [InputSet.IncludeEOS] = TransitionTableResultCollection.Empty.CloneCollection()
            };

            return table;
        }
    }
}
