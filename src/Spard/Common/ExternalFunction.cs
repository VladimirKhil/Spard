using Spard.Core;
using System;
using System.Collections;
using System.Threading;

namespace Spard.Common
{
    /// <summary>
    /// External function
    /// </summary>
    internal sealed class ExternalFunction: ITransformFunction
    {
        private readonly TransformerHelper.UserFunc _func;

        public ExternalFunction(TransformerHelper.UserFunc func)
        {
            _func = func;
        }

        public ITransformFunction Reverse() => new ExternalFunction(_func);

        public IEnumerable TransformCoreAll(object[] args, CancellationToken cancellationToken) => throw new NotImplementedException();
    }
}
