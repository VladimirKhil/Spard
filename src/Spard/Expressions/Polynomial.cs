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
        protected internal Expression[] operands = null;

        public override IEnumerable<Expression> Operands()
        {
            return operands;
        }

        public override void SetOperands(IEnumerable<Expression> operands)
        {
            var list = new List<Expression>();

            foreach (var item in operands)
            {
                if (GetType().Equals(item.GetType()))
                    list.AddRange(((Polynomial)item).operands);
                else
                    list.Add(item);
            }

            this.operands = list.ToArray();
        }

        public Expression[] OperandsArray
        {
            get { return operands; }
            set { operands = value; }
        }

        public Polynomial()
        {
        }

        public Polynomial(IEnumerable<Expression> operands)
        {
            SetOperands(operands);
        }

        internal override object Apply(IContext context)
        {
            return operands[0].Apply(context);
        }

        public override string ToString()
        {
            if (operands == null)
                return GetType().ToString();

            var result = new StringBuilder();

            for (int i = 0; i < operands.Length; i++)
            {
                if (i > 0)
                    result.Append(Sign);

                base.AppendOperand(result, operands[i]);
            }

            return result.ToString();
        }

        public override Expression CloneExpression()
        {
            var poly = (Polynomial)CloneCore();
            poly.operands = new Expression[operands.Length];

            for (int i = 0; i < operands.Length; i++)
            {
                poly.operands[i] = operands[i].CloneExpression();
            }

            return poly;
        }
    }
}
