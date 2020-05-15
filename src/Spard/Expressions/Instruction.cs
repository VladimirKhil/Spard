using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using Spard.Common;
using Spard.Sources;
using System.Linq;
using Spard.Transitions;
using Spard.Data;
using Spard.Core;

namespace Spard.Expressions
{
    /// <summary>
    /// Transformation instruction
    /// </summary>
    public sealed class Instruction: Dual
    {
        private IContext initContext = null;

        /// <summary>
        /// Does this instruction need extra right argument
        /// </summary>
        internal bool RightArgumentNeeded
        {
            get
            {
                return ((IInstructionExpression)_operand).RightArgumentNeeded;
            }
        }

        protected internal override Relationship Assotiative
        {
            get
            {
                return Relationship.Right;
            }
        }

        protected internal override string CloseSign
        {
            get { return "]"; }
        }

        protected internal override Priorities Priority
        {
            get { return Priorities.Instruction; }
        }

        protected internal override string Sign
        {
            get { return "["; }
        }

        /// <summary>
        /// Extra instruction argument
        /// </summary>
        public Expression Argument { get; set; } = null;

        public override IEnumerable<Expression> Operands()
        {
            yield return _operand;
            if (Argument != null)
                yield return Argument;
        }

        public override void SetOperands(IEnumerable<Expression> operands)
        {
            var opArray = operands.ToArray();
            if (opArray.Length > 0)
            {
                _operand = opArray[0];
                if (opArray.Length > 1)
                    Argument = opArray[1];
            }

            if (_operand is StringValueMatch sv)
            {
                if (Modifiers.ContainsKey(sv.Value))
                {
                    _operand = new TupleValueMatch(new StringValueMatch("on"), _operand);
                }
            }
            else
            {
                if (_operand is TupleValueMatch list)
                {
                    if (Modifiers.ContainsKey(list._operands[0].ToString()))
                    {
                        var ops = new List<Expression>(0)
                        {
                            new StringValueMatch("on")
                        };
                        ops.AddRange(list._operands);

                        _operand = new TupleValueMatch(ops.ToArray());
                    }
                }
            }
        }

        public Instruction()
        {

        }

        public Instruction(Expression operand)
            : base(operand)
        {
            
        }

        public Instruction(Expression operand, Expression argument)
            : base(operand)
        {
            Argument = argument;
        }

        private static readonly Dictionary<string, Parameters> Modifiers = new Dictionary<string, Parameters>()
        {
            { "ignoresp", Parameters.IgnoreSP },
            { "keepinitiator", Parameters.KeepInitiator },
            { "left", Parameters.Left },
            { "m", Parameters.Match },
            { "match", Parameters.FullMatch },
            { "multi", Parameters.Multi },
            { "lazy", Parameters.IsLazy },
            { "line", Parameters.Line },
            { "lrec", Parameters.LeftRecursion },
            { "opt", Parameters.Optional },
            { "ci", Parameters.CaseInsensitive },
            { "collect", Parameters.Collect }
        };

