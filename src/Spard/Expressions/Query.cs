using Spard.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using Spard.Sources;
using System.Collections;
using Spard.Data;
using Spard.Transitions;
using Spard.Core;

namespace Spard.Expressions
{
    public sealed class Query : Unary, IInstructionExpression
    {
        private Expression _type;
        private int _initStart;
        private IContext _initContext = null;
        private IEnumerator _enumerator;

        public bool RightArgumentNeeded
        {
            get
            {
                return false;
            }
        }

        protected internal override Relationship OperandPosition
        {
            get
            {
                return Relationship.Right;
            }
        }

        protected internal override Priorities Priority
        {
            get
            {
                return Priorities.Query;
            }
        }

        protected internal override string Sign
        {
            get
            {
                return "$";
            }
        }

        public string Name
        {
            get
            {
				if (_operand is StringValueMatch stringValueMatch)
					return stringValueMatch.Value;

				if (this._operand is TupleValueMatch tupleValueMatch && tupleValueMatch._operands.Length > 0)
					return tupleValueMatch._operands[0].ToString();

				return _operand.ToString();
            }
        }

        public Query()
        {

        }

        public Query(Expression operand)
            : base(operand)
        {

        }

        public override Expression CloneCore()
        {
            return new Query();
        }

        internal override object Apply(IContext context)
        {
            object value;

            if (!(_operand is StringValueMatch name))
            {
                if (!(_operand is TupleValueMatch tuple))
                    throw new NotImplementedException();

                if (!(tuple._operands[0] is StringValueMatch objName))
                    throw new NotImplementedException();

                if (!context.Vars.TryGetValue(objName.Value, out value))
                    return null;

                var val = value;
                for (int i = 1; i < tuple._operands.Length; i++)
                {
                    if (!(tuple._operands[i] is StringValueMatch stringQuery))
                        throw new NotImplementedException();

                    var query = stringQuery.Value;

                    if (val is object[] objVal)
                    {
                        var res = new List<object>();
                        foreach (var item in objVal)
                        {
                            var result = Select(item, query);
                            if (result != null)
                                res.Add(result);
                        }

                        val = res.ToArray();
                    }
                    else
                    {
                        val = Select(val, query);
                        if (val == null)
                            return null;
                    }
                }

                return val;
            }

            if (context.Vars.TryGetValue(name.Value, out value))
                return value;

            return BindingManager.UnsetValue;
        }

        internal object Select(object value, string query)
        {
            if (value is NamedValue named)
            {
                if (named.Name == query)
                    return named.Value;

                return null; // Unification is not allowed here
            }

            if (value is TupleValue tupleValue)
            {
                foreach (NamedValue item in tupleValue.Items)
                {
                    if (item.Name == query)
                        return item.Value;
                }

                return null; // Unification is not allowed here
            }

            return null;
        }

        internal bool Bind(IContext context, object value)
        {
            if (!(_operand is StringValueMatch))
                return true; // This binding is not currently supported

            if (context.Vars.TryGetValue(Name, out object val))
                return BindingManager.CompareValues(val, value);

            context.SetValue(Name, value);
            return true;
        }

        internal override bool MatchCore(ISource input, ref IContext context, bool next)
        {
            if (!(_operand is StringValueMatch name))
                throw new NotImplementedException();

            IContext workingContext;
            if (!next)
            {
                _initStart = input.Position;
                _initContext = context;
                workingContext = context.Clone();
            }
            else
            {
                workingContext = _initContext;
            }

            if (workingContext.Vars.TryGetValue(name.Value, out object val))
            {
                if (next)
                {
                    input.Position = _initStart;
                    return false;
                }

                if (val is IEnumerable enumerable)
                {
                    foreach (var item in enumerable)
                    {
                        if (input.EndOfSource || !item.Equals(input.Read()))
                        {
                            input.Position = _initStart;
                            return false;
                        }
                    }

                    return true;
                }

                if (input.EndOfSource || !val.Equals(input.Read()))
                {
                    input.Position = _initStart;
                    return false;
                }

                context = workingContext;

                return true;
            }
            else
            {
                if (_type == null)
                {
                    if (!workingContext.DefinitionsTable.TryGetValue(name.Value, out Expression expr))
                        expr = workingContext.Root.GetVariableType(name.Value);

                    _type = expr.CloneExpression();
                }

                if (_type.Match(input, ref workingContext, next))
                {
                    var matchedValue = input.GetValue(_initStart, input.Position - _initStart);
                    workingContext.SetValue(name.Value, matchedValue);

                    context = workingContext;

                    return true;
                }

                return false;
            }
        }

        internal void InitValuesEnumeration(IContext context)
        {
            _initContext = context;

            if (_type == null)
            {
                var name = _operand as StringValueMatch;

                if (!context.DefinitionsTable.TryGetValue(name.Value, out Expression expr))
                    expr = context.Root.GetVariableType(name.Value);

                _type = expr.CloneExpression();
            }

            if (_type is Or or)
            {
                _enumerator = or._operands.Select(expr => expr.Apply(context)).GetEnumerator();
            }
        }

        internal string EnumerateValue(ref IContext context)
        {
            if (!_enumerator.MoveNext())
                return null;

            var variant = _enumerator.Current.ToString();

            context = _initContext.Clone();

            context.SetValue(Name, variant);

            return variant;
        }

        internal Tuple<string, string> Unify(Expression expression, IContext context, IContext newContext)
        {
            var result = expression.Apply(context);
            var thisValue = _operand.ToString();

            string instructionValue = null;
            if (expression is Query query)
                instructionValue = query._operand.ToString();

            if (result != BindingManager.UnsetValue)
            {
                // Specific value specified, no return is required
                newContext.Vars[thisValue] = result;
                return null;
            }

            return Tuple.Create(instructionValue, thisValue);
        }

        internal bool IsDefined(IContext context)
        {
            return context.Vars.ContainsKey(Name);
        }

        internal override TransitionTable BuildTransitionTableCore(TransitionSettings settings, bool isLast)
        {
            _type = settings.Root.GetVariableType(Name);            

            var table = _type.BuildTransitionTable(settings, isLast);
            var result = new TransitionTable();

            var contextChange = new ContextChange(Name, (object)null);

            foreach (var transition in table)
            {
                var collection = new TransitionTableResultCollection();

                foreach (var item in transition.Value)
                {
                    Expression expr;
                    if (!item.IsFinished)
                        expr = new InlineTypeDefinition(this, item.Expression);
                    else
                        expr = item.Expression;

                    collection.Add(new TransitionTableResult(expr, false, expr != null || !object.Equals(transition.Key, InputSet.IncludeEOS) ? contextChange : null));
                }

                result[transition.Key] = collection;
            }

            return result;
        }

        internal override bool EqualsSmart(Expression other, Dictionary<string, string> varsMap)
        {
            if (!(other is Query query))
                return false;

            if (!varsMap.TryGetValue(Name, out string otherName))
                return false;

            return otherName == query.Name;
        }
    }
}
