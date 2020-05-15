using System;
using Spard.Sources;
using System.Numerics;
using Spard.Common;
using Spard.Core;

namespace Spard.Expressions
{
    /// <summary>
    /// Addition operation
    /// </summary>
    public sealed class Add: Polynomial, IInstructionExpression
    {
        protected internal override Priorities Priority
        {
            get { return Priorities.Add; }
        }

        protected internal override string Sign
        {
            get { return "+"; }
        }

        public Add()
        {

        }

        public Add(params Expression[] operands)
            : base(operands)
        {

        }

        internal override bool MatchCore(ISource input, ref IContext context, bool next)
        {
            throw new NotImplementedException();
        }

        internal override object Apply(IContext context)
        {
            BigInteger total = 0;
            foreach (var item in _operands)
            {
                total += ValueConverter.ConvertToNumber(item.Apply(context));
            }

            return total.ToString();
        }

        public override Expression CloneCore()
        {
            return new Add();
        }

        bool IInstructionExpression.RightArgumentNeeded
        {
            get { return false; }
        }
    }
}
