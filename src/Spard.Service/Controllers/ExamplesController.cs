using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Spard.Service.Contract;
using Spard.Service.Helpers;
using Spard.Service.Models;
using System.Collections.Generic;

namespace Spard.Service.Controllers
{
    /// <summary>
    /// Provides API for working with SPARD examples.
    /// </summary>
    [ApiController]
    [Route("api/v1/examples")]
    public sealed class ExamplesController : ControllerBase
    {
        private readonly IExamplesRepository _examplesRepository;

        /// <summary>
        /// Initializes a new instance of <see cref="ExamplesController" /> class.
        /// </summary>
        public ExamplesController(IExamplesRepository examplesRepository)
        {
            _examplesRepository = examplesRepository;
        }

        /// <summary>
        /// Gets all SPARD examples.
        /// </summary>
        /// <param name="acceptLanguage">Culture to use.</param>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IEnumerable<SpardExampleBaseInfo> GetExamples(
            [FromHeader(Name = "Accept-Language")] string acceptLanguage = Constants.DefaultCultureCode) =>
            _examplesRepository.GetExamples(CultureHelper.GetCultureFromAcceptLanguageHeader(acceptLanguage));

        /// <summary>
        /// Gets SPARD example by Id.
        /// </summary>
        /// <param name="id">Example Id.</param>
        /// <param name="acceptLanguage">Culture to use.</param>
        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult<SpardExampleInfo> GetExample(int id,
            [FromHeader(Name = "Accept-Language")] string acceptLanguage = Constants.DefaultCultureCode)
        {
            var example = _examplesRepository.GetExample(id, CultureHelper.GetCultureFromAcceptLanguageHeader(acceptLanguage));
            return example == null ? NotFound() : (ActionResult<SpardExampleInfo>)example;
        }
    }
}
