using System;
using System.Collections.Generic;
using Spard.Common;
using Spard.Sources;
using System.Linq;
using Spard.Transitions;
using Spard.Core;
using System.Text;

namespace Spard.Expressions
{
    /// <summary>
    /// Concatenation operation
    /// </summary>
    public sealed class Sequence: Polynomial
    {
        private IContext _initContext = null;
        private int _index = 0;

        private Stack<int> _positions = new Stack<int>();
        
        protected internal override Priorities Priority
        {
            get { return Priorities.Sequence; }
        }

        protected internal override string Sign
        {
            get { return string.Empty; }
        }

        public Sequence()
        {
        }

        public Sequence(params Expression[] operands)
            : base(operands)
        {

        }

        internal override bool MatchCore(ISource input, ref IContext context, bool next)
        {
            IContext workingContext = null;
            
            if (!next)
            {
                _initContext = context; // If the match is successful, we will still rewrite the context. Otherwise the value of initContext doesn’t bother us much
                workingContext = context.Clone();
                _index = 0;

                _positions.Clear();
            }
            else
            {
                workingContext = _initContext;
                _index = operands.Length - 1;

                input.Position = _positions.Pop();
            }

            _positions.Push(input.Position);
            while (-1 < _index && _index < operands.Length)
            {
                next = !operands[_index].Match(input, ref workingContext, next);
                _index += next ? -1 : 1;

                if (next)
                {
                    _positions.Pop();
                    if (_index > -1)
                        input.Position = _positions.Peek();
                }
                else if (_index < operands.Length)
                    _positions.Push(input.Position);
            }

            if (_index != -1)
            {
                context = workingContext;
                return true;
            }

            return false;
        }

        internal override object Apply(IContext context)
        {
            return ValueConverter.ConvertToEnumerable(operands.Select(item => item.Apply(context)));
        }

        internal override TransitionTable BuildTransitionTableCore(TransitionSettings settings, bool isLast)
        {
            var table = operands[0].BuildTransitionTable(settings, false);

            var result = new TransitionTable();

            Sequence tail = null;

            foreach (var item in table)
            {
                if (item.Key.Type == InputSetType.Zero) // Zero offset is processed separately
                {
                    Expression next;
                    if (operands.Length == 2)
                    {
                        next = operands[1];
                    }
                    else
                    {
                        if (tail == null)
                            tail = new Sequence(operands.Skip(1).ToArray());

                        next = tail;
                    }

                    var transitionResult = item.Value[0];
                    if (!transitionResult.IsFinished) // Postcondition
                    {
                        if (next is MultiTime multiTime && multiTime.reversed && operands.Length == 2)
                        {
                            InsertNewItem(result, item.Key, item.Value);
                            continue;
                        }

                        next = new And(next, new Sequence(transitionResult.Expression, new MultiTime(Any.Instance) { reversed = true }));
                    }

                    var nextTable = next.BuildTransitionTable(settings, operands.Length == 2);
                    foreach (var nextItem in nextTable)
                    {
                        InsertNewItem(result, nextItem.Key, nextItem.Value);
                    }
                }
                else
                {
                    var collection = new TransitionTableResultCollection();

                    foreach (var res in item.Value)
                    {
                        if (res.IsFinished)
                        {
                            if (operands.Length == 2)
                            {
                                collection.Add(new TransitionTableResult(operands[1], contextChange: res.ContextChange));
                            }
                            else
                            {
                                if (tail == null)
                                    tail = new Sequence(operands.Skip(1).ToArray());

                                collection.Add(new TransitionTableResult(tail, contextChange: res.ContextChange));
                            }
                        }
                        else
                        {
                            var newOperands = new Expression[operands.Length];
                            newOperands[0] = res.Expression;
                            Array.Copy(operands, 1, newOperands, 1, operands.Length - 1);
                            collection.Add(new TransitionTableResult(new Sequence(newOperands), contextChange: res.ContextChange));
                        }
                    }

                    InsertNewItem(result, item.Key, collection);
                }
            }

            return result;
        }

        private static void InsertNewItem(TransitionTable result, InputSet nextKey, TransitionTableResultCollection collection)
        {
            foreach (var resItem in result.ToArray())
            {
                var cross = nextKey.IntersectAndTwoExcepts(resItem.Key);
                if (cross.Item1.IsEmpty)
                    continue;

                if (!cross.Item3.IsEmpty)
                {
                    result.Remove(resItem.Key);
                    result[cross.Item3] = resItem.Value.CloneCollection();
                    result[cross.Item1] = resItem.Value;
                }

                resItem.Value.AddRange(collection.CloneCollection());

                nextKey = cross.Item2;
            }

            if (!nextKey.IsEmpty)
            {
                result[nextKey] = collection;
            }
        }

        public override bool Equals(Expression other)
        {
            if (!(other is Sequence sequence))
                return false;

            var length = operands.Length;

            if (length != sequence.operands.Length)
                return false;

            for (int i = 0; i < length; i++)
            {
                if (!operands[i].Equals(sequence.operands[i]))
                    return false;
            }

            return true;
        }

        public override Expression CloneCore()
        {
            return new Sequence();
        }

        public override string ToString()
        {
            if (operands == null)
                return GetType().ToString();

            var result = new StringBuilder();

            for (int i = 0; i < operands.Length; i++)
            {
                var putBrackets = operands[i] is Query;

                if (putBrackets)
                    result.Append('(');

                base.AppendOperand(result, operands[i]);

                if (putBrackets)
                    result.Append(')');
            }

            return result.ToString();
        }
    }
}
