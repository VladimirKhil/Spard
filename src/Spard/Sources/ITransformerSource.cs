using System;

namespace Spard.Sources
{
    /// <summary>
    /// Data source that can be reinitialized
    /// </summary>
    internal interface ITransformerSource
    {
        /// <summary>
        /// The data source has been reinitialized
        /// </summary>
        event Action<object> Initialized;
    }
}
