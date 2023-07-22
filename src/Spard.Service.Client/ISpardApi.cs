using Spard.Service.Contract;

namespace Spard.Service.Client;

/// <summary>
/// Provides API for refactoring SPARD expressions.
/// </summary>
public interface ISpardApi
{
    /// <summary>
    /// Creates table transformation visualization.
    /// </summary>
    /// <param name="transform">SPARD transformation rules.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Table with transformation rules.</returns>
    Task<ProcessResult<string>> GenerateTableAsync(string transform, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates source code for SPARD rules.
    /// </summary>
    /// <param name="transform">SPARD transformation rules.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>C# source code for transformation rules.</returns>
    Task<ProcessResult<string>> GenerateSourceCodeAsync(string transform, CancellationToken cancellationToken = default);
}
