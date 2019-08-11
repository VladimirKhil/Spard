using Spard.Sources;
using Spard.Common;
using Spard.Transitions;
using Spard.Core;

namespace Spard.Expressions
{
    /// <summary>
    /// Multiple appearance quantifier
    /// </summary>
    public sealed class MultiTime : Unary
    {
        private MultiMatchManager manager = new MultiMatchManager();

        protected internal override string Sign
        {
            get { return "*"; }
        }

        protected internal override Relationship OperandPosition
        {
            get { return Relationship.Left; }
        }

        protected internal override Priorities Priority
        {
            get { return Priorities.MultiTime; }
        }

        public MultiTime()
        {

        }

        public MultiTime(Expression operand)
            : base(operand)
        {

        }

        internal override bool MatchCore(ISource input, ref IContext context, bool next)
        {
            return manager.Match(_operand, input, ref context, next);
        }

        internal override object Apply(IContext context)
        {
            return null;
        }

        internal override TransitionTable BuildTransitionTableCore(TransitionSettings settings, bool isLast)
        {
            var table = _operand.BuildTransitionTable(settings, false);
            var result = new TransitionTable();

            if (reversed)
                result[InputSet.Zero] = TransitionTableResultCollection.Empty.CloneCollection();

            foreach (var item in table)
            {
                var collection = new TransitionTableResultCollection();
                foreach (var res in item.Value)
                {
                    if (res.IsFinished)
                    {
                        collection.Add(new TransitionTableResult(new MultiTime(_operand) { reversed = reversed }));
                    }
                    else
                    {
                        collection.Add(new TransitionTableResult(new Sequence(res.Expression, this)));
                    }  
                }

                result[item.Key] = collection;
            }

            if (!reversed)
            {
                result[InputSet.Zero] = /*isLast ? TransitionTableResultCollection.Create(new Not(this.operand)) :*/ TransitionTableResultCollection.Empty.CloneCollection();
            }

            return result;
        }

        /// <summary>
        /// Temporary 'lazy' replacement for building transition tables
        /// </summary>
        internal bool reversed = false;

        public override Expression CloneCore()
        {
            return new MultiTime();
        }
    }
}
