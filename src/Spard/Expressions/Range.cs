using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Spard.Sources;
using Spard.Core;
using Spard.Transitions;

namespace Spard.Expressions
{
    /// <summary>
    /// The interval that sets the limits for the assumption of elements
    /// </summary>
    public sealed class Range: Binary
    {
        private IContext _initContext = null;

        private string _lower = null;
        private string _upper = null;

        private int _lowerNumber = 0;
        private int _upperNumber = 0;

        private List<int> _matches = new List<int>();
        private int _count = 0;

        protected internal override Priorities Priority
        {
            get { return Priorities.Range; }
        }

        protected internal override string Sign
        {
            get { return "-"; }
        }

        public Range()
        {

        }

        public Range(Expression left, Expression right) : base(left, right)
        {

        }

        internal override bool MatchCore(ISource input, ref IContext context, bool next)
        {
            IContext workingContext = null;
            var initStart = input.Position;
            if (!next)
            {
                _initContext = context; // If the match is successful, we will still rewrite the context. Otherwise the value of initContext doesn’t bother us much
                workingContext = context.Clone();                

                _lower = _left?.Apply(context).ToString();
                _upper = _right?.Apply(context).ToString();

                if (_lower == null && _upper == null)
                    return false;

                _count = 0;
                _matches.Clear();

                if ((_lower == null || int.TryParse(_lower, out _lowerNumber)) && 
                    (_upper == null || int.TryParse(_upper, out _upperNumber)))
                {
                    // Numbers
                    var line = new StringBuilder();
                    int lineNumber = 0;
                    var lowerBoundReached = _lower == null;
                    while (!input.EndOfSource)
                    {
                        var newLine = input.Read().ToString();
                        if (newLine.Any(sym => !Char.IsDigit(sym)))
                            break;

                        line.Append(newLine);
                        if (!int.TryParse(line.ToString(), out lineNumber))
                            break;

                        if (!lowerBoundReached && lineNumber >= _lowerNumber)
                            lowerBoundReached = true;

                        if (lowerBoundReached)
                        {
                            if (_upper != null && lineNumber > _upperNumber)
                                break;

                            _matches.Add(input.Position);
                        }
                    }
                }
                else
                {
                    // Strings
                    var line = new StringBuilder();
                    var lowerBoundReached = _lower == null;
                    var maxLength = Math.Max(_lower == null ? 0 : _lower.Length, _upper == null ? 0 : _upper.Length);

                    var ignoreSP = workingContext.GetParameter(Parameters.IgnoreSP);
                    while (!input.EndOfSource)
                    {
                        var c = input.Read();
                        var isSpace = workingContext.IsIgnoredItem(c);
                        if (ignoreSP && isSpace)
                            continue;

                        line.Append(c);

                        if (!lowerBoundReached && (string.Compare(line.ToString(), _lower, StringComparison.Ordinal) > -1))
                            lowerBoundReached = true;

                        if (lowerBoundReached)
                        {
                            if (_upper != null && string.Compare(line.ToString(), _upper, StringComparison.Ordinal) > 0)
                                break;

                            _matches.Add(input.Position);
                        }

                        if (line.Length >= maxLength)
                            break;
                    }
                }

                if (!context.GetParameter(Parameters.IsLazy))
                    _matches.Reverse();
            }
            else
            {
                if (_lower == null && _upper == null)
                    return false;

                workingContext = _initContext;
            }

            if (_count < _matches.Count)
            {
                context = workingContext;
                input.Position = _matches[_count];
                _count++;
            }
            else
            {
                input.Position = initStart;
                return false;
            }

            return true;
        }

        internal override object Apply(IContext context)
        {
            throw new NotImplementedException();
        }

        public override Expression CloneCore()
        {
            return new Range();
        }

        internal override TransitionTable BuildTransitionTableCore(TransitionSettings settings, bool isLast)
        {
            var lower = _left != null ? ((StringValueMatch)_left).Value : null;
            var upper = _right != null ? ((StringValueMatch)_right).Value : null;

            var leftChar = lower[0];
            var rightChar = upper[0];

            var result = new TransitionTable
            {
                [new InputSet(InputSetType.Include, new ValuesRange(leftChar, rightChar))] = TransitionTableResultCollection.Empty.CloneCollection()
            };

            return result;
        }
    }
}
