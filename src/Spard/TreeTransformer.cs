using System;
using System.Collections.Generic;
using System.IO;
using Spard.Common;
using System.Diagnostics;
using System.Linq;
using Spard.Sources;
using Spard.Results;
using Spard.Expressions;
using Spard.Exceptions;
using Spard.Transitions;
using System.Collections;
using System.Threading;
using Spard.Core;

namespace Spard
{
    /// <summary>
    /// Represents SPARD transformation tree.
    /// </summary>
    public class TreeTransformer : Transformer, IExpressionRoot, ITransformFunction
    {
        #region Fields

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private TransformMode _mode = TransformMode.Modification;

        /// <summary>
        /// Main function definitions
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private Function[] _functions = null;

        /// <summary>
        /// Subfunctions definitions
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private Dictionary<Tuple<string, string>, Function[]> _functionDefinitions = new Dictionary<Tuple<string, string>, Function[]>();

        /// <summary>
        /// Sets definitions
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private Dictionary<Tuple<string, string, int>, Definition[]> setDefinitions = new Dictionary<Tuple<string, string, int>, Definition[]>();

        /// <summary>
        /// Transformer direction
        /// </summary>
        protected internal Directions _direction = Directions.Right;

        /// <summary>
        /// Function name that current transformer belongs to (if appliable)
        /// </summary>
        private string _name = null;

        #endregion

        #region Properties

        /// <summary>
        /// Transformation mode which affects transformer behavior when an error occurred
        /// </summary>
        public TransformMode Mode { get { return _mode; } set { _mode = value; } }

        /// <summary>
        /// Subfunctions definitions
        /// </summary>
        public Dictionary<Tuple<string, string>, Function[]> FunctionDefinitions { get { return _functionDefinitions; } set { _functionDefinitions = value; } }

        /// <summary>
        /// Sets definitions.
        /// </summary>
        public Dictionary<Tuple<string, string, int>, Definition[]> SetDefinitions { get { return setDefinitions; } set { setDefinitions = value; } }

        private Dictionary<string, TransformerHelper.UserFunc> userFunctions;

        /// <summary>
        /// External (client-defined) functions.
        /// </summary>
        public Dictionary<string, TransformerHelper.UserFunc> UserFunctions
        {
            get
            {
                if (userFunctions == null)
                    userFunctions = new Dictionary<string, TransformerHelper.UserFunc>();

                return userFunctions;
            }
        }

        /// <summary>
        /// Module loading event which allows to return links to other modules by their names.
        /// </summary>
        public event Func<string, TextReader> LoadModule;

        int IExpressionRoot.FunctionCallDepth { get; set; }

        public bool SimpleMatch { get; set; }

        /// <summary>
        /// Force all sets to be replaced inline with their definitions.
        /// </summary>
        public bool SuppressInline { get; private set; }

        #endregion

        #region Constructor

        private readonly IExpressionRoot root;
        public bool SearchBestVariant { get; set; }

        /// <summary>
        /// Create tranformer tree
        /// </summary>
        protected internal TreeTransformer()
        {
            root = this;
        }

        internal TreeTransformer(IExpressionRoot root)
        {
            this.root = root ?? this;
        }        

        #endregion

        #region Parse

        /// <summary>
        /// Build transformation tree based on SPARD definitions
        /// </summary>
        /// <param name="transformation">Transformation described in SPARD language</param>
        /// <returns>Transformation tree which performs described transformation</returns>
        public static TreeTransformer Create(string transformation)
        {
            using (var reader = new StringReader(transformation))
            {
                return Create(reader);
            }
        }

        /// <summary>
        /// Build transformation tree based on SPARD definitions
        /// </summary>
        /// <param name="transformation">Reader of transformation described in SPARD language</param>
        /// <returns>Transformation tree which performs described transformation</returns>
        public static TreeTransformer Create(TextReader transformation)
        {
            var transformer = new TreeTransformer();
            transformer.Parse(transformation);

            return transformer;
        }

