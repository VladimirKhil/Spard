using System;
using Spard.Common;
using Spard.Sources;
using Spard.Core;

namespace Spard.Expressions
{
    /// <summary>
    /// "Not equals" relation
    /// </summary>
    public sealed class NotEqual : Binary, IInstructionExpression
    {
        protected internal override Priorities Priority
        {
            get { return Priorities.Bigger; }
        }

        protected internal override string Sign
        {
            get { return "!="; }
        }

        internal override bool MatchCore(ISource input, ref IContext context, bool next)
        {
            var left = _left.Apply(context);
            var right = _right.Apply(context);
            return !Equals(left, right);
        }

        internal override object Apply(IContext context)
        {
            throw new NotImplementedException();
        }

        public override Expression CloneCore()
        {
            return new NotEqual();
        }

        bool IInstructionExpression.RightArgumentNeeded
        {
            get { return false; }
        }
    }
}
