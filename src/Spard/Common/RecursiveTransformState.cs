using Spard.Core;
using System;

namespace Spard.Common
{
    internal sealed class RecursiveTransformState: SimpleTransformState, IComparable<RecursiveTransformState>
    {
        internal int[] Derivation { get; }

        public RecursiveTransformState(int position, IContext context, int[] derivation)
            : base(position, context)
        {
            Derivation = derivation;
        }

        public RecursiveTransformState(int position, IContext context, int index)
            : base(position, context)
        {
            Derivation = new int[] { index };
        }

        public int CompareTo(RecursiveTransformState other)
        {
            var length = Math.Min(Derivation.Length, other.Derivation.Length);
            for (int i = 0; i < length; i++)
            {
                if (Derivation[i] < other.Derivation[i])
                    return -1;

                if (Derivation[i] > other.Derivation[i])
                    return 1;
            }

            return Derivation.Length.CompareTo(other.Derivation.Length); // Unreachable in fact
        }
    }
}