        /// <summary>
        /// Build transformation tree based on SPARD definitions
        /// </summary>
        /// <param name="transformation">Reader of transformation described in SPARD language</param>
        /// <param name="moduleLoader">Loader of additional modules</param>
        /// <returns>Transformation tree which performs described transformation</returns>
        public static TreeTransformer Create(TextReader transformation, Func<string, TextReader> moduleLoader)
        {
            var transformer = new TreeTransformer();
            transformer.LoadModule += moduleLoader;
            transformer.Parse(transformation);

            return transformer;
        }

        private static readonly string[] BuiltInFunctions = new string[] { "bagAdd", "call", "ifdef", "length", "linearize", "lower", "stringify", "upper", "getmatch", "set", "typeof", "foldl" };

        /// <summary>
        /// Parse transformation rules from SPARD
        /// </summary>
        /// <param name="transformation">Reader of transformation described in SPARD language</param>
        internal void Parse(TextReader transformation)
        {
            var functions = new List<Function>();
            var functionDefinitions = new List<Definition>();
            var setDefinitions = new List<Definition>();

            using (var builder = new ExpressionBuilder(transformation))
            {
                foreach (var expr in builder.Parse())
                {
                    if (expr is Function func)
                    {
                        functions.Add(func);
                        continue;
                    }

                    if (expr is Definition definition)
                    {
                        if (definition.Left is Set)
                            setDefinitions.Add(definition);
                        else if (definition.Left is StringValueMatch && (definition.Right is Function || definition.Right is ComplexValueMatch))
                            functionDefinitions.Add(definition);
                        else
                        {
                            var coords = builder.GetCoordinates(expr);
                            throw new ParseException(coords.Item1, coords.Item2, "Incorrect definitions");
                        }

                        continue;
                    }

                    if (expr is Instruction instr)
                    {
                        ProcessGlobalInstruction(instr);
                    }
                }

                this._functions = functions.ToArray();

                if (!this._functions.Any())
                    throw new ParseException("No transformation rules are set");

                var resultOfFunctionDefinitions = CreateFunctionDefinitions(functionDefinitions);

                foreach (var func in resultOfFunctionDefinitions)
                {
                    this._functionDefinitions.Add(Tuple.Create("", func.Key), func.Value.ToArray());
                }

                var resultOfSetDefinitions = CreateSetDefinitions(setDefinitions);

                foreach (var def in resultOfSetDefinitions)
                {
                    this.setDefinitions.Add(Tuple.Create("", def.Key.Item1, def.Key.Item2), def.Value);
                }

                if (!SuppressInline)
                {
                    InlineSets(builder);
                }

                // Check functions and sets existense
                foreach (var item in builder.FunctionCalls)
                {
                    var name = item.Value.Item2;
                    if (Array.IndexOf(BuiltInFunctions, name) > -1)
                        continue;

                    if (!this._functionDefinitions.ContainsKey(item.Value))
                    {
                        var coords = builder.GetCoordinates(item.Key);
                        throw new ParseException(coords.Item1, coords.Item2, string.Format("Function '{0}' definition not found", item.Key));
                    }
                }

                foreach (var item in builder.SetCalls)
                {
                    var name = item.Value.Item2;
                    if (name == "SP" || name == "BR" || name == "s" || name == "t" || name == "d" || name == "i" || name == "String" || name == "Text" || name == "Digit" || name == "Int")
                        continue;

                    if (!this.setDefinitions.ContainsKey(item.Value))
                    {
                        var coords = builder.GetCoordinates(item.Key);
                        throw new ParseException(coords.Item1, coords.Item2, string.Format("Set '{0}' definition not found", item.Key));
                    }
                }

                foreach (var recognizer in builder.TableRecognizers)
                {
                    recognizer.Build(this);
                }

                foreach (var item in optimizedFunctions.ToArray())
                {
                    optimizedFunctions[item.Key] = BuildOptimizedFunction(item.Key.Item1, item.Key.Item2);
                }

                foreach (var item in compiledFunctions)
                {
                    optimizedFunctions[item.Key] = BuildCompiledFunction(item.Key.Item1, item.Key.Item2);
                }
            }
        }

