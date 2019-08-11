using Spard.Sources;
using Spard.Transitions;
using Spard.Core;

namespace Spard.Expressions
{
    /// <summary>
    /// End of input or end of line
    /// </summary>
    public sealed class End: Primitive
    {
        protected internal override string Sign
        {
            get { return "%"; }
        }

        public static End Instance = new End();

        private End()
        {

        }

        internal override bool MatchCore(ISource input, ref IContext context, bool next)
        {
            if (next)
                return false;

            var initStart = input.Position;

            var endOfSource = input.EndOfSource;
            if (!context.GetParameter(Parameters.Line) || endOfSource)
                return endOfSource;
            
            object last;
            if (initStart > 0)
            {
                input.Position--;
                last = input.Read();
            }
            else
                last = null;

            var current = input.Read();
            input.Position = initStart;

            return current == null || object.Equals(current, '\r') || object.Equals(current, '\n') && !object.Equals(last, '\r');
        }

        internal override object Apply(IContext context)
        {
            return null;
        }

        internal override TransitionTable BuildTransitionTableCore(TransitionSettings settings, bool isLast)
        {
            var table = new TransitionTable
            {
                [InputSet.IncludeEOS] = TransitionTableResultCollection.Empty.CloneCollection()
            };

            return table;
        }

        public override bool Equals(Expression other)
        {
            return other is End;
        }
    }
}
