using System;
using System.Collections.Generic;
using System.Linq;
using Spard.Sources;
using Spard.Common;
using Spard.Exceptions;
using System.Collections;
using Spard.Data;
using Spard.Core;
using Spard.Transitions;

namespace Spard.Expressions
{
    /// <summary>
    /// Set (predicate)
    /// </summary>
    public sealed class Set: Dual
    {
        private IContext _initContext = null;
        /// <summary>
        /// Set definition
        /// </summary>
        internal Definition[] _definition = null;

        private int _definitionIndex = -1;

        private int _step = 0;

        private UnificationContext _unificationContext = null;

        private int[] _positions = null;
        private int _positionsIndex = -1;
        private int _parsingStep = -1;

        private RecursiveStateTable _table = null;
        private RecursiveState _recursiveState = null;

        private int _index;
        private List<RecursiveTransformState> _allResults = new List<RecursiveTransformState>();

        /// <summary>
        /// Array of digits
        /// </summary>
        private static readonly IList Digits = "0123456789".ToCharArray();

        internal TupleValueMatch List { get; private set; } = null;

        protected internal override Priorities Priority
        {
            get { return Priorities.Set; }
        }

        protected internal override string Sign
        {
            get { return "<"; }
        }

        protected internal override string CloseSign
        {
            get { return ">"; }
        }

        internal string Name
        {
            get { return ((StringValueMatch)List.operands[0]).ToString(); }
        }

        private List<RecursiveTransformState> collectedResults = new List<RecursiveTransformState>();

        public Set()
        {
        }

        public Set(string name)
        {
            _operand = List = new TupleValueMatch(new StringValueMatch(name));
        }

        public Set(TupleValueMatch operand)
            : base (operand)
        {
            List = operand;
        }

        // TODO: Replace set description with their equivalent set definitions
        // <BR> := \r\n|\r|\n
        // <SP> := ' |\t|...

        private object localName;
        private bool enumerate = false;

        private string name;
        private string module;

        private int initStart;

        internal override bool MatchCore(ISource input, ref IContext context, bool next)
        {
            IContext workingContext = null;
            if (!next)
            {
                _initContext = context; // If the match is successful, we will still rewrite the context. Otherwise the value of initContext doesn’t bother us much
                workingContext = context.Clone();
                _step = 0;

                initStart = input.Position;

                localName = List.operands[0].Apply(workingContext);

                if (localName == BindingManager.UnsetValue)
                {
                    enumerate = true;

                    ((Query)List.operands[0]).InitValuesEnumeration(workingContext);
                    name = ((Query)List.operands[0]).EnumerateValue(ref workingContext);

                    if (name == null)
                        return false;

                    _definition = null;

                    module = "";
                }
                else
                {
                    enumerate = false;

                    var res = localName as string;
                    //if (res == null)
                    //{
                    //    var qualifiedName = (QualifiedName)this.localName;
                    //    this.module = qualifiedName.path[0];
                    //    this.name = qualifiedName.path[1];
                    //}
                    //else
                    {
                        module = "";
                        name = res;
                    }
                }
            }
            else
            {
                workingContext = _initContext.Clone();
            }

            do
            {
                var result = SingleMatch(input, ref context, next, workingContext);

                if (!enumerate || result)
                    return result;

                name = ((Query)List.operands[0]).EnumerateValue(ref workingContext);

                if (name == null)
                    return false;

                _definition = null;
                next = false;

            } while (true);
        }

