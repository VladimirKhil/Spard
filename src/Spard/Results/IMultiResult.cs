using System.Collections;
using System.Collections.Generic;

namespace Spard.Results
{
    /// <summary>
    /// Result which allow polysemy
    /// </summary>
    public interface IMultiResult: IEnumerable
    {
        /// <summary>
        /// Get another transformation result in the same step
        /// </summary>
        /// <returns>Another transformation result</returns>
        IEnumerable GetAnotherVariant();

        /// <summary>
        /// Get all transformation results
        /// </summary>
        /// <returns>Get all transformation results on current transformation step</returns>
        IEnumerable<IEnumerable> GetAllVariants();
    }
}
