using System;

namespace Spard.Service.Configuration
{
    /// <summary>
    /// Describes SPARD service configuration.
    /// </summary>
    public sealed class SpardConfiguration
    {
        /// <summary>
        /// Maximum duration of SPARD transformation.
        /// </summary>
        public TimeSpan TransformMaximumDuration { get; set; } = TimeSpan.FromSeconds(2);
    }
}
