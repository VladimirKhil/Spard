using Spard.Service.Contract;
using System.Net.Http.Json;

namespace Spard.Service.Client;

/// <inheritdoc />
internal sealed class ExamplesApi : IExamplesApi
{
    private readonly HttpClient _httpClient;

    public ExamplesApi(HttpClient httpClient) => _httpClient = httpClient;

    public Task<SpardExampleBaseInfo[]?> GetExamplesAsync(CancellationToken cancellationToken = default) =>
        _httpClient.GetFromJsonAsync<SpardExampleBaseInfo[]>("examples", cancellationToken);

    public Task<SpardExampleInfo?> GetExampleAsync(int id, CancellationToken cancellationToken = default) =>
        _httpClient.GetFromJsonAsync<SpardExampleInfo>($"examples/{id}", cancellationToken);
}
