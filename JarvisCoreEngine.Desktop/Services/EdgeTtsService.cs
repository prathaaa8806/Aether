using System;
using System.IO;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using JarvisCoreEngine.Interfaces;

namespace JarvisCoreEngine.Desktop.Services
{
    public class EdgeTtsService
    {
        private const string TRUSTED_CLIENT_TOKEN = "6A5AA1D4EAFF4E9FB37E23D68491D6F4";
        private const string WSS_URL = "wss://speech.platform.bing.com/consumer/speech/synthesize/readaloud/edge/v1?TrustedClientToken=" + TRUSTED_CLIENT_TOKEN;
        
        // Voice constants
        public const string VOICE_JARVIS_BRITISH = "en-GB-RyanNeural";     // Elite, calm, British JARVIS voice
        public const string VOICE_JARVIS_AMERICAN = "en-US-ChristopherNeural"; // Smooth, authoritative American voice
        public const string VOICE_JARVIS_HINDI = "hi-IN-MadhurNeural";        // Natural fluent Hindi male voice
        public const string VOICE_JARVIS_FEMALE = "en-GB-SoniaNeural";        // British female option

        private readonly IAppLogger _logger;

        public EdgeTtsService(IAppLogger logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Synthesizes text using Microsoft Edge's Neural TTS service and returns an MP3 byte array.
        /// </summary>
        public async Task<byte[]?> SynthesizeToBytesAsync(string text, string voiceName = VOICE_JARVIS_BRITISH, int ratePercent = 0, int volumePercent = 0, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;

            try
            {
                using var ws = new ClientWebSocket();
                ws.Options.SetRequestHeader("Pragma", "no-cache");
                ws.Options.SetRequestHeader("Cache-Control", "no-cache");
                ws.Options.SetRequestHeader("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/130.0.0.0 Safari/537.36 Edg/130.0.0.0");
                ws.Options.SetRequestHeader("Origin", "chrome-extension://jdiccldimpdaibmpdkjnbmckianbfold");

                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(TimeSpan.FromSeconds(15)); // 15 sec timeout

                await ws.ConnectAsync(new Uri(WSS_URL), cts.Token);

                // 1. Send speech.config
                string dateStr = DateTime.UtcNow.ToString("r");
                string configMessage = $"X-Timestamp:{dateStr}\r\nContent-Type:application/json; charset=utf-8\r\nPath:speech.config\r\n\r\n" +
                    "{\"context\":{\"synthesis\":{\"audio\":{\"metadataoptions\":{\"sentenceBoundaryEnabled\":\"false\",\"wordBoundaryEnabled\":\"false\"},\"outputFormat\":\"audio-24khz-48kbitrate-mono-mp3\"}}}}";

                var configBytes = Encoding.UTF8.GetBytes(configMessage);
                await ws.SendAsync(new ArraySegment<byte>(configBytes), WebSocketMessageType.Text, true, cts.Token);

                // 2. Format SSML
                string requestId = Guid.NewGuid().ToString("N");
                string escapedText = System.Security.SecurityElement.Escape(text);
                string rateStr = (ratePercent >= 0 ? "+" : "") + ratePercent + "%";
                string volStr = (volumePercent >= 0 ? "+" : "") + volumePercent + "%";

                string ssml = $"<speak version='1.0' xmlns='http://www.w3.org/2001/10/synthesis' xml:lang='en-US'>" +
                              $"<voice name='{voiceName}'>" +
                              $"<prosody pitch='+0Hz' rate='{rateStr}' volume='{volStr}'>" +
                              $"{escapedText}" +
                              $"</prosody></voice></speak>";

                string ssmlMessage = $"X-RequestId:{requestId}\r\nContent-Type:application/ssml+xml\r\nX-Timestamp:{dateStr}Z\r\nPath:ssml\r\n\r\n{ssml}";
                var ssmlBytes = Encoding.UTF8.GetBytes(ssmlMessage);
                await ws.SendAsync(new ArraySegment<byte>(ssmlBytes), WebSocketMessageType.Text, true, cts.Token);

                // 3. Receive audio stream
                using var audioStream = new MemoryStream();
                var buffer = new byte[8192];

                while (ws.State == WebSocketState.Open && !cts.Token.IsCancellationRequested)
                {
                    var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), cts.Token);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        break;
                    }

                    if (result.MessageType == WebSocketMessageType.Binary && result.Count > 2)
                    {
                        // In Edge-TTS binary packets:
                        // First 2 bytes are a 16-bit big-endian integer indicating header length
                        int headerLength = (buffer[0] << 8) | buffer[1];
                        int payloadOffset = 2 + headerLength;

                        if (result.Count > payloadOffset)
                        {
                            audioStream.Write(buffer, payloadOffset, result.Count - payloadOffset);
                        }
                    }
                    else if (result.MessageType == WebSocketMessageType.Text)
                    {
                        string message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        if (message.Contains("Path:turn.end"))
                        {
                            // Synthesis complete
                            break;
                        }
                    }
                }

                try
                {
                    await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Complete", CancellationToken.None);
                }
                catch { }

                var audioData = audioStream.ToArray();
                return audioData.Length > 0 ? audioData : null;
            }
            catch (Exception ex)
            {
                _logger.Warn($"Edge-TTS synthesis error: {ex.Message}. Falling back to system voice.");
                return null;
            }
        }
    }
}
