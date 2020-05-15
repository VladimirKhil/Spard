using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Spard.Exceptions;
using Spard.Core;

namespace Spard.Expressions
{
    /// <summary>
    /// Expression trees builder
    /// </summary>
    public sealed class ExpressionBuilder: IDisposable
    {
        /// <summary>
        /// Expression parse data
        /// </summary>
        private sealed class ParseData
        {
            /// <summary>
            /// List of current parsed expressions
            /// </summary>
            public LinkedList<Expression> data = new LinkedList<Expression>();
            /// <summary>
            /// Stack of functional nodes (operators) in expression list
            /// </summary>
            public Stack<Expression> functions = new Stack<Expression>();
        }

        private readonly TextReader _text;
        private bool _disposeSource;

        private readonly bool _isStatement;

        private int _lineNumber = 0;
        private int _columnNumber = -1;

        private readonly bool _saveCoordinates;
        /// <summary>
        /// Whether to use only unique nodes in expression trees (except for the empty expression).
        /// Otherwise (by default), an attempt to reuse leaf nodes is made.
        /// </summary>
        private readonly bool _uniqueNodes;

        /// <summary>
        /// The coordinates of the beginning of the expression in the source code (row and column number). Useful for debugging.
        /// The coordinates of the end of the expression aren't needed, because they are equal to the coordinates of the beginning of the next expression
        /// </summary>
        private readonly Dictionary<Expression, Tuple<int, int>> _coordinates = new Dictionary<Expression, Tuple<int, int>>();

        public Tuple<int, int> GetCoordinates(Expression expr)
        {
            if (_coordinates.TryGetValue(expr, out var result))
                return result;

            return Tuple.Create(-1, -1);
        }

        internal Dictionary<Expression, Tuple<string, string>> FunctionCalls { get; } = new Dictionary<Expression, Tuple<string, string>>();
        internal Dictionary<Expression, Tuple<string, string, int>> SetCalls { get; } = new Dictionary<Expression, Tuple<string, string, int>>();

        /// <summary>
        /// Found table recognizers
        /// </summary>
        internal List<TableRecognizer> TableRecognizers { get; } = new List<TableRecognizer>();

        private readonly ParseData _parseData = new ParseData();

        /// <summary>
        /// Sets used in expresion
        /// </summary>
        internal Dictionary<Expression, HashSet<Tuple<string, string, int>>> UsedSets { get; } = new Dictionary<Expression, HashSet<Tuple<string, string, int>>>();

        /// <summary>
        /// Set calls
        /// </summary>
        internal List<Tuple<Expression, Expression, int>> SetCallsTable { get; } = new List<Tuple<Expression, Expression, int>>();

        public ExpressionBuilder(string text, bool isStatement = true, bool saveCoordinates = false, bool uniqueNodes = false)
            : this(new StringReader(text), isStatement, saveCoordinates, uniqueNodes)
        {
            _disposeSource = true;
        }

        public ExpressionBuilder(TextReader text, bool isStatement = true, bool saveCoordinates = false, bool uniqueNodes = false)
        {
            _text = text;

            _isStatement = isStatement;

            _saveCoordinates = saveCoordinates;
            _uniqueNodes = uniqueNodes;

            if (!_uniqueNodes)
            {
                stringValueCache = new Dictionary<string, StringValueMatch>();
            }
        }

        /// <summary>
        /// Build expression from input string
        /// </summary>
        /// <param name="text">Input text</param>
        /// <param name="isStatement">Is the text a description of a self-sufficient SPARD operator
        /// (functions, definitions, global instructions), or does the text describe only a template fragment</param>
        /// <returns>Expression corresponding to the input text</returns>
        public static Expression Parse(string text, bool isStatement = true)
        {
            if (text.Length == 0 && !isStatement)
                return Empty.Instance;

            using (var builder = new ExpressionBuilder(text, isStatement))
            {
                return builder.Parse().FirstOrDefault();
            }
        }

