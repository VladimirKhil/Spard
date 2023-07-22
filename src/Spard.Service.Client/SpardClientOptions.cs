namespace Spard.Service.Client;

/// <summary>
/// Provides options for <see cref="SpardClient" /> class.
/// </summary>
public sealed class SpardClientOptions
{
    /// <summary>
    /// Name of the configuration section holding these options.
    /// </summary>
    public const string ConfigurationSectionName = "SpardClient";

    /// <summary>
    /// SPARD service Uri.
    /// </summary>
    public Uri? ServiceUri { get; set; }

    /// <summary>
    /// Requests localization culture.
    /// </summary>
    public string? Culture { get; set; }
}
