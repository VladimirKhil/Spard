using Spard.Core;
using System;

namespace Spard.Common
{
    /// <summary>
    /// Transformer configuration information
    /// </summary>
    internal class SimpleTransformState: IEquatable<SimpleTransformState>
    {
        /// <summary>
        /// Input position
        /// </summary>
        public int Position { get; } = 0;
        /// <summary>
        /// Transformation context
        /// </summary>
        public IContext Context { get; } = null;

        public SimpleTransformState(int position, IContext context)
        {
            Position = position;
            Context = context.Clone();
        }

        public bool Equals(SimpleTransformState other)
        {
            return Position == other.Position && Context.Equals(other.Context);
        }

        public override bool Equals(object obj)
        {
            return obj is SimpleTransformState state && Equals(state);
        }

        public override int GetHashCode()
        {
            return Position;
        }
    }
}
