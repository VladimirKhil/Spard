using System;
using System.Collections;
using System.Collections.Generic;

namespace Spard.Transitions
{
    /// <summary>
    /// Appending value to a variable
    /// </summary>
    internal sealed class AppendVarAction : ContextAction, IEquatable<AppendVarAction>
    {
        /// <summary>
        /// Variable name
        /// </summary>
        internal string Name { get; }

        /// <summary>
        /// Value to append (if null, current input is appended)
        /// </summary>
        internal object Item { get; }

        public AppendVarAction(int depth, string name, object item)
            : base(depth)
        {
            Name = name;
            Item = item;
        }

        internal override IEnumerable Do(object item, ref TransitionContext context)
        {
            var sourceIndex = context.Results.Count - Depth;
            var sourceDict = context.GetVarsByIndex(sourceIndex);

            if (!sourceDict.TryGetValue(Name, out IList<object> var))
            {
                sourceDict[Name] = var = new List<object>();
            }

            // Appending value
            var.Add(Item ?? item);

            return null;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is AppendVarAction other))
                return base.Equals(obj);

            return Equals(other);
        }

        public override bool Equals(TransitionAction other)
        {
            if (!(other is AppendVarAction other2))
                return false;

            return Equals(other2);
        }

        public bool Equals(AppendVarAction other)
        {
            return Depth == other.Depth && Item == other.Item && Name == other.Name;
        }

        public override int GetHashCode()
        {
            return Item.GetHashCode() * 31 + Depth.GetHashCode();
        }

        internal bool EqualsByName(AppendVarAction other)
        {
            return Depth == other.Depth && Name == other.Name;
        }

        public override string ToString()
        {
            return string.Format("a{0},{1},{2}", Depth, Name, Item);
        }
    }
}
