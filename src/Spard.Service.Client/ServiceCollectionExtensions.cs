using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Spard.Service.Client
{
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
            services.Configure<SpardClientOptions>(configuration.GetSection(SpardClientOptions.ConfigurationSectionName));
            services.AddHttpClient<ISpardClient, SpardClient>();

            return services;
        }
    }
}