        internal override bool MatchCore(ISource input, ref IContext context, bool next)
        {
            IContext workingContext;
            if (!next)
            {
                initContext = context;
                workingContext = context.Clone();
            }
            else
            {
                workingContext = initContext.Clone();
            }

            var initStart = input.Position;

            if (_operand is Unification unification)
            {
                if (next)
                    return false;

                return unification.Unify(context);
            }

            if (_operand is FunctionCall functionCall)
            {
                if (next)
                    return false;

                var result = functionCall.Apply(context);

                if (result is bool b)
                    return b;

                return true;
            }

            var expressionList = _operand as TupleValueMatch;
            Expression[] arguments;
            if (expressionList != null)
            {
                arguments = expressionList._operands;
            }
            else
            {
                arguments = new Expression[] { _operand };
            }

            switch (arguments[0].ToString())
            {
                case "foreach":
                    {
                        if (!(arguments[2] is FunctionCall))
                            break;

                        ContextParameter parameter = null;
                        if (expressionList._operands.Length > 3)
                            parameter = workingContext.UseParameter(Parameters.Left);

                        var val = arguments[2].Apply(next ? null : workingContext);

                        if (parameter != null)
                            parameter.Free(workingContext);

                        var name = ((Query)arguments[1]).Name;
                        if (workingContext.Vars.TryGetValue(name, out object cVal))
                            return cVal.Equals(val);

                        workingContext.Vars[name] = val;
                        context = workingContext;
                        return true;
                    }

                case "one": // Cutoff: only the first result of the match is allowed
                    {
                        if (next)
                        {
                            input.Position = initStart;
                            return false;
                        }

                        return Argument.Match(input, ref context, next);
                    }

                case "on":
                case "off":
                    {
                        if (arguments.Length == 1)
                            break;

                        Modifiers.TryGetValue(arguments[1].ToString(), out Parameters parameter);

                        var param = context.UseParameter(parameter, arguments[0].ToString() == "on");

                        IContext origin = null;
                        if (arguments[0].ToString() == "on" && arguments.Length > 2)
                        {
                            origin = context;
                            context = context.Clone();

                            var argKey = "arg_" + parameter;
                            var list = new List<string>();
                            if (context.Vars.TryGetValue(argKey, out object val))
                            {
                                var value = (List<string>)val;
                                var originList = value as List<string>;
                                list.AddRange(originList);
                            }

                            list.Add(arguments[2].ToString());
                            context.Vars[argKey] = list;
                        }

                        var result = Argument.Match(input, ref context, next);
                        param.Free(context);
                        if (!result && origin != null)
                            context = origin;

                        return result;
                    }

                case "cont": // Context node: it is checked for its presence, but it is not involved in the transformations
                    {
                        int start = input.Position;
                        var result = Argument.Match(input, ref context, next);
                        input.Position = start; // In case of failure, the position will not change
                        return result;
                    }

                case "madd":
                    {
                        if (next)
                            return false;

                        if (arguments.Length == 3)
                        {
                            context.AddMatch(new NamedValue
                            {
                                Name = arguments[1].Apply(context).ToString(),
                                Value = arguments[2].Apply(context)
                            });

                            return true;
                        }

                        throw new NotImplementedException();
                    }

                case "debug":
                    {
                        Debugger.Break();
                        if (Argument == null)
                            return !next;

                        return Argument.Match(input, ref context, next);
                    }

                case "time":
                    {
                        var name = arguments[1].ToString();

                        var sw = new Stopwatch();
                        sw.Start();

                        var result = Argument.Match(input, ref context, next);

                        sw.Stop();

                        if (!context.Runtime.UsedTime.TryGetValue(name, out TimeSpan ts))
                            context.Runtime.UsedTime[name] = sw.Elapsed;
                        else
                            context.Runtime.UsedTime[name] = ts.Add(sw.Elapsed);

                        return result;
                    }

                case "cache":
                    {
                        // Resuls caching (memoization)
                        var cache = workingContext.Runtime.GetDict(this);

                        if (!next)
                        {
                            var state = new SimpleTransformState(input.Position, workingContext);

                            if (cache.Cache.TryGetValue(state, out List<SimpleTransformState> results))
                            {
                                cache.ActiveList = results;
                            }
                            else
                            {
                                cache.ActiveList = new List<SimpleTransformState>();
                                cache.Cache[state] = cache.ActiveList;
                            }

                            cache.Index = 0;
                        }

                        if (cache.Index < cache.ActiveList.Count)
                        {
                            var item = cache.ActiveList[cache.Index++];
                            if (item == null)
                                return false;

                            input.Position = item.Position;
                            context = item.Context.Clone();
                        }
                        else
                        {
                            var match = Argument.Match(input, ref context, next);
                            if (match)
                            {
                                var newState = new SimpleTransformState(input.Position, context);
                                cache.ActiveList.Add(newState);
                                cache.Index++;
                            }
                            else
                            {
                                cache.ActiveList.Add(null);
                            }

                            return match;
                        }

                        return true;
                    }

                case "any":
                    {
                        if (next)
                            return false;

                        if (!(Argument is StringValueMatch stringValue))
                            throw new NotImplementedException();

                        var item = input.Read();
                        if (!(item is char))
                        {
                            input.Position--;
                            return false;
                        }

                        var result = stringValue.Value.Contains((char)item);
                        if (!result)
                            input.Position--;

                        return result;
                    }

                default:
                    break;
            }

            if (next)
                return false;

            return _operand.Match(input, ref context, next);
        }

