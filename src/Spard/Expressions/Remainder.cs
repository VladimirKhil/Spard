using System;
using Spard.Common;
using Spard.Sources;
using Spard.Core;

namespace Spard.Expressions
{
    /// <summary>
    /// The operation of obtaining the remainder of the division
    /// </summary>
    public sealed class Remainder : Binary, IInstructionExpression
    {
        protected internal override Priorities Priority
        {
            get { return Priorities.Multiply; }
        }

        protected internal override string Sign
        {
            get { return "%"; }
        }

        public Remainder()
        {

        }

        public Remainder(Expression left, Expression right)
            : base(left, right)
        {

        }

        internal override bool MatchCore(ISource input, ref IContext context, bool next)
        {
            throw new NotImplementedException();
        }

        internal override object Apply(IContext context)
        {
            var result = ValueConverter.ConvertToNumber(_left.Apply(context)) % ValueConverter.ConvertToNumber(_right.Apply(context));
            return result.ToString();
        }

        public override Expression CloneCore()
        {
            return new Remainder();
        }

        bool IInstructionExpression.RightArgumentNeeded
        {
            get { return false; }
        }
    }
}
