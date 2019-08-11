using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spard.Transitions
{
    internal sealed class ValuesList : List<object>, IValues
    {
        public ValuesList(IEnumerable<object> collection) : base(collection)
        {
        }
    }
}
