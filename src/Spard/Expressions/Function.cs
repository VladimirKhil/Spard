using System;
using System.Collections.Generic;
using Spard.Sources;
using Spard.Common;
using Spard.Transitions;
using Spard.Core;

namespace Spard.Expressions
{
    /// <summary>
    /// Function
    /// </summary>
    public sealed class Function: Directed
    {
        protected internal override Priorities Priority
        {
            get { return Priorities.Function; }
        }

        protected internal override string Sign
        {
            get 
            {
                bool left = (Direction & Directions.Left) == Directions.Left;
                bool right = (Direction & Directions.Right) == Directions.Right;
                return left ? (right ? "=" : "<=") : (right ? "=>" : string.Empty);
            }
        }

        protected internal override Relationship Assotiative
        {
            get
            {
                return Relationship.Right;
            }
        }

        public Function()
        {
            
        }

        /// <summary>
        /// Creates a function
        /// </summary>
        /// <param name="direction">Function direction of application</param>
        internal Function(Directions direction)
            : base(direction)
        {
            
        }

        public Function(Expression left, Expression right)
            : base(left, right)
        {

        }

        internal override bool MatchCore(ISource input, ref IContext context, bool next)
        {
            throw new NotImplementedException();
        }

        internal override object Apply(IContext context)
        {
            throw new NotImplementedException();
        }

        internal override TransitionTable BuildTransitionTableCore(TransitionSettings settings, bool isLast)
        {
            var source = settings.Direction == Directions.Right ? _left : _right;
            var result = settings.Direction == Directions.Right ? _right : _left;

            var childTable = source.BuildTransitionTable(settings, true);
            var table = new TransitionTable();

            foreach (var item in childTable)
            {
                var collection = new TransitionTableResultCollection();
                foreach (var res in item.Value)
                {
                    if (res.IsFinished)
                    {
                        var expandedResult = Expand(result, settings.Root, out bool operandsChanged);

                        collection.Add(new TransitionTableResult(expandedResult, true, res.ContextChange));
                        break;
                    }
                    else
                    {
                        var newFunc = settings.Direction == Directions.Right ? new Function(res.Expression, result) : new Function(result, res.Expression);

                        collection.Add(new TransitionTableResult(newFunc, contextChange: res.ContextChange));
                    }
                }

                table[item.Key] = collection;
            }

            return table;
        }

        /// <summary>
        /// "Expand" expression by replacing sets in it with their definitions
        /// </summary>
        /// <param name="expression">Source expression</param>
        /// <param name="root">Expression root containing set definitions</param>
        /// <returns>Expanded expression</returns>
        private Expression Expand(Expression expression, IExpressionRoot root, out bool operandsChanged)
        {
            if (expression is Set set)
            {
                var setDefinitions = root.GetSet("", set.Name, set.List._operands.Length);
                var setDefinition = setDefinitions[0];

                operandsChanged = true;
                return Expand(setDefinition.Right, root, out bool ops);
            }

            var newOperands = new List<Expression>();
            operandsChanged = false;
            foreach (var item in expression.Operands())
            {
                var expr = Expand(item, root, out bool ops);
                operandsChanged |= ops;

                newOperands.Add(expr);
            }

            if (operandsChanged)
            {
                var clone = expression.CloneCore();
                clone.SetOperands(newOperands);

                return clone;
            }

            return expression;
        }

        /// <summary>
        /// Convert the input object according to internal rules and return the result
        /// </summary>
        /// <param name="input">Input object</param>
        /// <param name="direction">Rule application direction</param>
        /// <param name="next">Should next (otherwise first) transformation result be returned</param>
        /// <param name="runtime">Information about transformation</param>
        /// <param name="allowEmpty">Are empty results allowed</param>
        /// <returns>Transformation result or null in case of failure</returns>
        internal IEnumerable<object> Transform(ISource input, Directions direction, bool next, RuntimeInfo runtime, bool allowEmpty = false)
        {
            if ((Direction & direction) == 0)
                return null;

            IContext context = new Context(runtime);
            context.SetParameter(Parameters.SearchBestVariant, runtime.SearchBestVariant);
            var argument = direction == Directions.Left ? _right : _left;
            var result = direction == Directions.Left ? _left : _right;

            int saveStart = input.Position;
            bool isMatched;
            do
            {
                runtime.StackTrace.Push(new StackFrame(input.Position, this));
                isMatched = argument.Match(input, ref context, next);
                runtime.StackTrace.Pop();
                next = true;
            } while (isMatched && saveStart >= input.Position && !allowEmpty); // Templates matching empty strings are not allowed

            if (!isMatched)
                return null;

            return ValueConverter.ConvertToEnumerable(result.Apply(context));
        }

        public override bool Equals(Expression other)
        {
            if (!(other is Function function))
                return false;

            return _left.Equals(function._left) && _right.Equals(function._right);
        }

        public override Expression CloneCore()
        {
            return new Function(Direction);
        }
    }
}