        private bool SingleMatch(ISource input, ref IContext context, bool next, IContext workingContext)
        {
            // Predefined sets
            switch (name)
            {
                // Line break characters:
                // <BR> := \r\n|\r|\n
                case "BR":
                    #region BR
                    if (_step == 2)
                        break;

                    var c1 = input.Read();
                    while (_step < 2)
                    {
                        switch (_step)
                        {
                            case 0:
                                int pos = input.Position;
                                var c2 = input.Read();
                                if (Equals(c1, '\r') && Equals(c2, '\n'))
                                {
                                    context = workingContext;
                                    _step += 2;
                                    return true;
                                }
                                else
                                    input.Position = pos;
                                break;
                            default:
                                if (Equals(c1, '\r') || Equals(c1, '\n'))
                                {
                                    context = workingContext;
                                    _step++;
                                    return true;
                                }
                                else
                                    input.Position = initStart;
                                break;
                        }
                        _step++;
                    }
                    break;
                #endregion

                // Space characters
                // <SP> := ' |\t|...
                case "SP":
                    #region SP
                    if (_step == 0)
                    {
                        _step++;
                        var c = input.Read();
                        if (c != null && Char.IsWhiteSpace((char)(object)c) && !Equals(c, '\r') && !Equals(c, '\n'))
                        {
                            context = workingContext;
                            return true;
                        }
                        else
                            input.Position = initStart;
                    }
                    break;
                #endregion

                // Text string
                // <s> := [on, line].*
                // For specials sets only indexes of coincidences are saved. And they do not change the context!
                case "String":
                case "s":
                    #region String
                    if (workingContext.GetParameter(Parameters.IsLazy))
                    {
                        // Lazy algorithm
                        if (!next)
                        {
                            _positions = new int[] { input.Position };
                            context = workingContext;
                            SaveMatch(initStart, input, context);
                            return true;
                        }

                        input.Position = _positions[0];

                        object c;
                        if (!input.EndOfSource && !Equals((c = input.Read()), '\r') && !Equals(c, '\n'))
                        {
                            context = workingContext;
                            SaveMatch(initStart, input, context);
                            _positions[0] = input.Position;
                            return true;
                        }
                        else
                        {
                            input.Position = initStart;
                        }
                    }
                    else
                    {
                        // Greedy algorithm
                        if (!next)
                        {
                            var positionsMax = 0;
                            object c;

                            while (!input.EndOfSource && !Equals((c = input.Read()), '\r') && !Equals(c, '\n'))
                                positionsMax++;

                            _positionsIndex = positionsMax;
                        }

                        if (_positionsIndex >= 0)
                        {
                            context = workingContext;
                            input.Position = initStart + _positionsIndex--;
                            SaveMatch(initStart, input, context);
                            return true;
                        }
                        else
                        {
                            input.Position = initStart;
                        }
                    }
                    break;
                #endregion

                // Multilined text
                // <t> := .*
                case "Text":
                case "t":
                    #region Text
                    if (workingContext.GetParameter(Parameters.IsLazy))
                    {
                        // Lazy algorithm
                        if (!next)
                        {
                            _positions = new int[] { input.Position };
                            context = workingContext;
                            SaveMatch(initStart, input, context);
                            return true;
                        }

                        input.Position = _positions[0];

                        object c;
                        if (!input.EndOfSource)
                        {
                            c = input.Read();
                            if (Equals(c, '\r')) // \r\n should be counted together
                            {
                                c = input.Read();
                                if (!Equals(c, '\n'))
                                {
                                    input.Position--;
                                }
                            }
                            context = workingContext;
                            _positions[0] = input.Position;
                            SaveMatch(initStart, input, context);
                            return true;
                        }
                        else
                        {
                            input.Position = initStart;
                            return false;
                        }
                    }
                    else
                    {
                        // Greedy algorithm
                        if (!next)
                        {
                            var positionsMax = 0;
                            object c;
                            var posList = new List<int>();
                            while (!input.EndOfSource)
                            {
                                c = input.Read();
                                positionsMax++;
                                if (Equals(c, '\r')) // \r\n should be counted together
                                {
                                    c = input.Read();
                                    if (!Equals(c, '\n'))
                                    {
                                        input.Position--;
                                    }
                                    else
                                    {
                                        posList.Add(positionsMax);
                                        positionsMax++;
                                    }
                                }
                            }
                            _positionsIndex = positionsMax;
                            _positions = posList.ToArray();
                        }

                        if (_positionsIndex >= 0)
                        {
                            context = workingContext;
                            if (_positions.Contains(_positionsIndex))
                                _positionsIndex--;
                            input.Position = initStart + _positionsIndex--;
                            SaveMatch(initStart, input, context);
                            return true;
                        }
                        else
                        {
                            input.Position = initStart;
                        }
                    }
                    break;
                #endregion

                // Digit
                // <d> := 0-9
                case "Digit":
                case "d":
                    #region Digit
                    if (!next)
                    {
                        if (Digits.Contains(input.Read()))
                        {
                            context = workingContext;
                            SaveMatch(initStart, input, context);
                            return true;
                        }
                    }

                    input.Position = initStart;
                    break;
                #endregion

                // Integer
                // <i> := ('+|'-|)<Digit>+
                case "Int":
                case "i":
                    #region Int
                    if (workingContext.GetParameter(Parameters.IsLazy))
                    {
                        // Lazy algorithm
                        if (!next)
                            _parsingStep = 0;

                        object c;
                        if (!input.EndOfSource)
                        {
                            c = input.Read();
                            if (_parsingStep == 0 && (Equals(c, '+') || Equals(c, '-')))
                                c = input.Read();

                            _parsingStep = 1;
                            if (Digits.Contains(c))
                            {
                                context = workingContext;
                                SaveMatch(initStart, input, context);
                                return true;
                            }
                        }
                    }
                    else
                    {
                        // Greedy algorithm
                        if (!next)
                        {
                            _parsingStep = 0;
                            var posList = new List<int>
                            {
                                0
                            };

                            var positionsMax = 0;
                            object c;

                            while (!input.EndOfSource)
                            {
                                c = input.Read();
                                if (_parsingStep == 0)
                                {
                                    _parsingStep = 1;
                                    if (Equals(c, '+') || Equals(c, '-'))
                                    {
                                        c = input.Read();
                                        posList.Add(1);
                                        positionsMax++;
                                    }
                                }

                                if (!Digits.Contains(c))
                                    break;

                                _parsingStep = 2;
                                positionsMax++;
                            }

                            if (_parsingStep != 2)
                                _positionsIndex = 0;
                            else
                            {
                                _positionsIndex = positionsMax;
                                _positions = posList.ToArray();
                            }
                        }

                        if (_positionsIndex > 0)
                        {
                            context = workingContext;
                            if (_positions.Contains(_positionsIndex))
                                _positionsIndex--;
                            input.Position = initStart + _positionsIndex--;
                            SaveMatch(initStart, input, context);
                            return true;
                        }
                    }
                    input.Position = initStart;
                    return false;
                #endregion

                // Double
                // <Number> := ('+|'-|)<Digit>+('.<Digit>+)?
                //case "Number":
                //    #region Number

                //    break;
                //    #endregion

                default:
                    // Default behavior
                    return MatchDefault(input, ref context, next, workingContext, initStart);
            }

            return false;
        }

