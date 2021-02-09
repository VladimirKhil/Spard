using Spard.Service.Contract;
using System.Net.Http;
using System.Threading.Tasks;

namespace Spard.Service.Client
{
    /// <inheritdoc />
    internal sealed class TransformApi : ITransformApi
    {
        private readonly HttpClient _httpClient;

        public TransformApi(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public Task<ProcessResult<string>> TransformAsync(TransformRequest transformRequest) =>
            _httpClient.PostJsonAsync<TransformRequest, ProcessResult<string>>("transform", transformRequest);

        public Task<TransformTableResult> TransformTableAsync(TransformRequest transformRequest) =>
            _httpClient.PostJsonAsync<TransformRequest, TransformTableResult>("transform/table", transformRequest);
    }
}
