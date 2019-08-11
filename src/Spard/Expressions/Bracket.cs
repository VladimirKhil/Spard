using System;
using Spard.Sources;
using Spard.Common;
using Spard.Core;

namespace Spard.Expressions
{
    /// <summary>
    /// Brackets
    /// </summary>
    internal sealed class Bracket : Dual, IInstructionExpression
    {
        protected internal override Priorities Priority
        {
            get { return Priorities.Bracket; }
        }

        protected internal override string Sign
        {
            get { return "("; }
        }

        protected internal override string CloseSign
        {
            get { return ")"; }
        }

        public Bracket()
        {

        }

        public Bracket(Expression operand)
            : base(operand)
        {

        }

        internal override object Apply(IContext context)
        {
            throw new NotImplementedException();
        }

        internal override bool MatchCore(ISource input, ref IContext context, bool next)
        {
            throw new NotImplementedException();
        }

        public override Expression CloneCore()
        {
            return new Bracket();
        }

        bool IInstructionExpression.RightArgumentNeeded
        {
            get { throw new NotImplementedException(); }
        }
    }
}
