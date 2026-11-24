using JarvisCoreEngine.Commands;
using JarvisCoreEngine.Core;
using JarvisCoreEngine.Interfaces;
using JarvisCoreEngine.Jobs;
using JarvisCoreEngine.Models;
using JarvisCoreEngine.Services;
using JarvisCoreEngine.Utilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace JarvisCoreEngine
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Console.Title = "JARVIS Core Engine";
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("Initializing JARVIS Core...");
            await Task.Delay(800);

            // ===== CONFIGURATION =====
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            var appSettings = new AppSettings();
            configuration.GetSection("AppSettings").Bind(appSettings);

            var loggingSettings = new LoggingSettings();
            configuration.GetSection("LoggingSettings").Bind(loggingSettings);

            var workspaceSettings = new WorkspaceSettings();
            configuration.GetSection("WorkspaceSettings").Bind(workspaceSettings);

            var backgroundSettings = new BackgroundSettings();
            configuration.GetSection("BackgroundSettings").Bind(backgroundSettings);

            var fileWatcherSettings = new FileWatcherSettings();
            configuration.GetSection("FileWatcherSettings").Bind(fileWatcherSettings);

            var schedulerSettings = new SchedulerSettings();
            configuration.GetSection("SchedulerSettings").Bind(schedulerSettings);

            var httpSettings = new HttpSettings();
            configuration.GetSection("HttpSettings").Bind(httpSettings);

            var aiSettings = new AiSettings();
            configuration.GetSection("AiSettings").Bind(aiSettings);

            var memorySettings = new MemorySettings();
            configuration.GetSection("MemorySettings").Bind(memorySettings);

            // ===== DI CONTAINER =====
            var services = new ServiceCollection();

            // Configuration & Settings
            services.AddSingleton(configuration);
            services.AddSingleton(appSettings);
            services.AddSingleton(loggingSettings);
            services.AddSingleton(workspaceSettings);
            services.AddSingleton(backgroundSettings);
            services.AddSingleton(fileWatcherSettings);
            services.AddSingleton(schedulerSettings);
            services.AddSingleton(httpSettings);
            services.AddSingleton(aiSettings);
            services.AddSingleton(memorySettings);

            // Core Utilities
            services.AddSingleton<IAppLogger, Logger>();

            // HTTP
            services.AddHttpClient();
            services.AddSingleton<HttpService>();

            // Services
            services.AddSingleton<FileService>();
            services.AddSingleton<BackgroundWorkerService>();
            services.AddSingleton<FileWatcherService>();
            services.AddSingleton<SchedulerService>();
            services.AddSingleton<AiService>();
            services.AddSingleton<ICommand, ForgetCommand>();
            services.AddSingleton<MemoryService>();

            // Scheduled Jobs (Plugin-style)
            services.AddSingleton<IScheduledJob, HealthCheckJob>();
            services.AddSingleton<IScheduledJob, WorkspaceReportJob>();

            // Commands (Plugin-style)
            services.AddSingleton<ICommand, TimeCommand>();
            services.AddSingleton<ICommand, ClearCommand>();
            services.AddSingleton<ICommand, ExitCommand>();
            services.AddSingleton<ICommand, HelpCommand>();
            services.AddSingleton<ICommand, WriteFileCommand>();
            services.AddSingleton<ICommand, ReadFileCommand>();
            services.AddSingleton<ICommand, ListFilesCommand>();
            services.AddSingleton<ICommand, DeleteFileCommand>();
            services.AddSingleton<ICommand, FetchCommand>();
            services.AddSingleton<ICommand, AskCommand>();
            services.AddSingleton<ICommand, ResetCommand>();
            services.AddSingleton<ICommand, ForgetCommand>();

            // Core Engine
            services.AddSingleton<CommandRegistry>();
            services.AddSingleton<EngineService>();



            // ===== BUILD PROVIDER =====
            var provider = services.BuildServiceProvider();

            // ===== START BACKGROUND SYSTEMS =====
            var background = provider.GetRequiredService<BackgroundWorkerService>();
            background.Start();

            var fileWatcher = provider.GetRequiredService<FileWatcherService>();
            fileWatcher.Start();

            var scheduler = provider.GetRequiredService<SchedulerService>();
            scheduler.Start();

            // ===== START MAIN ENGINE =====
            var engine = provider.GetRequiredService<EngineService>();
            await engine.StartAsync();
        }
    }
}