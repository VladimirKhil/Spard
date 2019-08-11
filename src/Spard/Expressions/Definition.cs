using System;
using Spard.Sources;
using Spard.Core;

namespace Spard.Expressions
{
    /// <summary>
    /// Definition
    /// </summary>
    public sealed class Definition: Binary
    {
        protected internal override Priorities Priority
        {
            get { return Priorities.Definition; }
        }

        protected internal override string Sign
        {
            get { return ":="; }
        }

        public Definition()
        {

        }

        public Definition(Expression left, Expression right)
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

        public override Expression CloneCore()
        {
            return new Definition();
        }
    }
}
