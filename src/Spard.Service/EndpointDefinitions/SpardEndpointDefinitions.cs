using Microsoft.AspNetCore.Mvc;
using Spard.Service.Contracts;

namespace Spard.Service.EndpointDefinitions;

/// <summary>
/// Provides API for refactoring SPARD expressions.
/// </summary>
public static class SpardEndpointDefinitions
{
    public static void DefineExamplesEndpoint(WebApplication app)
    {
        // TODO: make this method return table model, not just a serialized string.
        app.MapPost("/api/v1/spard/table", async (
            ITransformManager transformManager,
            [FromBody] string transform,
            CancellationToken cancellationToken = default) =>
        {
            try
            {
                return Microsoft.AspNetCore.Http.Results.Ok(await transformManager.GenerateTableAsync(transform, cancellationToken));
            }
            catch (Exception exc)
            {
                return Microsoft.AspNetCore.Http.Results.BadRequest(exc.Message);
            }
        });

        app.MapPost("/api/v1/spard/source", async (
            ITransformManager transformManager,
            [FromBody] string transform,
            CancellationToken cancellationToken = default) =>
        {
            try
            {
                return Microsoft.AspNetCore.Http.Results.Ok(await transformManager.GenerateSourceCodeAsync(transform, cancellationToken));
            }
            catch (Exception exc)
            {
                return Microsoft.AspNetCore.Http.Results.BadRequest(exc.Message);
            }
        });
    }
}
