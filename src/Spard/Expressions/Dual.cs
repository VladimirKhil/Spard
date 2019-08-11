using System.Text;

namespace Spard.Expressions
{
    /// <summary>
    /// Operation denoted by two characters
    /// </summary>
    public abstract class Dual: Single
    {
        /// <summary>
        /// Closing operation character
        /// </summary>
        protected internal abstract string CloseSign { get; }

        public Dual()
        {

        }

        public Dual(Expression operand)
            : base(operand)
        {

        }

        public override string ToString()
        {
            var result = new StringBuilder();
            result.Append(Sign);
            if (_operand != null)
                result.Append(_operand);
            result.Append(CloseSign);
            return result.ToString();
        }
    }
}
