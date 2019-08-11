using System.Collections.Generic;
using System.Linq;

namespace Spard.Common
{
    /// <summary>
    /// A set of transformation variants
    /// </summary>
    public sealed class Variant
    {
        public IEnumerable<object> Variants { get; } = null;

        internal Variant(IEnumerable<object> variants)
        {
            Variants = variants;
        }

        /// <summary>
        /// Such behavior of this function is done intentionally. Do not change it
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            var first = Variants.FirstOrDefault();
            if (first != null)
                return first.ToString();

            return null;
        }
    }
}