        /// <summary>
        /// Build expression from input string
        /// </summary>
        /// <param name="text">Input text</param>
        /// <param name="isStatement">Is the text a description of a self-sufficient SPARD operator
        /// (functions, definitions, global instructions), or does the text describe only a template fragment</param>
        /// <returns>Expression corresponding to the input text</returns>
        public static Expression Parse(TextReader text, bool isStatement = true)
        {
            using (var builder = new ExpressionBuilder(text, isStatement))
            {
                return builder.Parse().FirstOrDefault();
            }
        }

        /// <summary>
        /// Main parse method
        /// </summary>
        /// <param name="insideInstruction">Is instruction syntax used</param>
        /// <returns>Expression corresponding to the input text</returns>
        /// <exception cref="ParseException" />
        internal IEnumerable<Expression> Parse(bool insideInstruction = false)
        {
            char currentSymbol = (char)0;
            while (true)
            {
                char previousSymbol = currentSymbol;
                int currentSymbolCode;
                var endOfStream = (currentSymbolCode = ReadChar()) == -1;
                if (endOfStream || (currentSymbol = (char)currentSymbolCode) == '\n' || insideInstruction && currentSymbol == ']')
                {
                    #region EndOfLine
                    FinishString();
                    if (!endOfStream && _parseData.functions.OfType<ComplexValueMatch>().Any()) // We are inside curly brackets
                    {
                        SkipSpace(true);
                        AppendExpression(new Block());
                    }
                    else if (_parseData.data.Count > 0) // Template line reading is completed
                    {
                        #region Issuing a template
                        while (_parseData.data.Count > 1 && _parseData.functions.Count > 0 && (!insideInstruction || !(_parseData.functions.Peek() is TypeDefinition)))
                        {
                            var peek = _parseData.functions.Peek();

                            if (peek is Dual && !(peek is Instruction))
                            {
                                if (peek is Bracket)
                                    OnParseError("Bracket '(' is not closed");
                                else if (peek is ComplexValueMatch)
                                    OnParseError("Bracket '{' is not closed");
                                else
                                    OnParseError("Bracket '<' is not closed");
                            }

                            Construct();
                        }

                        if (!insideInstruction && _parseData.data.Count > 1)
                        {
                            OnParseError("Parse error");
                        }
                        else if (_parseData.data.Count > 0)
                        {
                            var val = _parseData.data.Last.Value;

                            if (_parseData.data.Count != 1 || !(val is Sequence seq) || seq._operands != null)
                                if (insideInstruction || CheckExpression(val))
                                    yield return val;

                            _parseData.data.Clear();
                            _parseData.functions.Clear();
                        }
                        #endregion
                    }

                    if (endOfStream)
                        break;

                    _lineNumber++;
                    _columnNumber = -1;
                    continue;
                    #endregion
                }

                int next;
                Expression newExpr;
                char nextC;
                switch (currentSymbol)
                {
                    case '\r':
                    case '\t':
                        continue;

                    case ';': // Comment
                        while ((currentSymbolCode = _text.Peek()) != -1 && (currentSymbol = (char)currentSymbolCode) != '\n')
                            ReadChar();

                        continue;

                    case '\'':
                        next = ReadChar();
                        if (next != -1)
                        {
                            nextC = (char)next;
                            AppendChar(nextC, _text.Peek());
                        }
                        else
                            OnParseError("No escaped character after apostrophe");

                        continue;

                    case '`':
                        SkipSpace(true);
                        continue;

                    case '^':
                        AppendConcatenation();
                        AddData(OpenLine.Instance);
                        continue;

                    case '%':
                        AppendConcatenation();
                        AddData(End.Instance);
                        continue;

                    case '.':
                        AppendConcatenation();
                        AddData(Any.Instance);
                        continue;

                    case ':':
                        next = _text.Peek();
                        if (next != -1)
                        {
                            nextC = (char)next;
                            switch (nextC)
                            {
                                case '=':
                                    newExpr = new Definition();
                                    ReadChar();
                                    break;

                                case ':':
                                    newExpr = new InlineTypeDefinition();
                                    ReadChar();
                                    break;

                                default:
                                    newExpr = new NamedValueMatch();
                                    break;
                            }
                        }
                        else
                        {
                            AppendChar(currentSymbol, next);
                            continue;
                        }

                        break;

                    case '=':
                        next = _text.Peek();
                        if (next != -1)
                        {
                            nextC = (char)next;
                            switch (nextC)
                            {
                                case '>':
                                    newExpr = new Function();
                                    ReadChar();
                                    break;

                                default:
                                    newExpr = new Function(Directions.Both);
                                    break;
                            }
                        }
                        else
                        {
                            newExpr = new Function(Directions.Both);
                        }

                        break;

                    case '~':
                        newExpr = new Translation();
                        break;

                    case ' ':
                        SkipSpace();

                        next = _text.Peek();
                        if (next == -1)
                            newExpr = new Sequence();
                        else
                        {
                            nextC = (char)next;

                            if ((char.IsLetterOrDigit(previousSymbol) || previousSymbol == ')' || previousSymbol == '}' || previousSymbol == '>' || previousSymbol == '"' || previousSymbol == '.' || previousSymbol == '%' || previousSymbol == '_') &&
                                (char.IsLetterOrDigit(nextC) || nextC == '(' || nextC == '{' || nextC == '<' || nextC == '$' || nextC == '\\' || nextC == '"' || nextC == '.' || nextC == '@' || nextC == '%' || nextC == '_'))
                                newExpr = new TupleValueMatch();
                            else
                                newExpr = new Sequence();
                        }
                        break;

                    case '|':
                        newExpr = new Or();
                        break;

                    case '&':
                        newExpr = new And();
                        break;

                    case '@':
                        AppendConcatenation();
                        var func = new FunctionCall();
                        newExpr = func;
                        break;

                    case '!':
                        AppendConcatenation();
                        newExpr = new Not();
                        break;

                    case '*':
                        newExpr = new MultiTime();
                        break;

                    case '+':
                        newExpr = new SeveralTime();
                        break;

                    case '?':
                        newExpr = new Optional();
                        break;

                    case '#':
                        newExpr = new Counter();
                        break;

                    case '$':
                        newExpr = new Query();
                        break;

                    case '(':
                        AppendConcatenation();
                        newExpr = new Bracket();
                        break;

                    case '{':
                        AppendConcatenation();
                        newExpr = new ComplexValueMatch();
                        break;

                    case '[':
                        AppendConcatenation();
                        ParseInstruction();
                        continue;

                    case '<':
                        next = _text.Peek();
                        if (next != -1)
                        {
                            nextC = (char)next;
                            switch (nextC)
                            {
                                case '=':
                                    newExpr = new Function(Directions.Left);
                                    ReadChar();
                                    break;

                                default:
                                    AppendConcatenation();
                                    newExpr = new Set();
                                    break;
                            }
                        }
                        else
                        {
                            AppendChar(currentSymbol, next);
                            continue;
                        }

                        break;

                    case ')':
                        while (_parseData.functions.Count > 0 && !(_parseData.functions.Peek() is Bracket))
                        {
                            Construct();
                        }

                        if (_parseData.functions.Count > 0)
                        {
                            var function = _parseData.functions.Pop();

                            RemoveFromParseData(function);
                        }
                        else
                            OnParseError("Extra closing brace detected ')'");

                        continue;

                    case '}':
                        while (_parseData.functions.Count > 0 && !(_parseData.functions.Peek() is ComplexValueMatch))
                        {
                            Construct();
                        }

                        if (_parseData.functions.Count > 0)
                            Construct();
                        else
                            OnParseError("Extra closing brace detected '}'");

                        continue;

                    case '>':
                        while (_parseData.functions.Count > 0 && !(_parseData.functions.Peek() is Set))
                        {
                            Construct();
                        }

                        if (_parseData.functions.Count > 0)
                            Construct();
                        else
                            OnParseError("Extra closing brace detected '>'");

                        continue;

                    case '"':
                        while ((currentSymbolCode = ReadChar()) != -1 && (currentSymbol = (char)currentSymbolCode) != '"')
                        {
                            if (currentSymbol == '\n')
                            {
                                _lineNumber++;
                                _columnNumber = -1;
                            }

                            if (currentSymbol == '\'')
                            {
                                next = ReadChar();
                                if (next != -1)
                                {
                                    nextC = (char)next;
                                    AppendChar(nextC, -1);
                                }
                                else
                                    OnParseError("No escaped character after apostrophe");

                                continue;
                            }
                            else
                                AppendChar(currentSymbol, -1);
                        }

                        if (currentSymbolCode == -1)
                            OnParseError("Double quotes not closed");

                        continue;

                    case '_':
                        AppendConcatenation();
                        AddData(Anything.Instance);
                        continue;

                    case '-':
                        newExpr = new Range();
                        break;

                    default:
                        AppendChar(currentSymbol, _text.Peek());
                        continue;
                }

                AppendExpression(newExpr);
            }
        }

