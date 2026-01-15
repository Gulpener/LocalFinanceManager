using LocalFinanceManager.Data.Repositories;
using LocalFinanceManager.DTOs.ML;
using LocalFinanceManager.ML;
using LocalFinanceManager.Models;
using Microsoft.AspNetCore.Mvc;

namespace LocalFinanceManager.Controllers;

/// <summary>
/// API controller for ML-based category suggestions.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class SuggestionsController : ControllerBase
{
    private readonly IMLService _mlService;
    private readonly ILabeledExampleRepository _labeledExampleRepo;
    private readonly ILogger<SuggestionsController> _logger;

    public SuggestionsController(
        IMLService mlService,
        ILabeledExampleRepository labeledExampleRepo,
        ILogger<SuggestionsController> logger)
    {
        _mlService = mlService;
        _labeledExampleRepo = labeledExampleRepo;
        _logger = logger;
    }

    /// <summary>
    /// Gets category suggestion for a transaction.
    /// </summary>
    /// <param name="transactionId">Transaction ID</param>
    /// <returns>Category suggestion with confidence and explanation</returns>
    [HttpGet]
    public async Task<ActionResult<SuggestionDto>> GetSuggestion([FromQuery] Guid transactionId)
    {
        if (transactionId == Guid.Empty)
        {
            return BadRequest(new
            {
                type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                title = "Invalid request",
                status = 400,
                detail = "Transaction ID is required"
            });
        }

        try
        {
            var prediction = await _mlService.PredictCategoryAsync(transactionId);

            if (prediction == null)
            {
                return NotFound(new
                {
                    type = "https://tools.ietf.org/html/rfc7231#section-6.5.4",
                    title = "Not found",
                    status = 404,
                    detail = $"No suggestion available for transaction {transactionId}. Transaction may not exist or no ML model is active."
                });
            }

            var suggestionDto = new SuggestionDto
            {
                CategoryId = prediction.CategoryId,
                CategoryName = prediction.CategoryName,
                Confidence = prediction.Confidence,
                ModelVersion = prediction.ModelVersion,
                Explanation = prediction.TopFeatures.Select(f => new FeatureExplanationDto
                {
                    FeatureName = f.FeatureName,
                    FeatureValue = f.FeatureValue,
                    Importance = f.Importance
                }).ToList()
            };

            _logger.LogInformation(
                "Generated suggestion for transaction {TransactionId}: category {CategoryName} with confidence {Confidence:F4}",
                transactionId,
                prediction.CategoryName,
                prediction.Confidence);

            return Ok(suggestionDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating suggestion for transaction {TransactionId}", transactionId);

            return StatusCode(500, new
            {
                type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                title = "Internal Server Error",
                status = 500,
                detail = "Failed to generate category suggestion. Please try again later."
            });
        }
    }

    /// <summary>
    /// Records user feedback on a suggestion (accept/override).
    /// </summary>
    /// <param name="feedback">Feedback data</param>
    [HttpPost("feedback")]
    public async Task<IActionResult> RecordFeedback([FromBody] SuggestionFeedbackDto feedback)
    {
        try
        {
            var labeledExample = new LabeledExample
            {
                Id = Guid.NewGuid(),
                TransactionId = feedback.TransactionId,
                CategoryId = feedback.FinalCategoryId,
                UserId = null, // Single-user mode for now
                WasAutoApplied = false,
                AcceptedSuggestion = feedback.Accepted,
                SuggestionConfidence = feedback.SuggestionConfidence,
                ModelVersion = feedback.ModelVersion,
                IsArchived = false
            };

            await _labeledExampleRepo.AddAsync(labeledExample);

            _logger.LogInformation(
                "Recorded feedback for transaction {TransactionId}: accepted={Accepted}, category={CategoryId}",
                feedback.TransactionId,
                feedback.Accepted,
                feedback.FinalCategoryId);

            return Ok(new
            {
                message = "Feedback recorded successfully",
                labeledExampleId = labeledExample.Id
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording feedback for transaction {TransactionId}", feedback.TransactionId);

            return StatusCode(500, new
            {
                type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                title = "Internal Server Error",
                status = 500,
                detail = "Failed to record feedback. Please try again later."
            });
        }
    }

    /// <summary>
    /// Gets active model information.
    /// </summary>
    [HttpGet("active-model")]
    public async Task<ActionResult<ModelMetricsDto>> GetActiveModel()
    {
        try
        {
            var activeModel = await _mlService.GetActiveModelAsync();

            if (activeModel == null)
            {
                return NotFound(new
                {
                    type = "https://tools.ietf.org/html/rfc7231#section-6.5.4",
                    title = "Not found",
                    status = 404,
                    detail = "No active ML model available"
                });
            }

            // Parse metrics JSON
            var metricsJson = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(activeModel.Metrics);

            var metricsDto = new ModelMetricsDto
            {
                Version = activeModel.Version,
                TrainedAt = activeModel.TrainedAt,
                Precision = metricsJson.TryGetProperty("MacroAccuracy", out var precision) ? precision.GetDouble() : 0,
                Recall = metricsJson.TryGetProperty("MacroAccuracy", out var recall) ? recall.GetDouble() : 0,
                F1Score = metricsJson.TryGetProperty("F1Score", out var f1) ? f1.GetDouble() : 0,
                SampleSize = 0, // Not stored in metrics JSON currently
                CategoryCount = 0, // Not stored in metrics JSON currently
                IsActive = true
            };

            return Ok(metricsDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active model information");

            return StatusCode(500, new
            {
                type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                title = "Internal Server Error",
                status = 500,
                detail = "Failed to retrieve model information. Please try again later."
            });
        }
    }
}
