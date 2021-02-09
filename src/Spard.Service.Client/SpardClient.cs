using Microsoft.Extensions.Options;
using System;
using System.Net.Http;
using System.Net.Http.Headers;

namespace Spard.Service.Client
{
    /// <inheritdoc />
    public sealed class SpardClient : ISpardClient
    {
        public SpardClientOptions Options { get; }

        public IExamplesApi Examples { get; }

        public ITransformApi Transform { get; }

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