        private void SkipSpace(bool newLines = false)
        {
            int next;
            do
            {
                next = _text.Peek();
                if (next == -1)
                    break;

                var c = (char)next;
                if (!char.IsWhiteSpace(c) || !newLines && c == '\n')
                    break;

                ReadChar();
            } while (true);
        }

        private void RemoveFromParseData(Expression function)
        {
            for (var node = _parseData.data.First; node != null; node = node.Next)
            {
                if (node.Value == function)
                {
                    _parseData.data.Remove(node);
                    break;
                }
            }
        }

        private void Construct()
        {
            FinishString();
            var function = _parseData.functions.Pop();

            var arg = _parseData.data.Last;

            Expression prevFunction = _parseData.functions.Any() ? _parseData.functions.Peek() : null;

            var op = new List<Expression>();
            var lop = new List<LinkedListNode<Expression>>();

            var twoArgs = function is Binary || function is Polynomial;
            if (twoArgs && arg.Value == function)
            {
                if (function is TupleValueMatch || function is Sequence)
                {
                    _parseData.data.RemoveLast();
                    return;
                }

                op.Add(Empty.Instance);
            }

            var recognizers = new List<TableRecognizer>();

            for (; arg != null && arg.Value != prevFunction; arg = arg.Previous)
            {
                if (arg.Value != function)
                {
                    lop.Add(arg);

                    if (arg.Value is Instruction instruction)
                    {
                        var operandName = instruction.Operand.ToString();
                        if (operandName == "optimize")
                        {
                            // Substitute the table recognizer instead of the expression tree
                            var recognizer = new TableRecognizer(instruction.Argument);
                            recognizers.Add(recognizer);

                            op.Insert(0, recognizer);
                        }
                        else if (operandName == "compile")
                        {
                            throw new NotImplementedException();
                        }
                        else
                        {
                            op.Insert(0, arg.Value);
                        }
                    }
                    else
                    {
                        op.Insert(0, arg.Value);
                    }
                }
            }

            if (twoArgs && op.Count < 2)
            {
                if (function is TupleValueMatch || function is Sequence)
                {
                    arg = _parseData.data.Last;
                    for (; arg != null; arg = arg.Previous)
                    {
                        if (arg.Value == function)
                        {
                            _parseData.data.Remove(arg);
                            break;
                        }
                    }
                    
                    return;
                }

                op.Insert(0, Empty.Instance);
            }

            CheckExpressionHierarchy(function, op);

            foreach (var item in lop)
            {
                _parseData.data.Remove(item);
            }

            if (function is Instruction instr && instr.Operand != null)
                instr.Argument = op[0];
            else
                function.SetOperands(op);

            PostProcessConstruction(function);

            if (recognizers.Any())
                TableRecognizers.AddRange(recognizers);
        }

