using System.Linq;
using Spard.Common;
using Spard.Sources;
using Spard.Core;

namespace Spard.Expressions
{
    /// <summary>
    /// Quantifier with a limited number of appearances
    /// </summary>
    public sealed class Counter: Binary
    {
        private MultiMatchManager manager = new MultiMatchManager();

        protected internal override Priorities Priority
        {
            get { return Priorities.Counter; }
        }

        protected internal override string Sign
        {
            get { return "#"; }
        }

        /// <summary>
        /// Check if the number of matches is valid
        /// </summary>
        /// <param name="count">Number of template matches</param>
        /// <returns>If the number of matches is valid</returns>
        private bool CheckCount(int count)
        {
            return false;
        }

        private bool CheckCount(int count, ref IContext context, bool next)
        {
            return new Sequence(_right, End.Instance).Match(new StringSource(count.ToString()), ref context, next);
        }

        internal override bool MatchCore(ISource input, ref IContext context, bool next)
        {
            return manager.Match(_left, input, ref context, next, CheckCount);
        }

        internal override object Apply(IContext context)
        {
            var singleResult = _left.Apply(context);
            var count = (int)_right.Apply(context);            

            return ValueConverter.ConvertToEnumerable(Enumerable.Repeat(singleResult, count));
        }

        public override Expression CloneCore()
        {
            return new Counter();
        }
    }
}
