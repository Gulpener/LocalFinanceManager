using LocalFinanceManager.Configuration;
using LocalFinanceManager.Data;
using LocalFinanceManager.Services;
using Microsoft.EntityFrameworkCore;

namespace LocalFinanceManager.Services.Background;

/// <summary>
/// Background service for automatically applying ML category suggestions to transactions
/// when confidence exceeds the configured threshold.
/// Runs on a configurable interval. The interval and enabled flag are read per-user from
/// persisted AppSettings so each user's preferences are respected independently.
/// When no user-specific settings exist the service falls back to global defaults.
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
                // Use global (no-user) settings only to determine the scheduler interval.
                // Per-user job execution uses each user's own settings.
                var globalSettings = await _settingsProvider.GetSettingsAsync(stoppingToken);

                var now = DateTime.UtcNow;
                var delay = TimeSpan.FromMinutes(Math.Max(1, globalSettings.IntervalMinutes));
                var nextRun = now.Add(delay);

                _logger.LogInformation("Next auto-apply cycle scheduled at {NextRun} UTC (in {Delay})",
                    nextRun, delay);

                await Task.Delay(delay, stoppingToken);

                if (stoppingToken.IsCancellationRequested)
                {
                    break;
                }

                await ExecutePerUserJobsAsync(stoppingToken);
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

    /// <summary>
    /// Executes the auto-apply job for every active user that has enabled auto-apply in their settings.
    /// </summary>
    private async Task ExecutePerUserJobsAsync(CancellationToken cancellationToken)
    {
        List<Guid> userIds;
        using (var scope = _serviceProvider.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            userIds = await dbContext.Users
                .AsNoTracking()
                .Where(u => !u.IsArchived)
                .Select(u => u.Id)
                .ToListAsync(cancellationToken);
        }

        _logger.LogInformation("Auto-apply cycle: processing {UserCount} user(s)", userIds.Count);

        foreach (var userId in userIds)
        {
            if (cancellationToken.IsCancellationRequested) break;

            var userSettings = await _settingsProvider.GetSettingsAsync(userId, cancellationToken);

            if (!userSettings.Enabled)
            {
                _logger.LogDebug("Auto-apply skipped for user {UserId}: AutoApplyEnabled is false", userId);
                continue;
            }

            await ExecuteAutoApplyJobAsync(userId, userSettings, cancellationToken);
        }
    }

    private async Task ExecuteAutoApplyJobAsync(Guid userId, AutoApplyRuntimeSettings runtimeSettings, CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var jobService = scope.ServiceProvider.GetRequiredService<IAutoApplyJobService>();
            await jobService.ExecuteJobAsync(runtimeSettings, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Auto-apply job failed for user {UserId}", userId);
        }
    }
}
