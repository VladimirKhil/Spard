namespace Spard.Service.Client;

/// <summary>
/// Defines SPARD service client.
/// </summary>
public interface ISpardClient
{
    /// <summary>
    /// Gets client options.
    /// </summary>
    SpardClientOptions Options { get; }

    /// <summary>
    /// Provides API for working with SPARD examples.
    /// </summary>
    IExamplesApi Examples { get; }

    /// <summary>
    /// Provides API for executing SPARD expressions.
    /// </summary>
    ITransformApi Transform { get; }

    /// <summary>
    /// Provides API for refactoring SPARD expressions.
    /// </summary>
    ISpardApi Spard { get; }
}