        private void PostProcessConstruction(Expression parent)
        {
            HashSet<Tuple<string, string, int>> setNames = null;
            var children = parent.Operands().ToArray();
            for (int i = 0; i < children.Length; i++)
            {
                var item = children[i];
                if (item is Set set)
                {
                    if (parent is Definition def && def.Left == item) // This is the name of the set being defined
                        continue;

                    if (parent is Instruction inst && inst.Operand is TupleValueMatch tv && tv._operands.Length == 2 && tv._operands[0].ToString() == "on" && tv._operands[1].ToString() == "m")
                        continue;

                    if (!(((TupleValueMatch)set.Operand)._operands[0] is StringValueMatch localNameStr))
                        continue; // Not processed yet

                    var localName = localNameStr.Value;
                    if (localName == "SP" || localName == "BR" || localName == "s" || localName == "t" || localName == "d" || localName == "i" || localName == "Int")
                        continue;

                    SetCallsTable.Add(Tuple.Create(parent, item, i));

                    if (setNames == null)
                        UsedSets[parent] = setNames = new HashSet<Tuple<string, string, int>>();

                    if (SetCalls.TryGetValue(item, out var call))
                        setNames.Add(call);
                }
                else
                {
                    if (UsedSets.TryGetValue(item, out var childSetNames))
                    {
                        if (setNames == null)
                            UsedSets[parent] = setNames = new HashSet<Tuple<string, string, int>>();

                        foreach (var setName in childSetNames)
                        {
                            setNames.Add(setName);
                        }
                    }
                }
            }

            if (parent is Set)
            {
                if (ExtractName(children[0], out string globalName, out string localName))
                {
                    if (localName == "SP" || localName == "BR" || localName == "s" || localName == "t" || localName == "d" || localName == "i" || localName == "Int")
                        return;

                    SetCalls[parent] = Tuple.Create(globalName, localName, 1);
                    return;
                }

                if (children[0] is TupleValueMatch list)
                {
                    if (ExtractName(list._operands[0], out globalName, out localName))
                    {
                        if (localName == "SP" || localName == "BR" || localName == "s" || localName == "t" || localName == "d" || localName == "i")
                            return;

                        SetCalls[parent] = Tuple.Create(globalName, localName, list._operands.Length);
                        return;
                    }
                }

                return;
            }
        }

