using System;
using Spard.Sources;
using System.Numerics;
using Spard.Common;
using Spard.Core;

namespace Spard.Expressions
{
    /// <summary>
    /// Multiplication operation
    /// </summary>
    public sealed class Multiply : Polynomial, IInstructionExpression
    {
        protected internal override Priorities Priority
        {
            get { return Priorities.Multiply; }
        }

        protected internal override string Sign
        {
            get { return "*"; }
        }

        internal override bool MatchCore(ISource input, ref IContext context, bool next)
        {
            throw new NotImplementedException();
        }

        internal override object Apply(IContext context)
        {
            BigInteger total = 1;
            foreach (var item in operands)
            {
                total *= ValueConverter.ConvertToNumber(item.Apply(context));
            }

            return total.ToString();
        }

        public override Expression CloneCore()
        {
            return new Multiply();
        }

        bool IInstructionExpression.RightArgumentNeeded
        {
            get { return false; }
        }
    }
}
