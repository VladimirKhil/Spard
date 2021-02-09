using Spard.Service.Contract;
using System.Threading.Tasks;

namespace Spard.Service.Client
{
    /// <summary>
    /// Provides API for refactoring SPARD expressions.
    /// </summary>
    public interface ISpardApi
    {
        /// <summary>
        /// Creates table transformation visualization.
        /// </summary>
        /// <param name="transform">SPARD transformation rules.</param>
        /// <returns>Table with transformation rules.</returns>
        Task<ProcessResult<string>> GenerateTableAsync(string transform);

        /// <summary>
        /// Generates source code for SPARD rules.
        /// </summary>
        /// <param name="transform">SPARD transformation rules.</param>
        /// <returns>C# source code for transformation rules.</returns>
        Task<ProcessResult<string>> GenerateSourceCodeAsync(string transform);
    }
}
