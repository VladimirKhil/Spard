using System.ComponentModel;
using Spard.Core;

namespace Spard.Expressions
{
    /// <summary>
    /// Describes operation which has direction.
    /// </summary>
    /// <inheritdoc cref="Binary" />
    public abstract class Directed: Binary
    {
        /// <summary>
        /// Operation direction.
        /// </summary>
        [DefaultValue(Directions.Right)]
        public Directions Direction { get; set; }

        /// <summary>
        /// Initializes a new instance of <see cref="Directed" /> class.
        /// </summary>
        protected Directed() => Direction = Directions.Right;

        /// <summary>
        /// Initializes a new instance of <see cref="Directed" /> class.
        /// </summary>
        /// <param name="direction">Expression direction.</param>
        internal Directed(Directions direction) => Direction = direction;

        /// <summary>
        /// Initializes a new instance of <see cref="Directed" /> class.
        /// </summary>
        /// <param name="left">Left function argument.</param>
        /// <param name="right">Right function argument.</param>
        protected Directed(Expression left, Expression right)
            : base(left, right) => Direction = Directions.Right;
    }
}
