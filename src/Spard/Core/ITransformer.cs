using System;
using System.Collections.Generic;
using Spard.Results;
using System.Collections;
using System.Threading;

namespace Spard.Core
{
    /// <summary>
    /// Transformer based on SPARD instructions
    /// </summary>
    public interface ITransformer
    {
        /// <summary>
        /// Progress change event (progress runs from 0 to 100)
        /// </summary>
        event Action<int> ProgressChanged;

        /// <summary>
        /// Transform input data stream
        /// </summary>
        /// <param name="input">Input data stream</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Output data stream</returns>
        IEnumerable<object> Transform(IEnumerable input, CancellationToken cancellationToken = default);

        /// <summary>
        /// Perform multivalue transformation of input data stream
        /// </summary>
        /// <param name="input">Input data stream</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Set of multivalue results of output data</returns>
        IMultiResult MultiTransform(IEnumerable input, CancellationToken cancellationToken = default);

        /// <summary>
        /// Perform transformation as set of separate steps
        /// </summary>
        /// <param name="input">Input data stream</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Result of transformation as a collection of results of each step</returns>
        IEnumerable<IEnumerable<object>> StepTransform(IEnumerable input, CancellationToken cancellationToken = default);
    }
}
