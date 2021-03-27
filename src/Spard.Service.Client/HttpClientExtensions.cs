using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Spard.Service.Client
{
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
        /// Gets JSON object.
        /// </summary>
        /// <typeparam name="T">Result object type.</typeparam>
        /// <param name="httpClient">Http client to use.</param>
        /// <param name="uri">Object request uri.</param>
        /// <returns>Result object.</returns>
        internal static async Task<T> GetJsonAsync<T>(this HttpClient httpClient, string uri)
        {
            using var response = await httpClient.GetAsync($"api/v1/{uri}");

            if (response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<T>(responseBody, SerializerOptions);
            }

            var error = $"Error while making a call to \"{uri}\": ({response.StatusCode}) {response.Content.ReadAsStringAsync()}";
            throw new HttpRequestException(error);
        }

        /// <summary>
        /// Posts JSON object.
        /// </summary>
        /// <typeparam name="TRequest">Request object type.</typeparam>
        /// <typeparam name="TResult">Response object type.</typeparam>
        /// <param name="httpClient">Http client to use.</param>
        /// <param name="uri">Object request uri.</param>
        /// <param name="request">Object request body.</param>
        /// <returns>Result object.</returns>
        internal static async Task<TResult> PostJsonAsync<TRequest, TResult>(this HttpClient httpClient, string uri, TRequest request)
        {
            var requestJson = JsonSerializer.Serialize(request);

            using var stringContent = new StringContent(requestJson, Encoding.UTF8, "application/json");
            using var response = await httpClient.PostAsync($"api/v1/{uri}", stringContent);

            if (response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<TResult>(responseBody, SerializerOptions);
            }

            var error = $"Error while making a call to \"{uri}\" with request \"{requestJson}\": ({response.StatusCode}) {await response.Content.ReadAsStringAsync()}";
            throw new HttpRequestException(error);
        }
    }
}
