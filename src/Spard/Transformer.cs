using Spard.Core;
using Spard.Results;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace Spard
{
    /// <summary>
    /// Transformer based on SPARD instructions
    /// </summary>
    public abstract class Transformer: ITransformer
    {
        /// <summary>
        /// Progress change event (progress runs from 0 to 100)
        /// </summary>
        public virtual event Action<int> ProgressChanged;

        protected internal void OnProgressChanged(int progress)
        {
            ProgressChanged?.Invoke(progress);
        }

        /// <summary>
        /// Transform input data stream
        /// </summary>
        /// <param name="input">Input data stream</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Output data stream</returns>
        public abstract IEnumerable<object> Transform(IEnumerable input, CancellationToken cancellationToken = default);

        /// <summary>
        /// Perform multivalue transformation of input data stream
        /// </summary>
        /// <param name="input">Input data stream</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Set of multivalue results of output data</returns>
        public IMultiResult MultiTransform(IEnumerable input, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public abstract IEnumerable<IEnumerable<object>> StepTransform(IEnumerable input, CancellationToken cancellationToken = default);

        /// <summary>
        /// Merge with another transformer using another transformer as a data source
        /// </summary>
        /// <param name="transformer">Another transformer as a data source</param>
        /// <returns>Created chain of transformers as a new transformer</returns>
        public abstract Transformer ChainWith(Transformer transformer);
    }
}
