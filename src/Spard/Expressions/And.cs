using System.Collections.Generic;
using System.Linq;
using Spard.Common;
using Spard.Sources;
using Spard.Transitions;
using Spard.Core;

namespace Spard.Expressions
{
    /// <summary>
    /// AND operation
    /// </summary>
    public sealed class And : Polynomial
    {
        private IContext _initContext = null;
        private int _index = 0;
        private int _end = 0;

        protected internal override Priorities Priority
        {
            get { return Priorities.And; }
        }

        protected internal override string Sign
        {
            get { return "&"; }
        }

        public And()
        {

        }

        public And(params Expression[] operands)
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
                _index = _operands.Length - 1;
            }

            var initStart = input.Position;

            Stack<int> preferredLength;
            if (workingContext.Vars.TryGetValue("preferredLength", out object val))
            {
                preferredLength = (Stack<int>)val;
            }
            else
            {
                preferredLength = new Stack<int>();
                workingContext.Vars["preferredLength"] = preferredLength;
            }

            while (_index > -1 && _index < _operands.Length)
            {
                input.Position = initStart;

                if (_index > 0)
                    preferredLength.Push(_end - initStart);

                if (_operands[_index].Match(input, ref workingContext, next))
                {
                    if (_index > 0)
                        preferredLength.Pop();

                    /*if (this.operands[index] is Not)
                        ;
                    else */
                    if (_index == 0)
                    {
                        _end = input.Position;
                    }
                    else if (_end != input.Position) // For the AND operation, all internal predicates must be executed on a sequence of the same length
                    {
                        next = true;
                        continue;
                    }
                    _index++;
                    next = false;
                }
                else
                {
                    if (_index > 0)
                        preferredLength.Pop();

                    _index--;
                    next = true;
                    //if (index == 0)
                    //    this.end = -1;
                }
            }

            if (_index != -1)
            {
                context = workingContext;
                input.Position = _end;
                return true;
            }
            
            return false;
        }

        internal override TransitionTable BuildTransitionTableCore(TransitionSettings settings, bool isLast)
        {
            var tables = _operands.Select(expr => expr.BuildTransitionTable(settings, isLast));
            return TransitionTable.Intersect(tables.ToArray());
        }

        public override Expression CloneCore()
        {
            return new And();
        }
    }
}