        /// <summary>
        /// Replaces sets with their definitions.
        /// </summary>
        /// <param name="builder">Expression builder.</param>
        private void InlineSets(ExpressionBuilder builder)
        {
            // Create set dependency matrix
            var setList = setDefinitions.Keys.ToList();
            var setCount = setDefinitions.Count;
            var setMatrix = new bool[setCount, setCount];

            for (int i = 0; i < setCount; i++)
            {
                var definitions = setDefinitions[setList[i]];

                // Collect dependencies from all definitions of current set
                var dependencies = new HashSet<Tuple<string, string, int>>();
                foreach (var item in definitions)
                {
                    if (builder.UsedSets.TryGetValue(item, out var sets))
                    {
                        foreach (var set in sets)
                        {
                            dependencies.Add(set);
                        }
                    }
                }

                foreach (var item in dependencies)
                {
                    var otherSetIndex = setList.IndexOf(item);
                    setMatrix[i, otherSetIndex] = true;
                }
            }

            // Calculate all the cycles in the dependency matrix (build the reachability matrix)
            // Floyd-Warshall Algorithm
            for (int k = 0; k < setCount; k++)
            {
                for (int i = 0; i < setCount; i++)
                {
                    for (int j = 0; j < setCount; j++)
                    {
                        setMatrix[i, j] |= setMatrix[i, k] && setMatrix[k, j];
                    }
                }
            }

            // Get indices of non-recursive sets
            var nonRecursiveSets = new List<int>();
            var dependenciesCount = new int[setCount];
            for (int i = 0; i < setCount; i++)
            {
                for (int j = 0; j < setCount; j++)
                {
                    if (setMatrix[i, j])
                        dependenciesCount[i]++;
                    else if (i == j)
                        nonRecursiveSets.Add(i);
                }
            }

            var correctOrder = nonRecursiveSets.OrderBy(index => dependenciesCount[index]).ToArray();

            foreach (var setIndex in correctOrder)
            {
                var key = setList[setIndex];

                // Inline calls to non-recursive sets by their definitions
                foreach (var item in builder.SetCallsTable)
                {
                    var parent = item.Item1;
                    var calledSet = (Set)item.Item2;
                    var index = item.Item3;

                    var setKey = builder.SetCalls[calledSet];
                    if (!setKey.Equals(key))
                        continue;

                    var setDefinition = setDefinitions[setKey];
                    ReplaceSetCallWithDefinition(parent, calledSet, setDefinition, index);
                }
            }
        }

        /// <summary>
        /// Replace the call of the set with its definition, having correctly registered the unifications and converters, while avoiding duplication of the names of variables
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="calledSet"></param>
        /// <param name="setDefinition"></param>
        /// <param name="index"></param>
        private void ReplaceSetCallWithDefinition(Expression parent, Set calledSet, Definition[] setDefinition, int index)
        {
            if (setDefinition.Any(def => def.Right is Translation)) // Skip such definitions
                return;

            var newExpr = setDefinition.Length == 1 ? InlineSetDefiniton(calledSet, setDefinition[0]) : new Or(setDefinition.Select(def => InlineSetDefiniton(calledSet, def)).ToArray());
            var operands = parent.Operands().ToArray();
            operands[index] = newExpr;
            parent.SetOperands(operands);
        }

