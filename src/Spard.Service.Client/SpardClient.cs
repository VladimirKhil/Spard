using Microsoft.Extensions.Options;
using System;
using System.Net.Http;
using System.Net.Http.Headers;

namespace Spard.Service.Client
{
    /// <inheritdoc />
    public sealed class SpardClient : ISpardClient
    {
        /// <summary>
        /// Gets client options.
        /// </summary>
        public SpardClientOptions Options { get; }

        /// <summary>
        /// Provides API for working with SPARD examples.
        /// </summary>
        public IExamplesApi Examples { get; }

        /// <summary>
        /// Provides API for executing SPARD expressions.
        /// </summary>
        public ITransformApi Transform { get; }

        /// <summary>
        /// Provides API for refactoring SPARD expressions.
        /// </summary>
        public ISpardApi Spard { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="SpardClient" /> class.
        /// </summary>
        /// <param name="httpClient">HTTP client to use.</param>
        /// <param name="options">Client options.</param>
        public SpardClient(HttpClient httpClient, IOptions<SpardClientOptions> options)
        {
            Options = options.Value;

            httpClient.BaseAddress = new Uri(Options.ServiceUri);
            if (Options.Culture != null)
            {
                httpClient.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue(Options.Culture));
            }

            Examples = new ExamplesApi(httpClient);
            Transform = new TransformApi(httpClient);
            Spard = new SpardApi(httpClient);
        }
    }
}
