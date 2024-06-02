using NUnit.Framework;
using Spard.Service.Client;
using System.Linq;
using System.Threading.Tasks;

namespace Spard.Service.IntegrationTests;

public sealed class ExamplesTests : TestsBase
{
    [Test]
    public async Task GetExamples_OkAsync()
    {
        var examples = await SpardClient.Examples.GetExamplesAsync();
        Assert.That(examples.Length, Is.GreaterThan(0));

        var firstExample = examples.OrderBy(e => e.Id).First();
        var example = await SpardClient.Examples.GetExampleAsync(firstExample.Id);
        Assert.That(example, Is.Not.Null);
        Assert.That(firstExample.Id, Is.EqualTo(example.Id));
        Assert.That(firstExample.Name, Is.EqualTo(example.Name));
    }

    [Test]
    public async Task GetExample_Localized_OkAsync()
    {
        var options = new SpardClientOptions
        {
            ServiceUri = SpardClient.Options.ServiceUri,
            Culture = "ru-RU"
        };

        var spardClientRus = ServiceCollectionExtensions.CreateSpardClient(options);

        var example = await spardClientRus.Examples.GetExampleAsync(5);
        Assert.That(example.Name, Is.EqualTo("Двойная замена"));

        var example2 = await SpardClient.Examples.GetExampleAsync(5);
        Assert.That(example2.Name, Is.EqualTo("Double replacement"));
    }
}