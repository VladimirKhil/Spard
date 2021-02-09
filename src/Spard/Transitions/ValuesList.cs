using System.Collections.Generic;

namespace Spard.Transitions
{
    internal sealed class ValuesList : List<object>, IValues
    {
        public ValuesList(IEnumerable<object> collection) : base(collection)
        {
        }
    }
}
