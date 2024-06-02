using Spard.Service.Contract;

namespace Spard.Service.Contracts;

/// <summary>
/// Provides methods for SPARD transformations.
/// </summary>
public interface ITransformManager
{
    /// <summary>
    /// Transforms input using SPARD rules.
    /// </summary>
    /// <param name="transformRequest">Transformation request.</param>
    /// <returns>Transformation result.</returns>
    Task<ProcessResult<string>> TransformAsync(TransformRequest transformRequest, CancellationToken cancellationToken = default);

    /// <summary>
    /// Transforms input with table transformer using SPARD rules.
    /// </summary>
    /// <param name="transformRequest">Transformation request.</param>
    /// <returns>Transformation result including comparison with standard transformer.</returns>
    Task<TransformTableResult> TransformTableAsync(TransformRequest transformRequest, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates table transformation visualization.
    /// </summary>
    /// <param name="transform">SPARD transformation rules.</param>
    /// <returns>Table with transformation rules.</returns>
    Task<ProcessResult<string>> GenerateTableAsync(string transform, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates source code for SPARD rules.
    /// </summary>
    /// <param name="transform">SPARD transformation rules.</param>
    /// <returns>C# source code for transformation rules.</returns>
    Task<ProcessResult<string>> GenerateSourceCodeAsync(string transform, CancellationToken cancellationToken = default);
}
