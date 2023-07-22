using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Http.Headers;

namespace Spard.Service.Client;

/// <summary>
/// Provides extension methods for <see cref="IServiceCollection"/> class.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Spard client to Service collection.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="configuration">Configuration to use.</param>
    /// <returns>Modified service collection.</returns>
    public static IServiceCollection AddSpardClient(this IServiceCollection services, IConfiguration configuration)
    {
        var optionsSection = configuration.GetSection(SpardClientOptions.ConfigurationSectionName);
        services.Configure<SpardClientOptions>(optionsSection);

        var options = optionsSection.Get<SpardClientOptions>();

        services.AddHttpClient<ISpardClient, SpardClient>(client =>
        {
            if (options != null)
            {
                var serviceUri = options.ServiceUri;
                client.BaseAddress = serviceUri != null ? new Uri(serviceUri, "api/v1/") : null;

                if (options.Culture != null)
                {
                    client.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue(options.Culture));
                }
            }

            client.DefaultRequestVersion = HttpVersion.Version20;
        });

        return services;
    }

    /// <summary>
    /// Allows to create custom Spard client.
    /// </summary>
    /// <param name="options">Client options.</param>
    public static ISpardClient CreateSpardClient(SpardClientOptions options)
    {
        var serviceUri = options.ServiceUri;

        var client = new HttpClient
        {
            BaseAddress = serviceUri != null ? new Uri(serviceUri, "api/v1/") : null,
            DefaultRequestVersion = HttpVersion.Version20
        };

        if (options.Culture != null)
        {
            client.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue(options.Culture));
        }

        return new SpardClient(client, new OptionsWrapper<SpardClientOptions>(options));
    }
}
