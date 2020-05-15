using System;
using Spard.Common;
using Spard.Sources;
using Spard.Core;

namespace Spard.Expressions
{
    /// <summary>
    /// Unification
    /// </summary>
    public sealed class Unification: Binary, IInstructionExpression
    {
        protected internal override Priorities Priority
        {
            get { return Priorities.Unification; }
        }

        protected internal override string Sign
        {
            get { return "="; }
        }

        public Unification()
        {

        }

        public Unification(Expression left, Expression right)
            : base(left, right)
        {

        }

        internal override bool MatchCore(ISource input, ref IContext context, bool next)
        {
            throw new NotImplementedException();
        }

        internal override object Apply(IContext context)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Perform unification
        /// </summary>
        /// <param name="context">Unification context</param>
        /// <returns>Was unification successfull</returns>
        internal bool Unify(IContext context)
        {
            if (!BindingManager.Unify(_right, _left, context, context, out BindingFormula bindingFormula))
                return false;

            if (bindingFormula != null)
                context.AddFormula(bindingFormula);

            return true;
        }

        public override Expression CloneCore()
        {
            return new Unification();
        }

        bool IInstructionExpression.RightArgumentNeeded
        {
            get { return _right.ToString().Length == 0; }
        }
    }
}
