using Microsoft.Extensions.DependencyInjection;
using JarvisCoreEngine.Desktop.Views;
using System;
using System.Windows;
using System.Windows.Media.Imaging;
using Hardcodet.Wpf.TaskbarNotification;
using JarvisCoreEngine.Interfaces;

namespace JarvisCoreEngine.Desktop.Services
{
    public class TrayService
    {
        private readonly IAppLogger _logger;
        private readonly IServiceProvider _services;
        private TaskbarIcon? _trayIcon;
        private Window? _mainWindow;

        public TrayService(IAppLogger logger, IServiceProvider services)
        {
            _logger = logger;
            _services = services;
        }

        public void Initialize(Window mainWindow)
        {
            _mainWindow = mainWindow;

            try
            {
                _logger.Log("🔵 Initializing tray icon...");

                _trayIcon = new TaskbarIcon();
                _trayIcon.ToolTipText = "AETHER Core Engine";

                // Try custom icon first, fallback to extracting from exe
                try
                {
                    Uri iconUri;
                    try
                    {
                        iconUri = new Uri("pack://application:,,,/Assets/aether-icon.ico", UriKind.Absolute);
                    }
                    catch
                    {
                        iconUri = new Uri("pack://application:,,,/Assets/jarvis-icon.ico", UriKind.Absolute);
                    }
                    _trayIcon.IconSource = new BitmapImage(iconUri);
                    _logger.Log("✅ Custom AETHER icon loaded.");
                }
                catch (Exception iconEx)
                {
                    _logger.Warn($"Custom icon failed: {iconEx.Message}. Using exe icon.");

                    // Fallback — extract icon from running exe
                    var exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                    if (string.IsNullOrEmpty(exePath))
                        exePath = Environment.ProcessPath ?? "";

                    if (!string.IsNullOrEmpty(exePath))
                    {
                        var iconHandle = System.Drawing.Icon.ExtractAssociatedIcon(exePath);
                        if (iconHandle != null)
                        {
                            _trayIcon.Icon = iconHandle;
                        }
                    }
                }

                _trayIcon.Visibility = Visibility.Visible;

                // Left-click: toggle window
                _trayIcon.TrayLeftMouseDown += (s, e) => ToggleWindow();

                // Right-click context menu
                BuildContextMenu();

                _logger.Log("🔵 Tray icon initialized SUCCESSFULLY.");

                // Welcome notification
                ShowNotification("AETHER Online", "Alt+Space: Summon HUD | Alt+S: Vision Screen Scan.");
            }
            catch (Exception ex)
            {
                _logger.Error($"❌ Tray init FAILED: {ex.Message}");
                _logger.Error($"Stack: {ex.StackTrace}");
            }
        }

        private void BuildContextMenu()
        {
            if (_trayIcon == null) return;

            var menu = new System.Windows.Controls.ContextMenu();

            var openItem = new System.Windows.Controls.MenuItem { Header = "🪟 Open AETHER" };
            openItem.Click += (s, e) => ShowWindow();

            var hideItem = new System.Windows.Controls.MenuItem { Header = "👁 Hide" };
            hideItem.Click += (s, e) => HideWindow();

            var separator1 = new System.Windows.Controls.Separator();

            var settingsItem = new System.Windows.Controls.MenuItem { Header = "⚙ Settings" };
            settingsItem.Click += (s, e) => OpenSettings();

            var separator2 = new System.Windows.Controls.Separator();

            var exitItem = new System.Windows.Controls.MenuItem { Header = "❌ Exit" };
            exitItem.Click += (s, e) => ExitApp();

            menu.Items.Add(openItem);
            menu.Items.Add(hideItem);
            menu.Items.Add(separator1);
            menu.Items.Add(settingsItem);
            menu.Items.Add(separator2);
            menu.Items.Add(exitItem);

            _trayIcon.ContextMenu = menu;
        }

        // ===== OPEN SETTINGS WINDOW =====
        private void OpenSettings()
        {
            try
            {
                var settingsWindow = _services.GetRequiredService<SettingsWindow>();
                settingsWindow.Owner = _mainWindow;
                settingsWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                settingsWindow.ShowDialog();

                _logger.Log("⚙ Settings window opened.");
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to open settings: {ex.Message}");
            }
        }

        public void ShowNotification(string title, string message)
        {
            try
            {
                _trayIcon?.ShowBalloonTip(title, message, BalloonIcon.Info);
            }
            catch (Exception ex)
            {
                _logger.Error($"Notification failed: {ex.Message}");
            }
        }

        private void ToggleWindow()
        {
            if (_mainWindow == null) return;

            if (_mainWindow.IsVisible)
                HideWindow();
            else
                ShowWindow();
        }

        private void ShowWindow()
        {
            if (_mainWindow == null) return;

            _mainWindow.Show();
            _mainWindow.WindowState = WindowState.Normal;
            _mainWindow.Activate();
            _mainWindow.Topmost = true;
            _mainWindow.Topmost = false;
            _mainWindow.Focus();

            _logger.Log("🪟 Window shown.");
        }

        private void HideWindow()
        {
            _mainWindow?.Hide();
            ShowNotification("AETHER", "Still running in background. Click tray icon to reopen.");
            _logger.Log("👁 Window hidden to tray.");
        }

        private void ExitApp()
        {
            _logger.Warn("Exiting AETHER via tray.");
            _trayIcon?.Dispose();
            Application.Current.Shutdown();
        }

        public void Dispose()
        {
            _trayIcon?.Dispose();
        }
    }
}