        private bool MatchDefault(ISource input, ref IContext context, bool next, IContext workingContext, int initStart)
        {
            if (_step == -1)
                return false;

            if (_definition == null)
            {
                _definition = _initContext.Root.GetSet(module, name, List.operands.Length);
                if (_definition == null)
                    throw new SetDefinitionNotFoundException { SetNameAndAttributes = List.operands.Select(item => item.ToString()).ToArray() };
            }

            if (!next)
                _definitionIndex = 0;

            do
            {
                var result = MatchDefinition(input, ref context, next, workingContext, initStart);

                if (result)
                    return result;

                _definitionIndex++;
                next = false;
                _step = 0;
                workingContext = _initContext.Clone();

            } while (_definitionIndex < _definition.Length);

            return false;           
        }

        private bool MatchDefinition(ISource input, ref IContext context, bool next, IContext workingContext, int initStart)
        {
            var currentDefinition = _definition[_definitionIndex];

            var isLeftRecursion = workingContext.GetParameter(Parameters.LeftRecursion);
            if (isLeftRecursion)
            {
                var argKey = "arg_" + Parameters.LeftRecursion;
                if (context.Vars.TryGetValue(argKey, out object value))
                {
                    var list = (List<string>)value;
                    if (!list.Contains(name))
                        isLeftRecursion = false;
                }
            }

            IContext innerContext = null;
            if (_step == 0)
            {
                innerContext = ((Set)currentDefinition.Left).Unify(this, workingContext, ref _unificationContext);

                if (innerContext == null)
                {
                    _step = -1;
                    return false;
                }

                if (workingContext.SearchBestVariant)
                {
                    if (workingContext.Vars.TryGetValue(Context.MatchKey, out object m))
                    {
                        // Made for CollectedMatch collection
                    }
                }

                _step++;

                if (isLeftRecursion)
                {
                    #region Left recursion
                    // Left recursion processing algorithm
                    // Check whether we have previously fallen into a similar state (detecting the fact of left recursion)
                    // The key is determined by the name of the set, the actual values of the arguments and the position in the input
                    collectedResults.Clear();

                    var args = List.operands.Skip(1).Select(op => op.Apply(workingContext)).ToArray();
                    var stateKey = new RecursiveStateKey(input.Position, name, args, _definitionIndex);

                    // The states variable holds a table of all the recursive states passed. Having found the second repeating state, we determine the left recursion
                    if (workingContext.Vars.TryGetValue("states", out object wrapper))
                    {
                        _table = (RecursiveStateTable)wrapper; // --- Clone? Create a clone of the table to transfer further (each step keeps its own table)
                    }
                    else
                    {
                        _table = new RecursiveStateTable();
                    }

                    innerContext.Vars["states"] = _table;

                    // Is there a left recursion (a call with the same state)?
                    if (_table.TryGetValue(stateKey, out _recursiveState))
                    {
                        if (_recursiveState.Top != this)
                        {
                            if (_recursiveState.Results.Count == 0) // Zero step, always return false
                            {
                                _recursiveState.Index++;
                                RecursiveIndex = _recursiveState.Index;
                                return false;
                            }

                            _index = 0;

                            return GetRecursiveResult(input, ref context, workingContext, initStart);
                        }
                    }
                    else
                    {
                        _table[stateKey] = _recursiveState = new RecursiveState { Top = this }; // Top Recursion Node (First)
                    }
                    #endregion
                }
            }
            else
            {
                // step != 0, continuation of the comparison

                // Set the context. Not completely, since with next = true the child templates will use the internal context
                // But without context, it is also impossible to pass null - you need to know the context parameters
                innerContext = new Context(_initContext);
                if (!innerContext.Vars.ContainsKey("states") && _table != null)
                    innerContext.Vars["states"] = _table;

                if (isLeftRecursion)
                {
                    if (_recursiveState.Fired)
                    {
                        if (_recursiveState.Top == this)
                        {
                            if (_index == _allResults.Count)
                            {
                                var key = _table.FirstOrDefault(p => p.Value == _recursiveState).Key;
                                if (key != null)
                                    _table.Remove(key);

                                return false;
                            }

                            var activeState = _allResults[_index++];
                            input.Position = activeState.Position;
                            ApplyUnification(initStart, input, workingContext, activeState.Context);
                            context = workingContext;

                            return true;
                        }
                        else
                        {
                            return GetRecursiveResult(input, ref context, workingContext, initStart);
                        }
                    }
                }
            }

            var origin = innerContext;
            var match = currentDefinition.Right.Match(input, ref innerContext, next);

            if (isLeftRecursion) // Left recursion has occurred, you need to collect all the results
            {
                if (_recursiveState.Index > 0) // == the number of recursive calls passed at a time (maybe several if they were in a row: <A> := <A>a|<A>b|c)
                {
                    _recursiveState.Fired = true;
                    _index = collectedResults.Count; // Where do we start producing results

                    if (match)
                    {
                        collectedResults.Add(new RecursiveTransformState(input.Position, innerContext.Clone(), ++_recursiveState.Index));

                        // We get the remaining results
                        next = true;
                        bool localMatch;
                        do
                        {
                            input.Position = initStart;
                            localMatch = currentDefinition.Right.Match(input, ref innerContext, next);

                            if (localMatch)
                                collectedResults.Add(new RecursiveTransformState(input.Position, innerContext.Clone(), ++_recursiveState.Index));

                        } while (localMatch);
                    }

                    // All results that were found
                    _allResults = new List<RecursiveTransformState>(collectedResults);
                    // All results of the current step
                    var results = new List<RecursiveTransformState>();

                    _recursiveState.Results.Clear();
                    _recursiveState.Results.AddRange(collectedResults);

                    // Switch to upstream transformation mode
                    // Having left recursion in the form <A> => <A>P | Q, act like that:
                    // All matching chains must be like QP*
                    // At the zero step we postulate match(<A>) = false, <A> => <false> | Q and collect match(Q)
                    // At the first step match(<A>) = match(Q), A => QP | Q and collect match(QP)
                    // At the second step match(<A>) = match(QP) and collect match(QPP)
                    // Etc. as long as we can get new matches
                    // Next, we cache the results and consistently return them in the reverse order of receipt
                    do
                    {
                        var localNext = false;
                        var tempContext = origin.Clone();
                        results.Clear();

                        while (currentDefinition.Right.Match(input, ref tempContext, localNext))
                        {
                            localNext = true;

                            var newState = new RecursiveTransformState(input.Position, tempContext, _recursiveState.CurrentDerivation.ToArray());
                            if (!_allResults.Contains(newState) && !results.Contains(newState)) // There must be a change
                            {
                                results.Add(newState);
                            }

                            input.Position = initStart;
                            _recursiveState.CurrentDerivation.Clear();
                        }

                        _allResults.AddRange(results);

                        _recursiveState.Results.Clear();
                        _recursiveState.Results.AddRange(results);
                    } while (results.Any());

                    _allResults.Sort();

                    if (_index == _allResults.Count)
                    {
                        if (_recursiveState.Top == this)
                        {
                            var key = _table.FirstOrDefault(p => p.Value == _recursiveState).Key;
                            if (key != null)
                                _table.Remove(key);
                        }

                        return false;
                    }

                    var activeState = _allResults[_index++];
                    input.Position = activeState.Position;
                    ApplyUnification(initStart, input, workingContext, activeState.Context);
                    context = workingContext;

                    return true;
                }
                else if (match)
                {
                    collectedResults.Add(new RecursiveTransformState(input.Position, innerContext.Clone(), 0));
                }
            }

            if (match)
            {
                ApplyUnification(initStart, input, workingContext, innerContext);

                context = workingContext;
                return true;
            }

            if (isLeftRecursion) // Left recursion has occurred, we need to collect all the results
            {
                if (_recursiveState != null && _recursiveState.Top == this)
                {
                    var key = _table.FirstOrDefault(p => p.Value == _recursiveState).Key;
                    if (key != null)
                        _table.Remove(key);
                }
            }

            _step = 0;
            next = false;
            return false;
        }

