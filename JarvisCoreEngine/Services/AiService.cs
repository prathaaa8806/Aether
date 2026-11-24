using System.Text;
using System.Text.Json;
using System.Runtime.CompilerServices;
using JarvisCoreEngine.Interfaces;
using JarvisCoreEngine.Models;

namespace JarvisCoreEngine.Services
{
    public class AiService
    {
        private readonly HttpService _http;
        private readonly IAppLogger _logger;
        private readonly AiSettings _settings;
        private readonly MemoryService _memory;
        private readonly List<ChatMessage> _history;

        public AiService(HttpService http, IAppLogger logger, AiSettings settings, MemoryService memory)
        {
            _http = http;
            _logger = logger;
            _settings = settings;
            _memory = memory;

            // Load persistent memory
            _history = _memory.Load();

            // Ensure system prompt is present at index 0
            if (_history.Count == 0 || _history[0].Role != "system")
            {
                _history.Insert(0, new ChatMessage
                {
                    Role = "system",
                    Content = _settings.SystemPrompt
                });
            }
            else
            {
                // Always refresh system prompt (in case user updated it)
                _history[0].Content = _settings.SystemPrompt;
            }
        }

        public async Task<string?> AskAsync(string userMessage)
        {
            if (string.IsNullOrWhiteSpace(_settings.ApiKey) ||
                _settings.ApiKey.Contains("PASTE"))
            {
                _logger.Error("AI API key missing. Set it in appsettings.json.");
                return null;
            }

            _history.Add(new ChatMessage { Role = "user", Content = userMessage });
            TrimHistory();

            var payload = new
            {
                model = _settings.Model,
                messages = _history.Select(m => new { role = m.Role, content = m.Content })
            };

            _logger.Log("🧠 Thinking...");

            var responseJson = await _http.PostJsonAsync(_settings.Endpoint, payload, _settings.ApiKey);

            if (string.IsNullOrEmpty(responseJson))
            {
                _logger.Error("AI returned no response.");
                return null;
            }

            try
            {
                using var doc = JsonDocument.Parse(responseJson);
                var content = doc.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString();

                if (string.IsNullOrEmpty(content))
                {
                    _logger.Warn("AI response was empty.");
                    return null;
                }

                _history.Add(new ChatMessage { Role = "assistant", Content = content });

                // 💾 Save memory after every exchange
                _memory.Save(_history);

                return content.Trim();
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to parse AI response: {ex.Message}");
                return null;
            }
        }

        public async IAsyncEnumerable<string> AskStreamAsync(string userMessage, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(_settings.ApiKey) ||
                _settings.ApiKey.Contains("PASTE"))
            {
                _logger.Error("AI API key missing. Set it in appsettings.json or GROQ_API_KEY environment variable.");
                yield break;
            }

            _history.Add(new ChatMessage { Role = "user", Content = userMessage });
            TrimHistory();

            var payload = new
            {
                model = _settings.Model,
                messages = _history.Select(m => new { role = m.Role, content = m.Content }),
                stream = true
            };

            _logger.Log("🧠 Thinking (Streaming)...");

            var response = await _http.PostJsonStreamResponseAsync(_settings.Endpoint, payload, _settings.ApiKey, cancellationToken);
            if (response == null)
            {
                _logger.Error("AI returned no streaming response.");
                yield break;
            }

            var fullResponse = new StringBuilder();

            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var reader = new StreamReader(stream);

            while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
            {
                string? line;
                try
                {
                    line = await reader.ReadLineAsync(cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.Error($"Error reading stream line: {ex.Message}");
                    break;
                }

                if (string.IsNullOrWhiteSpace(line)) continue;

                if (line.StartsWith("data: "))
                {
                    var data = line.Substring(6).Trim();
                    if (data == "[DONE]") break;

                    string? delta = null;
                    try
                    {
                        using var doc = JsonDocument.Parse(data);
                        if (doc.RootElement.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
                        {
                            var choice = choices[0];
                            if (choice.TryGetProperty("delta", out var deltaProp) &&
                                deltaProp.TryGetProperty("content", out var contentProp))
                            {
                                delta = contentProp.GetString();
                            }
                        }
                    }
                    catch
                    {
                        // Ignore any incomplete SSE chunk
                    }

                    if (!string.IsNullOrEmpty(delta))
                    {
                        fullResponse.Append(delta);
                        yield return delta;
                    }
                }
            }

            string finalContent = fullResponse.ToString().Trim();
            if (!string.IsNullOrEmpty(finalContent))
            {
                _history.Add(new ChatMessage { Role = "assistant", Content = finalContent });
                _memory.Save(_history);
            }
        }

        public void ResetConversation()
        {
            _history.Clear();
            _history.Add(new ChatMessage
            {
                Role = "system",
                Content = _settings.SystemPrompt
            });
            _memory.Save(_history);
            _logger.Warn("AI conversation reset.");
        }

        public void ForgetEverything()
        {
            _history.Clear();
            _history.Add(new ChatMessage
            {
                Role = "system",
                Content = _settings.SystemPrompt
            });
            _memory.Clear();
            _logger.Warn("🧠 JARVIS memory completely erased.");
        }

        private void TrimHistory()
        {
            int maxTotal = _settings.MaxHistory + 1; // +1 for system
            if (_history.Count > maxTotal)
            {
                int excess = _history.Count - maxTotal;
                _history.RemoveRange(1, excess); // keep system message
            }
        }
    }
}