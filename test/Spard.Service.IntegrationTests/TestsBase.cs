using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Spard.Service.Client;

namespace Spard.Service.IntegrationTests;

/// <summary>
/// Provides base methods for SPARD service integration tests.
/// </summary>
/// <remarks>
/// SPARD service must be started before tests to run.
/// </remarks>
public abstract class TestsBase
{
    private readonly ISpardClient _spardClient;

    protected ISpardClient SpardClient => _spardClient;

    public TestsBase()
    {
        var builder = new ConfigurationBuilder().AddJsonFile("appsettings.json");
        var configuration = builder.Build();

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSpardClient(configuration);

        var serviceProvider = serviceCollection.BuildServiceProvider();
        _spardClient = serviceProvider.GetRequiredService<ISpardClient>();
    }
}
