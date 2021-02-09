using Spard.Service.Contract;
using System.Threading.Tasks;

namespace Spard.Service.Client
{
    /// <summary>
    /// Provides API for executing SPARD expressions.
    /// </summary>
    public interface ITransformApi
    {
        /// <summary>
        /// Transforms input using SPARD rules.
        /// </summary>
        /// <param name="transformRequest">Transformation request.</param>
        /// <returns>Transformation result.</returns>
        Task<ProcessResult<string>> TransformAsync(TransformRequest transformRequest);

        /// <summary>
        /// Transforms input with table transformer using SPARD rules.
        /// </summary>
        /// <param name="transformRequest">Transformation request.</param>
        /// <returns>Transformation result including comparison with standard transformer.</returns>
        Task<TransformTableResult> TransformTableAsync(TransformRequest transformRequest);
    }
}
