using Spard.Sources;
using Spard.Transitions;
using Spard.Core;

namespace Spard.Expressions
{
    /// <summary>
    /// Any object
    /// </summary>
    public sealed class Any: Primitive
    {
        protected internal override string Sign
        {
            get { return "."; }
        }

        public static Any Instance = new Any();

        private Any()
        {

        }

        internal override bool MatchCore(ISource input, ref IContext context, bool next)
        {
            if (next)
                return false;

            var c = input.Read();
            var isDefault = c == null;
            if (isDefault || context.GetParameter(Parameters.Line) && (object.Equals(c, '\r') || object.Equals(c, '\n')))
            {
                if (!isDefault)
                    input.Position--;

                return false;
            }
            return true;
        }

        internal override object Apply(IContext context)
        {
            return null;
        }

        internal override TransitionTable BuildTransitionTableCore(TransitionSettings settings, bool isLast)
        {
            // '\r', '\n' - will be added later for 'line' mode
            var table = new TransitionTable
            {
                [InputSet.ExcludeEOS] = TransitionTableResultCollection.Empty.CloneCollection()
            };

            return table;
        }

        //public override Expression CloneCore()
        //{
        //    return new Any();
        //}
    }
}
