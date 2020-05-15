using System;
using Spard.Sources;
using Spard.Common;
using System.Linq;
using Spard.Data;
using Spard.Core;

namespace Spard.Expressions
{
    /// <summary>
    /// A tuple value
    /// </summary>
    public sealed class TupleValueMatch: Polynomial, IInstructionExpression
    {
        private IContext _initContext = null;
        private int _index = 0;
        
        private ISource[] _sources;
        private int[] _initialPositions;

        private int[] _matchedIndicies;

        protected internal override Priorities Priority
        {
            get { return Priorities.TupleValue; }
        }

        protected internal override string Sign
        {
            get { return " "; }
        }

        public TupleValueMatch(params Expression[] operands)
            : base (operands)
        {

        }

        internal override bool MatchCore(ISource input, ref IContext context, bool next)
        {
            IContext workingContext = null;
            if (!next)
            {
                if (input.EndOfSource)
                    return false;

                _initContext = context; // If the match is successful, we will still rewrite the context. Otherwise the value of initContext doesn’t bother us much
                workingContext = context.Clone();
                _index = 0;

                var source = input.Read();
                if (source is TupleValue tupleValue && tupleValue.Items.Length >= _operands.Length)
                {
                    _sources = tupleValue.Items.Select(item => ValueConverter.ConvertToSource(ValueConverter.ConvertToEnumerable(item))).ToArray();
                }
                else
                {
                    if (source is TupleSource tupleSource && tupleSource.Sources.Length == _operands.Length)
                    {
                        _sources = tupleSource.Sources;
                    }
                    else
                    {
                        input.Position--;
                        return false;
                    }
                }

                _initialPositions = _sources.Select(s => s.Position).ToArray();
                _matchedIndicies = Enumerable.Repeat(-1, _sources.Length).ToArray();
            }
            else
            {
                workingContext = _initContext;
                _index = _operands.Length - 1;
            }

            var maxIndex = _index;

            while (-1 < _index && _index < _operands.Length)
            {
                if (next)
                    _matchedIndicies[_index] = -1;

                var sourceIndex = 0;

                while (_matchedIndicies.Contains(sourceIndex))
                {
                    sourceIndex++;
                }

                var more = false;

                do
                {
                    more = false;

                    next = !_operands[_index].Match(_sources[sourceIndex], ref workingContext, next);
                    if (next) // Does not match
                    {
                        if (_operands[_index] is NamedValueMatch)
                        {
                            // A special case; it is allowed to match the source on another index
                            sourceIndex++;
                            while (_matchedIndicies.Contains(sourceIndex))
                            {
                                sourceIndex++;
                            }

                            if (sourceIndex < _sources.Length)
                            {
                                more = true;
                                next = false;
                                continue;
                            }
                        }

                        _index--;
                    }
                    else
                    {
                        _matchedIndicies[_index] = sourceIndex;

                        _index++;
                        maxIndex = Math.Max(maxIndex, _index);
                    }

                } while (more);
            }

            if (_index != -1)
            {
                context = workingContext;
                return true;
            }

            for (int i = 0; i < maxIndex; i++)
            {
                _sources[i].Position = _initialPositions[i];
            }

            input.Position--;
            return false;
        }

        internal override object Apply(IContext context)
        {
            return new TupleValue { Items = _operands.Select(item => item.Apply(context)).ToArray() };
        }

        public override Expression CloneCore()
        {
            return new TupleValueMatch();
        }

        bool IInstructionExpression.RightArgumentNeeded
        {
            get
            {
                var op = _operands[0].ToString();
                return op == "on" || op == "off" || op == "time";
            }
        }
    }
}
