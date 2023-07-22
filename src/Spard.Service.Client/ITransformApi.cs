using Spard.Service.Contract;

namespace Spard.Service.Client;

/// <summary>
/// Provides API for executing SPARD expressions.
/// </summary>
public interface ITransformApi
{
    /// <summary>
    /// Transforms input using SPARD rules.
    /// </summary>
    /// <param name="transformRequest">Transformation request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Transformation result.</returns>
    Task<ProcessResult<string>> TransformAsync(TransformRequest transformRequest, CancellationToken cancellationToken = default);

    /// <summary>
    /// Transforms input with table transformer using SPARD rules.
    /// </summary>
    /// <param name="transformRequest">Transformation request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Transformation result including comparison with standard transformer.</returns>
    Task<TransformTableResult> TransformTableAsync(TransformRequest transformRequest, CancellationToken cancellationToken = default);
}
