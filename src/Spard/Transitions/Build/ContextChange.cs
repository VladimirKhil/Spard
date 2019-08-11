using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spard.Transitions
{
    /// <summary>
    /// Changes happen to context
    /// </summary>
    internal sealed class ContextChange: Tuple<string, object>
    {
        /// <summary>
        /// Creates changes
        /// </summary>
        /// <param name="name">Context variable name</param>
        /// <param name="value">Value to append (or null if current input item is append)</param>
        public ContextChange(string name, object value)
            : base(name, value)
        {

        }
    }
}
