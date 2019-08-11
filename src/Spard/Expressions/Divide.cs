using System;
using Spard.Common;
using Spard.Sources;
using Spard.Core;

namespace Spard.Expressions
{
    /// <summary>
    /// Divide operation
    /// </summary>
    public sealed class Divide : Binary, IInstructionExpression
    {
        protected internal override Priorities Priority
        {
            get { return Priorities.Multiply; }
        }

        protected internal override string Sign
        {
            get { return "/"; }
        }

        public Divide()
        {

        }

        public Divide(Expression left, Expression right)
            : base(left, right)
        {

        }

        internal override bool MatchCore(ISource input, ref IContext context, bool next)
        {
            throw new NotImplementedException();
        }

        internal override object Apply(IContext context)
        {
            var result = ValueConverter.ConvertToNumber(_left.Apply(context)) / ValueConverter.ConvertToNumber(_right.Apply(context));
            return result.ToString();
        }

        public override Expression CloneCore()
        {
            return new Divide();
        }

        bool IInstructionExpression.RightArgumentNeeded
        {
            get { return false; }
        }
    }
}
