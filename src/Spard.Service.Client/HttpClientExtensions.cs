using System.Net.Http.Json;
using System.Text.Json;

namespace Spard.Service.Client;

/// <summary>
/// Contains extension methods for <see cref="HttpClient "/> class.
/// </summary>
internal static class HttpClientExtensions
{
    private static readonly JsonSerializerOptions SerializerOptions = new JsonSerializerOptions
    {
        Converters = { new TimeSpanConverter() },
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Posts JSON object.
    /// </summary>
    /// <typeparam name="TRequest">Request object type.</typeparam>
    /// <typeparam name="TResult">Response object type.</typeparam>
    /// <param name="httpClient">Http client to use.</param>
    /// <param name="uri">Object request uri.</param>
    /// <param name="request">Object request body.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result object.</returns>
    internal static async Task<TResult> PostJsonAsync<TRequest, TResult>(
        this HttpClient httpClient,
        string uri,
        TRequest request,
        CancellationToken cancellationToken)
    {
        using var response = await httpClient.PostAsJsonAsync(uri, request, cancellationToken);
        
        if (response.IsSuccessStatusCode)
        {
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonSerializer.Deserialize<TResult>(responseBody, SerializerOptions);

            return result == null ? throw new InvalidOperationException($"No result on request {uri}") : result;
        }

        var error = $"Error while making a call to \"{uri}\" with request \"{request}\": " +
            $"({response.StatusCode}) {await response.Content.ReadAsStringAsync(cancellationToken)}";

        throw new HttpRequestException(error);
    }
}
