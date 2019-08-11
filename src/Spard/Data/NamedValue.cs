using System;
using System.IO;
using Spard.Common;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

namespace Spard.Data
{
    /// <summary>
    /// Named object
    /// </summary>
    public sealed class NamedValue
    {
        /// <summary>
        /// Object name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Object value
        /// </summary>
        public object Value { get; set; }

        public NamedValue()
        {

        }

        public NamedValue(string name, object value)
        {
            Name = name;
            Value = value;
        }

        public override string ToString()
        {
            var format = Value is string || Value is char ? "{0}:{1}" : "{0}:{{{1}}}";
			var value = ValueConverter.ConvertToString(Value);

            return string.Format(format, ValueConverter.Escape(Name), value);
        }
    }
}
