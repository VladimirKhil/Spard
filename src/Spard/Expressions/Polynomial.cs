using System.Collections.Generic;
using System.Text;
using Spard.Core;

namespace Spard.Expressions
{
    /// <summary>
    /// Expression with several arguments
    /// </summary>
    public abstract class Polynomial: Expression
    {
        protected internal Expression[] _operands = null;

        public override IEnumerable<Expression> Operands()
        {
            return _operands;
        }

        public override void SetOperands(IEnumerable<Expression> operands)
        {
            var list = new List<Expression>();

            foreach (var item in operands)
            {
                if (GetType().Equals(item.GetType()))
                    list.AddRange(((Polynomial)item)._operands);
                else
                    list.Add(item);
            }

            _operands = list.ToArray();
        }

        protected Polynomial()
        {
        }

        protected Polynomial(IEnumerable<Expression> operands)
        {
            SetOperands(operands);
        }

        internal override object Apply(IContext context)
        {
            return _operands[0].Apply(context);
        }

        public override string ToString()
        {
            if (_operands == null)
                return GetType().ToString();

            var result = new StringBuilder();

            for (int i = 0; i < _operands.Length; i++)
            {
                if (i > 0)
                    result.Append(Sign);

                base.AppendOperand(result, _operands[i]);
            }

            return result.ToString();
        }

        public override Expression CloneExpression()
        {
            var poly = (Polynomial)CloneCore();
            poly._operands = new Expression[_operands.Length];

            for (int i = 0; i < _operands.Length; i++)
            {
                poly._operands[i] = _operands[i].CloneExpression();
            }

            return poly;
        }
    }
}
