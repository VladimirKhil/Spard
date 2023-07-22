using Spard.Service.Contract;

namespace Spard.Service.Client;

/// <inheritdoc />
internal sealed class SpardApi : ISpardApi
{
    private readonly HttpClient _httpClient;

    public SpardApi(HttpClient httpClient) => _httpClient = httpClient;

    public Task<ProcessResult<string>> GenerateTableAsync(string transform, CancellationToken cancellationToken = default) =>
        _httpClient.PostJsonAsync<string, ProcessResult<string>>("spard/table", transform, cancellationToken);

    public Task<ProcessResult<string>> GenerateSourceCodeAsync(string transform, CancellationToken cancellationToken = default) =>
        _httpClient.PostJsonAsync<string, ProcessResult<string>>("spard/source", transform, cancellationToken);
}
