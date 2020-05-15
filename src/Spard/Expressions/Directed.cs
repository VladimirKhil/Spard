using System.ComponentModel;
using Spard.Core;

namespace Spard.Expressions
{
    /// <summary>
    /// Operation which has direction
    /// </summary>
    public abstract class Directed: Binary
    {
        /// <summary>
        /// Operation direction
        /// </summary>
        [DefaultValue(Directions.Right)]
        public Directions Direction { get; set; }

        protected Directed()
        {
            Direction = Directions.Right;
        }

        internal Directed(Directions direction)
        {
            Direction = direction;
        }

        protected Directed(Expression left, Expression right)
            : base(left, right)
        {
            Direction = Directions.Right;
        }
    }
}