        private bool GetRecursiveResult(ISource input, ref IContext context, IContext workingContext, int initStart)
        {
            _recursiveState.Index = RecursiveIndex;

            if (_index >= _recursiveState.Results.Count)
            {
                if (_recursiveState.Top == this)
                {
                    var key = _table.FirstOrDefault(p => p.Value == _recursiveState).Key;
                    if (key != null)
                        _table.Remove(key);
                }

                return false;
            }

            // Here we take turns returning the already prepared results of the previous step
            var activeState = _recursiveState.Results[_index++];
            input.Position = activeState.Position;
            ApplyUnification(initStart, input, workingContext, activeState.Context);
            context = workingContext;

            _recursiveState.CurrentDerivation.Add(RecursiveIndex);
            _recursiveState.CurrentDerivation.AddRange(activeState.Derivation);

            return true;
        }

        private void ApplyUnification(int initStart, ISource input, IContext targetContext, IContext sourceContext)
        {
            BindingManager.PostUnify(_unificationContext, targetContext, sourceContext);

			if (sourceContext.Vars.TryGetValue(Context.MatchKey, out object match))
			{
				var matchValue = new NamedValue { Name = Name, Value = match };
				targetContext.AddMatch(matchValue, initStart);
			}
			else
				SaveMatch(initStart, input, targetContext);
		}

