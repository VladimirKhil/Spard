using System;
using Spard.Common;
using Spard.Sources;
using Spard.Core;

namespace Spard.Expressions
{
    /// <summary>
    /// Substraction operation
    /// </summary>
    public sealed class Substract: Binary, IInstructionExpression
    {
        protected internal override Priorities Priority
        {
            get { return Priorities.Add; }
        }

        protected internal override string Sign
        {
            get { return "-"; }
        }

        public Substract()
        {

        }

        public Substract(Expression left, Expression right)
            : base(left, right)
        {

        }

        internal override bool MatchCore(ISource input, ref IContext context, bool next)
        {
            throw new NotImplementedException();
        }

        internal override object Apply(IContext context)
        {
            var result = ValueConverter.ConvertToNumber(_left.Apply(context)) - ValueConverter.ConvertToNumber(_right.Apply(context));
            return result.ToString();
        }

        public override Expression CloneCore()
        {
            return new Substract();
        }

        bool IInstructionExpression.RightArgumentNeeded
        {
            get { return false; }
        }
    }
}
