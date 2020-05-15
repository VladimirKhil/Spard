using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Linq;

namespace Spard.Expressions
{
    /// <summary>
    /// Binary operation
    /// </summary>
    public abstract class Binary: Expression
    {
        /// <summary>
        /// Left part of operation
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        protected Expression _left = null;
        /// <summary>
        /// Right part of operation
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        protected Expression _right = null;

        /// <summary>
        /// Left part of operation
        /// </summary>
        public Expression Left
        {
            get { return _left; }
            set { _left = value; }
        }

        /// <summary>
        /// Right part of operation
        /// </summary>
        public Expression Right
        {
            get { return _right; }
            set { _right = value; }
        }

        protected Binary()
        {

        }

        protected Binary(Expression left, Expression right)
        {
            _left = left;
            _right = right;
        }

        /// <summary>
        /// Expression operands
        /// </summary>
        public override IEnumerable<Expression> Operands()
        {
            yield return _left;
            yield return _right;
        }

        public override void SetOperands(IEnumerable<Expression> operands)
        {
            var opArray = operands.ToArray();
            var length = opArray.Length;
            if (length > 0)
            {
                _left = opArray[0];
                if (length > 1)
                    _right = opArray[1];
                else
                    _right = Empty.Instance;
            }
            else
                _left = _right = Empty.Instance;
        }

        public override string ToString()
        {
            if (_left == null)
                return Sign;

            var result = new StringBuilder();

            base.AppendOperand(result, _left);
            if (Sign.Length > 0) result.AppendFormat(" {0} ", Sign);
            base.AppendOperand(result, _right);

            return result.ToString();
        }

        public override Expression CloneExpression()
        {
            var binary = (Binary)CloneCore();
            binary._left = _left.CloneExpression();
            binary._right = _right.CloneExpression();
            return binary;
        }
    }
}