        private static Expression InlineSetDefiniton(Set calledSet, Definition definition)
        {
            // Variables unification
            // TODO: Rename duplicate variables
            var expressions = new List<Expression>();
            
            var calledArgs = (TupleValueMatch)calledSet.Operand;
            var definedArgs = (TupleValueMatch)((Set)definition.Left).Operand;

            for (int i = 1; i < calledArgs._operands.Length; i++)
            {
                var first = calledArgs._operands[i];
                var second = definedArgs._operands[i];

                if (first is Query firstQuery && second is Query secondQuery && firstQuery.Name == secondQuery.Name)
                    continue; // It is the same variable, no unification is required

                var unification = new Unification(first, second);
                var inst = new Instruction(unification);
                expressions.Add(inst);
            }

            if (definition.Right is Sequence seq)
                expressions.AddRange(seq._operands);
            else if (!(definition.Right is Empty))
                expressions.Add(definition.Right);

            if (expressions.Count == 1)
                return expressions[0];

            return new Sequence(expressions.ToArray());
        }

        private ITransformFunction BuildCompiledFunction(string functionName, Directions direction)
        {
            var tableTransformer = BuildOptimizedFunction(functionName, direction);
            return tableTransformer.Compile();
        }

        /// <summary>
        /// Build optimized version of function
        /// </summary>
        /// <param name="functionName">Function name</param>
        /// <param name="direction">Function direction</param>
        /// <returns>Table converter equivalent to the original function</returns>
        private TableTransformer BuildOptimizedFunction(string functionName, Directions direction)
        {
            if (!_functionDefinitions.TryGetValue(Tuple.Create("", functionName), out Function[] functions))
                return null;

            var collection = TransitionTableResultCollection.Create(functions);
            var state = TransitionGraphBuilder.Create(collection, this, direction: direction);

            return new TableTransformer(state);
        }

        private Dictionary<Tuple<string, Directions>, ITransformFunction> optimizedFunctions = new Dictionary<Tuple<string, Directions>, ITransformFunction>();
        private readonly Dictionary<Tuple<string, Directions>, ITransformFunction> compiledFunctions = new Dictionary<Tuple<string, Directions>, ITransformFunction>();

        private void ProcessGlobalInstruction(Instruction instr)
        {
            if (instr.Operand is TypeDefinition def)
            {
                _typesTable[((Query)def.Left).Name] = def.Right;
                return;
            }

            if (instr.Operand is StringValueMatch str)
            {
                switch (str.Value)
                {
                    case "simplematch":
                        SimpleMatch = true;
                        break;

                    case "suppressinline":
                        SuppressInline = true;
                        break;
                }
            }

            if (!(instr.Operand is TupleValueMatch list) || list._operands.Length != 2 && list._operands.Length != 3)
                return;

            var name = list._operands[0].ToString();

            if (name == "optimize" || name == "compile")
            {
                // Optimizing function
                var functionName = list._operands[1].ToString();
                var isLeft = list._operands.Length > 2 && list._operands[2].ToString() == "left";

                if (name == "optimize")
                    optimizedFunctions[Tuple.Create(functionName, isLeft ? Directions.Left : Directions.Right)] = null;
                else
                {
                    // Compiling function
                    compiledFunctions[Tuple.Create(functionName, isLeft ? Directions.Left : Directions.Right)] = null;
                }
            }
            else if (name == "module")
            {
                if (LoadModule == null)
                    return;


                if (!(list._operands[1] is StringValueMatch moduleExtValue) || !(list._operands[2] is StringValueMatch moduleIntValue))
                    return;

                var moduleExtKey = moduleExtValue.Value;
                var moduleIntKey = moduleIntValue.Value;

                Instruction[] innerInstructions;

                var module = new TreeTransformer();
                using (var reader = LoadModule(moduleExtKey))
                {
                    innerInstructions = module.ParseModule(reader);
                }

                foreach (var item in module._functionDefinitions)
                {
                    _functionDefinitions.Add(Tuple.Create(moduleIntKey, item.Key.Item2), item.Value.ToArray());
                }

                foreach (var item in module.setDefinitions)
                {
                    setDefinitions.Add(Tuple.Create(moduleIntKey, item.Key.Item2, item.Key.Item3), item.Value);
                }

                foreach (var item in innerInstructions)
                {
                    ProcessGlobalInstruction(item);
                }
            }
        }

