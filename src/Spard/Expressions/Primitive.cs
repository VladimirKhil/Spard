using Spard.Core;
using System.Collections.Generic;

namespace Spard.Expressions
{
    /// <summary>
    /// Simplest value (expression tree leaf)
    /// </summary>
    public abstract class Primitive: Expression
    {
        protected internal override Priorities Priority
        {
            get { return Priorities.Primitive; }
        }

        public override IEnumerable<Expression> Operands()
        {
            yield break;
        }

        public override void SetOperands(IEnumerable<Expression> operands)
        {
            
        }

        protected Primitive()
        {
            
        }

        public override string ToString()
        {
            return Sign;
        }

        public override Expression CloneCore()
        {
            return this;
        }

        public override Expression CloneExpression()
        {
            return CloneCore();
        }
    }
}
