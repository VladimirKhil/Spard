using System;
using System.Collections.Generic;
using System.Linq;
using Spard.Expressions;

namespace Spard.Transitions
{
    /// <summary>
    /// Collection of alternate transition results.
    /// Is is linked to some state
    /// </summary>
    internal sealed class TransitionTableResultCollection: List<TransitionTableResult>, IEquatable<TransitionTableResultCollection>
    {
        /// <summary>
        /// Empty results collection
        /// </summary>
        internal static TransitionTableResultCollection Empty = new TransitionTableResultCollection();

        /// <summary>
        /// Names of all variables used in collection expressions
        /// </summary>
        internal string[] UsedVars { get; set; }

        static TransitionTableResultCollection()
        {
            Empty.Add(TransitionTableResult.Empty);
        }

        public TransitionTableResultCollection()
        {

        }

        public TransitionTableResultCollection(IEnumerable<TransitionTableResult> collection)
            : base (collection)
        {

        }

        /// <summary>
        /// Create results collection from expression
        /// </summary>
        /// <param name="expression"></param>
        /// <param name="isResult"></param>
        /// <returns></returns>
        internal static TransitionTableResultCollection Create(Expression expression, bool isResult = false) =>
            new TransitionTableResultCollection
            {
                new TransitionTableResult(expression, isResult)
            };

        /// <summary>
        /// Create results collection from expressions set
        /// </summary>
        /// <param name="expressions"></param>
        /// <param name="isResult"></param>
        /// <returns></returns>
        internal static TransitionTableResultCollection Create(IEnumerable<Expression> expressions, bool isResult = false)
        {
            var collection = new TransitionTableResultCollection();
            collection.AddRange(expressions.Select(expr => new TransitionTableResult(expr, isResult)));
            return collection;
        }

        public override string ToString() => string.Join(", ", this);

        public override bool Equals(object obj)
        {
            if (obj is TransitionTableResultCollection other)
                return Equals(other);

            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            var hash = Count * 31;
            foreach (var item in this)
            {
                hash = hash * 31 + item.GetHashCode();
            }

            return hash;
        }

        internal TransitionTableResultCollection CloneCollection()
        {
            var result = new TransitionTableResultCollection { UsedVars = UsedVars };
            foreach (var item in this)
            {
                result.Add(item.CloneResult());
            }

            return result;
        }

        public bool Equals(TransitionTableResultCollection other)
        {
            var length = Count;

            if (length != other.Count)
                return false;

            if (UsedVars.Length != other.UsedVars.Length)
                return false;

            var varsMap = new Dictionary<string, string>();
            for (int i = 0; i < UsedVars.Length; i++)
            {
                varsMap[UsedVars[i]] = other.UsedVars[i];
            }

            for (int i = 0; i < length; i++)
            {
                if (!this[i].EqualsSmart(other[i], varsMap))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Fill in the names of all the variables used in the expression
        /// </summary>
        /// <param name="collection"></param>
        internal void CalculateUsedVars()
        {
            foreach (var record in this)
            {
                record.CalculateUsedVars();
            }

            SetUsedVars();
        }

        /// <summary>
        /// Set a list of all variables used in the collection
        /// </summary>
        internal void SetUsedVars()
        {
            var allUsedVars = new List<string>();
            foreach (var record in this)
            {
                if (record.UsedVars != null)
                    allUsedVars.AddRange(record.UsedVars);
            }

            UsedVars = allUsedVars.Distinct().ToArray();
        }
    }
}
