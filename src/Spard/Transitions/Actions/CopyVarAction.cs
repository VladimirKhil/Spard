using System.Collections;
using System.Collections.Generic;

namespace Spard.Transitions
{
    /// <summary>
    /// Copying value from one variable to another
    /// </summary>
    internal sealed class CopyVarAction : ContextAction
    {
        /// <summary>
        /// Source variable name
        /// </summary>
        internal string SourceName { get; }
        /// <summary>
        /// Target variable name
        /// </summary>
        internal string TargetName { get; }

        public CopyVarAction(int depth, string sourceName, string targetName)
            : base(depth)
        {
            SourceName = sourceName;
            TargetName = targetName;
        }

        internal override IEnumerable Do(object item, ref TransitionContext context)
        {
            var sourceIndex = context.Results.Count - Depth;
            var sourceDict = context.GetVarsByIndex(sourceIndex);

            if (sourceDict.TryGetValue(SourceName, out IList<object> value))
            {
                sourceDict[TargetName] = new List<object>(value);
            }

            return null;
        }

        public override bool Equals(TransitionAction other)
        {
            if (!(other is CopyVarAction other2))
                return false;

            return Depth == other2.Depth && SourceName == other2.SourceName && TargetName == other2.TargetName;
        }

        public override int GetHashCode()
        {
            return Depth.GetHashCode() * 31 * 31 + SourceName.GetHashCode() * 31 + TargetName.GetHashCode();
        }

        public override string ToString()
        {
            return string.Format("c{0},{1},{2}", Depth, SourceName, TargetName);
        }
    }
}