        private static Dictionary<Tuple<string, int>, Definition[]> CreateSetDefinitions(List<Definition> setDefinitions)
        {
            var resultOfSetDefinitions = new Dictionary<Tuple<string, int>, List<Definition>>();
            foreach (var definition in setDefinitions)
            {
                var set = (Set)definition.Left;
                var setName = set.Name;
                var numOfParams = set.List._operands.Length;
                var key = Tuple.Create(setName, numOfParams);

                if (!resultOfSetDefinitions.TryGetValue(key, out List<Definition> defs))
                {
                    defs = new List<Definition>();
                    resultOfSetDefinitions[key] = defs;
                }

                defs.Add(definition);
            }

            var result = new Dictionary<Tuple<string, int>, Definition[]>();
            foreach (var def in resultOfSetDefinitions)
            {
                result[def.Key] = def.Value.ToArray();
            }

            return result;
        }

        private static Dictionary<string, List<Function>> CreateFunctionDefinitions(List<Definition> functionDefinitions)
        {
            var resultOfFunctionDefinitions = new Dictionary<string, List<Function>>();
            foreach (var function in functionDefinitions)
            {
                var funcName = function.Left as StringValueMatch;
                var functionName = funcName.Value;

                if (BuiltInFunctions.Contains(functionName))
                    throw new Exception($"Function \"{functionName}\" cannot be defined because this name is reserved!");

                if (!resultOfFunctionDefinitions.TryGetValue(functionName, out List<Function> branches))
                {
                    branches = new List<Function>();
                    resultOfFunctionDefinitions[functionName] = branches;
                }

                if (function.Right is ComplexValueMatch complexValue)
                {
                    if (complexValue.Operand is Block block)
                    {
                        foreach (var subFunction in block._operands.OfType<Function>())
                        {
                            branches.Add(subFunction);
                        }
                    }

                    if (complexValue.Operand is TupleValueMatch tupleValue)
                    {
                        foreach (var subFunction in tupleValue._operands.OfType<Function>())
                        {
                            branches.Add(subFunction);
                        }
                    }
                }
                else
                {
                    branches.Add((Function)function.Right);
                }
            }

            return resultOfFunctionDefinitions;
        }

        private Instruction[] ParseModule(TextReader transformation)
        {
            var functionDefinitions = new List<Definition>();
            var setDefinitions = new List<Definition>();
            var instructions = new List<Instruction>();

            using (var builder = new ExpressionBuilder(transformation))
            {
                foreach (var expr in builder.Parse())
                {
                    if (expr is Definition def)
                    {
                        if (def.Left is Set)
                            setDefinitions.Add(def);
                        else if (def.Left is StringValueMatch && (def.Right is Function || def.Right is Block))
                            functionDefinitions.Add(def);

                        continue;
                    }

                    if (expr is Instruction instr)
                    {
                        instructions.Add(instr);
                    }
                }
            }

            _functions = Array.Empty<Function>();

            var resultOfFunctionDefinitions = CreateFunctionDefinitions(functionDefinitions);

            foreach (var func in resultOfFunctionDefinitions)
            {
                this._functionDefinitions.Add(Tuple.Create("", func.Key), func.Value.ToArray());
            }

            var resultOfSetDefinitions = CreateSetDefinitions(setDefinitions);

            foreach (var def in resultOfSetDefinitions)
            {
                this.setDefinitions.Add(Tuple.Create("", def.Key.Item1, def.Key.Item2), def.Value);
            }

            return instructions.ToArray();
        }

        #endregion

        #region Construct new transformer