        private void SaveMatch(int initStart, ISource input, IContext context)
        {
            if (context.GetParameter(Parameters.Match) || context.GetParameter(Parameters.FullMatch))
            {
                // Save the matched part as a match
                var matchValue = new NamedValue { Name = Name, Value = input.GetValue(initStart, input.Position - initStart) };
                context.AddMatch(matchValue, initStart);
            }
        }

        /// <summary>
        /// Unify with a given set and its context
        /// </summary>
        /// <param name="set">The set whose variables need to be unified</param>
        /// <param name="context">Context of a unified set</param>
        /// <param name="unificationContext">Special unification context to be reset</param>
        /// <returns>Unification context. In case of failure, returns null</returns>
        private IContext Unify(Set set, IContext context, ref UnificationContext unificationContext)
        {
            var newContext = new Context(context);
            unificationContext = new UnificationContext();

            for (int i = 1; i < List.operands.Length; i++)
            {
                var sourceExpression = set.List.operands[i];
                var targetExpression = List.operands[i];

                if (!BindingManager.Unify(targetExpression, sourceExpression, newContext, context, out BindingFormula bindingFormula))
                    return null;

                if (bindingFormula != null)
                {
                    if (unificationContext.BindingTable.TryGetValue(sourceExpression, out Expression t))
                    {
                        // Bind the value of the variable targetExpression to the value of the variable t
                        //newContext.Vars[((Query)targetExpression).Name] = newContext.Vars[((Query)t).Name];
                        newContext.AddFormula(new BindingFormula((Query)targetExpression, (Query)t));
                    }
                    else
                    {
                        unificationContext.BindingTable[sourceExpression] = targetExpression;
                        //newContext.Vars[((Query)targetExpression).Name] = new ValueReference();
                    }
                }
            }

            return newContext;
        }

