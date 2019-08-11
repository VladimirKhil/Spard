using Spard.Core;
using Spard.Sources;
using System;

namespace Spard.Expressions
{
    /// <summary>
    /// Block of expressions
    /// </summary>
    internal sealed class Block: Polynomial
    {
        protected internal override Priorities Priority
        {
            get { return Priorities.Block; }
        }

        protected internal override string Sign
        {
            get { return Environment.NewLine; }
        }

        internal override bool MatchCore(ISource input, ref IContext context, bool next)
        {
            throw new NotImplementedException();
        }

        public override Expression CloneCore()
        {
            return new Block();
        }
    }
}