        /// <summary>
        /// Creates a table transformer from expression trees that is equivalent to them
        /// </summary>
        /// <returns>Created transformer</returns>
        /// <remarks>The resulting transformer is faster than the source forest of expressions and is designed to handle a large data set</remarks>
        public TableTransformer BuildTableTransformer(CancellationToken cancellationToken = default)
        {
            // Let's add to the list of expressions an additional expression depending on the selected conversion mode
            IEnumerable<Expression> expressions;
            switch (_mode)
            {
                case TransformMode.Reading:
                    expressions = _functions.Concat(new Expression[] { new Function(Any.Instance, Empty.Instance) }); // . =>
                    break;

                case TransformMode.Modification:
                    expressions = _functions.Concat(new Expression[] {
                        new Function(new Query(new StringValueMatch("x")),
                        new Query(new StringValueMatch("x")))
                    }); // $x => $x
                    break;

                case TransformMode.Function:
                default: // Leave as is
                    expressions = _functions;
                    break;
            }

            var collection = TransitionTableResultCollection.Create(expressions);
            var state = TransitionGraphBuilder.Create(collection, this, false, Directions.Right, cancellationToken);

            return new TableTransformer(state);
        }

        /// <summary>
        /// Create reversed transformation
        /// </summary>
        /// <returns>The expression tree that performs the inverse transform</returns>
        public TreeTransformer Reverse()
        {
            var root = this.root == this ? null : this.root; // If it is a top-level transformer, then it should be the root for itself

            var transformer = new TreeTransformer(root) 
            {
                _functions = _functions,
                FunctionDefinitions = FunctionDefinitions,
                _mode = _mode,
                SetDefinitions = SetDefinitions,
                _direction = _direction == Directions.Left ? Directions.Right : Directions.Left,
                _typesTable = _typesTable,
                optimizedFunctions = optimizedFunctions
            };

            transformer.SearchBestVariant = SearchBestVariant;

            return transformer;
        }

        #endregion

        public override IEnumerable<object> Transform(IEnumerable input, CancellationToken cancellationToken = default)
        {
            var source = ValueConverter.ConvertToSource(input);

            var runtime = new RuntimeInfo(root, cancellationToken)
            {
                SearchBestVariant = SearchBestVariant
            };

            return TransformInternal(source, runtime);
        }

        public override IEnumerable<IEnumerable<object>> StepTransform(IEnumerable input, CancellationToken cancellationToken = default)
        {
            var source = ValueConverter.ConvertToSource(input);

            var runtime = new RuntimeInfo(root, cancellationToken)
            {
                SearchBestVariant = SearchBestVariant
            };

            return StepTransformInternal(source, runtime);
        }

        #region Transform

#if DEBUG
        public RuntimeInfo LastRuntime { get; private set; }
#endif

        private IEnumerable<IEnumerable<object>> TransformCore(ISource source, RuntimeInfo runtime)
        {
#if DEBUG
            LastRuntime = runtime;
#endif

            IEnumerable<object> result = null;

            while (!source.EndOfSource)
            {
                result = null;
                for (var i = 0; i < _functions.Length; i++)
                {
                    result = _functions[i].Transform(source, _direction, false, runtime);
                    if (result != null)
                    {
                        yield return result;

                        break;
                    }
                }

                if (result != null)
                {
                    OnProgressChanged(source.Position);
                    continue;
                }

                if (runtime.CancellationToken.IsCancellationRequested)
                {
                    throw new SpardCancelledException("Transformation was cancelled!");
                }

                switch (_mode)
                {
                    case TransformMode.Reading:
                        source.Read(); // Implemented as . =>
                        continue;

                    case TransformMode.Modification:
                        yield return ValueConverter.ConvertToEnumerable(source.Read()); // Implemented as $x => $x
                        break;

                    case TransformMode.Function:
                        throw new TransformException("Transformation error in function " + (_name ?? "_MAIN_!"))
                        {
                             TransformSource = source,
                             Runtime = runtime
                        };
                }
            }

            yield break;
        }

