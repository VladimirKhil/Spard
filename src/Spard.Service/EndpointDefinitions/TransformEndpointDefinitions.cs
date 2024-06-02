using Microsoft.AspNetCore.Mvc;
using Spard.Exceptions;
using Spard.Service.Contract;
using Spard.Service.Contracts;
using System.Net;

namespace Spard.Service.EndpointDefinitions;

/// <summary>
/// Provides API for executing SPARD expressions.
/// </summary>
public static class TransformEndpointDefinitions
{
    public static void DefineExamplesEndpoint(WebApplication app)
    {
        app.MapPost("/api/v1/transform", async (
            ITransformManager transformManager,
            [FromBody] TransformRequest transformRequest,
            CancellationToken cancellationToken = default) =>
        {
            try
            {
                return Microsoft.AspNetCore.Http.Results.Ok(await transformManager.TransformAsync(transformRequest, cancellationToken));
            }
            catch (SpardCancelledException)
            {
                return Microsoft.AspNetCore.Http.Results.StatusCode((int)HttpStatusCode.RequestTimeout);
            }
            catch (Exception exc)
            {
                return Microsoft.AspNetCore.Http.Results.BadRequest(exc.Message);
            }
        });

        app.MapPost("/api/v1/transform/table", async (
            ITransformManager transformManager,
            [FromBody] TransformRequest transformRequest,
            CancellationToken cancellationToken = default) =>
        {
            try
            {
                return Microsoft.AspNetCore.Http.Results.Ok(await transformManager.TransformTableAsync(transformRequest, cancellationToken));
            }
            catch (Exception exc)
            {
                return Microsoft.AspNetCore.Http.Results.BadRequest(exc.Message);
            }
        });
    }
}
