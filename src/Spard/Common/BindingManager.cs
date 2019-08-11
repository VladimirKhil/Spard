using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Spard.Expressions;
using Spard.Data;
using Spard.Core;

namespace Spard.Common
{
    internal static class BindingManager
    {
        /// <summary>
        /// Unset result (not equal to empty result)
        /// </summary>
        internal static object UnsetValue = new object();
        /// <summary>
        /// Incorrect value
        /// </summary>
        internal static object[] NullValue = new object[0];

        internal static bool CompareValues(object leftValue, object rightValue)
        {
            var leftEnum = Enumerate(leftValue);
            var rightEnum = Enumerate(rightValue);

            var leftEnumerator = leftEnum.GetEnumerator();
            var rightEnumerator = rightEnum.GetEnumerator();

            while (true)
            {
                var moveLeft = leftEnumerator.MoveNext();
                var moveRight = rightEnumerator.MoveNext();

                if (!moveLeft && !moveRight)
                    return true;

                if (!object.Equals(leftEnumerator.Current, rightEnumerator.Current))
                    return false;
            }
        }

        internal static IEnumerable<object> Enumerate(object value)
        {
            var enumerable = value as IEnumerable;
            if (enumerable == null)
            {
                if (value is EnumerableValue enumerableValue)
                    enumerable = enumerableValue.Value;
            }

            if (enumerable != null)
            {
                var result = Enumerable.Empty<object>();
                foreach (var item in enumerable)
                {
                    foreach (var obj in Enumerate(item))
                    {
                        yield return obj;
                    }
                }

                yield break;
            }

            yield return value;
        }

        private static bool CompareCollectionWithValue(IEnumerable collection, object value)
        {
            var enumerator = collection.GetEnumerator();
            if (!enumerator.MoveNext())
                return false;

            if (!object.Equals(enumerator.Current, value))
                return false;

            if (enumerator.MoveNext())
                return false;

            return true;
        }

        internal static bool Unify(Expression targetExpression, Expression sourceExpression, IContext targetContext, IContext sourceContext, out BindingFormula bindingFormula)
        {
            bindingFormula = null;

            var sourceVars = new HashSet<string>();
            var targetVars = new HashSet<string>();

            CheckDefined(sourceExpression, sourceContext, sourceVars);
            CheckDefined(targetExpression, targetContext, targetVars);

            var definedSource = !sourceVars.Any();
            var definedTarget = !targetVars.Any();

            var sourceValue = definedSource ? sourceExpression.Apply(sourceContext) : null;
            var targetValue = definedTarget ? targetExpression.Apply(targetContext) : null;

            if (definedSource && definedTarget)
                return CompareValues(sourceValue, targetValue);

            if (definedSource)
                return Bind(targetExpression, targetContext, sourceValue);

            if (definedTarget)
                return Bind(sourceExpression, sourceContext, targetValue);

            bindingFormula = new BindingFormula(sourceVars, targetVars) { LeftExpression = sourceExpression, RightExpression = targetExpression };

            return true;
        }

        internal static bool UnifySimple(Expression definedExpression, Expression otherExpression, IContext context)
        {
            var vars = new HashSet<string>();

            CheckDefined(otherExpression, context, vars);

            var definedOther = !vars.Any();

            var definedValue = definedExpression.Apply(context);

            if (definedOther)
                return CompareValues(definedExpression.Apply(context), otherExpression.Apply(context));

            return BindSimple(otherExpression, context, definedExpression);
        }

        private static bool BindSimple(Expression expression, IContext context, Expression definedExpression)
        {
            if (expression is Query query)
                return query.Bind(context, definedExpression.Apply(context));

            if (expression is FunctionCall functionCall)
            {
                var value = FunctionCall.Call(functionCall.Name, new object[] { definedExpression.Apply(context) }, context, Relationship.Left);
                Bind(functionCall.Args, context, value);
            }

            return false;
        }

        private static void CheckDefined(Expression expression, IContext context, HashSet<string> undefinedVars)
        {
            if (expression is Query query)
            {
                if (!query.IsDefined(context))
                    undefinedVars.Add(query.Name);

                return;
            }

            foreach (var expr in expression.Operands())
            {
                CheckDefined(expr, context, undefinedVars);
            }
        }

        internal static bool Bind(Expression expression, IContext context, object value)
        {
            if (expression is Query query)
                return query.Bind(context, value);

            if (expression is TupleValueMatch tupleValueMatch)
            {
                if (tupleValueMatch.operands.Length == 1)
                {
                    return Bind(tupleValueMatch.operands[0], context, value);
                }
                else
                {
                    var single = ValueConverter.ConvertToSingle(value);

                    if (single is TupleValue tupleValue)
                    {
                        if (tupleValueMatch.operands.Length == tupleValue.Items.Length)
                        {
                            for (int i = 0; i < tupleValue.Items.Length; i++)
                            {
                                Bind(tupleValueMatch.operands[i], context, tupleValue.Items[i]);
                            }

                            return true;
                        }

                        return false;
                    }
                }
            }

            if (expression is FunctionCall functionCall)
            {
                var val = FunctionCall.Call(functionCall.Name, new object[] { value }, context, Relationship.Left);
                return Bind(functionCall.Args, context, val);
            }

            return false;
        }

        internal static void PostUnify(UnificationContext unificationContext, IContext targetContext, IContext sourceContext)
        {
            foreach (var item in unificationContext.BindingTable)
            {
                if (item.Key is Query query)
                {
                    var value = item.Value.Apply(sourceContext);

                    if (value != UnsetValue)
                        Bind(item.Key, targetContext, value);
                }
                else
                {
                    if (item.Key is FunctionCall functionCall)
                    {
                        //var backFunction = new FunctionCall();
                        //backFunction.SetOperands(new Expression[] { new TupleValueMatch(new StringValueMatch(functionCall.Name), item.Value.CloneExpression()) });
                        //var param = sourceContext.UseParameter(Parameters.Left);
                        //var value = backFunction.Apply(sourceContext);
                        //param.Free(sourceContext);

                        var value = FunctionCall.Call(functionCall.Name, new object[] { item.Value.Apply(sourceContext) }, sourceContext, Relationship.Left);
                        Bind(functionCall.Args, targetContext, value);
                    }
                }
            }
        }
    }
}