        internal override object Apply(IContext context)
        {
            if (_operand is Unification unification)
            {
                return unification.Unify(context) ? null : BindingManager.NullValue;
            }

            Expression[] arguments = _operand is TupleValueMatch expressionList ? expressionList._operands : (new Expression[] { _operand });
            
            switch (arguments[0].ToString())
            {
                case "cont":
                    return null;

                case "on":
                case "off":
                    {
                        if (arguments.Length == 1)
                            break;

                        Modifiers.TryGetValue(arguments[1].ToString(), out Parameters parameter);

                        var param = context.UseParameter(parameter, arguments[0].ToString() == "on");
                        var result = Argument.Apply(context);
                        param.Free(context);
                        return result;
                    }

                case "debug":
                    {
                        Debugger.Break();
                        return Argument.Apply(context);
                    }

                case "time":
                    {
                        var name = arguments[1].ToString();

                        var sw = new Stopwatch();
                        sw.Start();

                        var result = Argument.Apply(context);

                        sw.Stop();

                        if (!context.Runtime.UsedTime.TryGetValue(name, out TimeSpan ts))
                            context.Runtime.UsedTime[name] = sw.Elapsed;
                        else
                            context.Runtime.UsedTime[name] = ts.Add(sw.Elapsed);

                        return result;
                    }

                default:
                    break;
            } 

            return _operand.Apply(context);
        }
        
        internal Tuple<string, string> Unify(Expression expression, IContext context, IContext newContext)
        {
            var result = expression.Apply(context);
            var thisValue = _operand.ToString();

            string instructionValue = null;
            if (expression is Instruction instruction)
                instructionValue = instruction._operand.ToString();

            if (result != BindingManager.UnsetValue)
            {
                // Specific value specified, no return required
                newContext.Vars[thisValue] = result;
                return null;
            }

            return Tuple.Create(instructionValue, thisValue);
        }

        public override string ToString()
        {
            var result = new StringBuilder(base.ToString());
            base.AppendOperand(result, Argument);
            return result.ToString();
        }

        internal override TransitionTable BuildTransitionTableCore(TransitionSettings settings, bool isLast)
        {
            var arguments = _operand is TupleValueMatch expressionList ? expressionList._operands : (new Expression[] { _operand });
            
            switch (arguments[0].ToString())
            {
                case "any":
                    if (!(Argument is StringValueMatch stringValue))
                        throw new NotImplementedException();

                    var result = new TransitionTable();
                    foreach (var item in stringValue.Value)
                    {
                        result[new InputSet(InputSetType.Include, item)] = TransitionTableResultCollection.Empty.CloneCollection();
                    }

                    return result;

                default:
                    return Argument.BuildTransitionTable(settings, isLast);
            }
        }

        public override Expression CloneCore()
        {
            return new Instruction { Argument = Argument?.CloneExpression() };
        }

        /// <summary>
        /// Check instruction validity
        /// </summary>
        /// <returns>Is instruction valid</returns>
        internal bool Check()
        {
            if (_operand is Unification || _operand is FunctionCall)
                return true;

            var arguments = _operand is TupleValueMatch expressionList ? expressionList._operands : (new Expression[] { _operand });
            
            switch (arguments[0].ToString())
            {
                case "foreach":
                    {
                        if (arguments.Length < 3 || arguments.Length > 4)
                            return false;

                        if (!(arguments[2] is FunctionCall))
                            return false;

                        return arguments.Length == 3 || arguments[3].ToString() == "left";
                    }

                case "one":
                case "cont":
                case "debug":
                case "cache":
                case "any":
                    return arguments.Length == 1;

                case "on":
                case "off":
                    if (arguments.Length < 2)
                        return false;

                    return Modifiers.TryGetValue(arguments[1].ToString(), out _);

                case "madd":
                    return arguments.Length >= 2 && arguments.Length <= 3;

                case "optimize":
                case "simplematch":
                case "suppressinline":
                    return true;
            }

            return _operand is IInstructionExpression && (!(_operand is StringValueMatch));
        }
    }
}
