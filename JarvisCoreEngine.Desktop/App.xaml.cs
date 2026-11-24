using System;
using System.Windows;
using JarvisCoreEngine.Interfaces;
using JarvisCoreEngine.Models;
using JarvisCoreEngine.Services;
using JarvisCoreEngine.Utilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace JarvisCoreEngine.Desktop
{
    public partial class App : Application
    {
        public static IServiceProvider? Services { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // ===== LOAD CONFIGURATION =====
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            // ===== BIND SETTINGS =====
            var appSettings = new AppSettings();
            configuration.GetSection("AppSettings").Bind(appSettings);

            var loggingSettings = new LoggingSettings();
            configuration.GetSection("LoggingSettings").Bind(loggingSettings);

            var workspaceSettings = new WorkspaceSettings();
            configuration.GetSection("WorkspaceSettings").Bind(workspaceSettings);

            var httpSettings = new HttpSettings();
            configuration.GetSection("HttpSettings").Bind(httpSettings);

            var aiSettings = new AiSettings();
            configuration.GetSection("AiSettings").Bind(aiSettings);

            // Allow environment variable override for security (e.g. GROQ_API_KEY)
            var envApiKey = Environment.GetEnvironmentVariable("GROQ_API_KEY")
                         ?? Environment.GetEnvironmentVariable("GROQ_API_KEY", EnvironmentVariableTarget.User)
                         ?? Environment.GetEnvironmentVariable("JARVIS_API_KEY")
                         ?? Environment.GetEnvironmentVariable("JARVIS_API_KEY", EnvironmentVariableTarget.User);
            if (!string.IsNullOrWhiteSpace(envApiKey))
            {
                aiSettings.ApiKey = envApiKey;
            }

            var memorySettings = new MemorySettings();
            configuration.GetSection("MemorySettings").Bind(memorySettings);

            // ===== DI CONTAINER =====
            var services = new ServiceCollection();

            // Register Settings
            services.AddSingleton(appSettings);
            services.AddSingleton(loggingSettings);
            services.AddSingleton(workspaceSettings);
            services.AddSingleton(httpSettings);
            services.AddSingleton(aiSettings);
            services.AddSingleton(memorySettings);

            // Register Core Services
            services.AddSingleton<IAppLogger, Logger>();
            services.AddHttpClient();
            services.AddSingleton<HttpService>();
            services.AddSingleton<FileService>();
            services.AddSingleton<MemoryService>();
            services.AddSingleton<AiService>();

            // Transliteration Service
            services.AddSingleton<Desktop.Services.TransliterationService>();

            // Neural Edge TTS Service
            services.AddSingleton<Desktop.Services.EdgeTtsService>();

            // Voice Service (Desktop only)
            services.AddSingleton<Desktop.Services.VoiceService>();

            // System Automation & Telemetry Service
            services.AddSingleton<Desktop.Services.SystemControlService>();

            // Vision & Ghost Agent Services
            services.AddSingleton<Desktop.Services.VisionService>();
            services.AddSingleton<Desktop.Services.GhostAgentService>();

            // Tray Service
            services.AddSingleton<Desktop.Services.TrayService>();

            // Settings Window (transient — naya window har baar)
            services.AddTransient<Desktop.Views.SettingsWindow>();

            // Register Main Window
            services.AddSingleton<MainWindow>();

            Services = services.BuildServiceProvider();

            // Launch Main Window
            var mainWindow = Services.GetRequiredService<MainWindow>();
            mainWindow.Show();

            // Initialize Tray Icon
            var tray = Services.GetRequiredService<Desktop.Services.TrayService>();
            tray.Initialize(mainWindow);
        }
    }
}