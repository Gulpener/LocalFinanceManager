using LocalFinanceManager.Configuration;
using LocalFinanceManager.Services;

namespace LocalFinanceManager.Services.Background;

/// <summary>
/// Background service for automatically applying ML category suggestions to transactions
/// when confidence exceeds the configured threshold.
/// Runs on a configurable interval sourced from persisted AppSettings.
/// </summary>
public class AutoApplyBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AutoApplyBackgroundService> _logger;
    private readonly IAutoApplySettingsProvider _settingsProvider;

    public AutoApplyBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<AutoApplyBackgroundService> logger,
        IAutoApplySettingsProvider settingsProvider)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _settingsProvider = settingsProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Auto-Apply Background Service started (runtime settings source: AppSettings with cache)");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var runtimeSettings = await _settingsProvider.GetSettingsAsync(stoppingToken);

                var now = DateTime.UtcNow;
                var delay = TimeSpan.FromMinutes(Math.Max(1, runtimeSettings.IntervalMinutes));
                var nextRun = now.Add(delay);

                _logger.LogInformation("Next auto-apply job scheduled at {NextRun} UTC (in {Delay}, enabled={Enabled}, threshold={Threshold:F2})",
                    nextRun,
                    delay,
                    runtimeSettings.Enabled,
                    runtimeSettings.MinimumConfidence);

                await Task.Delay(delay, stoppingToken);

                if (stoppingToken.IsCancellationRequested)
                {
                    break;
                }

                runtimeSettings = await _settingsProvider.GetSettingsAsync(stoppingToken);

                if (runtimeSettings.Enabled)
                {
                    await ExecuteAutoApplyJobAsync(runtimeSettings, stoppingToken);
                }
                else
                {
                    _logger.LogInformation("Auto-apply job skipped: AutoApplyEnabled is false");
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Auto-Apply Background Service is stopping");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in auto-apply scheduler loop");
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }

        _logger.LogInformation("Auto-Apply Background Service stopped");
    }

    private async Task ExecuteAutoApplyJobAsync(AutoApplyRuntimeSettings runtimeSettings, CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var jobService = scope.ServiceProvider.GetRequiredService<IAutoApplyJobService>();
            await jobService.ExecuteJobAsync(runtimeSettings, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Auto-apply job failed");
        }
    }
}
