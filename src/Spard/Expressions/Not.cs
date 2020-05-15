using Spard.Core;
using Spard.Sources;
using Spard.Transitions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Spard.Expressions
{
    /// <summary>
    /// NOT operation
    /// </summary>
    public sealed class Not: Unary
    {
        private int _length = 0;

        protected internal override Relationship OperandPosition
        {
            get { return Relationship.Right; }
        }

        protected internal override Relationship Assotiative
        {
            get
            {
                return Relationship.Right;
            }
        }

        protected internal override Priorities Priority
        {
            get { return Priorities.Not; }
        }

        protected internal override string Sign
        {
            get { return "!"; }
        }

        public Not()
        {

        }

        public Not(Expression operand)
            : base(operand)
        {

        }

        /// <remarks>If the template is successfully compared, the context is not unified</remarks>
        internal override bool MatchCore(ISource input, ref IContext context, bool next)
        {
            int preferredLength = -1;
            if (context.Vars.TryGetValue("preferredLength", out object val))
            {
                preferredLength = ((Stack<int>)val).Peek();
            }

            var initStart = input.Position;

            if (next)
            {
                if (preferredLength > -1 && _length > preferredLength)
                    return false;

                while (!input.EndOfSource && input.Position < initStart + _length)
                    input.Read();

                if (input.Position < initStart + _length)
                {
                    input.Position = initStart;
                    return false;
                }

                _length++;
                return true;
            }
            
            var workingContext = context.Clone();
            bool match = !_operand.Match(input, ref workingContext, false);

            _length = preferredLength > -1 ? preferredLength : 0;

            if (!match)
            {
                input.Position = initStart;
                return false;
            }
            else
                input.Position += _length;

            _length++;
            return true;
        }

        internal override object Apply(IContext context)
        {
            throw new NotImplementedException();
        }

        internal override TransitionTable BuildTransitionTableCore(TransitionSettings settings, bool isLast)
        {
            var table = _operand.BuildTransitionTable(settings, false);
            var result = new TransitionTable
            {
                // Zero shift is possible at first
                // BUT: with the condition that subsequent nodes check our condition
                [InputSet.Zero] = TransitionTableResultCollection.Create(_operand is Not not ? not._operand : new Not(_operand))
            };

            // Shifts by one character are possible futher
            var negativeKey = new InputSet(InputSetType.Exclude);
            foreach (var item in table)
            {
                if (item.Key.Type == InputSetType.Zero)
                {
                    if (!item.Value.Any(ttr => ttr.IsFinished))
                    {
                        // Long Not
                        var collection = new TransitionTableResultCollection();

                        foreach (var res in item.Value)
                        {
                            if (res.Expression is Not not2)
                                collection.Add(new TransitionTableResult(not2._operand));
                            else
                            {
                                if (!(res.Expression is MultiTime multiTime) || !multiTime.reversed)
                                    collection.Add(new TransitionTableResult(new Not(res.Expression)));
                            }
                        }

                        if (collection.Any())
                        {
                            result[item.Key] = collection;
                        }
                    }

                    continue;
                }

                if (!item.Value.Any(ttr => ttr.IsFinished))
                {
                    // Long Not
                    var collection = new TransitionTableResultCollection();

                    foreach (var res in item.Value)
                    {
                        if (!(res.Expression is MultiTime multiTime) || !multiTime.reversed)
                            collection.Add(new TransitionTableResult(new Not(res.Expression)));
                    }

                    if (collection.Any())
                    {
                        result[item.Key] = collection;
                    }
                }

                negativeKey = negativeKey.Except(item.Key);
            }

            if (!negativeKey.IsEmpty)
            {
                // There are no limits here, you can move as far as you want
                result[negativeKey] = isLast ? TransitionTableResultCollection.Empty.CloneCollection() : TransitionTableResultCollection.Create(new MultiTime(Any.Instance) { reversed = true });
            }

            return result;
        }

        public override Expression CloneCore()
        {
            return new Not();
        }
    }
}
