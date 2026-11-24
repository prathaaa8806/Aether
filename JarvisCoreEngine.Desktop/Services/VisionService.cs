using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using JarvisCoreEngine.Interfaces;
using JarvisCoreEngine.Models;

namespace JarvisCoreEngine.Desktop.Services
{
    public class VisionService
    {
        private readonly IAppLogger _logger;
        private readonly AiSettings _aiSettings;
        private readonly HttpClient _http;
        private const string VISION_MODEL = "qwen/qwen3.8-27b";

        public VisionService(IAppLogger logger, AiSettings aiSettings)
        {
            _logger = logger;
            _aiSettings = aiSettings;
            _http = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
        }

        /// <summary>
        /// Captures the full screen, saves a local copy, and returns base64 JPEG string and file path.
        /// </summary>
        public (string base64Image, string savedFilePath) CaptureScreen()
        {
            try
            {
                var bounds = Screen.PrimaryScreen?.Bounds ?? new Rectangle(0, 0, 1920, 1080);

                using var bitmap = new Bitmap(bounds.Width, bounds.Height);
                using (var g = Graphics.FromImage(bitmap))
                {
                    g.CopyFromScreen(bounds.X, bounds.Y, 0, 0, bounds.Size, CopyPixelOperation.SourceCopy);
                }

                // Resize down if screen is 4K/2K to keep API payload optimal & fast
                int targetWidth = bounds.Width;
                int targetHeight = bounds.Height;
                if (targetWidth > 1600)
                {
                    double ratio = 1600.0 / targetWidth;
                    targetWidth = 1600;
                    targetHeight = (int)(bounds.Height * ratio);
                }

                using var scaledBitmap = new Bitmap(targetWidth, targetHeight);
                using (var gScaled = Graphics.FromImage(scaledBitmap))
                {
                    gScaled.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    gScaled.DrawImage(bitmap, 0, 0, targetWidth, targetHeight);
                }

                // Save to local Workspace/Screenshots folder
                string workspaceDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Workspace", "Screenshots");
                Directory.CreateDirectory(workspaceDir);
                string savedPath = Path.Combine(workspaceDir, $"Vision_{DateTime.Now:yyyyMMdd_HHmmss}.jpg");

                using var ms = new MemoryStream();
                var jpegEncoder = GetEncoder(ImageFormat.Jpeg);
                var encoderParams = new EncoderParameters(1);
                encoderParams.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 82L);

                scaledBitmap.Save(ms, jpegEncoder, encoderParams);
                scaledBitmap.Save(savedPath, jpegEncoder, encoderParams);

                byte[] imageBytes = ms.ToArray();
                string base64 = Convert.ToBase64String(imageBytes);

                _logger.Log($"📸 Screen captured ({targetWidth}x{targetHeight}) → {savedPath}");
                return (base64, savedPath);
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to capture screen: {ex.Message}");
                return ("", "");
            }
        }

        /// <summary>
        /// Streams vision analysis using Groq's llama-3.2-11b-vision-preview model.
        /// </summary>
        public async IAsyncEnumerable<string> AnalyzeScreenStreamAsync(string base64Image, string? userPrompt = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(_aiSettings.ApiKey))
            {
                _logger.Error("API Key missing for Vision analysis.");
                yield return "⚠ API Key is missing. Please configure it in Settings or set GROQ_API_KEY.";
                yield break;
            }

            string instruction = string.IsNullOrWhiteSpace(userPrompt)
                ? "You are AETHER — tactical AI assistant for Commander Pratha. Analyze what is currently displayed on this screen. Identify any code, errors, terminal messages, active applications, or visual context. Present a crisp, tactical summary and point out anything needing attention."
                : $"You are AETHER. The Commander asks: \"{userPrompt}\". Analyze the provided screenshot and answer his request with tactical precision.";

            var payload = new
            {
                model = VISION_MODEL,
                messages = new object[]
                {
                    new
                    {
                        role = "user",
                        content = new object[]
                        {
                            new { type = "text", text = instruction },
                            new
                            {
                                type = "image_url",
                                image_url = new { url = $"data:image/jpeg;base64,{base64Image}" }
                            }
                        }
                    }
                },
                stream = true
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(HttpMethod.Post, _aiSettings.Endpoint) { Content = content };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _aiSettings.ApiKey);

            HttpResponseMessage? response = null;
            string? connectionError = null;
            try
            {
                response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.Error($"Vision request failed: {ex.Message}");
                connectionError = $"Vision service connection failed: {ex.Message}";
            }

            if (connectionError != null)
            {
                yield return connectionError;
                yield break;
            }

            if (response == null || !response.IsSuccessStatusCode)
            {
                string err = response != null ? await response.Content.ReadAsStringAsync(cancellationToken) : "No response";
                _logger.Error($"Vision API error: {err}");
                yield return $"⚠ Vision API error: {response?.ReasonPhrase ?? "Connection unavailable"}";
                yield break;
            }

            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var reader = new StreamReader(stream);

            while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
            {
                string? line;
                try
                {
                    line = await reader.ReadLineAsync(cancellationToken);
                }
                catch
                {
                    break;
                }

                if (string.IsNullOrWhiteSpace(line)) continue;

                if (line.StartsWith("data: "))
                {
                    string data = line.Substring(6).Trim();
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
                    catch { }

                    if (!string.IsNullOrEmpty(delta))
                    {
                        yield return delta;
                    }
                }
            }
        }

        private ImageCodecInfo GetEncoder(ImageFormat format)
        {
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageDecoders();
            foreach (ImageCodecInfo codec in codecs)
            {
                if (codec.FormatID == format.Guid)
                {
                    return codec;
                }
            }
            return codecs[0];
        }
    }
}
