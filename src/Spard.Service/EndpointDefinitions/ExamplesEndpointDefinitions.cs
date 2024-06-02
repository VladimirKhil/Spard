using Microsoft.AspNetCore.Mvc;
using Spard.Service.Contract;
using Spard.Service.Contracts;
using Spard.Service.Helpers;
using Spard.Service.Models;

namespace Spard.Service.EndpointDefinitions;

/// <summary>
/// Provides API for working with SPARD examples.
/// </summary>
public static class ExamplesEndpointDefinitions
{
    public static void DefineExamplesEndpoint(WebApplication app)
    {
        app.MapGet(
            "/api/v1/examples",
            (IExamplesRepository examplesRepository, [FromHeader(Name = "Accept-Language")] string acceptLanguage = Constants.DefaultCultureCode) =>
        {
            var culture = CultureHelper.GetCultureFromAcceptLanguageHeader(acceptLanguage);
            var examples = examplesRepository.GetExamples(culture);
            return examples;
        }).Produces<IEnumerable<SpardExampleBaseInfo>>();

        app.MapGet(
            "/api/v1/examples/{id}",
            (IExamplesRepository examplesRepository, int id, [FromHeader(Name = "Accept-Language")] string acceptLanguage = Constants.DefaultCultureCode) =>
        {
            var culture = CultureHelper.GetCultureFromAcceptLanguageHeader(acceptLanguage);
            var example = examplesRepository.TryGetExample(id, culture);
            
            if (example == null)
            {
                return Microsoft.AspNetCore.Http.Results.NotFound();
            }

            return Microsoft.AspNetCore.Http.Results.Ok(example);
        }).Produces<SpardExampleInfo>();
    }
}
