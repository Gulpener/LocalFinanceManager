using LocalFinanceManager.Configuration;
using LocalFinanceManager.ML;
using Microsoft.Extensions.Options;

namespace LocalFinanceManager.Services.Background;

/// <summary>
/// Background service for automated ML model retraining on a scheduled basis.
/// Runs weekly (default: Sunday 2 AM UTC) to train new models on latest labeled data.
/// </summary>
public class MLRetrainingBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MLRetrainingBackgroundService> _logger;
    private readonly MLOptions _mlOptions;
    private readonly AutomationOptions _automationOptions;

    public MLRetrainingBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<MLRetrainingBackgroundService> logger,
        IOptions<MLOptions> mlOptions,
        IOptions<AutomationOptions> automationOptions)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _mlOptions = mlOptions.Value;
        _automationOptions = automationOptions.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ML Retraining Background Service started with schedule: {Schedule}", 
            _automationOptions.RetrainingScheduleCron);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var now = DateTime.UtcNow;
                var nextRun = CronParser.GetNextOccurrence(_automationOptions.RetrainingScheduleCron, now);
                var delay = nextRun - now;

                _logger.LogInformation("Next ML retraining scheduled at {NextRun} UTC (in {Delay})", 
                    nextRun, delay);

                await Task.Delay(delay, stoppingToken);

                if (stoppingToken.IsCancellationRequested)
                {
                    break;
                }

                await ExecuteRetrainingJobAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Expected when service is stopping
                _logger.LogInformation("ML Retraining Background Service is stopping");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ML retraining scheduler loop");
                // Wait 5 minutes before retrying after error
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }

        _logger.LogInformation("ML Retraining Background Service stopped");
    }

    private async Task ExecuteRetrainingJobAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting scheduled ML model retraining job");

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var mlService = scope.ServiceProvider.GetRequiredService<IMLService>();

            var result = await mlService.TrainModelAsync(_mlOptions.TrainingWindowDays);

            if (result.SampleSize == 0)
            {
                _logger.LogWarning("Retraining skipped: No training data available");
                return;
            }

            if (result.MeetsApprovalThreshold)
            {
                _logger.LogInformation(
                    "Retraining completed successfully. Model v{Version} approved with F1 score {F1Score:F4} " +
                    "({SampleSize} samples, {CategoryCount} categories). Model activated.",
                    result.Version,
                    result.F1Score,
                    result.SampleSize,
                    result.CategoryCount);
            }
            else
            {
                _logger.LogWarning(
                    "Retraining completed but model v{Version} rejected. F1 score {F1Score:F4} below threshold {Threshold}. " +
                    "Previous model remains active. ({SampleSize} samples, {CategoryCount} categories)",
                    result.Version,
                    result.F1Score,
                    _mlOptions.MinF1ScoreForApproval,
                    result.SampleSize,
                    result.CategoryCount);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ML retraining job failed. Previous model remains active.");
        }
    }
}
