using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using JarvisCoreEngine.Interfaces;

namespace JarvisCoreEngine.Desktop.Services
{
    public class SystemControlService
    {
        private readonly IAppLogger _logger;

        // ===== WIN32 STRUCTS & IMPORTS =====
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private class MEMORYSTATUSEX
        {
            public uint dwLength;
            public uint dwMemoryLoad;
            public ulong ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual;
            public ulong ullAvailVirtual;
            public ulong ullAvailExtendedVirtual;

            public MEMORYSTATUSEX()
            {
                this.dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
            }
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool GlobalMemoryStatusEx([In, Out] MEMORYSTATUSEX lpBuffer);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GetSystemTimes(out System.Runtime.InteropServices.ComTypes.FILETIME lpIdleTime,
                                                  out System.Runtime.InteropServices.ComTypes.FILETIME lpKernelTime,
                                                  out System.Runtime.InteropServices.ComTypes.FILETIME lpUserTime);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool LockWorkStation();

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessageW(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

        private const int WM_APPCOMMAND = 0x0319;
        private const int APPCOMMAND_VOLUME_MUTE = 0x80000;
        private const int APPCOMMAND_VOLUME_DOWN = 0x90000;
        private const int APPCOMMAND_VOLUME_UP = 0xA0000;

        // CPU calculation state
        private ulong _prevIdleTime;
        private ulong _prevKernelTime;
        private ulong _prevUserTime;
        private bool _isFirstCpuSample = true;

        public SystemControlService(IAppLogger logger)
        {
            _logger = logger;
            // Initialize CPU times
            GetCpuPercentage();
        }

        // ===== LIVE TELEMETRY =====

        /// <summary>
        /// Returns total RAM, used RAM, and usage percentage.
        /// </summary>
        public (double totalGb, double usedGb, int percent) GetMemoryStats()
        {
            try
            {
                var mem = new MEMORYSTATUSEX();
                if (GlobalMemoryStatusEx(mem))
                {
                    double totalGb = mem.ullTotalPhys / (1024.0 * 1024.0 * 1024.0);
                    double availGb = mem.ullAvailPhys / (1024.0 * 1024.0 * 1024.0);
                    double usedGb = totalGb - availGb;
                    int percent = (int)mem.dwMemoryLoad;
                    return (totalGb, usedGb, percent);
                }
            }
            catch (Exception ex)
            {
                _logger.Warn($"Memory stats query failed: {ex.Message}");
            }

            return (0, 0, 0);
        }

        /// <summary>
        /// Returns CPU load percentage (0 - 100).
        /// </summary>
        public int GetCpuPercentage()
        {
            try
            {
                if (GetSystemTimes(out var idle, out var kernel, out var user))
                {
                    ulong idleTime = ((ulong)idle.dwHighDateTime << 32) | (uint)idle.dwLowDateTime;
                    ulong kernelTime = ((ulong)kernel.dwHighDateTime << 32) | (uint)kernel.dwLowDateTime;
                    ulong userTime = ((ulong)user.dwHighDateTime << 32) | (uint)user.dwLowDateTime;

                    if (_isFirstCpuSample)
                    {
                        _prevIdleTime = idleTime;
                        _prevKernelTime = kernelTime;
                        _prevUserTime = userTime;
                        _isFirstCpuSample = false;
                        return 0;
                    }

                    ulong usrDiff = userTime - _prevUserTime;
                    ulong kerDiff = kernelTime - _prevKernelTime;
                    ulong idlDiff = idleTime - _prevIdleTime;

                    ulong sysDiff = usrDiff + kerDiff;

                    _prevIdleTime = idleTime;
                    _prevKernelTime = kernelTime;
                    _prevUserTime = userTime;

                    if (sysDiff > 0)
                    {
                        double cpu = ((double)(sysDiff - idlDiff) / sysDiff) * 100.0;
                        return Math.Clamp((int)Math.Round(cpu), 0, 100);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Warn($"CPU stats query failed: {ex.Message}");
            }

            return 0;
        }

        public string GetSystemReport()
        {
            int cpu = GetCpuPercentage();
            var (totalGb, usedGb, ramPercent) = GetMemoryStats();
            return $"System Status Report:\n• CPU Load: {cpu}%\n• Memory: {usedGb:F1} GB / {totalGb:F1} GB ({ramPercent}% utilized)\n• Machine: {Environment.MachineName}\n• OS: Windows {Environment.OSVersion.Version.Major}.{Environment.OSVersion.Version.Minor}\n• All subsystems fully operational, Commander.";
        }

        // ===== AUTOMATION ACTIONS =====

        public bool OpenApplication(string appName, out string feedback)
        {
            string key = appName.Trim().ToLower();

            try
            {
                switch (key)
                {
                    case "chrome":
                    case "google chrome":
                        Process.Start(new ProcessStartInfo("chrome") { UseShellExecute = true });
                        feedback = "Launching Google Chrome, Commander.";
                        return true;

                    case "edge":
                    case "microsoft edge":
                        Process.Start(new ProcessStartInfo("msedge") { UseShellExecute = true });
                        feedback = "Launching Microsoft Edge, Commander.";
                        return true;

                    case "code":
                    case "vscode":
                    case "vs code":
                    case "visual studio code":
                        Process.Start(new ProcessStartInfo("cmd", "/c code") { CreateNoWindow = true, UseShellExecute = false });
                        feedback = "Initializing Visual Studio Code workspace.";
                        return true;

                    case "notepad":
                        Process.Start(new ProcessStartInfo("notepad.exe") { UseShellExecute = true });
                        feedback = "Opening Notepad.";
                        return true;

                    case "calculator":
                    case "calc":
                        Process.Start(new ProcessStartInfo("calc.exe") { UseShellExecute = true });
                        feedback = "Opening Calculator.";
                        return true;

                    case "task manager":
                    case "taskmgr":
                    case "task manager window":
                        Process.Start(new ProcessStartInfo("taskmgr.exe") { UseShellExecute = true });
                        feedback = "Opening Windows Task Manager.";
                        return true;

                    case "cmd":
                    case "terminal":
                    case "powershell":
                        Process.Start(new ProcessStartInfo("powershell.exe") { UseShellExecute = true });
                        feedback = "Deploying PowerShell terminal.";
                        return true;

                    case "explorer":
                    case "files":
                    case "file explorer":
                        Process.Start(new ProcessStartInfo("explorer.exe") { UseShellExecute = true });
                        feedback = "Opening File Explorer.";
                        return true;

                    case "spotify":
                        Process.Start(new ProcessStartInfo("spotify") { UseShellExecute = true });
                        feedback = "Launching Spotify.";
                        return true;

                    default:
                        // Attempt generic shell execution
                        Process.Start(new ProcessStartInfo(appName) { UseShellExecute = true });
                        feedback = $"Launching {appName}, Commander.";
                        return true;
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to launch application '{appName}': {ex.Message}");
                feedback = $"Could not launch '{appName}'. Error: {ex.Message}";
                return false;
            }
        }

        public void OpenUrl(string url)
        {
            try
            {
                if (!url.StartsWith("http://") && !url.StartsWith("https://"))
                {
                    url = "https://" + url;
                }
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                _logger.Log($"🌐 Opened URL: {url}");
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to open URL '{url}': {ex.Message}");
            }
        }

        public void SearchWeb(string query)
        {
            string url = $"https://www.google.com/search?q={Uri.EscapeDataString(query)}";
            OpenUrl(url);
        }

        public void LockPc(IntPtr hwnd)
        {
            try
            {
                _logger.Log("🔒 Locking workstation.");
                LockWorkStation();
            }
            catch (Exception ex)
            {
                _logger.Error($"LockWorkStation failed: {ex.Message}");
            }
        }

        public void ToggleMute(IntPtr hwnd)
        {
            try
            {
                SendMessageW(hwnd, WM_APPCOMMAND, hwnd, (IntPtr)APPCOMMAND_VOLUME_MUTE);
            }
            catch (Exception ex)
            {
                _logger.Error($"Mute toggle failed: {ex.Message}");
            }
        }

        public void VolumeUp(IntPtr hwnd, int steps = 2)
        {
            try
            {
                for (int i = 0; i < steps; i++)
                {
                    SendMessageW(hwnd, WM_APPCOMMAND, hwnd, (IntPtr)APPCOMMAND_VOLUME_UP);
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Volume up failed: {ex.Message}");
            }
        }

        public void VolumeDown(IntPtr hwnd, int steps = 2)
        {
            try
            {
                for (int i = 0; i < steps; i++)
                {
                    SendMessageW(hwnd, WM_APPCOMMAND, hwnd, (IntPtr)APPCOMMAND_VOLUME_DOWN);
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Volume down failed: {ex.Message}");
            }
        }

        // ===== INTENT PARSER =====
        public bool TryExecuteSystemCommand(string userMessage, IntPtr hwnd, out string feedback)
        {
            string raw = (userMessage ?? "").Trim().ToLowerInvariant();

            // 1. Lock Computer
            if (raw == "lock pc" || raw == "lock computer" || raw == "lock workstation" || raw == "lock screen" || raw.StartsWith("lock my pc"))
            {
                LockPc(hwnd);
                feedback = "Workstation secured and locked, Commander.";
                return true;
            }

            // 2. Volume Controls
            if (raw == "mute" || raw == "mute volume" || raw == "unmute" || raw == "mute sound")
            {
                ToggleMute(hwnd);
                feedback = "Audio mute toggled, Commander.";
                return true;
            }

            if (raw == "volume up" || raw == "increase volume" || raw == "louder")
            {
                VolumeUp(hwnd, 3);
                feedback = "Master volume increased.";
                return true;
            }

            if (raw == "volume down" || raw == "decrease volume" || raw == "softer" || raw == "quieter")
            {
                VolumeDown(hwnd, 3);
                feedback = "Master volume decreased.";
                return true;
            }

            // 3. System Health / Telemetry
            if (raw == "system status" || raw == "check status" || raw == "health check" || raw == "system report" ||
                raw == "pc health" || raw == "how is my pc" || raw == "check system health" || raw == "how is my cpu")
            {
                feedback = GetSystemReport();
                return true;
            }

            // 4. Open Applications
            if (raw.StartsWith("open ") || raw.StartsWith("launch ") || raw.StartsWith("start "))
            {
                string target = raw;
                if (target.StartsWith("open ")) target = target.Substring(5).Trim();
                else if (target.StartsWith("launch ")) target = target.Substring(7).Trim();
                else if (target.StartsWith("start ")) target = target.Substring(6).Trim();

                // Web shortcuts
                if (target == "youtube" || target == "youtube.com")
                {
                    OpenUrl("https://youtube.com");
                    feedback = "Opening YouTube, Commander.";
                    return true;
                }
                if (target == "github" || target == "github.com")
                {
                    OpenUrl("https://github.com");
                    feedback = "Accessing GitHub repository.";
                    return true;
                }
                if (target == "google" || target == "google.com")
                {
                    OpenUrl("https://google.com");
                    feedback = "Opening Google Search.";
                    return true;
                }

                return OpenApplication(target, out feedback);
            }

            // 5. Web Search
            if (raw.StartsWith("search for ") || raw.StartsWith("google "))
            {
                string query = raw.StartsWith("search for ") ? raw.Substring(11).Trim() : raw.Substring(7).Trim();
                SearchWeb(query);
                feedback = $"Initiating web reconnaissance for: \"{query}\".";
                return true;
            }

            feedback = "";
            return false;
        }
    }
}