        private IEnumerable TransformEmpty(CancellationToken cancellationToken)
        {
            var source = new ObjectSource(Enumerable.Empty<object>());
            var runtime = new RuntimeInfo(root, cancellationToken)
            {
                SearchBestVariant = SearchBestVariant
            };

            for (var i = 0; i < _functions.Length; i++)
            {
                IEnumerable<object> result = _functions[i].Transform(source, _direction, false, runtime, true);
                if (result != null)
                {
                    foreach (var item in result)
                    {
                        yield return item;
                    }

                    yield break;
                }
            }
        }

        /// <summary>
        /// The main cycle of the transformer
        /// </summary>
        /// <returns>Sequential output of transformation results</returns>
        internal IEnumerable<object> TransformInternal(ISource source, RuntimeInfo runtime)
        {
            foreach (var result in TransformCore(source, runtime))
            {
                foreach (var item in result)
                {
                    yield return item;
                }
            }
        }

        internal IEnumerable<IEnumerable<object>> StepTransformInternal(ISource source, RuntimeInfo runtime)
        {
            foreach (var result in TransformCore(source, runtime))
            {
                yield return result;
            }
        }

        #endregion

        /// <summary>
        /// Merge with another transformer using another transformer as a data source
        /// </summary>
        /// <param name="transformer">Another transformer as a data source</param>
        /// <returns>Created chain of transformers as a new transformer</returns>
        public override Transformer ChainWith(Transformer transformer)
        {
            return new TransformersChain(transformer, this);
        }

        #region IExpressionRoot

        /// <summary>
        /// Get function by name
        /// </summary>
        /// <param name="module">Name of module containing the function</param>
        /// <param name="functionName">Function name</param>
        /// <param name="direction">Function direction</param>
        /// <returns>Transformation which the function describes</returns>
        ITransformFunction IExpressionRoot.GetFunction(string module, string functionName, Directions direction)
        {
            if (optimizedFunctions.TryGetValue(Tuple.Create(functionName, direction), out ITransformFunction transformFunction))
            {
                return transformFunction;
            }

            if (!_functionDefinitions.TryGetValue(Tuple.Create(module, functionName), out Function[] result))
            {
                // Look for the function definition in other modules
                if (module.Length == 0)
                {
                    result = _functionDefinitions.FirstOrDefault(k => k.Key.Item1.Length > 0 && k.Key.Item2 == functionName).Value;

                    if (result == null)
                    {
                        // Look among the user (external) functions
                        if (UserFunctions.TryGetValue(functionName, out TransformerHelper.UserFunc func))
                            return new ExternalFunction(func);
                    }
                }

                if (result == null)
                    return null;
            }

            var transformer = new TreeTransformer(this) { _functions = result.Select(f => (Function)f.CloneExpression()).ToArray(), _mode = TransformMode.Function, _name = functionName };

            if (direction == Directions.Left)
                transformer = transformer.Reverse();

            return transformer;
        }

        /// <summary>
        /// Get set definition
        /// </summary>
        /// <param name="setName">Set name</param>
        /// <param name="numOfParams">Number of set arguments</param>
        /// <returns>Collection of set definitions with given name and number of arguments</returns>
        Definition[] IExpressionRoot.GetSet(string module, string setName, int numOfParams)
        {
            if (!setDefinitions.TryGetValue(Tuple.Create(module, setName, numOfParams), out var result))
            {
                // Look for the set definition in other modules.
                if (!string.IsNullOrEmpty(module))
                    result = setDefinitions.FirstOrDefault(k => k.Key.Item1.Length > 0 && k.Key.Item2 == setName && k.Key.Item3 == numOfParams).Value;

                if (result == null)
                    return null;
            }

            return result.Select(s => (Definition)s.CloneExpression()).ToArray();
        }

        #endregion

        #region Structure

