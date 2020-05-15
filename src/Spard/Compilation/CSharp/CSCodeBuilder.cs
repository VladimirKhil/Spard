using Spard.Data;
using Spard.Expressions;
using Spard.Transitions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Spard.Compilation.CSharp
{
    /// <summary>
    /// C# source code generator that performs equivalent transformation
    /// </summary>
    internal sealed class CSCodeBuilder
    {
        private readonly TransitionStateBase _initialState;

        private bool _useContext = false;
        private bool _useResults = false;
        private bool _useAppendDepth = false;
        private bool _useCopyDepth = false;
        private bool _moreThanOneNormalState = false;
        private bool _useError = false;

        private bool _switchMode = false;

        private readonly bool _charInput = false;
        private readonly bool _charOutput = false;

        private readonly HashSet<string> _vars = new HashSet<string>();

        private CSCodeBuilder(TransitionStateBase initialState)
        {
            _initialState = initialState;
        }

        public static void CreateTransformerCode(TransitionStateBase initialState, IndentWriter writer)
        {
            new CSCodeBuilder(initialState).CreateTransformerCode(writer);
        }

        public static void CreateRecognizerCode(TransitionStateBase initialState, IndentWriter writer)
        {
            new CSCodeBuilder(initialState).CreateRecognizerCode(writer);
        }

        private void CreateTransformerCode(IndentWriter writer)
        {
            var list = LoadStatesList();

            _switchMode = list.Count < 1000;

            WriteTransformerHeader(writer);

            if (_switchMode)
            {
                writer.WriteLine("foreach ({0} item in input)", _charInput ? "char": "var");
                writer.WriteLine("{");

                if (_moreThanOneNormalState)
                {
                    writer.WriteLine("this.beforeStart = false;");
                }

                GoToNextState(writer, "item", list);
                ProcessStateMove(writer);
                writer.WriteLine("}");
                
                if (_moreThanOneNormalState)
                {
                    writer.WriteLine();
                    writer.WriteLine("if (this.beforeStart)");
                    writer.WriteLine("    yield break;");
                    writer.WriteLine();

                    GoToNextState(writer, "\'\\0\'", list);
                }

                ProcessStateMove(writer);
            }
            else
            {
                writer.WriteLine("IEnumerable<char> result;");
                writer.WriteLine("var source = input.GetEnumerator();");
                writer.WriteLine("var isNotFinal = false;");
                writer.WriteLine("do");
                writer.WriteLine("{");

                writer.WriteLine("isNotFinal = source.MoveNext();");

                writer.WriteLine("if (isNotFinal)");
                writer.WriteLine("{");

                if (_moreThanOneNormalState)
                {
                    writer.WriteLine("this.beforeStart = false;");
                }

                GoToNextState(writer, "source.Current", list);

                writer.WriteLine("}");

                writer.WriteLine("else");
                writer.WriteLine("{");

                if (_moreThanOneNormalState)
                {
                    writer.WriteLine("if (this.beforeStart)");
                    writer.WriteLine("    yield break;");
                    writer.WriteLine();

                    GoToNextState(writer, "\'\\0\'", list);
                }
                else
                {
                    writer.WriteLine("yield break;");
                }

                writer.WriteLine("}");
                writer.WriteLine();

                ProcessStateMove(writer);
                writer.WriteLine();

                writer.Indent--;
                writer.WriteLine("} while (isNotFinal);");
            }

            writer.WriteLine("}");

            WriteOtherFunctions(writer, list);

            writer.WriteLine("}");
        }

        private void CreateRecognizerCode(IndentWriter writer)
        {
            LoadStatesList();

            writer.Write(@"public sealed class CompiledRecognizer: Primitive
    {
        private Expression origin;
        private TransitionStateBase initialState;

        private int initStart, currentPosition;
        private TransitionStateBase currentState;
        private TransitionContext context;

        private IContext initContext;

        private bool isFinished = false;
        private List<CachedResult> cachedResults = new List<CachedResult>();
        private int cacheIndex = -1;

        /// <summary>
        /// Saved state of successfull parsing
        /// </summary>
        private sealed class CachedResult
        {
            /// <summary>
            /// Position in input
            /// </summary>
            public int InputPosition { get; set; }
            /// <summary>
            /// Variables length
            /// </summary>
            public Dictionary<string, int> VariablesLength { get; set; } = new Dictionary<string, int>();
        }

        protected internal override string Sign
        {
            get { return ""; }
        }

        internal CompiledRecognizer(Expression origin)
        {
            this.origin = origin;
        }

        internal void Build(IExpressionRoot root)
        {
            var collection = TransitionTableResultCollection.Create(this.origin);
            this.initialState = TransitionGraphBuilder.Create(collection, root, true);
        }

        internal override bool MatchCore(ISource input, ref IContext context, bool next)
        {
            if (next && this.isFinished)
            {
                if (this.cacheIndex > -1)
                {
                    SetResult(input, ref context);
                    return true;
                }

                return false;
            }

            if (!next)
            {
                this.initStart = this.currentPosition = input.Position;
                this.initContext = context;

                this.currentState = this.initialState;
                this.context = new TransitionContext();

                this.isFinished = false;
                this.cachedResults.Clear();
                this.cacheIndex = -1;

                // There may be an intermediate result for the initial state
                var resultIndex = this.currentState.IntermediateResultIndex;
                if (resultIndex == -1)
                {
                    this.currentPosition = input.Position;
                    return true;
                }
                else if (resultIndex > -1)
                {
                    // Saving result
                    var cachedResult = new CachedResult { InputPosition = input.Position };

                    foreach (var item in this.context.Vars)
                    {
                        cachedResult.VariablesLength[item.Key] = item.Value.Count;
                    }

                    this.cachedResults.Insert(resultIndex, cachedResult);
                }
            }
            else
                input.Position = this.currentPosition;

            if (input.EndOfSource)
                return false;

            IEnumerable result;

            do
            {
                var c = input.Read();");
            writer.Write(@"
                var newState = this.currentState.Move(c, ref this.context, out result);

                if (newState == null)
                {
                    this.isFinished = true;
                    if (this.cachedResults.Count > 0)
                    {
                        this.cacheIndex = this.cachedResults.Count - 1;
                        SetResult(input, ref context);
                        return true; // Some fragment is recognized - this is what we needed
                    }

                    input.Position = this.initStart;
                    return false;
                }

                this.currentState = newState;

                var resultIndex = this.currentState.IntermediateResultIndex + this.context.ResultIndexIncrease;
                if (resultIndex == -1)
                {
                    this.currentPosition = input.Position;

                    context = this.initContext.Clone();

                    foreach (var item in this.context.Vars)
                    {
                        context.Vars[item.Key] = item.Value;
                    }

                    return true;
                }
                else if (resultIndex > -1)
                {
                    // Saving result
                    var cachedResult = new CachedResult { InputPosition = input.Position };

                    foreach (var item in this.context.Vars)
                    {
                        cachedResult.VariablesLength[item.Key] = item.Value.Count;
                    }

                    this.cachedResults.Insert(resultIndex, cachedResult);
                }
                
            } while (!input.EndOfSource);

            this.isFinished = true;
            if (this.cachedResults.Count > 0)
            {
                this.cacheIndex = this.cachedResults.Count - 1;
                SetResult(input, ref context);
                return true; // Some fragment is recognized - this is what we needed
            }

            input.Position = this.initStart;
            return false;
        }

        private void SetResult(ISource input, ref IContext context)
        {
            var cachedResult = this.cachedResults[this.cacheIndex--];
            input.Position = cachedResult.InputPosition;

            context = this.initContext.Clone();

            foreach (var item in cachedResult.VariablesLength)
            {
                context.SetValue(item.Key, this.context.Vars[item.Key].Take(item.Value));
            }
        }

        internal override object Apply(IContext context)
        {
            return this.origin.Apply(context);
        }

        public override Expression CloneCore()
        {
            return new CompiledRecognizer(this.origin) { initialState = this.initialState };
        }

        public override string ToString()
        {
            return this.origin.ToString();
        }
    }");
        }

        private void ProcessStateMove(IndentWriter writer)
        {
            if (_switchMode)
            {
                if (!_moreThanOneNormalState)
                    return;

                writer.WriteLine();
                writer.WriteLine("if (this.state == -1)");
            }
            else
            {
                writer.WriteLine();
                writer.WriteLine("if (this.state == null)");
            }

            writer.WriteLine("{");

            if (_useResults)
            {
                writer.WriteLine("foreach (var r in this.results)");
                writer.WriteLine("{");

                writer.WriteLine("foreach (var rItem in r.Data)");
                writer.WriteLine("{");

                writer.WriteLine("yield return rItem;");

                writer.WriteLine("}");
                writer.WriteLine("}");

                writer.WriteLine();
            }

            if (_useError)
                writer.WriteLine("throw new Exception();");
            else
                writer.WriteLine("yield break;");

            writer.WriteLine("}");
            writer.WriteLine();

            if (!_switchMode)
            {
                writer.WriteLine("if (result != null)");
                writer.WriteLine("{");
                writer.WriteLine("foreach (var res in result)");
                writer.WriteLine("   yield return res;");
                writer.WriteLine("}");
            }
        }

        private void WriteOtherFunctions(IndentWriter writer, List<TransitionStateBase> list)
        {
            if (_switchMode)
            {
                if (_useContext || _moreThanOneNormalState)
                {
                    writer.WriteLine();
                    writer.WriteLine("private void Reset()");
                    writer.WriteLine("{");

                    writer.WriteLine("this.state = 0;");
                    if (_useContext)
                    {
                        if (_useResults)
                        {
                            writer.WriteLine("this.vars.Clear();");
                            writer.WriteLine("this.results.Clear();");
                        }
                        else
                        {
                            foreach (var item in _vars)
                            {
                                writer.WriteLine("this.{1} = new {0}();", _charInput ? "StringBuilder" : "List<object>", item);
                            }
                        }
                    }

                    if (_moreThanOneNormalState)
                    {
                        writer.WriteLine("this.beforeStart = true;");
                    }

                    writer.WriteLine("}");
                }
            }
            else
            {
                for (int i = 0; i < list.Count; i++)
                {
#if DEBUG                    
                    Debug.WriteLine(i);
#endif

                    var stateBase = list[i];

                    if (stateBase.IsFinal || stateBase is BadTransitionState)
                        continue;

                    writer.WriteLine();
                    writer.WriteLine("private State F{0}(char item, out IEnumerable<char> result)", i);
                    writer.WriteLine("{");

                    var putBreak = WriteState(writer, list, i, stateBase);

                    writer.WriteLine("}");
                }

                writer.WriteLine();
                writer.WriteLine("private State Reset()");
                writer.WriteLine("{");

                writer.WriteLine("this.state = F0;");

                if (_useContext)
                {
                    if (_useResults)
                    {
                        writer.WriteLine("this.vars.Clear();");
                        writer.WriteLine("this.results.Clear();");
                    }
                    else
                    {
                        foreach (var item in _vars)
                        {
                            writer.WriteLine("this.{1} = new {0}();", _charInput ? "StringBuilder" : "List<object>", item);
                        }
                    }
                }

                if (_moreThanOneNormalState)
                {
                    writer.WriteLine("this.beforeStart = true;");
                }

                writer.WriteLine("return F0;");
                writer.WriteLine("}");
            }

            if (_useContext)
            {
                var outputType = _charOutput ? "char" : "object";
                var simpleType = _charOutput ? "<char>" : "";

                if (_useResults)
                {
                    writer.WriteLine();
                    writer.WriteLine("private void InsertResult(int remove, Func<Context, IEnumerable{0}> result)", simpleType);
                    writer.WriteLine("{");

                    writer.WriteLine("if (remove == -1)");
                    writer.Indent++;
                    writer.WriteLine("this.results.Clear();");
                    writer.Indent--;

                    writer.WriteLine("else");
                    writer.Indent++;
                    writer.WriteLine("this.results.RemoveRange(results.Count - remove, remove);");
                    writer.Indent--;

                    writer.WriteLine();

                    writer.WriteLine("var context = this.results.Count == 0 ? this.vars : this.results[this.results.Count - 1].Vars;");
                    writer.WriteLine("this.results.Add(new Result(result(context)));");

                    writer.WriteLine("}");

                    writer.WriteLine();
                    writer.WriteLine("private void InsertResult(int remove, IEnumerable{0} result = null)", simpleType);
                    writer.WriteLine("{");

                    writer.WriteLine("if (remove == -1)");
                    writer.Indent++;
                    writer.WriteLine("this.results.Clear();");
                    writer.Indent--;

                    writer.WriteLine("else");
                    writer.Indent++;
                    writer.WriteLine("this.results.RemoveRange(results.Count - remove, remove);");
                    writer.Indent--;

                    writer.WriteLine();

                    writer.WriteLine("if (result != null)");
                    writer.WriteLine("{");
                    writer.WriteLine("this.results.Add(new Result(result));");
                    writer.WriteLine("}");

                    writer.WriteLine("}");

                    writer.WriteLine();
                    writer.WriteLine("private void InsertResult(Func<Context, IEnumerable{0}> result)", simpleType);
                    writer.WriteLine("{");

                    writer.WriteLine("var context = this.results.Count == 0 ? this.vars : this.results[this.results.Count - 1].Vars;");
                    writer.WriteLine("this.results.Add(new Result(result(context)));");

                    writer.WriteLine("}");

                    writer.WriteLine();
                    writer.WriteLine("private void InsertResult(IEnumerable{0} result)", simpleType);
                    writer.WriteLine("{");

                    writer.WriteLine("this.results.Add(new Result(result));");

                    writer.WriteLine("}");

                    writer.WriteLine();
                    writer.WriteLine("private void InsertResult()");
                    writer.WriteLine("{");

                    writer.WriteLine("this.results.Add(new Result(\"\"));");

                    writer.WriteLine("}");

                    writer.WriteLine();
                    writer.WriteLine("private IEnumerable<{0}> ReturnResult(int left = 0)", outputType);
                    writer.WriteLine("{");

                    writer.WriteLine("var count = this.results.Count;");
                    writer.WriteLine("var take = count - left;");
                    writer.WriteLine("var res = this.results.Take(take).ToArray().SelectMany<Result, {0}>(r => r.Data{1});", outputType, _charOutput ? "" : ".Cast<object>()");

                    writer.WriteLine();

                    writer.WriteLine("if (take > 0)");
                    writer.WriteLine("{");
                    writer.WriteLine("this.vars = this.results[take - 1].Vars;");
                    writer.WriteLine("this.results.RemoveRange(0, take);");
                    writer.WriteLine("}");

                    writer.WriteLine("return res;");

                    writer.WriteLine("}");

                    writer.WriteLine();
                    writer.WriteLine("private void AppendVar(string name, {0} item{1})", outputType, _useAppendDepth ? ", int depth = 0" : "");
                    writer.WriteLine("{");

                    writer.WriteLine("var contextIndex = this.results.Count{0};", _useAppendDepth ? " - depth" : "");
                    writer.WriteLine("var context = contextIndex == 0 ? this.vars : this.results[contextIndex - 1].Vars;");

                    writer.WriteLine();

                    var collector = _charOutput ? "StringBuilder" : "List<object>";

                    writer.WriteLine("{0} var;", collector);
                    writer.WriteLine("if (context.TryGetValue(name, out var))");
                    writer.Indent++;
                    writer.WriteLine("var.{0}(item);", _charOutput ? "Append" : "Add");
                    writer.Indent--;
                    writer.WriteLine("else");
                    writer.Indent++;
                    writer.WriteLine("context[name] = new {0};", _charOutput ? "StringBuilder(item.ToString())" : "List<object>(new object[] { item })");
                    writer.Indent--;

                    writer.WriteLine("}");

                    writer.WriteLine();
                    writer.WriteLine("private void CopyVar(string source, string target{0})", _useCopyDepth ? ", int depth = 0" : "");
                    writer.WriteLine("{");

                    writer.WriteLine("var contextIndex = this.results.Count{0};", _useCopyDepth ? " - depth" : "");
                    writer.WriteLine("var context = contextIndex == 0 ? this.vars : this.results[contextIndex - 1].Vars;");

                    writer.WriteLine();

                    writer.WriteLine("{0} var;", collector);
                    writer.WriteLine("if (context.TryGetValue(source, out var))");
                    writer.Indent++;
                    writer.WriteLine("context[target] = new {0};", _charOutput ? "StringBuilder(var.ToString())" : "List<object>(var)");
                    writer.Indent--;

                    writer.WriteLine("}");

                    writer.WriteLine();
                    writer.WriteLine("private void RenameVar(string source, string target)");
                    writer.WriteLine("{");

                    writer.WriteLine("Rename(this.vars, source, target);");
                    writer.WriteLine("foreach (var result in this.results)");

                    writer.WriteLine("{");
                    writer.WriteLine("Rename(result.Vars, source, target);");
                    writer.WriteLine("}");

                    writer.WriteLine("}");

                    writer.WriteLine();
                    writer.WriteLine("private void Rename(Context context, string source, string target)");
                    writer.WriteLine("{");

                    writer.WriteLine("{0} var;", collector);
                    writer.WriteLine("if (context.TryGetValue(source, out var))");

                    writer.WriteLine("{");
                    writer.WriteLine("context[target] = var;");
                    writer.WriteLine("context.Remove(source);");
                    writer.WriteLine("}");

                    writer.WriteLine("}");
                }
            }
        }

        private void GoToNextState(IndentWriter writer, string item, List<TransitionStateBase> list)
        {
            if (_switchMode)
            {
                var final = item != "source.Current" && item != "item";

                if (!final && item == "source.Current")
                {
                    writer.WriteLine("var item = source.Current;");
                }

                if (_moreThanOneNormalState)
                {
                    writer.WriteLine("switch (this.state)");
                    writer.WriteLine("{");
                }

                for (int i = 0; i < list.Count; i++)
                {
                    var stateBase = list[i];

                    if (stateBase.IsFinal || stateBase is BadTransitionState)
                        continue;

                    if (final)
                    {
                        WriteStateEOF(writer, list, i, stateBase);
                    }
                    else
                    {
                        if (_moreThanOneNormalState)
                        {
                            writer.WriteLine("case {0}:", i);
                            writer.WriteLine("{");
                        }

                        var putBreak = WriteState(writer, list, i, stateBase);

                        if (_moreThanOneNormalState)
                        {
                            if (putBreak)
                                writer.WriteLine("break;");

                            writer.WriteLine("}");
                        }
                    }
                }

                if (_moreThanOneNormalState)
                {
                    if (final)
                    {
                        writer.WriteLine("default:");
                        writer.WriteLine("    this.state = -1;");
                        writer.WriteLine("    break;");
                    }

                    writer.WriteLine("}");
                }
            }
            else
            {
                writer.WriteLine("state = state({0}, out result);", item);
            }
        }

        private void WriteStateEOF(IndentWriter writer, List<TransitionStateBase> list, int i, TransitionStateBase stateBase)
        {
            if (i == 0)
                return; // Let it be for now

            var normalState = (TransitionState)stateBase;
            if (!normalState.table.TryGetValue(InputSet.EndOfSource, out TransitionLink link))
            {
                foreach (var pair in normalState.secondTable)
                {
                    if (!pair.Item1.Intersect(InputSet.IncludeEOS).IsEmpty)
                    {
                        link = pair.Item2;
                        break;
                    }
                }

                if (link == null)
                    return;
            }

            var linkCode = WriteLink(list, i, link);
            if (linkCode.Count > 0)
            {
                Statement statement = linkCode;

                if (_moreThanOneNormalState)
                {
                    linkCode.Add("break;");
                    statement = new ComplexStatement(string.Format("case {0}:", i), linkCode);
                }

                writer.WriteLine(statement.ToString(writer.Indent), false);
            }
        }

        private bool WriteState(IndentWriter writer, List<TransitionStateBase> list, int i, TransitionStateBase stateBase)
        {
            var normalState = (TransitionState)stateBase;
            var putElse = false;

            // All other symbols
            var exceptKey = InputSet.ExcludeEOS;
            var defaultSection = false;

            if (!_switchMode)
            {
                writer.WriteLine("result = null;");
            }

            var allExceptEOF = normalState.table.Where(pair => !object.Equals(pair.Key, InputSet.EndOfSource)).ToArray();
            if (allExceptEOF.Length > 0)
            {
                if (allExceptEOF.Length > 1)
                {
                    writer.WriteLine("switch (item)");
                    writer.WriteLine("{");

                    var first = true;

                    foreach (var item in allExceptEOF)
                    {
                        if (!first)
                            writer.WriteLine();

                        var linkCode = WriteLink(list, i, item.Value);
                        linkCode.Add("break;");
                        var statement = new ComplexStatement(string.Format("case '{0}':", item.Key), linkCode);
                        writer.WriteLine(statement.ToString(writer.Indent), false);

                        exceptKey = exceptKey.Except(new InputSet(InputSetType.Include, item.Key));

                        first = false;
                    }

                    if (!exceptKey.IsEmpty)
                    {
                        defaultSection = true;

                        writer.WriteLine();
                        writer.WriteLine("default:");

                        if (_switchMode)
                        {
                            writer.WriteLine("{");
                        }
                        else
                        {
                            writer.Indent++;
                        }
                    }
                }
                else
                {
                    var singleItem = allExceptEOF.First();
                    var linkCode = WriteLink(list, i, singleItem.Value);

                    var conditionTextFormat = _charInput ? "item == '{0}'" : "object.Equals(item, '{0}')";
                    var conditionText = string.Format(conditionTextFormat, singleItem.Key);

                    var condition = new ComplexStatement(string.Format("if ({0})", conditionText), linkCode);
                    writer.WriteLine(condition.ToString(writer.Indent), false);

                    exceptKey = exceptKey.Except(new InputSet(InputSetType.Include, singleItem.Key));
                    putElse = true;
                }
            }

            if (!_switchMode && normalState.table.ContainsKey(InputSet.EndOfSource))
            {
                var linkCode = WriteLink(list, i, normalState.table[InputSet.EndOfSource]);
                if (linkCode.Count > 0)
                {
                    string mainText = "";
                    if (object.Equals(exceptKey, InputSet.IncludeEOS))
                    {
                        if (allExceptEOF.Length == 1)
                            mainText = "else";
                    }
                    else
                    {
                        mainText = string.Format("{0}if (item == '\\0')", putElse ? "else " : "");
                    }

                    exceptKey = exceptKey.Except(InputSet.IncludeEOS);

                    var cond = new ComplexStatement(mainText, linkCode);
                    writer.WriteLine(cond.ToString(writer.Indent), false);

                    putElse = true;
                }
            }

            foreach (var item in normalState.secondTable)
            {
                var usedKey = _switchMode ? item.Item1.Except(InputSet.IncludeEOS) : item.Item1;
                exceptKey = exceptKey.Except(item.Item1);

                var linkCode = WriteLink(list, i, item.Item2);

                if (linkCode.Count > 0)
                {
                    string mainText = "";
                    if (exceptKey.IsEmpty)
                    {
                        if (allExceptEOF.Length == 1)
                            mainText = "else";
                    }
                    else
                    {
                        mainText = string.Format("{0}if ({1})", putElse ? "else " : "", CreateCondition(usedKey));
                    }

                    var cond = new ComplexStatement(mainText, linkCode);
                    writer.WriteLine(cond.ToString(writer.Indent), false);

                    putElse = true;
                }
            }

            var putBreak = _switchMode;
            if (!exceptKey.IsEmpty)
            {
                if (_switchMode)
                {
                    if (putElse)
                    {
                        writer.WriteLine("else");
                        writer.WriteLine("{");
                    }
                    //else
                    //    putBreak = false;

                    if (_moreThanOneNormalState)
                    {
                        writer.WriteLine("this.state = -1;");
                    }
                    else
                    {
                        writer.WriteLine("throw new Exception();");
                        putBreak = false;
                    }

                    if (putElse)
                    {
                        writer.WriteLine("}");
                    }
                    //else
                    //{
                    //    if (putBreak)
                    //        writer.WriteLine("break;");
                    //}
                }
                else
                {
                    writer.WriteLine("return null;");
                }
            }

            if (allExceptEOF.Length > 1) // we need to close `switch`
            {
                if (defaultSection) // closing `default`
                {
                    if (putBreak)
                        writer.WriteLine("break;");
                    writer.WriteLine("}");
                }

                if (_switchMode)
                {
                    writer.WriteLine("}");
                    putBreak = true;
                }
                else
                {
                    writer.Indent--;
                }
            }

            return putBreak;
        }

        private void WriteTransformerHeader(IndentWriter writer)
        {
            writer.WriteLine("public sealed class CompiledTransformerImpl: CompiledTransformer");
            writer.WriteLine("{");

            var inputType = _charInput ? "char" : "object";
            var outputType = _charOutput ? "<char>" : "";
            var collector = _charInput ? "StringBuilder" : "List<object>";

            if (_switchMode)
            {
                writer.WriteLine("private int state;");
            }
            else
            {
                writer.WriteLine("private State state;");
            }

            if (_useContext)
            {
                if (_useResults)
                {
                    writer.WriteLine("private sealed class Context: Dictionary<string, {0}> {{ }}", collector);

                    writer.WriteLine();
                    writer.WriteLine("private sealed class Result");
                    writer.WriteLine("{");

                    writer.WriteLine("internal IEnumerable{0} Data {{ get; set; }}", outputType);
                    writer.WriteLine("internal Context Vars { get; set; }");

                    writer.WriteLine();

                    writer.WriteLine("internal Result(IEnumerable{0} data)", outputType);
                    writer.WriteLine("{");

                    writer.WriteLine("this.Data = data;");
                    writer.WriteLine("this.Vars = new Context();");

                    writer.WriteLine("}");

                    writer.WriteLine("}");

                    writer.WriteLine();

                    writer.WriteLine("private Context vars = new Context();");
                    writer.WriteLine("private List<Result> results = new List<Result>();");
                }
                else
                {
                    foreach (var item in _vars)
                    {
                        writer.WriteLine("private {0} {1};", collector, item);
                    }
                }
            }

            if (_moreThanOneNormalState)
            {
                writer.WriteLine("private bool beforeStart;");
            }

            if (_useContext || _moreThanOneNormalState)
            {
                writer.WriteLine();
            }

            if (!_switchMode)
            {
                writer.WriteLine("private delegate State State({0} item, out IEnumerable{1} result);", inputType, outputType);
                writer.WriteLine();
            }

            writer.WriteLine("public override IEnumerable<object> Transform(IEnumerable input, CancellationToken cancellationToken = default(CancellationToken))");
            writer.WriteLine("{");

            if (_useContext)
            {
                writer.WriteLine("Reset();");
                //if (this.useResults)
                //{
                //    writer.WriteLine("this.results.Clear();");
                //    writer.WriteLine("this.vars = new Context();");
                //}
                //else
                //{
                //    foreach (var item in this.vars)
                //    {
                //        writer.WriteLine("this.{1} = new {0}();", collector, item);
                //    }
                //}
            }
        }

        internal static void WriteUsings(IndentWriter writer)
        {
            writer.WriteLine("using System;");
            writer.WriteLine("using System.Text;");
            writer.WriteLine("using System.Linq;");
            writer.WriteLine("using System.Collections.Generic;");

            writer.WriteLine();
        }

        /// <summary>
        /// First we’ll do the analysis and find out what we will need to output
        /// </summary>
        private List<TransitionStateBase> LoadStatesList()
        {
            var active = new Queue<TransitionStateBase>();
            active.Enqueue(_initialState);

            var list = new List<TransitionStateBase>
            {
                _initialState
            };

            // First, fill out the list of all states (it’s more convenient find them by the index, plus calculate some features of the graph)
            while (active.Any())
            {
#if DEBUG
                Debug.WriteLine(list.Count);
#endif
                var index = list.Count - 1;
                var stateBase = active.Dequeue();

                var exceptKey = new InputSet(InputSetType.Exclude);

                var normalState = (TransitionState)stateBase;
                foreach (var item in normalState.table)
                {
                    if (!IsLinkSimple(index, item.Value))
                    {
                        AnalyzeActions(item.Value.Actions);
                    }

                    var newState = item.Value.State;
                    if (!newState.IsFinal && !list.Contains(newState))
                    {
                        list.Add(newState);
                        active.Enqueue(newState);

                        if (!(newState is BadTransitionState))
                            _moreThanOneNormalState = true;
                    }

                    exceptKey = exceptKey.Except(new InputSet(InputSetType.Include, item.Key));
                }

                foreach (var item in normalState.secondTable)
                {
                    if (!IsLinkSimple(index, item.Item2))
                    {
                        AnalyzeActions(item.Item2.Actions);
                    }

                    var newState = item.Item2.State;
                    if (!newState.IsFinal && !list.Contains(newState))
                    {
                        list.Add(newState);
                        active.Enqueue(newState);

                        if (!(newState is BadTransitionState))
                            _moreThanOneNormalState = true;
                    }

                    exceptKey = exceptKey.Except(item.Item1);
                }

                if (_moreThanOneNormalState && !exceptKey.IsEmpty || !_moreThanOneNormalState && !exceptKey.Except(InputSet.IncludeEOS).IsEmpty)
                    _useError = true;
            }

            return list;
        }

        private void AnalyzeActions(IEnumerable<TransitionAction> actions)
        {
            foreach (var action in actions)
            {
                _useContext = true;
                if (action is InsertResultAction)
                {
                    _useResults = true;
                    continue;
                }

                if (action is AppendVarAction appendAction)
                {
                    if (appendAction.Depth > 0)
                        _useAppendDepth = true;

                    _vars.Add(appendAction.Name);
                }

                if (action is CopyVarAction copyAction)
                {
                    if (copyAction.Depth > 0)
                        _useCopyDepth = true;

                    _vars.Add(copyAction.TargetName);
                }
            }
        }

        private string CreateCondition(InputSet inputSet)
        {
            return inputSet.Type == InputSetType.Include ?
                   string.Join(" || ", inputSet.Values.Select(val => string.Format(_charInput ? "item == '{0}'" : "object.Equals(item, '{0}')", val)))
                   : string.Join(" && ", inputSet.Values.Select(val => string.Format(_charInput ? "item != '{0}'" : "!object.Equals(item, '{0}')", val)));
        }

        private static bool IsLinkSimple(int rootIndex, TransitionLink link)
        {
            // Special situation (but is common): filling in a variable and returning it immediately
            var state = link.State;
            if (rootIndex == 0 && state.IsFinal && link.Actions.Count == 1)
            {
                var finalState = (FinalTransitionState)state;
                if (finalState.Result is Query query)
                {
                    if (link.Actions[0] is AppendVarAction appendAction && appendAction.Name == query.Name && appendAction.Depth == 0)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private StatementBlock WriteLink(List<TransitionStateBase> list, int rootIndex, TransitionLink link)
        {
            var block = new StatementBlock();

            if (IsLinkSimple(rootIndex, link))
            {
                WriteReturnCurrentItem(block);
            }
            else
            {
                if (link.Actions.Any())
                    WriteActions(block, link);

                var state = link.State;
                if (state.IsFinal)
                {
                    if (_useContext)
                    {
                        var result = CreateResultFromExpression(((FinalTransitionState)state).Result, out bool needVars, out bool isEnumerable);
                        if (result != null)
                        {
                            var currentBlock = block;

                            if (needVars)
                            {
                                currentBlock = new StatementBlock();
                                if (_useResults)
                                    currentBlock.Add("var context = this.results.Count == 0 ? this.vars : this.results[this.results.Count - 1].Vars;");

                                block.Add(currentBlock);
                            }

                            if (_switchMode)
                            {
                                if (!needVars && result.Length == 3) // "v"
                                {
                                    currentBlock.Add("yield return '{0}';", result[1]);
                                }
                                else if (isEnumerable)
                                {
                                    currentBlock.Add("foreach (var r in {0})", result);
                                    currentBlock.Add("    yield return r;");
                                }
                                else
                                {
                                    currentBlock.Add("yield return {0};", result);
                                }
                            }
                            else
                            {
                                if (link.Actions.OfType<ReturnResultAction>().Any())
                                    currentBlock.Add("result = result.Concat({0});", result);
                                else
                                    currentBlock.Add("result = {0};", result);
                            }
                        }

                        if (_switchMode)
                        {
                            //if (this.moreThanOneNormalState && rootIndex != 0)
                            //{
                            //    block.Add("state = 0;");
                            //}

                            if (_useContext || _moreThanOneNormalState)
                            {
                                block.Add("Reset();");
                            }
                        }
                        else
                        {
                            block.Add("return Reset();");
                        }
                    }
                    else
                    {
                        if (_switchMode)
                        {
                            var constantResult = ((FinalTransitionState)state).GetResult(new TransitionContext());
                            foreach (var res in constantResult)
                            {
                                block.Add("yield return {0};", PrintObject(res));
                            }

                            if (_useContext || _moreThanOneNormalState)
                            {
                                block.Add("Reset();");
                            }
                        }
                        else
                        {
                            var result = CreateResultFromExpression(((FinalTransitionState)state).Result, out _, out _);

                            block.Add("result = {0};", result);
                            block.Add("return Reset();");
                        }
                    }
                }
                else // moreThanOneNormalState = true always here
                {
                    var index = list.IndexOf(state);

                    if (_switchMode)
                    {
                        if (rootIndex != index)
                            block.Add("this.state = {0};", index);
                    }
                    else
                    {
                        if (state is BadTransitionState)
                            block.Add("return null;", index);
                        else
                            block.Add("return F{0};", index);
                    }
                }
            }

            return block;
        }

        private string PrintObject(object res)
        {
            if (res is char)
                return string.Format("'{0}'", res);

            if (res is string)
                return string.Format("\"{0}\"", res);

            if (res is null)
                return "null";

            if (res is TupleValue tuple)
            {
                var sb = new StringBuilder("new TupleValue(");

                for (int i = 0; i < tuple.Items.Length; i++)
                {
                    if (i > 0)
                        sb.Append(", ");

                    sb.Append(PrintObject(tuple.Items[i]));
                }

                sb.Append(')');
                return sb.ToString();
            }

            if (res is NamedValue named)
            {
                var sb = new StringBuilder("new NamedValue(\"")
                    .Append(named.Name).Append("\", ");

                sb.Append(PrintObject(named.Value));

                sb.Append(')');
                return sb.ToString();
            }

            return res.ToString();
        }

        private void WriteReturnCurrentItem(StatementBlock block)
        {
            block.Add("yield return item;");
            if (_useContext || _moreThanOneNormalState)
            {
                block.Add("Reset();");
            }
        }

        private void WriteActions(StatementBlock block, TransitionLink link)
        {
            foreach (var action in link.Actions)
            {
                if (action is AppendVarAction append)
                {
                    if (append.Depth > 0)
                        block.Add("AppendVar(\"{0}\", item, {1});", append.Name, append.Depth);
                    else
                    {
                        if (_useResults)
                            block.Add("AppendVar(\"{0}\", item);", append.Name);
                        else
                            block.Add("this.{0}.{1}(item);", append.Name, _charInput ? "Append" : "Add");
                    }

                    continue;
                }

                if (action is CopyVarAction copy)
                {
                    if (copy.Depth > 0)
                        block.Add("CopyVar(\"{0}\", \"{1}\", {2});", copy.SourceName, copy.TargetName, copy.Depth);
                    else
                    {
                        if (_useResults)
                            block.Add("CopyVar(\"{0}\", \"{1}\");", copy.SourceName, copy.TargetName);
                        else
                            block.Add("{1}.{2}({0});", copy.SourceName, copy.TargetName, _charInput ? "Append" : "AddRange");
                    }

                    continue;
                }

                if (action is RenameVarAction rename)
                {
                    block.Add("RenameVar(\"{0}\", \"{1}\");", rename.SourceName, rename.TargetName);

                    continue;
                }

                if (action is InsertResultAction insert)
                {
                    if (insert.Result == null)
                        block.Add("InsertResult({0});", insert.RemoveLastCount);
                    else
                    {
                        var result = CreateResultFromExpression(insert.Result, out bool needVars, out _);

                        if (insert.RemoveLastCount > 0)
                        {
                            if (needVars)
                                block.Add("InsertResult({0}, context => {1});", insert.RemoveLastCount, result);
                            else
                                block.Add("InsertResult({0}, {1});", insert.RemoveLastCount, result);
                        }
                        else
                        {
                            if (needVars)
                                block.Add("InsertResult(context => {0});", result);
                            else
                                block.Add("InsertResult({0});", result);
                        }
                    }

                    continue;
                }

                if (action is ReturnResultAction returnResult)
                {
                    var call = returnResult.LeftResultsCount > 0 ? string.Format("ReturnResult({0})", returnResult.LeftResultsCount) : "ReturnResult()";
                    if (_switchMode)
                    {
                        block.Add("foreach (var r in {0})", call);
                        block.Add("    yield return r;", call);
                    }
                    else
                    {
                        block.Add("result = {0};", call);
                    }
                }
            }
        }

        private string CreateResultFromExpression(Expression expr, out bool needVars, out bool isEnumerable)
        {
            needVars = false;
            isEnumerable = false;
            if (expr == Empty.Instance)
                return null;

            if (expr is Query query)
            {
                needVars = true;
                isEnumerable = true;
                if (_useResults)
                    return string.Format("context[\"{0}\"]{1}", query.Name, _charInput ? ".ToString()" : "");
                else
                    return string.Format("this.{0}{1}", query.Name, _charInput ? ".ToString()" : "");
            }

            if (expr is StringValueMatch stringValue)
            {
                isEnumerable = true;
                return string.Format("\"{0}\"", stringValue.Value);
            }

            if (expr is Sequence sequence)
            {
                needVars = true;

                var values = new List<string>();
                foreach (var item in sequence._operands)
                {
                    if (item is Query subQuery)
                    {
                        values.Add(string.Format(_useResults ? "context[\"{0}\"]" : "this.{0}", subQuery.Name));
                        continue;
                    }

                    if (item is StringValueMatch stringValue2)
                        values.Add(string.Format("\"{0}\"", stringValue2.Value));
                }

                isEnumerable = true;
                if (values.Count == 0)
                    return _charOutput ? "new StringBuilder()" : "new object[0]";

                if (values.Count == 1)
                    return values[0];

                var sb = new StringBuilder(string.Format(_charOutput ? "new StringBuilder({0})" : "{0}.Cast<object>()", values[0]));
                for (int i = 1; i < values.Count; i++)
                {
                    sb.AppendFormat(_charOutput ? ".Append({0})" : ".Concat<object>({0}.Cast<object>())", values[i]);
                }
                
                sb.Append(_charOutput ? ".ToString()" : "");

                return sb.ToString();
            }

            if (expr is TupleValueMatch tupleValueMatch)
            {
                var sb = new StringBuilder("new TupleValue(");

                for (int i = 0; i < tupleValueMatch._operands.Length; i++)
                {
                    if (i > 0)
                        sb.Append(", ");
                    sb.Append(CreateResultFromExpression(tupleValueMatch._operands[i], out bool needVarsLocal, out _));

                    needVars |= needVarsLocal;
                }

                sb.Append(')');
                return sb.ToString();
            }

            if (expr is Anything)
                return "null";

            if (expr is Or or)
            {
                if (or._operands.Length > 0)
                {
                    return CreateResultFromExpression(or._operands[0], out needVars, out isEnumerable);
                }

                throw new NotImplementedException();
            }

            throw new NotImplementedException();
        }
    }
}
