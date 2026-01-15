using LocalFinanceManager.Configuration;
using LocalFinanceManager.Data.Repositories;
using LocalFinanceManager.DTOs.ML;
using LocalFinanceManager.ML;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace LocalFinanceManager.Controllers;

/// <summary>
/// API controller for ML model training and management.
/// </summary>
[ApiController]
[Route("api/ml")]
public class MLController : ControllerBase
{
    private readonly IMLService _mlService;
    private readonly ILabeledExampleRepository _labeledExampleRepo;
    private readonly MLOptions _options;
    private readonly ILogger<MLController> _logger;

    public MLController(
        IMLService mlService,
        ILabeledExampleRepository labeledExampleRepo,
        IOptions<MLOptions> options,
        ILogger<MLController> logger)
    {
        _mlService = mlService;
        _labeledExampleRepo = labeledExampleRepo;
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>
    /// Triggers manual retraining of the ML model.
    /// </summary>
    /// <param name="windowDays">Optional: override training window days (default from config)</param>
    [HttpPost("retrain")]
    public async Task<ActionResult<ModelMetricsDto>> RetrainModel([FromQuery] int? windowDays = null)
    {
        try
        {
            var trainingWindow = windowDays ?? _options.TrainingWindowDays;

            _logger.LogInformation("Manual retraining triggered with {WindowDays}-day window", trainingWindow);

            // Check if we have enough data
            var countsPerCategory = await _labeledExampleRepo.GetCountPerCategoryAsync(trainingWindow);

            if (countsPerCategory.Count == 0)
            {
                return BadRequest(new
                {
                    type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                    title = "Insufficient training data",
                    status = 400,
                    detail = "No labeled examples available for training. Please assign categories to transactions first."
                });
            }

            var underThreshold = countsPerCategory
                .Where(kvp => kvp.Value < _options.MinLabeledExamplesPerCategory)
                .ToList();

            if (underThreshold.Count == countsPerCategory.Count)
            {
                return BadRequest(new
                {
                    type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                    title = "Insufficient training data",
                    status = 400,
                    detail = $"All categories have fewer than {_options.MinLabeledExamplesPerCategory} labeled examples. More training data is needed."
                });
            }

            // Train the model
            var result = await _mlService.TrainModelAsync(trainingWindow);

            if (result.SampleSize == 0)
            {
                return BadRequest(new
                {
                    type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                    title = "Training failed",
                    status = 400,
                    detail = result.Message
                });
            }

            var metricsDto = new ModelMetricsDto
            {
                Version = result.Version,
                TrainedAt = result.TrainedAt,
                Precision = result.Precision,
                Recall = result.Recall,
                F1Score = result.F1Score,
                SampleSize = result.SampleSize,
                CategoryCount = result.CategoryCount,
                IsActive = result.MeetsApprovalThreshold
            };

            if (!result.MeetsApprovalThreshold)
            {
                _logger.LogWarning(
                    "Model v{Version} trained but does not meet approval threshold. F1 score: {F1Score:F4}, threshold: {Threshold}",
                    result.Version,
                    result.F1Score,
                    _options.MinF1ScoreForApproval);
            }

            return Ok(new
            {
                model = metricsDto,
                message = result.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during model retraining");

            return StatusCode(500, new
            {
                type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                title = "Internal Server Error",
                status = 500,
                detail = "Model training failed. Please check logs for details."
            });
        }
    }

    /// <summary>
    /// Gets training data statistics.
    /// </summary>
    [HttpGet("stats")]
    public async Task<ActionResult> GetTrainingStats()
    {
        try
        {
            var windowDays = _options.TrainingWindowDays;
            var countsPerCategory = await _labeledExampleRepo.GetCountPerCategoryAsync(windowDays);
            var (acceptedCount, totalSuggestions) = await _labeledExampleRepo.GetAcceptanceRateAsync(windowDays);

            var acceptanceRate = totalSuggestions > 0
                ? (double)acceptedCount / totalSuggestions
                : 0.0;

            return Ok(new
            {
                trainingWindowDays = windowDays,
                totalLabeledExamples = countsPerCategory.Values.Sum(),
                categoriesWithData = countsPerCategory.Count,
                categoriesBelowThreshold = countsPerCategory.Count(kvp => kvp.Value < _options.MinLabeledExamplesPerCategory),
                minExamplesThreshold = _options.MinLabeledExamplesPerCategory,
                acceptanceRate = acceptanceRate,
                acceptedSuggestions = acceptedCount,
                totalSuggestions = totalSuggestions,
                countsPerCategory = countsPerCategory
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving training statistics");

            return StatusCode(500, new
            {
                type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                title = "Internal Server Error",
                status = 500,
                detail = "Failed to retrieve training statistics. Please try again later."
            });
        }
    }
}
