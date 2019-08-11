using Spard.Sources;
using Spard.Common;
using Spard.Transitions;
using Spard.Core;

namespace Spard.Expressions
{
    /// <summary>
    /// Quantifier of the appearance of 1 or more times (+)
    /// </summary>
    public sealed class SeveralTime : Unary
    {
        private MultiMatchManager _manager = new MultiMatchManager();

        protected internal override Relationship OperandPosition
        {
            get { return Relationship.Left; }
        }

        protected internal override Priorities Priority
        {
            get { return Priorities.MultiTime; }
        }

        protected internal override string Sign
        {
            get { return "+"; }
        }

        public SeveralTime()
        {

        }

        public SeveralTime(Expression operand)
            : base(operand)
        {

        }

        private bool CheckCount(int count, ref IContext context, bool next)
        {
            return count > 0;
        }

        internal override bool MatchCore(ISource input, ref IContext context, bool next)
        {
            return _manager.Match(_operand, input, ref context, next, CheckCount);
        }

        internal override object Apply(IContext context)
        {
            return null;
        }

        internal override TransitionTable BuildTransitionTableCore(TransitionSettings settings, bool isLast)
        {
            var table = _operand.BuildTransitionTable(settings, false);
            var result = new TransitionTable();

            foreach (var item in table)
            {
                var collection = new TransitionTableResultCollection();
                foreach (var res in item.Value)
                {
                    if (res.IsFinished)
                    {
                        collection.Add(new TransitionTableResult(new MultiTime(_operand)));
                    }
                    else
                    {
                        collection.Add(new TransitionTableResult(new Sequence(res.Expression, new MultiTime(_operand))));
                    }
                }

                result[item.Key] = collection;
            }

            return result;
        }

        public override Expression CloneCore()
        {
            return new SeveralTime();
        }
    }
}
