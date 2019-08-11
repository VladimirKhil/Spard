using System;
using Spard.Sources;
using Spard.Common;
using Spard.Core;

namespace Spard.Expressions
{
    /// <summary>
    /// The operation 'larger' ('less' with the opposite direction)
    /// </summary>
    public sealed class Bigger : Directed, IInstructionExpression
    {
        protected internal override Priorities Priority
        {
            get { return Priorities.Bigger; }
        }

        protected internal override string Sign
        {
            get { return Direction == Directions.Right ? ">" : "<"; }
        }

        public Bigger()
        {

        }

        public Bigger(Directions direction)
            : base(direction)
        {

        }

        public Bigger(Expression left, Expression right)
            : base(left, right)
        {

        }

        internal override bool MatchCore(ISource input, ref IContext context, bool next)
        {
            var leftValue = _left.Apply(context);
            var rightValue = _right.Apply(context);

            int left, right;

            if (leftValue is int)
                left = (int)leftValue;
            else
            {
                var leftString = leftValue.ToString();
                if (!int.TryParse(leftString, out left))
                    return CompareAsStrings(leftString, rightValue.ToString());
            }

            if (rightValue is int)
                right = (int)rightValue;
            else
            {
                var rightString = rightValue.ToString();
                if (!int.TryParse(rightString, out right))
                    return CompareAsStrings(leftValue.ToString(), rightString);
            }

            return left != right && ((Direction == Directions.Right) ^ (left < right));
        }

        private bool CompareAsStrings(string left, string right)
        {
            return left != right && ((Direction == Directions.Right) ^ (string.Compare(left, right, StringComparison.Ordinal) < 0));
        }

        internal override object Apply(IContext context)
        {
            throw new NotImplementedException();
        }

        public override Expression CloneCore()
        {
            return new Bigger(Direction);
        }

        bool IInstructionExpression.RightArgumentNeeded
        {
            get { return false; }
        }
    }
}
