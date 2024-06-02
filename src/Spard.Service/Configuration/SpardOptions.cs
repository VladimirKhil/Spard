namespace Spard.Service.Configuration;

/// <summary>
/// Describes SPARD service configuration.
/// </summary>
public sealed class SpardOptions
{
    public const string ConfigurationSectionName = "Spard";

    /// <summary>
    /// Maximum duration of SPARD transformation.
    /// </summary>
    public TimeSpan TransformMaximumDuration { get; set; } = TimeSpan.FromSeconds(2);
}
