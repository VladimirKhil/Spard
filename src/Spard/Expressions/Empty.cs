using Spard.Transitions;
using Spard.Sources;
using Spard.Core;

namespace Spard.Expressions
{
    /// <summary>
    /// Empty expression
    /// </summary>
    public sealed class Empty: Primitive
    {
        public static Empty Instance = new Empty();

        private Empty()
        {

        }

        protected internal override string Sign
        {
            get { return ""; }
        }

        internal override bool MatchCore(ISource input, ref IContext context, bool next)
        {
            return !next;
        }

        internal override object Apply(IContext context)
        {
            return null;
        }

        internal override TransitionTable BuildTransitionTableCore(TransitionSettings settings, bool isLast)
        {
            var table = new TransitionTable
            {
                [InputSet.Zero] = TransitionTableResultCollection.Empty.CloneCollection()
            };

            return table;
        }

        public override string ToString()
        {
            return "";
        }
    }
}
