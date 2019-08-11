using System.Collections;
using System.Threading;

namespace Spard.Core
{
    internal interface ITransformFunction
    {
        IEnumerable TransformCoreAll(object[] args, CancellationToken cancellationToken);
    }
}
