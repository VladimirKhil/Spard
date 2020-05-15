using Spard.Core;
using System.Text;

namespace Spard.Expressions
{
    /// <summary>
    /// Unary operation
    /// </summary>
    public abstract class Unary: Single
    {
        /// <summary>
        /// The position of the argument in relation to the operation
        /// </summary>
        protected internal abstract Relationship OperandPosition { get; }

        protected Unary()
        {
            
        }

        protected Unary(Expression operand)
            : base(operand)
        {
            
        }

        public override string ToString()
        {
            if (_operand == null)
                return Sign;
            var result = new StringBuilder();

            bool left = OperandPosition == Relationship.Left;
            if (left)
                AppendOperand(result, _operand);
            result.Append(Sign);
            if (!left)
                AppendOperand(result, _operand);

            return result.ToString();
        }
    }
}
