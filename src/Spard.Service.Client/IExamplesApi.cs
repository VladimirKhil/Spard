using Spard.Service.Contract;

namespace Spard.Service.Client;

/// <summary>
/// Provides API for working with SPARD examples.
/// </summary>
public interface IExamplesApi
{
    /// <summary>
    /// Gets all examples.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<SpardExampleBaseInfo[]?> GetExamplesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets example by Id.
    /// </summary>
    /// <param name="id">Example Id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<SpardExampleInfo?> GetExampleAsync(int id, CancellationToken cancellationToken = default);
}
