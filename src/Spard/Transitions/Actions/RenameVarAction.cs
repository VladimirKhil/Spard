using System.Collections;
using System.Collections.Generic;

namespace Spard.Transitions
{
    /// <summary>
    /// Variable renaming. It is performed at all levels of context
    /// </summary>
    internal sealed class RenameVarAction: TransitionAction
    {
        /// <summary>
        /// Source variable name
        /// </summary>
        internal string SourceName { get; }
        /// <summary>
        /// New variable name
        /// </summary>
        internal string TargetName { get; }

        public RenameVarAction(string sourceName, string targetName)
        {
            SourceName = sourceName;
            TargetName = targetName;
        }

        internal override IEnumerable Do(object item, ref TransitionContext context)
        {
            Rename(context.Vars);
            foreach (var result in context.Results)
            {
                Rename(result.Vars);
            }

            return null;
        }

        private void Rename(Dictionary<string, IList<object>> dict)
        {
            if (dict.TryGetValue(SourceName, out IList<object> value))
            {
                //if (dict.ContainsKey(targetName))
                //    throw new Exception();

                dict[TargetName] = value;
                dict.Remove(SourceName);
            }
        }

        public override bool Equals(TransitionAction other)
        {
            if (!(other is RenameVarAction other2))
                return false;

            return SourceName == other2.SourceName && TargetName == other2.TargetName;
        }

        public override string ToString()
        {
            return string.Format("n{0},{1}", SourceName, TargetName);
        }
    }
}
