using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using JarvisCoreEngine.Interfaces;
using JarvisCoreEngine.Models;

namespace JarvisCoreEngine.Services
{
    public class HttpService
    {
        private readonly HttpClient _client;
        private readonly IAppLogger _logger;
        private readonly HttpSettings _settings;

        public HttpService(IHttpClientFactory factory, IAppLogger logger, HttpSettings settings)
        {
            _logger = logger;
            _settings = settings;

            _client = factory.CreateClient("JarvisHttpClient");
            _client.Timeout = TimeSpan.FromSeconds(_settings.DefaultTimeoutSeconds);

            _client.DefaultRequestHeaders.UserAgent.Clear();
            _client.DefaultRequestHeaders.UserAgent.ParseAdd(_settings.UserAgent);
        }

        // GET (raw text)
        public async Task<string?> GetAsync(string url)
        {
            try
            {
                _logger.Log($"🌐 GET → {url}");
                var response = await _client.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    string errorBody = await response.Content.ReadAsStringAsync();
                    _logger.Error($"HTTP {(int)response.StatusCode}: {response.ReasonPhrase} | Details: {errorBody}");
                    return null;
                }

                return await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                _logger.Error($"GET failed: {ex.Message}");
                return null;
            }
        }

        // GET (typed JSON)
        public async Task<T?> GetJsonAsync<T>(string url)
        {
            try
            {
                _logger.Log($"🌐 GET-JSON → {url}");
                var json = await _client.GetStringAsync(url);
                return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (Exception ex)
            {
                _logger.Error($"GET-JSON failed: {ex.Message}");
                return default;
            }
        }

        // POST (JSON)
        public async Task<string?> PostJsonAsync(string url, object payload, string? bearerToken = null)
        {
            try
            {
                _logger.Log($"📤 POST → {url}");

                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                using var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = content };

                if (!string.IsNullOrWhiteSpace(bearerToken))
                {
                    request.Headers.Authorization =
                        new AuthenticationHeaderValue("Bearer", bearerToken);
                }

                var response = await _client.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    string errorBody = await response.Content.ReadAsStringAsync();
                    _logger.Error($"HTTP {(int)response.StatusCode}: {response.ReasonPhrase} | Details: {errorBody}");
                    return null;
                }

                return await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                _logger.Error($"POST failed: {ex.Message}");
                return null;
            }
        }

        // POST (Stream Response)
        public async Task<HttpResponseMessage?> PostJsonStreamResponseAsync(string url, object payload, string? bearerToken = null, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.Log($"📤 POST-STREAM → {url}");

                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = content };

                if (!string.IsNullOrWhiteSpace(bearerToken))
                {
                    request.Headers.Authorization =
                        new AuthenticationHeaderValue("Bearer", bearerToken);
                }

                var response = await _client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    string errorBody = await response.Content.ReadAsStringAsync();
                    _logger.Error($"HTTP {(int)response.StatusCode}: {response.ReasonPhrase} | Details: {errorBody}");
                    return null;
                }

                return response;
            }
            catch (Exception ex)
            {
                _logger.Error($"POST-STREAM failed: {ex.Message}");
                return null;
            }
        }
    }
}