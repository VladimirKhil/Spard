using Spard.Sources;
using Spard.Common;
using Spard.Core;
using Spard.Transitions;
using System;

namespace Spard.Expressions
{
    public sealed class InlineTypeDefinition : Binary
    {
        protected internal override Priorities Priority
        {
            get
            {
                return Priorities.InlineTypeDefinition;
            }
        }

        protected internal override string Sign
        {
            get
            {
                return "::";
            }
        }

        public InlineTypeDefinition()
        {

        }

        public InlineTypeDefinition(Expression left, Expression right)
            : base(left, right)
        {

        }

        public override Expression CloneCore()
        {
            return new InlineTypeDefinition();
        }

        internal override object Apply(IContext context)
        {
            return _left.Apply(context);
        }

        internal override bool MatchCore(ISource input, ref IContext context, bool next)
        {
            if (_left is Query query)
            {
                var name = query.Name;

                if (_right is Query rightQuery)
                {
                    // special case - unification inside a type
                }

                context.DefinitionsTable[name] = _right;
                try
                {
                    return _left.Match(input, ref context, next);
                }
                finally
                {
                    context.DefinitionsTable.Remove(name);
                }
            }

            if (_left is FunctionCall functionCall)
            {
                var start = input.Position;
                if (!_right.Match(input, ref context, next))
                    return false;

                var matchedValue = input.Subarray(start, input.Position - start);

                // Run the function in the opposite direction on the recognized fragment in order to bind variables
                var value = FunctionCall.Call(functionCall.Name, new object[] { matchedValue }, context, Relationship.Left);
                return BindingManager.Bind(functionCall.Args, context, value);
            }

            return false;
        }

        internal override TransitionTable BuildTransitionTableCore(TransitionSettings settings, bool isLast)
        {
            var table = _right.BuildTransitionTable(settings, isLast);

            var result = new TransitionTable();

            var contextChange = new ContextChange(((Query)_left).Name, (object)null);

            foreach (var transition in table)
            {
                var collection = new TransitionTableResultCollection();

                foreach (var item in transition.Value)
                {
                    Expression expr;
                    if (!item.IsFinished)
                        expr = new InlineTypeDefinition(_left, item.Expression);
                    else
                        expr = item.Expression;

                    collection.Add(new TransitionTableResult(expr, false, expr != null || !object.Equals(transition.Key, InputSet.IncludeEOS) ? contextChange : null));
                }

                result[transition.Key] = collection;
            }

            return result;
        }
    }
}
