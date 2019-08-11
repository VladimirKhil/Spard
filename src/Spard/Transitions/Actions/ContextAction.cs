namespace Spard.Transitions
{
    /// <summary>
    /// Action associated with changing the transformation context
    /// </summary>
    internal abstract class ContextAction: TransitionAction
    {
        /// <summary>
        /// Depth in context (counted from the end) at which the action is performed
        /// </summary>
        internal int Depth { get; private set; }

        public ContextAction(int depth)
        {
            Depth = depth;
        }

        internal void IncreaseDepth()
        {
            Depth++;
        }

        internal void DecreaseDepth()
        {
            Depth--;
        }
    }
}
