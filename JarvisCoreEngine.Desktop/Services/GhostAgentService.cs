using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using JarvisCoreEngine.Interfaces;
using JarvisCoreEngine.Models;

namespace JarvisCoreEngine.Desktop.Services
{
    public class GhostAgentService
    {
        private readonly IAppLogger _logger;
        private readonly AiSettings _aiSettings;
        private readonly HttpClient _http;

        public bool IsRunning { get; private set; }
        public string CurrentMission { get; private set; } = "";

        public event Action<string>? OnMissionStarted;
        public event Action<string>? OnMissionProgress;
        public event Action<string, string, string>? OnMissionCompleted; // title, filePath, summary
        public event Action<string, string>? OnMissionFailed; // title, error

        public GhostAgentService(IAppLogger logger, AiSettings aiSettings)
        {
            _logger = logger;
            _aiSettings = aiSettings;
            _http = new HttpClient { Timeout = TimeSpan.FromMinutes(3) };
        }

        /// <summary>
        /// Dispatches a background autonomous agent to research, draft, or build a file in the Workspace folder.
        /// </summary>
        public void DispatchMission(string missionGoal)
        {
            if (IsRunning)
            {
                _logger.Warn("A Ghost Mission is already in progress.");
                return;
            }

            Task.Run(async () =>
            {
                IsRunning = true;
                CurrentMission = missionGoal;
                OnMissionStarted?.Invoke(missionGoal);
                _logger.Log($"⚡ Ghost Agent dispatched: {missionGoal}");

                try
                {
                    OnMissionProgress?.Invoke("Agent scanning global intelligence & compiling synthesis...");

                    string prompt =
                        "You are AETHER Ghost Agent — an autonomous tactical intelligence operative serving Commander Pratha.\n" +
                        $"MISSION DIRECTIVE: {missionGoal}\n\n" +
                        "INSTRUCTIONS:\n" +
                        "1. Produce a comprehensive, publication-quality, exhaustive intelligence dossier in Github-flavored Markdown.\n" +
                        "2. Structure it with an Executive Summary, Key Findings, In-Depth Technical Analysis, Architecture/Code if applicable, and Actionable Recommendations.\n" +
                        "3. Make it thorough, professional, and directly ready for Commander Pratha's review.";

                    var payload = new
                    {
                        model = string.IsNullOrWhiteSpace(_aiSettings.Model) ? "openai/gpt-oss-120b" : _aiSettings.Model,
                        messages = new object[]
                        {
                            new { role = "system", content = "You are AETHER Ghost Agent. Deliver high-level intelligence dossiers formatted in Markdown with maximum depth, clarity, and precision." },
                            new { role = "user", content = prompt }
                        }
                    };

                    var json = JsonSerializer.Serialize(payload);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    var request = new HttpRequestMessage(HttpMethod.Post, _aiSettings.Endpoint) { Content = content };
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _aiSettings.ApiKey);

                    var response = await _http.SendAsync(request);
                    if (!response.IsSuccessStatusCode)
                    {
                        string err = await response.Content.ReadAsStringAsync();
                        throw new Exception($"API error ({(int)response.StatusCode}): {err}");
                    }

                    OnMissionProgress?.Invoke("Formatting dossier and finalizing file structure...");

                    var responseJson = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(responseJson);
                    string dossierContent = doc.RootElement
                        .GetProperty("choices")[0]
                        .GetProperty("message")
                        .GetProperty("content")
                        .GetString() ?? "";

                    if (string.IsNullOrWhiteSpace(dossierContent))
                    {
                        throw new Exception("Agent returned empty intelligence payload.");
                    }

                    // Create output file in Workspace/
                    string workspaceDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Workspace");
                    Directory.CreateDirectory(workspaceDir);

                    string sanitizedTitle = Regex.Replace(missionGoal, @"[^\w\-]", "_").Trim('_');
                    if (sanitizedTitle.Length > 30) sanitizedTitle = sanitizedTitle.Substring(0, 30);
                    if (string.IsNullOrWhiteSpace(sanitizedTitle)) sanitizedTitle = "Mission";

                    string fileName = $"Dossier_{sanitizedTitle}_{DateTime.Now:yyyyMMdd_HHmmss}.md";
                    string filePath = Path.Combine(workspaceDir, fileName);

                    // Add metadata header
                    var sb = new StringBuilder();
                    sb.AppendLine($"# AETHER GHOST MISSION DOSSIER");
                    sb.AppendLine($"**Directive:** {missionGoal}");
                    sb.AppendLine($"**Timestamp:** {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                    sb.AppendLine($"**Operative:** AETHER Ghost Autonomous Agent");
                    sb.AppendLine($"**Status:** COMPLETE");
                    sb.AppendLine($"---");
                    sb.AppendLine();
                    sb.AppendLine(dossierContent);

                    await File.WriteAllTextAsync(filePath, sb.ToString());

                    // Create short 2-sentence summary
                    string summary = $"Dossier generated successfully ({new FileInfo(filePath).Length / 1024.0:F1} KB) and archived to Workspace\\{fileName}.";

                    _logger.Log($"✅ Ghost Mission Complete → {filePath}");
                    OnMissionCompleted?.Invoke(missionGoal, filePath, summary);
                }
                catch (Exception ex)
                {
                    _logger.Error($"Ghost Mission failed: {ex.Message}");
                    OnMissionFailed?.Invoke(missionGoal, ex.Message);
                }
                finally
                {
                    IsRunning = false;
                    CurrentMission = "";
                }
            });
        }

        public void OpenFile(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to open file: {ex.Message}");
            }
        }

        public void OpenWorkspaceFolder()
        {
            try
            {
                string workspaceDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Workspace");
                Directory.CreateDirectory(workspaceDir);
                Process.Start(new ProcessStartInfo(workspaceDir) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to open Workspace folder: {ex.Message}");
            }
        }
    }
}
