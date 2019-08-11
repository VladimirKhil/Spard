using Spard.Common;
using Spard.Sources;
using Spard.Core;
using Spard.Transitions;

namespace Spard.Expressions
{
    public sealed class TypeDefinition : Binary, IInstructionExpression
    {
        public bool RightArgumentNeeded
        {
            get
            {
                return false;
            }
        }

        protected internal override Priorities Priority
        {
            get
            {
                return Priorities.TypeDefinition;
            }
        }

        protected internal override string Sign
        {
            get
            {
                return "::";
            }
        }

        public override Expression CloneCore()
        {
            return new TypeDefinition();
        }

        internal override object Apply(IContext context)
        {
            return null;
        }

        internal override bool MatchCore(ISource input, ref IContext context, bool next)
        {
            if (next)
                return false;

            var name = ((Query)_left).Name;
            context.DefinitionsTable[name] = _right;
            return true;
        }

        internal override TransitionTable BuildTransitionTableCore(TransitionSettings settings, bool isLast)
        {
            // Here we will need to replace all the 'Query's in the expression with the InlineTypeDefinition with the required type

            return base.BuildTransitionTableCore(settings, isLast);
        }
    }
}