        private LinkedListNode<Expression> AddData(Expression expr)
        {
            FinishString();

            if (_saveCoordinates || expr is Set || expr is FunctionCall)
                _coordinates[expr] = Tuple.Create(_lineNumber, _columnNumber);

            return _parseData.data.AddLast(expr);
        }

        private readonly Dictionary<string, StringValueMatch> stringValueCache;

        private void FinishString()
        {
            if (isBuilding)
            {
                var newVal = stringValueBuilder.ToString();
                var stringValue = CreateStringValue(newVal);

                if (_saveCoordinates)
                    _coordinates[stringValue] = stringValueStart;

                _parseData.data.AddLast(stringValue);
                isBuilding = false;
            }
        }

        private StringValueMatch CreateStringValue(string newVal)
        {
            if (_uniqueNodes)
                return new StringValueMatch(newVal);

            if (!stringValueCache.TryGetValue(newVal, out StringValueMatch stringValue))
                stringValueCache[newVal] = stringValue = new StringValueMatch(newVal);

            return stringValue;
        }

        private bool CheckExpression(Expression expr)
        {
            if (!_isStatement)
                return true;

            if (expr is Function)
                return true;

            if (expr is Definition def)
            {
                if (def.Left is Set)
                    return true;
                else if (def.Left is StringValueMatch)
                {
                    if (!(def.Right is Function || def.Right is ComplexValueMatch))
                        OnParseError("Function definition must be a function!");
                    else
                        return true;
                }
                else
                    OnParseError("Operation ':=' must have set or function name at the left!");

                return false;
            }

            if (expr is Instruction instr)
            {
                if (!CheckGlobalInstruction(instr))
                    OnParseError("Incorrect global instruction");
                else
                    return true;
            }
            else
                OnParseError("The exression is not an instruction, definition or transforming rule");

            return false;
        }

