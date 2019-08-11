using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spard.Transitions
{
    public interface IValues: IEnumerable<object>
    {
        bool Contains(object item);
    }
}
