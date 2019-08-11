using Spard.Expressions;

namespace Spard.Common
{
    /// <summary>
    /// Stack trace frame
    /// </summary>
    public sealed class StackFrame
    {
        /// <summary>
        /// Position of input
        /// </summary>
        public int InputPosition { get; }

        /// <summary>
        /// Called expression
        /// </summary>
        public Expression Expression { get; }

        public StackFrame(int inputPosition, Expression expression)
        {
            InputPosition = inputPosition;
            Expression = expression;
        }

        public override string ToString()
        {
            return $"{InputPosition}: {Expression}";
        }
    }
}
