using System.Collections.Generic;
using System.Linq;

namespace Spard.Expressions
{
    /// <summary>
    /// An expression that has a single operand
    /// </summary>
    public abstract class Single: Expression
    {
        /// <summary>
        /// Expression operand
        /// </summary>
        protected Expression _operand = null;

        public Expression Operand
        {
            get { return _operand; }
            set { _operand = value; }
        }

        public override IEnumerable<Expression> Operands()
        {
            yield return _operand;
        }

        public override void SetOperands(IEnumerable<Expression> operands)
        {
            _operand = operands.FirstOrDefault() ?? Empty.Instance;
        }

        protected Single()
        {

        }

        protected Single(Expression operand)
        {
            _operand = operand;
        }

        public override Expression CloneExpression()
        {
            var single = (Single)CloneCore();
            single._operand = _operand.CloneExpression();
            return single;
        }
    }
}
