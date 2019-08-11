using Spard.Sources;
using Spard.Common;
using Spard.Data;
using Spard.Core;

namespace Spard.Expressions
{
    /// <summary>
    /// Named object
    /// </summary>
    public sealed class NamedValueMatch : Binary, IInstructionExpression
    {
        private int initStart;
        private ISource currentSource;

        protected internal override Priorities Priority
        {
            get { return Priorities.NamedValue; }
        }

        protected internal override string Sign
        {
            get { return ":"; }
        }

        public NamedValueMatch()
        {

        }

        public NamedValueMatch(Expression left, Expression right)
            : base(left, right)
        {

        }

        internal override bool MatchCore(ISource input, ref IContext context, bool next)
        {
            var checkedName = false;

            if (!next)
            {
                initStart = input.Position;

                var currentObject = input.Read();
                var namedValue = currentObject as NamedValue;
                if (namedValue == null)
                {
					if (currentObject is TupleValue tupleValue)
					{
						foreach (var item in tupleValue.Items)
						{
							namedValue = item as NamedValue;
							if (namedValue != null && _left.Match(ValueConverter.ConvertToSource(namedValue.Name), ref context, false))
							{
								checkedName = true;
								break;
							}
						}
					}

					if (!checkedName)
                    {
                        input.Position = initStart;
                        return false;
                    }
                }

                if (!checkedName)
                {
                    if (!_left.Match(ValueConverter.ConvertToSource(namedValue.Name), ref context, false))
                    {
                        input.Position = initStart;
                        return false;
                    }
                }

                var value = namedValue.Value;
                currentSource = ValueConverter.ConvertToSource(value);
            }
            
            if (!_right.Match(currentSource, ref context, next))
            {
                input.Position = initStart;
                return false;
            }

            return true;
        }

        internal override object Apply(IContext context)
        {
            var propertyName = _left.Apply(context).ToString();
            var propertyValue = _right.Apply(context);

            return new NamedValue { Name = propertyName, Value = ValueConverter.ConvertToSingle(propertyValue) };
        }

        public override Expression CloneCore()
        {
            return new NamedValueMatch();
        }

        bool IInstructionExpression.RightArgumentNeeded
        {
            get { return false; }
        }
    }
}