        private static bool CheckGlobalInstruction(Instruction instr)
        {
            if (instr.Operand is TypeDefinition)
                return true;

            if (instr.Operand is StringValueMatch str)
                return str.Value == "simplematch" || str.Value == "suppressinline";

            if (!(instr.Operand is TupleValueMatch list) || list._operands.Length < 2 || list._operands.Length > 3)
                return false;

            if (!(list._operands[0] is StringValueMatch name))
                return false;

            if (name.Value != "module" && name.Value != "optimize" && name.Value != "compile")
                return false;

            if (name == null)
                return false;

            if (list._operands.Length == 3)
            {
                if (list._operands[2] as StringValueMatch == null)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Syntax parse error
        /// </summary>
        /// <param name="message">Error message</param>
        private void OnParseError(string message)
        {
            throw new ParseException(_lineNumber + 1, _columnNumber + 1, message);
        }

        private int ReadChar()
        {
            _columnNumber++;
            return _text.Read();
        }

        /// <summary>
        /// Recognize the instruction in the input text. The instruction uses its own syntax, so its parsing function is different
        /// </summary>
        private void ParseInstruction()
        {
            char c = (char)0;

            var breakAll = false;

            var instruction = new Instruction();

            AppendInstruction(instruction);
            int sym;
            while (!breakAll && (sym = ReadChar()) != -1 && (c = (char)sym) != '\n' && c != ']')
            {
                int next;
                char nextC;
                Expression newExpr;
                switch (c)
                {
                    case '\r':
                        continue;

                    case '\'':
                        next = ReadChar();
                        if (next != -1)
                        {
                            nextC = (char)next;
                            AppendCharInstruction(nextC);
                        }
                        else
                            OnParseError("No escaped character after apostrophe");

                        continue;

                    case '"':
                        while ((sym = ReadChar()) != -1 && (c = (char)sym) != '"')
                        {
                            if (c == '\'')
                            {
                                next = ReadChar();
                                if (next != -1)
                                {
                                    nextC = (char)next;
                                    AppendCharInstruction(nextC);
                                }
                                else
                                    OnParseError("No escaped character after apostrophe");

                                continue;
                            }
                            else
                                AppendCharInstruction(c);
                        }
                        continue;

                    case ':':
                        next = _text.Peek();
                        if (next != -1)
                        {
                            nextC = (char)next;
                            switch (nextC)
                            {
                                case ':':
                                    newExpr = new TypeDefinition();
                                    ReadChar();

                                    AppendExpression(newExpr);
                                    // We just need it to execute
                                    Parse(true).FirstOrDefault();

                                    breakAll = true;
                                    continue;

                                default:
                                    newExpr = new NamedValueMatch();
                                    break;
                            }
                        }
                        else
                        {
                            newExpr = new NamedValueMatch();
                        }

                        break;

                    case '$':
                        newExpr = new Query();
                        break;

                    case '=':
                        newExpr = new Unification();
                        break;

                    case '+':
                        newExpr = new Add();
                        break;

                    case '-':
                        newExpr = new Substract();
                        break;

                    case '*':
                        newExpr = new Multiply();
                        break;

                    case '/':
                        newExpr = new Divide();
                        break;

                    case '%':
                        newExpr = new Remainder();
                        break;

                    case '>':
                        newExpr = new Bigger();
                        break;

                    case '<':
                        newExpr = new Bigger() { Direction = Directions.Left };
                        break;

                    case '!':
                        next = _text.Peek();
                        if (next != -1)
                        {
                            nextC = (char)next;
                            switch (nextC)
                            {
                                case '=':
                                    newExpr = new NotEqual();
                                    ReadChar();
                                    break;

                                default:
                                    AppendCharInstruction(c);
                                    continue;
                            }
                        }
                        else
                        {
                            AppendCharInstruction(c);
                            continue;
                        }

                        break;

                    case '@':
                        AppendConcatenation();
                        var func = new FunctionCall();
                        newExpr = func;
                        break;

                    case ' ':
                        FinishString();
                        newExpr = new TupleValueMatch();
                        break;

                    case '(':
                        AppendConcatenation();
                        newExpr = new Bracket();
                        break;

                    case '[':
                        AppendConcatenation();
                        ParseInstruction();
                        continue;

                    case '{':
                        AppendConcatenation();
                        newExpr = new ComplexValueMatch();
                        break;

                    case ')':
                        while (_parseData.functions.Count > 0 && !(_parseData.functions.Peek() is Bracket))
                        {
                            Construct();
                        }

                        if (_parseData.functions.Count > 0)
                        {
                            var function = _parseData.functions.Pop();
                            RemoveFromParseData(function);
                        }
                        else
                            OnParseError("Extra closing brace detected ')'");
                        continue;

                    case '}':
                        while (_parseData.functions.Count > 0 && !(_parseData.functions.Peek() is ComplexValueMatch))
                        {
                            Construct();
                        }

                        if (_parseData.functions.Count > 0)
                            Construct();
                        else
                            OnParseError("Extra closing brace detected '}'");
                        continue;

                    default:
                        AppendCharInstruction(c);
                        continue;
                }

                AppendExpression(newExpr);
            }

            FinishString();
            while (true)
            {
                var peek = _parseData.functions.Peek();
                if (peek is Dual)
                {
                    if (peek is Bracket)
                        OnParseError("Bracket '(' is not closed");
                    else if (peek is ComplexValueMatch)
                        OnParseError("Bracket '{' is not closed");
                    else if (peek is Instruction && c != ']' && !breakAll)
                        OnParseError("Bracket '[' is not closed");
                    else if (peek is Set)
                        OnParseError("Bracket '<' is not closed");
                }

                Construct();

                if (peek == instruction)
                    break;
            }

            if (!instruction.Check())
            {
                OnParseError("Incorrect instruction");
            }

            if (instruction.RightArgumentNeeded)
            {
                _parseData.functions.Push(instruction);
            }
        }

        /// <summary>
        /// Add symbol to the instruction
        /// </summary>
        /// <param name="c">Symbol to add</param>
        private void AppendCharInstruction(char c)
        {
            if (!isBuilding)
            {
                if (_parseData.data.Count > 0)
                    AppendConcatenation();

                if (_saveCoordinates)
                    stringValueStart = Tuple.Create(_lineNumber, _columnNumber);

                stringValueBuilder.Clear();
                isBuilding = true;
            }

            stringValueBuilder.Append(c);
        }

        /// <summary>
        /// Add symbol to the expression
        /// </summary>
        /// <param name="c">Symbol to add</param>
        /// <param name="next">Next symbol code</param>
        private void AppendChar(char c, int next)
        {
            // Fast concatenation:                                    
            if (next != -1)
            {
                switch ((char)next)
                {
                    case '?':
                    case '*':
                    case '+':
                    case '#':
                        AppendConcatenation();
                        AddData(CreateStringValue(c.ToString()));
                        return;
                }
            }

            if (!isBuilding)
            {
                if (_parseData.data.Count > 0)
                    AppendConcatenation();

                if (_saveCoordinates)
                    stringValueStart = Tuple.Create(_lineNumber, _columnNumber);

                stringValueBuilder.Clear();
                isBuilding = true;
            }

            stringValueBuilder.Append(c);
        }

        private readonly StringBuilder stringValueBuilder = new StringBuilder();
        private Tuple<int, int> stringValueStart;
        private bool isBuilding = false;

        /// <summary>
        /// Add a concatenation operation to the end of the sequence of operations if necessary
        /// </summary>
        private void AppendConcatenation()
        {
            FinishString();
            if (_parseData.data.Count > 0)
            {
                var last = _parseData.data.Last.Value;
                var unaryNode = last as Unary;

                if (!_parseData.functions.Any(f => f == last)
                    || unaryNode != null && unaryNode.OperandPosition == Relationship.Left
                    || unaryNode == null && (last is Primitive))
                    AppendExpression(new Sequence());
            }
        }

        /// <summary>
        /// Add expression to the end of expressions list
        /// </summary>
        /// <param name="newExpr">Expression to add (it's not an Instruction, Instructions have AppendInstruction method)</param>
        private void AppendExpression(Expression newExpr)
        {
            FinishString();

            var functions = _parseData.functions;
            if (newExpr.Priority >= 0)
            {
                while (functions.Count > 0)
                {
                    var peek = functions.Peek();
                    if (peek.Priority < newExpr.Priority + (newExpr.Assotiative == Relationship.Left ? 0 : 1))
                        break;

                    if (peek is Dual dual)
                    {
                        if (!(dual is Instruction instruction) || instruction.Operand == null)
                            break;
                    }

                    Construct();
                }
            }

            if (_parseData.data.Count > 0)
            {
                if (newExpr is Unary unaryNode && unaryNode.OperandPosition == Relationship.Right)
                    AppendConcatenation();
            }

            AddData(newExpr);
            functions.Push(newExpr);
        }

        private void AppendInstruction(Instruction newInstruction)
        {
            FinishString();

            var functions = _parseData.functions;
            while (functions.Count > 0)
            {
                var peek = functions.Peek();
                if (peek.Priority < newInstruction.Priority + 1)
                    break;

                if (peek is Dual dual)
                {
                    if (!(dual is Instruction instruction) || instruction.Operand == null)
                        break;
                }

                Construct();
            }

            AddData(newInstruction);
            functions.Push(newInstruction);
        }

        private void CheckExpressionHierarchy(Expression parent, List<Expression> children)
        {
            if (parent is TupleValueMatch && children.Count < 2)
                OnParseError("The tuple cannot contain less than two items!");

            if (children.OfType<Definition>().Any())
                OnParseError(string.Format("Operation '{0}' cannot be directly subordinated to the operation '{1}'", children.OfType<Definition>().First(), parent));

            if (parent is Function)
            {
                if (children.OfType<Function>().Any())
                    OnParseError(string.Format("Operation '{0}' cannot be directly subordinated to the operation '{1}'", children.OfType<Function>().First(), parent));

                return;
            }

            if (parent is Instruction instruction)
            {
                if (!children.Any() && instruction.RightArgumentNeeded && instruction.Operand != null)
                    OnParseError("The instruction requires additional argument at right");

                return;
            }

            if (parent is Set)
            {
                if (!children.Any())
                    OnParseError("Incorrect set");

                var isNotSetCall = _parseData.functions.OfType<Definition>().Any() && _parseData.functions.OfType<Function>().Any();

                if (children[0] is StringValueMatch)
                    return;

                if (children[0] is TupleValueMatch list && list._operands.Length > 0 && list._operands[0] is StringValueMatch)
                    return;

                if (children[0] is Query)
                    return;
                
                OnParseError("Incorrect set");
                return;
            }

            if (parent is FunctionCall)
            {
                if (!children.Any())
                    OnParseError("Incorrect function call");

                if (ExtractName(children[0], out string globalName, out string localName))
                {
                    FunctionCalls[parent] = Tuple.Create(globalName, localName);
                    return;
                }

                if (children[0] is TupleValueMatch list)
                {
                    if (ExtractName(list._operands[0], out globalName, out localName))
                    {
                        FunctionCalls[parent] = Tuple.Create(globalName, localName);
                        return;
                    }
                }

                OnParseError("Incorrect function call");
            }
            if (parent is Query)
            {
                if (children.Count == 0 || children.Count == 1 && children[0] is Empty)
                {
                    OnParseError("Empty variable name!");
                }
                else if (children[0] is StringValueMatch)
                {
                    var name = children[0].ToString();
                    if (name.Any(c => !char.IsLetterOrDigit(c)))
                        OnParseError("Incorrect variable name '" + name + "'!");
                }
            }

            if (parent is InlineTypeDefinition)
            {
                if (!(children[0] is Query || children[0] is FunctionCall))
                    OnParseError("Type definition is not linked to the variable!");
            }

            if (parent is TypeDefinition)
            {
                if (!(children[0] is Query))
                    OnParseError("Type definition is not linked to the variable!");
            }
        }

        private static bool ExtractName(Expression expression, out string globalName, out string localName)
        {
            if (expression is StringValueMatch value)
            {
                globalName = "";
                localName = value.Value;
                return true;
            }

            globalName = null;
            localName = null;
            return false;
        }

        public void Dispose()
        {
            if (_disposeSource)
            {
                _text.Dispose();
                _disposeSource = false;
            }

            _coordinates.Clear();
        }
    }
}
