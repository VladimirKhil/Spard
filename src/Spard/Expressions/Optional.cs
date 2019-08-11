using System;
using Spard.Sources;
using Spard.Core;

namespace Spard.Expressions
{
    /// <summary>
    /// Quantifier of single or empty appearance
    /// </summary>
    public sealed class Optional: Unary
    {
        private IContext initContext = null;
        private bool lastChance = true;
        
        protected internal override Priorities Priority
        {
            get { return Priorities.Optional; }
        }

        protected internal override string Sign
        {
            get { return "?"; }
        }

        protected internal override Relationship OperandPosition
        {
            get { return Relationship.Left; }
        }

        public Optional()
        {

        }

        public Optional(Expression operand)
            : base(operand)
        {

        }

        internal override bool MatchCore(ISource input, ref IContext context, bool next)
        {
            IContext workingContext = null;
            if (!next)
            {
                initContext = context; // If the match is successful, we will still rewrite the context. Otherwise the value of initContext doesn’t bother us much
                workingContext = context.Clone();
                lastChance = true;
            }
            else
            {
                workingContext = initContext;
            }

            bool res = false;
            if (lastChance)
            {
                res = _operand.Match(input, ref workingContext, next);
                if (!res)
                {
                    lastChance = false; // We use match with emptiness
                }

                context = workingContext;
                return true;
            }

            return false;
        }

        internal override object Apply(IContext context)
        {
            throw new NotImplementedException();
        }

        public override Expression CloneCore()
        {
            return new Optional();
        }
    }
}
