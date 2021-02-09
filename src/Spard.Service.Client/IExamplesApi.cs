using Spard.Service.Contract;
using System.Threading.Tasks;

namespace Spard.Service.Client
{
    /// <summary>
    /// Provides API for working with SPARD examples.
    /// </summary>
    public interface IExamplesApi
    {
        /// <summary>
        /// Gets all examples.
        /// </summary>
        Task<SpardExampleBaseInfo[]> GetExamplesAsync();

        /// <summary>
        /// Gets example by Id.
        /// </summary>
        /// <param name="id">Example Id.</param>
        Task<SpardExampleInfo> GetExampleAsync(int id);
    }
}
