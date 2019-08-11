using Spard.Common;
using Spard.Sources;
using System.Collections;
using Spard.Data;
using Spard.Core;

namespace Spard.Expressions
{
    /// <summary>
    /// Value representing a complex object
    /// </summary>
    public sealed class ComplexValueMatch : Dual, IInstructionExpression
    {
        private int _initStart;
        private ISource _source;

        protected internal override string CloseSign
        {
            get { return "}"; }
        }

        protected internal override Priorities Priority
        {
            get { return Priorities.ComplexValue; }
        }

        protected internal override string Sign
        {
            get { return "{"; }
        }

        public ComplexValueMatch()
        {
            
        }

        public ComplexValueMatch(Expression operand)
            : base(operand)
        {
            
        }

        internal override bool MatchCore(ISource input, ref IContext context, bool next)
        {
            if (!next)
            {
                _initStart = input.Position;
                var item = input.Read();
                IEnumerable source;

                if (item is EnumerableValue enumerable)
                    source = enumerable.Value;
                else
                    source = new object[] { item };

                _source = ValueConverter.ConvertToSource(source);
            }

            var match = _operand.Match(_source, ref context, next);

            if (!match)
                input.Position = _initStart;

            return match;
        }

        internal override object Apply(IContext context)
        {
            var result = _operand.Apply(context);

            if (result is IEnumerable enumerable)
                return new EnumerableValue { Value = enumerable };

            return result;
        }

        public override Expression CloneCore()
        {
            return new ComplexValueMatch();
        }

        bool IInstructionExpression.RightArgumentNeeded
        {
            get { return false; }
        }
    }
}
