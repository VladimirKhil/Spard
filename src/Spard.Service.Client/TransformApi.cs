using Spard.Service.Contract;

namespace Spard.Service.Client;

/// <inheritdoc />
internal sealed class TransformApi : ITransformApi
{
    private readonly HttpClient _httpClient;

    public TransformApi(HttpClient httpClient) => _httpClient = httpClient;

    public Task<ProcessResult<string>> TransformAsync(TransformRequest transformRequest, CancellationToken cancellationToken) =>
        _httpClient.PostJsonAsync<TransformRequest, ProcessResult<string>>("transform", transformRequest, cancellationToken);

    public Task<TransformTableResult> TransformTableAsync(TransformRequest transformRequest, CancellationToken cancellationToken) =>
        _httpClient.PostJsonAsync<TransformRequest, TransformTableResult>("transform/table", transformRequest, cancellationToken);
}
