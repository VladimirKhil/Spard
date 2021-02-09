using Spard.Common;
using Spard.Core;
using Spard.Data;
using Spard.Exceptions;
using Spard.Sources;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Spard.Expressions
{
    /// <summary>
    /// Function call
    /// </summary>
    public sealed class FunctionCall : Unary, IInstructionExpression
    {
        /// <summary>
        /// Maximum allowed call stack depth
        /// </summary>
        private const int MaxFunctionCallDepth = 50;

        private IContext _initContext = null;

        private StringValueMatch _name = null;

        /// <summary>
        /// Вызываемая функция
        /// </summary>
        private ITransformFunction _function = null;

        protected internal override Relationship Assotiative
        {
            get
            {
                return Relationship.Right;
            }
        }

        protected internal override Relationship OperandPosition
        {
            get { return Relationship.Right; }
        }

        protected internal override Priorities Priority
        {
            get { return Priorities.FunctionCall; }
        }

        protected internal override string Sign
        {
            get { return "@"; }
        }

        public FunctionCall()
        {

        }

        public FunctionCall(Expression operand)
            : base(operand)
        {
            Init();
        }

        internal override bool MatchCore(ISource input, ref IContext context, bool next)
        {
            throw new NotImplementedException();
        }

        internal override object Apply(IContext context)
        {
            if (context == null) // second function call
            {
                if (_function == null)
                    return null;
            }
            else
                _initContext = context;

            if (context.Runtime != null && context.Runtime.CancellationToken.IsCancellationRequested)
                return null;

            var args = (TupleValue)Args.Apply(_initContext);
            var argsArray = args.Items;

            var res = _name.Apply(_initContext).ToString();

            return Call(res, argsArray, _initContext, ref _function);
        }

        /// <summary>
        /// Call SPARD function
        /// </summary>
        internal static object Call(string name, object[] args, IContext context, Relationship direction = Relationship.Right)
        {
            ITransformFunction function = null;
            ContextParameter param = null;
            if (direction == Relationship.Left)
                param = context.UseParameter(Parameters.Left);

            try
            {
                return Call(name, args, context, ref function);
            }
            finally
            {
                if (direction == Relationship.Left)
                    param.Free(context);
            }
        }

        private static object Call(string name, object[] args, IContext context, ref ITransformFunction function)
        {
            switch (name)
            {
                case "bagAdd":
                    {
                        var bag = args[0];
                        var item = ValueConverter.ConvertToSingle(args[1]);

                        if (bag == BindingManager.UnsetValue)
                            return item;

                        if (bag is TupleValue tupleValue)
                        {
                            var items = tupleValue.Items;
                            var newItems = new object[items.Length + 1];
                            Array.Copy(items, newItems, items.Length);
                            newItems[items.Length] = item;
                            return new TupleValue(newItems);
                        }

                        return new TupleValue(bag, item);
                    }

                case "call": // Calling a function parsed from a string (metaprogramming)
                    {
                        var transformation = args[0].ToString();

                        var transformer = new TreeTransformer(context.Root);
                        using (var reader = new StringReader(transformation))
                        {
                            transformer.Parse(reader);
                        }

                        transformer.Mode = TransformMode.Function;

                        if (context.GetParameter(Parameters.Left))
                            transformer = transformer.Reverse();

                        var parameters = args.Skip(1);
                        return ((ITransformFunction)transformer).TransformCoreAll(parameters.ToArray(), context.Runtime.CancellationToken);
                    }

                case "foldl":
                    {
                        if (function == null)
                        {
                            function = context.Root.GetFunction("", (string)args[1], context.GetParameter(Parameters.Left) ? Directions.Left : Directions.Right); // Вызов пользовательской функции
                            if (function == null)
                                throw new FunctionDefinitionNotFoundException() { FunctionName = args[1].ToString() };
                        }

                        var parameter = args[2];
                        object value = args[0];

                        if (parameter is IEnumerable enumerable1)
                        {
                            foreach (var item in enumerable1)
                            {
                                var list = new object[] { item, value };
                                value = function.TransformCoreAll(list, context.Runtime.CancellationToken);
                            }
                        }
                        else
                        {
                            var list = new object[] { parameter, value };
                            value = function.TransformCoreAll(list, context.Runtime.CancellationToken);
                        }

                        return value;
                    }

                case "ifdef":
                    {
                        var parameter = args[0].ToString();
                        return context.Vars.ContainsKey(parameter);
                    };

                case "length":
                    return ((IEnumerable)args[0]).Cast<object>().Count().ToString();

                case "lower":
                    return args[0].ToString().ToLower();

                case "stringify":
                    var argument = args[0];

                    if (argument is IEnumerable enumerable)
                    {
                        var result = new StringBuilder();

                        foreach (var item in enumerable)
                        {
                            result.Append('{').Append(item).Append('}');
                        }

                        return result.ToString();
                    }

                    return args[0].ToString();

                case "upper":
                    return args[0].ToString().ToUpper();

                default: // It is not a builtin function
                    {
                        if (context.Root.FunctionCallDepth > MaxFunctionCallDepth)
                            throw new Exception("Maximum number of nested function calls exceeded! Infinite recursion possible");

                        if (function == null)
                        {
                            function = context.Root.GetFunction("", name, context.GetParameter(Parameters.Left) ? Directions.Left : Directions.Right); // User function call
                            if (function == null)
                                throw new FunctionDefinitionNotFoundException { FunctionName = name };
                        }

                        try
                        {
                            // IMPORTANT: function is used in lazy calculations, and it is unknown when it will actually be called
                            // In this regard, there is a risk of inoperability (if the same function will be simultaneously called for two different sources)
                            // There are two solutions of this problem:
                            // 1. Create new function instanse on every call.
                            // 2. Make all Expression objects transform-independent (do not store transformation state inside them, move everything into the context)

                            context.Root.FunctionCallDepth++;

                            return function.TransformCoreAll(args, context.Runtime.CancellationToken);
                        }
                        finally
                        {
                            context.Root.FunctionCallDepth--;
                        }
                    }
            }
        }

        internal bool Refresh()
        {
            var result = _function != null;
            _function = null;
            return result;
        }

        public override Expression CloneCore()
        {
            throw new NotImplementedException();
        }

        public override Expression CloneExpression()
        {
            return new FunctionCall(_operand.CloneExpression());
        }

        bool IInstructionExpression.RightArgumentNeeded
        {
            get { return false; }
        }

        public string Name
        {
            get
            {
                return _name.Value;
            }
        }

        public TupleValueMatch Args { get; private set; } = null;

        public override void SetOperands(IEnumerable<Expression> operands)
        {
            base.SetOperands(operands);
            Init();
        }

        private void Init()
        {
            if (!(_operand is TupleValueMatch args))
            {
                _name = _operand as StringValueMatch;
                Args = new TupleValueMatch();
            }
            else
            {
                _name = args._operands[0] as StringValueMatch;
                Args = new TupleValueMatch(args._operands.Skip(1).ToArray());
            }
        }
    }
}
