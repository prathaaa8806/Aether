using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using JarvisCoreEngine.Interfaces;
using JarvisCoreEngine.Services;
using JarvisCoreEngine.Desktop.Services;

namespace JarvisCoreEngine.Desktop
{
    public partial class MainWindow : Window
    {
        private readonly AiService _ai;
        private readonly IAppLogger _logger;
        private readonly VoiceService _voice;
        private readonly SystemControlService _systemControl;
        private readonly VisionService _vision;
        private readonly GhostAgentService _ghost;
        private System.Windows.Threading.DispatcherTimer? _telemetryTimer;

        public MainWindow(AiService ai, IAppLogger logger, VoiceService voice,
                          SystemControlService systemControl, VisionService vision, GhostAgentService ghost)
        {
            InitializeComponent();

            _ai = ai;
            _logger = logger;
            _voice = voice;
            _systemControl = systemControl;
            _vision = vision;
            _ghost = ghost;

            WireVoiceEvents();
            WireGhostAgentEvents();

            _logger.Log("🖥 AETHER Desktop UI Online (Vision & Ghost Agent Active).");
        }

        private IntPtr _hwnd = IntPtr.Zero;

        // ===== WINDOW LOADED =====
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            InputBox.Focus();

            _hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
            var source = System.Windows.Interop.HwndSource.FromHwnd(_hwnd);
            source?.AddHook(WindowProc);

            RegisterGlobalHotkey();
            StartTelemetryTimer();
        }