        /// <summary>
        /// Execute in all expression nodes
        /// </summary>
        private void CallEverywhere(Action<Expression> action)
        {
            foreach (var expr in _functions)
            {
                CallEverywhereInTree(expr, action);
            }

            foreach (var func in _functionDefinitions.Values)
            {
                foreach (var expr in func)
                {
                    CallEverywhereInTree(expr, action);
                }
            }

            foreach (var def in setDefinitions.Values)
            {
                foreach (var expr in def)
                {
                    CallEverywhereInTree(expr, action);
                }
            }
        }

        private static void CallEverywhereInTree(Expression expr, Action<Expression> action)
        {
            Debug.Assert(expr != null);
            foreach (var descedant in GetNodes(expr))
            {
                action(descedant);
            }
        }

        /// <summary>
        /// Get all nodes in expression
        /// </summary>
        /// <param name="expr">Expression</param>
        /// <returns>Enumeration of all expression nodes</returns>
        private static IEnumerable<Expression> GetNodes(Expression expr)
        {
            yield return expr;
            foreach (var child in expr.Operands())
            {
                foreach (var item in GetNodes(child))
                {
                    yield return item;
                }
            }
        }

        /// <summary>
        /// Add set definitions
        /// </summary>
        /// <param name="def">New set definitions</param>
        public void AddSetDefinition(Definition def)
        {
            var set = (Set)def.Left;
            var key = Tuple.Create("", set.Name, set.List._operands.Length);
            var has = setDefinitions.ContainsKey(key);

            if (setDefinitions.TryGetValue(key, out Definition[] existing))
            {
                var newDef = new Definition[existing.Length + 1];
                Array.Copy(existing, newDef, existing.Length);
                newDef[existing.Length] = def;

                setDefinitions[key] = newDef;

                CallEverywhere(RefreshDefinitions);
            }
            else
            {
                setDefinitions[key] = new Definition[] { def };
            }
        }

        /// <summary>
        /// Replaces set definition.
        /// </summary>
        /// <param name="def">New set definition that replaces existing.</param>
        public void ReplaceSetDefinition(Definition def)
        {
            var set = (Set)def.Left;
            var key = Tuple.Create("", set.Name, set.List._operands.Length);
            var has = setDefinitions.ContainsKey(key);

            setDefinitions[key] = new Definition[] { def };

            if (has)
            {
                CallEverywhere(RefreshDefinitions);
            }
        }

        private void RefreshDefinitions(Expression expr)
        {
            if (expr is Set set)
            {
                set.Refresh();
            }
            else if(expr is FunctionCall call)
            {
                call.Refresh();
            }
        }

        #endregion

        IEnumerable ITransformFunction.TransformCoreAll(object[] args, CancellationToken cancellationToken)
        {
            IEnumerable result;

            if (args.Length == 0)
            {
                result = TransformEmpty(cancellationToken);
            }
            else if (args.Length == 1)
            {
                if (args[0] is IEnumerable enumerable)
                {
                    var casted = enumerable;
                    result = casted.Cast<object>().Any() ? Transform(casted) : TransformEmpty(cancellationToken);
                }
                else
                {
                    result = Transform(new object[] { args[0] });
                }
            }
            else
            {
                var source = new TupleSource
                {
                    Sources = args.Select(item => ValueConverter.ConvertToSource(ValueConverter.ConvertToEnumerable(item))).ToArray()
                };

                var runtime = new RuntimeInfo(root, cancellationToken)
                {
                    SearchBestVariant = SearchBestVariant
                };

                result = TransformInternal(source, runtime);
            }

            return new BufferedEnumerable(result);
        }

        private Dictionary<string, Expression> _typesTable = new Dictionary<string, Expression>();

        /// <summary>
        /// Gets variable type as expression.
        /// </summary>
        /// <param name="name">Variable name.</param>
        /// <returns>Expression describing variable type.</returns>
        public Expression GetVariableType(string name) =>
            _typesTable.TryGetValue(name, out Expression result) ? result : Any.Instance;
    }
}
