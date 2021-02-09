using Spard.Service.Contract;
using System.Net.Http;
using System.Threading.Tasks;

namespace Spard.Service.Client
{
    /// <inheritdoc />
    internal sealed class ExamplesApi : IExamplesApi
    {
        private readonly HttpClient _httpClient;

        public ExamplesApi(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public Task<SpardExampleBaseInfo[]> GetExamplesAsync() =>
            _httpClient.GetJsonAsync<SpardExampleBaseInfo[]>("examples");

        public Task<SpardExampleInfo> GetExampleAsync(int id) =>
            _httpClient.GetJsonAsync<SpardExampleInfo>($"examples/{id}");
    }
}
