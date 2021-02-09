using Spard.Service.Contract;
using System.Net.Http;
using System.Threading.Tasks;

namespace Spard.Service.Client
{
    /// <inheritdoc />
    internal sealed class SpardApi : ISpardApi
    {
        private readonly HttpClient _httpClient;

        public SpardApi(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public Task<ProcessResult<string>> GenerateTableAsync(string transform) =>
            _httpClient.PostJsonAsync<string, ProcessResult<string>>("spard/table", transform);

        public Task<ProcessResult<string>> GenerateSourceCodeAsync(string transform) =>
            _httpClient.PostJsonAsync<string, ProcessResult<string>>("spard/source", transform);
    }
}
