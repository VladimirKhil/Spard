using Microsoft.AspNetCore.Mvc;
using Spard.Exceptions;
using Spard.Service.Contract;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Spard.Service.Controllers
{
    /// <summary>
    /// Provides API for executing SPARD expressions.
    /// </summary>
    [ApiController]
    [Route("api/v1/transform")]
    public sealed class TransformController : ControllerBase
    {
        private readonly ITransformManager _transformManager;

        /// <summary>
        /// Initializes a new instance of <see cref="TransformController" /> class.
        /// </summary>
        public TransformController(ITransformManager transformManager)
        {
            _transformManager = transformManager;
        }

        /// <summary>
        /// Transforms input using SPARD rules.
        /// </summary>
        /// <param name="transformRequest">Transformation request.</param>
        /// <returns>Transformation result.</returns>
        [HttpPost]
        public async Task<ActionResult<ProcessResult<string>>> TransformAsync(
            [FromBody] TransformRequest transformRequest,
            CancellationToken cancellationToken = default)
        {
            try
            {
                return await _transformManager.TransformAsync(transformRequest, cancellationToken);
            }
            catch (SpardCancelledException exc)
            {
                return StatusCode((int)HttpStatusCode.RequestTimeout, exc.Message);
            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }

        /// <summary>
        /// Transforms input with table transformer using SPARD rules.
        /// </summary>
        /// <param name="transformRequest">Transformation request.</param>
        /// <returns>Transformation result including comparison with standard transformer.</returns>
        [HttpPost("table")]
        public async Task<ActionResult<TransformTableResult>> TransformTableAsync(
            [FromBody] TransformRequest transformRequest,
            CancellationToken cancellationToken = default)
        {
            try
            {
                return await _transformManager.TransformTableAsync(transformRequest, cancellationToken);
            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }
    }
}