        private void StartTelemetryTimer()
        {
            UpdateTelemetry();
            _telemetryTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(2)
            };
            _telemetryTimer.Tick += (s, e) => UpdateTelemetry();
            _telemetryTimer.Start();
        }

        private void UpdateTelemetry()
        {
            try
            {
                int cpu = _systemControl.GetCpuPercentage();
                var (totalGb, usedGb, ramPercent) = _systemControl.GetMemoryStats();

                CpuText.Text = $"{cpu}%";
                RamText.Text = $"{ramPercent}%";
                RamText.ToolTip = $"RAM: {usedGb:F1} GB / {totalGb:F1} GB ({ramPercent}%)";
            }
            catch { }
        }

        protected override void OnClosed(EventArgs e)
        {
            _telemetryTimer?.Stop();
            if (_hwnd != IntPtr.Zero)
            {
                UnregisterHotKey(_hwnd, HOTKEY_ID);
                UnregisterHotKey(_hwnd, HOTKEY_VISION_ID);
            }
            base.OnClosed(e);
        }

        // ===== TITLE BAR =====
        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                if (e.ClickCount == 2)
                {
                    ToggleMaximize();
                    return;
                }
                this.DragMove();
            }
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e) => this.WindowState = WindowState.Minimized;
        private void MaxButton_Click(object sender, RoutedEventArgs e) => ToggleMaximize();
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            // Hide to tray instead of closing
            this.Hide();
        }

        private void ToggleMaximize()
        {
            if (this.WindowState == WindowState.Maximized)
            {
                this.WindowState = WindowState.Normal;
                MaxButton.Content = "▢";
                MaxButton.ToolTip = "Maximize";
            }
            else
            {
                this.WindowState = WindowState.Maximized;
                MaxButton.Content = "❐";
                MaxButton.ToolTip = "Restore";
            }
        }

        // ===== TASKBAR FIX & GLOBAL HOTKEYS =====
        private const int WM_GETMINMAXINFO = 0x0024;
        private const int WM_HOTKEY = 0x0312;
        private const int HOTKEY_ID = 9001;
        private const int HOTKEY_VISION_ID = 9002;
        private const uint MOD_ALT = 0x0001;
        private const uint MOD_NOREPEAT = 0x4000;
        private const uint VK_SPACE = 0x20;
        private const uint VK_J = 0x4A;
        private const uint VK_S = 0x53; // 'S' key for Screen / Vision

        private void RegisterGlobalHotkey()
        {
            // Register Alt + Space (preferred) or Alt + J as fallback for Summon
            bool ok = RegisterHotKey(_hwnd, HOTKEY_ID, MOD_ALT | MOD_NOREPEAT, VK_SPACE);
            if (!ok)
            {
                ok = RegisterHotKey(_hwnd, HOTKEY_ID, MOD_ALT | MOD_NOREPEAT, VK_J);
                if (ok) _logger.Log("⌨ Global summon hotkey active: [Alt + J]");
                else _logger.Warn("⚠ Failed to register global summon hotkey.");
            }
            else
            {
                _logger.Log("⌨ Global summon hotkey active: [Alt + Space]");
            }

            // Register Alt + S for AETHER Vision Instant Screen Scan
            bool okVision = RegisterHotKey(_hwnd, HOTKEY_VISION_ID, MOD_ALT | MOD_NOREPEAT, VK_S);
            if (okVision)
            {
                _logger.Log("👁 Global Vision hotkey active: [Alt + S]");
            }
            else
            {
                _logger.Warn("⚠ Failed to register Alt+S Vision hotkey.");
            }
        }

        public void ToggleSummon()
        {
            if (this.IsVisible && this.WindowState != WindowState.Minimized && this.IsActive)
            {
                this.Hide();
            }
            else
            {
                this.Show();
                if (this.WindowState == WindowState.Minimized)
                {
                    this.WindowState = WindowState.Normal;
                }
                this.Activate();
                if (_hwnd != IntPtr.Zero)
                {
                    SetForegroundWindow(_hwnd);
                }
                InputBox.Focus();
            }
        }

        private IntPtr WindowProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_GETMINMAXINFO)
            {
                WmGetMinMaxInfo(hwnd, lParam);
                handled = true;
            }
            else if (msg == WM_HOTKEY)
            {
                int id = wParam.ToInt32();
                if (id == HOTKEY_ID)
                {
                    ToggleSummon();
                    handled = true;
                }
                else if (id == HOTKEY_VISION_ID)
                {
                    _ = TriggerVisionAnalysis();
                    handled = true;
                }
            }
            return IntPtr.Zero;
        }

        private void WmGetMinMaxInfo(IntPtr hwnd, IntPtr lParam)
        {
            var mmi = System.Runtime.InteropServices.Marshal.PtrToStructure<MINMAXINFO>(lParam);
            int MONITOR_DEFAULTTONEAREST = 0x00000002;
            IntPtr monitor = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);

            if (monitor != IntPtr.Zero)
            {
                var monitorInfo = new MONITORINFO();
                monitorInfo.cbSize = System.Runtime.InteropServices.Marshal.SizeOf(typeof(MONITORINFO));
                GetMonitorInfo(monitor, ref monitorInfo);

                var rcWorkArea = monitorInfo.rcWork;
                var rcMonitorArea = monitorInfo.rcMonitor;

                mmi.ptMaxPosition.x = Math.Abs(rcWorkArea.left - rcMonitorArea.left);
                mmi.ptMaxPosition.y = Math.Abs(rcWorkArea.top - rcMonitorArea.top);
                mmi.ptMaxSize.x = Math.Abs(rcWorkArea.right - rcWorkArea.left);
                mmi.ptMaxSize.y = Math.Abs(rcWorkArea.bottom - rcWorkArea.top);
            }

            System.Runtime.InteropServices.Marshal.StructureToPtr(mmi, lParam, true);
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern IntPtr MonitorFromWindow(IntPtr handle, int flags);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        private struct POINT { public int x, y; }

        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        private struct RECT { public int left, top, right, bottom; }

        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        private struct MINMAXINFO
        {
            public POINT ptReserved;
            public POINT ptMaxSize;
            public POINT ptMaxPosition;
            public POINT ptMinTrackSize;
            public POINT ptMaxTrackSize;
        }

        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        private struct MONITORINFO
        {
            public int cbSize;
            public RECT rcMonitor;
            public RECT rcWork;
            public int dwFlags;
        }

        // ===== VOICE WIRING =====
        private void WireVoiceEvents()
        {
            _voice.OnListeningStarted += () => Dispatcher.Invoke(() =>
            {
                SetOrbState("listening");
            });

            _voice.OnListeningStopped += () => Dispatcher.Invoke(() =>
            {
                SetOrbState("idle");
                MicButton.Opacity = 1.0;
            });

            _voice.OnSpeechRecognized += (text) => Dispatcher.Invoke(async () =>
            {
                InputBox.Text = text;
                await SendMessage(text);
            });

            _voice.OnRecognitionError += (err) => Dispatcher.Invoke(() =>
            {
                AddJarvisMessage($"⚠ {err}");
            });

            _voice.OnSpeakingStarted += () => Dispatcher.Invoke(() =>
            {
                SetOrbState("speaking");
            });

            _voice.OnSpeakingStopped += () => Dispatcher.Invoke(() =>
            {
                SetOrbState("idle");
            });

            _voice.OnVolumeLevel += (level) => Dispatcher.Invoke(() =>
            {
                AnimateBars(level);
            });
        }

        // ===== GHOST AGENT WIRING =====
        private void WireGhostAgentEvents()
        {
            _ghost.OnMissionStarted += (goal) => Dispatcher.Invoke(() =>
            {
                if (GhostBanner != null) GhostBanner.Visibility = Visibility.Visible;
                if (GhostStatusText != null) GhostStatusText.Text = $"Directive: {goal}";
                AddJarvisMessage($"⚡ **GHOST AGENT DISPATCHED**\n\n**Directive:** \"{goal}\"\n\nOperating autonomously in background. I will alert you and archive the dossier upon completion.");
                _voice.Speak("Ghost Agent deployed on background directive, Commander.");
            });

            _ghost.OnMissionProgress += (msg) => Dispatcher.Invoke(() =>
            {
                if (GhostStatusText != null) GhostStatusText.Text = msg;
            });

            _ghost.OnMissionCompleted += (goal, filePath, summary) => Dispatcher.Invoke(() =>
            {
                if (GhostBanner != null) GhostBanner.Visibility = Visibility.Collapsed;
                string fileName = System.IO.Path.GetFileName(filePath);
                string msg = $"✅ **GHOST MISSION COMPLETE**\n\n**Directive:** {goal}\n**Archived to:** `Workspace\\{fileName}`\n\n{summary}\n\n*Click OPEN WORKSPACE to inspect.*";
                AddJarvisMessage(msg);
                _voice.Speak("Ghost Mission complete. Dossier archived to Workspace, Commander.");
            });

            _ghost.OnMissionFailed += (goal, error) => Dispatcher.Invoke(() =>
            {
                if (GhostBanner != null) GhostBanner.Visibility = Visibility.Collapsed;
                AddJarvisMessage($"⚠ **Ghost Mission Failed** on \"{goal}\":\n{error}");
                _voice.Speak("Ghost Agent encountered an error on mission execution, Commander.");
            });
        }

        // ===== VISION BUTTON & SCREEN ANALYSIS =====
        private async void VisionButton_Click(object sender, RoutedEventArgs e)
        {
            string prompt = InputBox.Text.Trim();
            InputBox.Text = "";
            await TriggerVisionAnalysis(string.IsNullOrWhiteSpace(prompt) ? null : prompt);
        }

        public async System.Threading.Tasks.Task TriggerVisionAnalysis(string? prompt = null)
        {
            try
            {
                // Momentarily hide AETHER so it captures the active screen behind it
                this.Hide();
                await System.Threading.Tasks.Task.Delay(180);

                var (base64, filePath) = _vision.CaptureScreen();

                // Restore window
                this.Show();
                if (this.WindowState == WindowState.Minimized) this.WindowState = WindowState.Normal;
                this.Activate();

                if (string.IsNullOrEmpty(base64))
                {
                    AddJarvisMessage("⚠ Screen capture failed. Ensure display access is granted.");
                    return;
                }

                AddUserMessage(prompt != null ? $"👁 [VISION] {prompt}" : "👁 [VISION] Analyze active screen");
                SetOrbState("listening");
                SetThinking(true);

                var thinkingBubble = AddJarvisMessage("AETHER Vision: Analyzing display buffers...", isThinking: true);

                var fullResponse = new System.Text.StringBuilder();
                Border? streamBubble = null;
                TextBlock? streamTextBlock = null;

                await foreach (var token in _vision.AnalyzeScreenStreamAsync(base64, prompt))
                {
                    if (streamBubble == null)
                    {
                        ChatPanel.Children.Remove(thinkingBubble);
                        var (bubble, textBlock) = AddJarvisMessageStream();
                        streamBubble = bubble;
                        streamTextBlock = textBlock;
                    }

                    fullResponse.Append(token);
                    if (streamTextBlock != null)
                    {
                        streamTextBlock.Text = fullResponse.ToString();
                    }
                    ScrollToBottom();
                }

                if (streamBubble == null)
                {
                    ChatPanel.Children.Remove(thinkingBubble);
                    AddJarvisMessage("⚠ Vision analysis returned no response.");
                }
                else
                {
                    string finalText = fullResponse.ToString().Trim();
                    if (!string.IsNullOrEmpty(finalText))
                    {
                        _voice.Speak("Screen analyzed, Commander. Here is my tactical assessment.");
                    }
                }
            }
            catch (Exception ex)
            {
                AddJarvisMessage($"⚠ Vision error: {ex.Message}");
                _logger.Error($"Vision error: {ex.Message}");
            }
            finally
            {
                SetOrbState("idle");
                SetThinking(false);
                InputBox.Focus();
            }
        }

        // ===== GHOST AGENT BUTTON =====
        private void GhostButton_Click(object sender, RoutedEventArgs e)
        {
            string directive = InputBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(directive))
            {
                AddJarvisMessage("⚡ **GHOST AGENT MISSION MODE**\n\nEnter your research or generation directive in the input bar, then click **⚡ GHOST**.\n\n*Examples:*\n• `Research WebAssembly performance in .NET and compile dossier`\n• `Analyze security vulnerabilities in OAuth2 flows`\n• `Draft an architectural blueprint for real-time messaging`");
                InputBox.Focus();
                return;
            }

            InputBox.Text = "";
            _ghost.DispatchMission(directive);
        }

        private void OpenWorkspaceButton_Click(object sender, RoutedEventArgs e)
        {
            _ghost.OpenWorkspaceFolder();
        }

        // ===== MIC BUTTON =====
        private void MicButton_Click(object sender, RoutedEventArgs e)
        {
            if (_voice.IsListening)
            {
                _voice.StopListening();
            }
            else
            {
                _voice.StartListening();
            }
        }

        // ===== ORB STATE COLORS =====
        private void SetOrbState(string state)
        {
            SolidColorBrush color;
            switch (state)
            {
                case "listening":
                    color = (SolidColorBrush)FindResource("SuccessBrush"); // green
                    break;
                case "speaking":
                    color = (SolidColorBrush)FindResource("AccentCyanBrush"); // cyan
                    break;
                default:
                    color = (SolidColorBrush)FindResource("AccentCyanBrush");
                    break;
            }

            OrbCore.Fill = color;
            OrbRing.Stroke = color;
        }

        // ===== LIVE AUDIO BARS =====
        private readonly Random _random = new Random();

        private void AnimateBars(int level)
        {
            // level 0-100
            double maxHeight = 50;
            double minHeight = 6;

            Rectangle[] bars = { BarL1, BarL2, BarL3, BarL4, BarL5, BarR1, BarR2, BarR3, BarR4, BarR5 };

            foreach (var bar in bars)
            {
                double variation = _random.NextDouble();
                double newHeight = minHeight + (level / 100.0) * maxHeight * (0.4 + variation * 0.6);

                var anim = new DoubleAnimation
                {
                    To = newHeight,
                    Duration = TimeSpan.FromMilliseconds(120),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };
                bar.BeginAnimation(HeightProperty, anim);
            }
        }

        // ===== SEND =====
        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {
            string userMessage = InputBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(userMessage)) return;
            await SendMessage(userMessage);
        }

        private async System.Threading.Tasks.Task SendMessage(string userMessage)
        {
            if (string.IsNullOrWhiteSpace(userMessage)) return;

            string lower = userMessage.Trim().ToLowerInvariant();

            // Vision trigger via chat or voice
            if (lower == "analyze screen" || lower == "scan screen" || lower == "look at my screen" ||
                lower == "aether vision" || lower == "what is on my screen" || lower == "check screen")
            {
                InputBox.Text = "";
                await TriggerVisionAnalysis();
                return;
            }

            // Ghost Agent mission trigger via chat or voice
            if (lower.StartsWith("ghost mission:") || lower.StartsWith("ghost agent:") || lower.StartsWith("research and save "))
            {
                InputBox.Text = "";
                string goal = userMessage;
                int colonIdx = userMessage.IndexOf(':');
                if (colonIdx > 0 && colonIdx < userMessage.Length - 1)
                {
                    goal = userMessage.Substring(colonIdx + 1).Trim();
                }
                else if (lower.StartsWith("research and save ") && userMessage.Length > 18)
                {
                    goal = userMessage.Substring(18).Trim();
                }

                if (!string.IsNullOrWhiteSpace(goal))
                {
                    _ghost.DispatchMission(goal);
                }
                return;
            }

            AddUserMessage(userMessage);
            InputBox.Text = "";

            // 1. Check local PC automation command first (instant zero-latency execution)
            if (_systemControl.TryExecuteSystemCommand(userMessage, _hwnd, out string feedback))
            {
                AddJarvisMessage(feedback);
                _voice.Speak(feedback);
                InputBox.Focus();
                return;
            }

            // 2. Query AI LLM stream
            SetThinking(true);
            var thinkingBubble = AddJarvisMessage("Thinking...", isThinking: true);

            try
            {
                var fullResponse = new System.Text.StringBuilder();
                Border? streamBubble = null;
                TextBlock? streamTextBlock = null;

                await foreach (var token in _ai.AskStreamAsync(userMessage))
                {
                    if (streamBubble == null)
                    {
                        ChatPanel.Children.Remove(thinkingBubble);
                        var (bubble, textBlock) = AddJarvisMessageStream();
                        streamBubble = bubble;
                        streamTextBlock = textBlock;
                    }

                    fullResponse.Append(token);
                    if (streamTextBlock != null)
                    {
                        streamTextBlock.Text = fullResponse.ToString();
                    }
                    ScrollToBottom();
                }

                if (streamBubble == null)
                {
                    ChatPanel.Children.Remove(thinkingBubble);
                    AddJarvisMessage("⚠ No response received.\n\nPossible reasons:\n• API key invalid or expired\n• Model not available on Groq\n• Internet connection issue\n• Rate limit reached\n\nCheck Logs folder for exact error.");
                }
                else
                {
                    string finalText = fullResponse.ToString().Trim();
                    if (!string.IsNullOrEmpty(finalText))
                    {
                        _voice.Speak(finalText); // 🔊 JARVIS speaks when complete
                    }
                }
            }
            catch (Exception ex)
            {
                ChatPanel.Children.Remove(thinkingBubble);
                AddJarvisMessage($"⚠ Error: {ex.Message}");
                _logger.Error($"AI Error: {ex.Message}");
            }
            finally
            {
                SetThinking(false);
                InputBox.Focus();
            }
        }

        // ===== ENTER KEY =====
        private void InputBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                SendButton_Click(sender, e);
        }

        // ===== BUBBLES =====
        private void AddUserMessage(string text)
        {
            var bubble = CreateBubble(text, "YOU",
                (SolidColorBrush)FindResource("TextSecondaryBrush"),
                (SolidColorBrush)FindResource("BgHoverBrush"),
                Color.FromArgb(80, 139, 160, 199),
                HorizontalAlignment.Right);

            ChatPanel.Children.Add(bubble);
            AnimateBubbleIn(bubble);
            ScrollToBottom();
        }

        private Border AddJarvisMessage(string text, bool isThinking = false)
        {
            var bubble = CreateBubble(text, "AETHER",
                (SolidColorBrush)FindResource("AccentCyanBrush"),
                (SolidColorBrush)FindResource("BgCardBrush"),
                Color.FromArgb(80, 0, 229, 255),
                HorizontalAlignment.Left);

            if (isThinking) bubble.Opacity = 0.6;

            ChatPanel.Children.Add(bubble);
            AnimateBubbleIn(bubble);
            ScrollToBottom();
            return bubble;
        }

        private (Border bubble, TextBlock textBlock) AddJarvisMessageStream()
        {
            var labelBlock = new TextBlock
            {
                Text = "AETHER",
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Foreground = (SolidColorBrush)FindResource("AccentCyanBrush"),
                Margin = new Thickness(0, 0, 0, 6)
            };

            var textBlock = new TextBlock
            {
                Text = "",
                FontSize = 14,
                Foreground = (SolidColorBrush)FindResource("TextPrimaryBrush"),
                TextWrapping = TextWrapping.Wrap
            };

            var stack = new StackPanel();
            stack.Children.Add(labelBlock);
            stack.Children.Add(textBlock);

            var border = new Border
            {
                Background = (SolidColorBrush)FindResource("BgCardBrush"),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(20, 15, 20, 15),
                Margin = new Thickness(0, 0, 0, 15),
                MaxWidth = 700,
                HorizontalAlignment = HorizontalAlignment.Left,
                Child = stack,
                Opacity = 0,
                Effect = new DropShadowEffect
                {
                    Color = Color.FromArgb(80, 0, 229, 255),
                    BlurRadius = 20,
                    ShadowDepth = 0,
                    Opacity = 0.5
                }
            };

            ChatPanel.Children.Add(border);
            AnimateBubbleIn(border);
            ScrollToBottom();

            return (border, textBlock);
        }

        private Border CreateBubble(string text, string label, SolidColorBrush labelColor,
                                    SolidColorBrush bgBrush, Color glowColor, HorizontalAlignment alignment)
        {
            var labelBlock = new TextBlock
            {
                Text = label,
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Foreground = labelColor,
                Margin = new Thickness(0, 0, 0, 6)
            };

            var textBlock = new TextBlock
            {
                Text = text,
                FontSize = 14,
                Foreground = (SolidColorBrush)FindResource("TextPrimaryBrush"),
                TextWrapping = TextWrapping.Wrap
            };

            var stack = new StackPanel();
            stack.Children.Add(labelBlock);
            stack.Children.Add(textBlock);

            var border = new Border
            {
                Background = bgBrush,
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(20, 15, 20, 15),
                Margin = new Thickness(0, 0, 0, 15),
                MaxWidth = 700,
                HorizontalAlignment = alignment,
                Child = stack,
                Opacity = 0,
                Effect = new DropShadowEffect
                {
                    Color = glowColor,
                    BlurRadius = 20,
                    ShadowDepth = 0,
                    Opacity = 0.5
                }
            };

            return border;
        }

        private void AnimateBubbleIn(Border bubble)
        {
            var fade = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(350),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            bubble.BeginAnimation(OpacityProperty, fade);
        }

        private void SetThinking(bool thinking)
        {
            InputBox.IsEnabled = !thinking;
            SendButton.IsEnabled = !thinking;
            SendButton.Opacity = thinking ? 0.5 : 1.0;
        }

        private void ScrollToBottom()
        {
            ChatScroll?.ScrollToEnd();
        }
    }
}