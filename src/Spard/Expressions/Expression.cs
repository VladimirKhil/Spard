using Spard.Common;
using Spard.Core;
using Spard.Sources;
using Spard.Transitions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Spard.Expressions
{
    /// <summary>
    /// SPARD Expression
    /// </summary>
    public abstract class Expression: IEquatable<Expression>
    {
        /// <summary>
        /// Max depth of expressions call stack
        /// </summary>
        private const int MaxStackDepth = 500;

        /// <summary>
        /// Expression operands
        /// </summary>
        public abstract IEnumerable<Expression> Operands();

        /// <summary>
        /// Replace current operands with new values
        /// </summary>
        /// <param name="operands">New expression operands</param>
        public abstract void SetOperands(IEnumerable<Expression> operands);

        /// <summary>
        /// Expression priority
        /// </summary>
        protected internal abstract Priorities Priority { get; }

        /// <summary>
        /// Expression sign
        /// </summary>
        protected internal abstract string Sign { get; }

        /// <summary>
        /// Associativity of expression:
        /// left if a.b.c == (a.b).c;
        /// right if a.b.c == a.(b.c)
        /// </summary>
        protected internal virtual Relationship Assotiative
        {
            get { return Relationship.Left; }
        }

        /// <summary>
        /// Expression creation
        /// </summary>
        internal Expression()
        {
            
        }

        /// <summary>
        /// Add one of the operands to the string representation of the node
        /// </summary>
        /// <param name="result">Constructed string representation of the node</param>
        /// <param name="operand">Operand to add</param>
        protected internal void AppendOperand(StringBuilder result, Expression operand)
        {
            if (operand != null)
            {
                bool prior = operand.Priority > 0 && operand.Priority <= Priority;
                result.AppendFormat(prior ? "({0})" : "{0}", operand);
            }
        }

        /// <summary>
        /// Check if the input object matches the node template
        /// </summary>
        /// <param name="input">Input object. If next = false, then the data source is already set to the required position</param>
        /// <param name="context">Matching context</param>
        /// <param name="next">If true, return next result; Otherwise return first result</param>
        /// <returns>Wherther the input object matches the node template</returns>
        /// <remarks>Matching conventions:
        /// 1. In the case of a successful match, the context must contain a new (!) Context of the successful match, and the input must be in the position after the last character used in the match.
        /// 2. In case of unsuccessful match, the context value should remain the same as it was before the function was called, and input should not change its position</remarks>
        internal bool Match(ISource input, ref IContext context, bool next)
        {
            if (context.Runtime.StackTrace.Count > MaxStackDepth)
                throw new Exception("Maximum number of nested expression calls exceeded! Infinite recursion is possible.");

            if (context.Runtime.CancellationToken.IsCancellationRequested)
                return false;

            context.Runtime.StackTrace.Push(new StackFrame(input.Position, this));
            
            var match = MatchCore(input, ref context, next);

            // Matches are created only on leaves; the rest just pass them up
            // StringValue executes this code by itself
            if (!match && context.SearchBestVariant)
            {
                var leaf = this is Primitive && !(this is StringValueMatch);
                if (!leaf)
                {
                    leaf = this is Set set && set._definition == null;
                }

                if (leaf)
                {
                    if (context.Vars.TryGetValue(Context.MatchKey, out object m))
                    {
                        context.Runtime.SaveBestTry(new MatchInfo(input.Position, context.Runtime.StackTrace.ToArray(), match: m));
                    }
                }
            }

            context.Runtime.StackTrace.Pop();

            return match;
        }

        /// <summary>
        /// Check if the input object matches the node template
        /// </summary>
        /// <param name="input">Input object. If next = false, then the data source is already set to the required position</param>
        /// <param name="context">Matching context</param>
        /// <param name="next">If true, return next result; Otherwise return first result</param>
        /// <returns>Wherther the input object matches the node template</returns>
        /// <remarks>Matching conventions:
        /// 1. In the case of a successful match, the context must contain a new (!) Context of the successful match, and the input must be in the position after the last character used in the match.
        /// 2. In case of unsuccessful match, the context value should remain the same as it was before the function was called, and input should not change its position</remarks>
        internal abstract bool MatchCore(ISource input, ref IContext context, bool next);

        /// <summary>
        /// Get the output object by applying context to the node template
        /// </summary>
        /// <param name="context">Applicable context. If you want to get the next output object in the same context, this parameter is null</param>
        /// <returns>Output object</returns>
        /// <remarks>Output data types:
        /// null
        /// char
        /// IEnumerable
        /// NamedValue
        /// EnumerableValue
        /// TupleValue
        /// </remarks>
        internal abstract object Apply(IContext context);

        public override int GetHashCode()
        {
            var hash = Sign.GetHashCode();

            foreach (var item in Operands())
            {
                hash *= 31;
                if (item != null)
                    hash += item.GetHashCode();
            }

            return hash;
        }

        /// <summary>
        /// Build transiton table: [input element => resuls set]
        /// </summary>
        /// <param name="settings">Advanced build options</param>
        /// <param name="isLast">Is the current expression last in the template</param>
        /// <returns>Formed transiton table</returns>
        internal TransitionTable BuildTransitionTable(TransitionSettings settings, bool isLast)
        {
            TransitionTable table/*;
            if (!settings.TransitionsCache.TryGetValue(this, out table))
                settings.TransitionsCache[this] = table*/ = BuildTransitionTableCore(settings, isLast);

            return table;
        }

        /// <summary>
        /// Build a transition table for this expression
        /// </summary>
        /// <returns>A transition table in which an expression is specified for each condition on the input element to check the remaining part of the input.
        /// The table allows you to check the partial match of the input on one element</returns>
        internal virtual TransitionTable BuildTransitionTableCore(TransitionSettings settings, bool isLast)
        {
            // This methos is virtual temporarily. It would be abstract later
            throw new NotImplementedException("The implementation of the table transformer for this expression will be added later!");
        }

        /// <summary>
        /// Clone the whole expression (with subnodes)
        /// </summary>
        /// <returns>Expression clone</returns>
        public abstract Expression CloneExpression();

        /// <summary>
        /// Clone the node of this expression
        /// </summary>
        /// <returns>Expression node clone</returns>
        public abstract Expression CloneCore();

        public override bool Equals(object obj)
        {
            if (obj is Expression other)
                return Equals(other);

            return base.Equals(obj);
        }

        public virtual bool Equals(Expression other)
        {
            return GetType() == other.GetType() && Operands().SequenceEqual(other.Operands());
        }

        internal virtual bool EqualsSmart(Expression other, Dictionary<string, string> varsMap)
        {
            if (GetType() != other.GetType())
                return false;

            var thisOperands = Operands().ToArray();
            var otherOperands = other.Operands().ToArray();

            if (thisOperands.Length != otherOperands.Length)
                return false;

            for (int i = 0; i < thisOperands.Length; i++)
            {
                if (!thisOperands[i].EqualsSmart(otherOperands[i], varsMap))
                    return false;
            }

            return true;
        }
    }
}
