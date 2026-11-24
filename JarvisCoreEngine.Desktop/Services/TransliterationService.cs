using System;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using JarvisCoreEngine.Interfaces;

namespace JarvisCoreEngine.Desktop.Services
{
    public class TransliterationService
    {
        private readonly HttpClient _http;
        private readonly IAppLogger _logger;

        public TransliterationService(IAppLogger logger)
        {
            _logger = logger;
            _http = new HttpClient();
            _http.Timeout = TimeSpan.FromSeconds(10);
        }

        /// <summary>
        /// Converts Hinglish (Roman) text to Devanagari (Hindi script).
        /// Example: "main theek hu" → "मैं ठीक हूं"
        /// English words remain as-is.
        /// </summary>
        public async Task<string> RomanToDevanagariAsync(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return text;

            try
            {
                // Split text into words
                var words = Regex.Split(text, @"(\s+|[,.!?;:()\-—""'])");
                var result = new System.Text.StringBuilder();

                foreach (var word in words)
                {
                    if (string.IsNullOrWhiteSpace(word) || IsPunctuation(word))
                    {
                        result.Append(word);
                        continue;
                    }

                    // Skip if already Devanagari
                    if (IsDevanagari(word))
                    {
                        result.Append(word);
                        continue;
                    }

                    // Skip pure English words (common ones)
                    if (IsCommonEnglish(word))
                    {
                        result.Append(word);
                        continue;
                    }

                    // Transliterate Hinglish word
                    var hindi = await TransliterateWordAsync(word);
                    result.Append(hindi);
                }

                return result.ToString();
            }
            catch (Exception ex)
            {
                _logger.Error($"Transliteration failed: {ex.Message}");
                return text; // fallback to original
            }
        }

        private async Task<string> TransliterateWordAsync(string word)
        {
            try
            {
                // Google Input Tools API (free, no key needed)
                string url = $"https://inputtools.google.com/request?text={Uri.EscapeDataString(word)}&itc=hi-t-i0-und&num=1&cp=0&cs=1&ie=utf-8&oe=utf-8";

                var response = await _http.GetStringAsync(url);

                using var doc = JsonDocument.Parse(response);
                var root = doc.RootElement;

                if (root[0].GetString() == "SUCCESS")
                {
                    var results = root[1][0][1];
                    if (results.GetArrayLength() > 0)
                    {
                        return results[0].GetString() ?? word;
                    }
                }

                return word;
            }
            catch
            {
                return word;
            }
        }

        private bool IsDevanagari(string word)
        {
            foreach (var c in word)
            {
                if (c >= 0x0900 && c <= 0x097F)
                    return true;
            }
            return false;
        }

        private bool IsPunctuation(string word)
        {
            return Regex.IsMatch(word, @"^[\s,.!?;:()\-—""']+$");
        }

        private bool IsCommonEnglish(string word)
        {
            // Common English words that should NOT be transliterated
            var commonWords = new[]
            {
                "Aether", "AETHER", "Commander", "Pratha", "JARVIS", "AI", "API", "OK", "okay",
                "yes", "no", "thanks", "thank", "you", "hello", "hi",
                "internet", "email", "mobile", "laptop", "computer", "google",
                "youtube", "whatsapp", "facebook", "instagram", "twitter",
                "github", "code", "coding", "programming", "software"
            };

            foreach (var cw in commonWords)
            {
                if (word.Equals(cw, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }
    }
}