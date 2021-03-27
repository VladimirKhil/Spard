using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Spard.Service.Contract;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Spard.Service.Controllers
{
    /// <summary>
    /// Provides API for refactoring SPARD expressions.
    /// </summary>
    [ApiController]
    [Route("api/v1/spard")]
    public sealed class SpardController : ControllerBase
    {
        private readonly ITransformManager _transformManager;

        /// <summary>
        /// Initializes a new instance of <see cref="SpardController" /> class.
        /// </summary>
        public SpardController(ITransformManager transformManager)
        {
            _transformManager = transformManager;
        }

        /// <summary>
        /// Creates table transformation visualization.
        /// </summary>
        /// <param name="transform">SPARD transformation rules.</param>
        /// <returns>Table with transformation rules.</returns>
        // TODO: make this method return table model, not just a serialized string.
        [HttpPost("table")]
        public async Task<ActionResult<ProcessResult<string>>> GenerateTableAsync(
            [FromBody] string transform,
            CancellationToken cancellationToken = default)
        {
            try
            {
                return await _transformManager.GenerateTableAsync(transform, cancellationToken);
            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }

        /// <summary>
        /// Generates source code for SPARD rules.
        /// </summary>
        /// <param name="transform">SPARD transformation rules.</param>
        /// <returns>C# source code for transformation rules.</returns>
        [HttpPost("source")]
        public async Task<ActionResult<ProcessResult<string>>> GenerateSourceCodeAsync(
            [FromBody] string transform,
            CancellationToken cancellationToken = default)
        {
            try
            {
                return await _transformManager.GenerateSourceCodeAsync(transform, cancellationToken);
            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }
    }
}
