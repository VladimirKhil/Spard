using System.Collections.Generic;

namespace Spard.Transitions
{
    public interface IValues: IEnumerable<object>
    {
        bool Contains(object item);
    }
}
