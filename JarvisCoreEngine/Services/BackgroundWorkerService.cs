using JarvisCoreEngine.Interfaces;
using JarvisCoreEngine.Models;

namespace JarvisCoreEngine.Services
{
    public class BackgroundWorkerService
    {
        private readonly IAppLogger _logger;
        private readonly BackgroundSettings _settings;
        private CancellationTokenSource? _cts;

        public BackgroundWorkerService(IAppLogger logger, BackgroundSettings settings)
        {
            _logger = logger;
            _settings = settings;
        }

        public void Start()
        {
            if (!_settings.EnableBackgroundService)
            {
                _logger.Warn("Background service disabled in config.");
                return;
            }

            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            Task.Run(async () =>
            {
                _logger.Log("Background service started.");

                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        await Heartbeat();
                        await Task.Delay(TimeSpan.FromSeconds(_settings.HeartbeatIntervalSeconds), token);
                    }
                    catch (TaskCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger.Error($"Background error: {ex.Message}");
                    }
                }

                _logger.Warn("Background service stopped.");
            }, token);
        }

        public void Stop()
        {
            _cts?.Cancel();
        }

        private Task Heartbeat()
        {
            _logger.Log($"💓 Heartbeat — system healthy at {DateTime.Now:HH:mm:ss}");
            return Task.CompletedTask;
        }
    }
}