using Microsoft.Extensions.Options;
using NUnit.Framework;
using Spard.Service.Client;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Spard.Service.IntegrationTest
{
    public sealed class ExamplesTests : TestsBase
    {
        [Test]
        public async Task GetExamples_OkAsync()
        {
            var examples = await SpardClient.Examples.GetExamplesAsync();
            Assert.Greater(examples.Length, 0);

            var firstExample = examples.OrderBy(e => e.Id).First();
            var example = await SpardClient.Examples.GetExampleAsync(firstExample.Id);
            Assert.NotNull(example);
            Assert.AreEqual(firstExample.Id, example.Id);
            Assert.AreEqual(firstExample.Name, example.Name);
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
            Assert.AreEqual("Двойная замена", example.Name);

            var example2 = await SpardClient.Examples.GetExampleAsync(5);
            Assert.AreEqual("Double replacement", example2.Name);
        }
    }
}