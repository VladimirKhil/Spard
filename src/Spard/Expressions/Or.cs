using System.Collections.Generic;
using Spard.Sources;
using Spard.Common;
using System.Linq;
using Spard.Transitions;
using Spard.Core;

namespace Spard.Expressions
{
    /// <summary>
    /// OR operation
    /// </summary>
    public sealed class Or: Polynomial
    {
        private IContext _initContext = null;
        private int _index = 0;

        protected internal override Priorities Priority
        {
            get { return Priorities.Or; }
        }

        protected internal override string Sign
        {
            get { return "|"; }
        }

        public Or()
        {

        }

        public Or(params Expression[] operands)
            : base(operands)
        {

        }

        internal override bool MatchCore(ISource input, ref IContext context, bool next)
        {
            IContext workingContext;
            if (!next)
            {
                _initContext = context; // If the match is successful, we will still rewrite the context. Otherwise the value of initContext doesn’t bother us much
                workingContext = context.Clone();
                _index = 0;
            }
            else
            {
                workingContext = _initContext;
            }
            
            while (_index < _operands.Length)
            {
                if (_operands[_index].Match(input, ref workingContext, next))
                {
                    context = workingContext;
                    return true;
                }

                _index++;
                next = false;
            }

            return false;
        }

        internal override object Apply(IContext context)
        {
            if (_operands.Length == 0)
                return null;

            for (int i = 0; i < _operands.Length; i++)
            {
                var result = _operands[i].Apply(context);
                if (result != BindingManager.NullValue)
                    return result;
            }

            return null;
        }

        internal override TransitionTable BuildTransitionTableCore(TransitionSettings settings, bool isLast)
        {
            var tables = this._operands.Select(expr => expr.BuildTransitionTable(settings, isLast));
            var table = TransitionTable.Join(tables.ToArray());

            var result = new TransitionTable();

            foreach (var row in table)
            {
                if (row.Value.Count == 1)
                {
                    result[row.Key] = row.Value;
                    continue;
                }

                var collection = new TransitionTableResultCollection();
                var operands = new List<Expression>();

                foreach (var item in row.Value)
                {
                    if (!item.IsFinished)
                    {
                        operands.Add(item.Expression);
                    }
                    else
                    {
                        if (operands.Count > 1)
                        {
                            collection.Add(new TransitionTableResult(new Or(operands.ToArray())));
                        }
                        else if (operands.Count > 0)
                        {
                            collection.Add(new TransitionTableResult(operands[0]));
                        }

                        operands.Clear();

                        collection.Add(item);
                    }
                }

                if (operands.Count > 1)
                {
                    collection.Add(new TransitionTableResult(new Or(operands.ToArray())));
                }
                else if (operands.Count > 0)
                {
                    collection.Add(new TransitionTableResult(operands[0]));
                }

                result[row.Key] = collection;
            }

            return result;
        }

        public override bool Equals(Expression other)
        {
            if (this == other)
                return true;

            if (!(other is Or or))
                return false;

            var length = _operands.Length;

            if (length != or._operands.Length)
                return false;

            for (int i = 0; i < length; i++)
            {
                if (!or._operands[i].Equals(_operands[i]))
                    return false;
            }

            return true;
        }

        public override Expression CloneCore()
        {
            return new Or();
        }
    }
}