        internal override object Apply(IContext context)
        {
            if (context.Runtime != null && context.Runtime.CancellationToken.IsCancellationRequested)
                return null;

            name = List.operands[0].Apply(context).ToString();
            module = "";
            switch (name)
            {
                case "BR":
                    return Environment.NewLine;

                case "SP":
                    return ' ';

                default:
                    {
                        if (context.Vars.ContainsKey(Context.TranslateKey))
                        {
                            // SDT-mode
                            if (context.Vars.TryGetValue(Context.MatchKey, out object match))
                            {
                                if (match is NamedValue namedValue)
                                {
                                    if (namedValue.Name == name)
                                        return namedValue.Value;
                                }

                                if (match is TupleValue tupleValue)
                                {
                                    foreach (NamedValue item in tupleValue.Items)
                                    {
                                        if (item.Name == name)
                                            return item.Value;
                                    }
                                }
                            }

                            return null;
                        }
                        else
                        {
                            if (_definition == null)
                            {
                                _definition = context.Root.GetSet(module, name, List.operands.Length);
                                if (_definition == null)
                                    throw new SetDefinitionNotFoundException() { SetNameAndAttributes = List.operands.Select(item => item.ToString()).ToArray() };
                            }

                            for (int i = 0; i < _definition.Length; i++)
                            {
                                IContext innerContext = ((Set)_definition[i].Left).Unify(this, context, ref _unificationContext);

                                if (innerContext == null)
                                    continue;

                                return _definition[i].Right.Apply(innerContext);
                            }

                            return null;
                        }
                    }
            }
        }

        public override Expression CloneCore()
        {
            throw new NotImplementedException();
        }

        public override Expression CloneExpression()
        {
            return new Set((TupleValueMatch)List.CloneExpression());
        }

        internal override TransitionTable BuildTransitionTableCore(TransitionSettings settings, bool isLast)
        {
            if (List.operands[0] is StringValueMatch nameExpr)
            {
                var name = nameExpr.Value;
                switch (name)
                {
                    case "BR":
                        {
                            var result = new TransitionTable
                            {
                                [new InputSet(InputSetType.Include, '\n')] = TransitionTableResultCollection.Empty.CloneCollection()
                            };

                            var collection = new TransitionTableResultCollection
                            {
                                new TransitionTableResult(new StringValueMatch("\n")),
                                new TransitionTableResult(null)
                            };

                            result[new InputSet(InputSetType.Include, '\r')] = collection;
                            return result;
                        }

                    case "d":
                        {
                            var result = new TransitionTable
                            {
                                [new InputSet(InputSetType.Include, new ValuesRange('0', '9'))] = TransitionTableResultCollection.Empty.CloneCollection()
                            };
                            return result;
                        }

                    default:
                        var definition = settings.Root.GetSet("", name, List.operands.Length);
                        var tables = definition.Select(def => def.Right.BuildTransitionTable(settings, isLast)).ToArray();
                        return TransitionTable.Join(tables);
                }
            }

            return base.BuildTransitionTableCore(settings, isLast);
        }

        internal bool Refresh()
        {
            var result = _definition != null;
            _definition = null;
            return result;
        }

        public override void SetOperands(IEnumerable<Expression> operands)
        {
            base.SetOperands(operands);

            List = _operand as TupleValueMatch;
            if (List == null)
            {
                _operand = List = new TupleValueMatch(_operand);
            }
        }

        private int RecursiveIndex { get; set; }
    }
}
