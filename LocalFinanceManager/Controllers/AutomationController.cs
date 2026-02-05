using LocalFinanceManager.Services;
using LocalFinanceManager.DTOs.ML;
using LocalFinanceManager.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace LocalFinanceManager.Controllers;

/// <summary>
/// API controller for automation operations: undo auto-applied assignments and monitoring.
/// </summary>
[ApiController]
[Route("api/automation")]
public class AutomationController : ControllerBase
{
    private readonly IUndoService _undoService;
    private readonly IMonitoringService _monitoringService;
    private readonly AutomationOptions _automationOptions;
    private readonly ILogger<AutomationController> _logger;

    public AutomationController(
        IUndoService undoService,
        IMonitoringService monitoringService,
        IOptions<AutomationOptions> automationOptions,
        ILogger<AutomationController> logger)
    {
        _undoService = undoService;
        _monitoringService = monitoringService;
        _automationOptions = automationOptions.Value;
        _logger = logger;
    }

    /// <summary>
    /// Undoes an auto-applied assignment for a transaction.
    /// </summary>
    /// <param name="transactionId">Transaction ID</param>
    /// <returns>Undo result</returns>
    [HttpPost("undo/{transactionId}")]
    public async Task<IActionResult> UndoAutoApply(Guid transactionId)
    {
        _logger.LogInformation("Undo auto-apply requested for transaction {TransactionId}", transactionId);

        var result = await _undoService.UndoAutoApplyAsync(transactionId);

        if (!result.Success)
        {
            if (result.ConflictDetected)
            {
                return Conflict(new
                {
                    type = "https://tools.ietf.org/html/rfc7231#section-6.5.8",
                    title = "Conflict - Cannot undo",
                    status = 409,
                    detail = result.Message
                });
            }

            return BadRequest(new
            {
                type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                title = "Bad Request",
                status = 400,
                detail = result.Message
            });
        }

        return Ok(new { success = true, message = result.Message });
    }

    /// <summary>
    /// Checks if a transaction can be undone.
    /// </summary>
    /// <param name="transactionId">Transaction ID</param>
    /// <returns>True if undo is available</returns>
    [HttpGet("can-undo/{transactionId}")]
    public async Task<IActionResult> CanUndo(Guid transactionId)
    {
        var canUndo = await _undoService.CanUndoAsync(transactionId);
        return Ok(new { transactionId, canUndo });
    }

    /// <summary>
    /// Gets auto-apply statistics for monitoring dashboard.
    /// </summary>
    /// <param name="windowDays">Number of days to look back (default: 7)</param>
    /// <returns>Auto-apply statistics</returns>
    [HttpGet("stats")]
    public async Task<IActionResult> GetAutoApplyStats([FromQuery] int windowDays = 7)
    {
        if (windowDays < 1 || windowDays > 365)
        {
            return BadRequest(new
            {
                type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                title = "Bad Request",
                status = 400,
                detail = "windowDays must be between 1 and 365"
            });
        }

        var stats = await _monitoringService.GetAutoApplyStatsAsync(windowDays);
        return Ok(stats);
    }

    /// <summary>
    /// Checks if undo rate is above alert threshold.
    /// </summary>
    /// <param name="windowDays">Number of days to look back (default: 7)</param>
    /// <returns>Alert status</returns>
    [HttpGet("undo-rate-alert")]
    public async Task<IActionResult> CheckUndoRateAlert([FromQuery] int windowDays = 7)
    {
        if (windowDays < 1 || windowDays > 365)
        {
            return BadRequest(new
            {
                type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                title = "Bad Request",
                status = 400,
                detail = "windowDays must be between 1 and 365"
            });
        }

        var isAboveThreshold = await _monitoringService.IsUndoRateAboveThresholdAsync(windowDays);
        return Ok(new { windowDays, isAboveThreshold });
    }

    /// <summary>
    /// Gets auto-apply configuration settings.
    /// </summary>
    /// <returns>Auto-apply settings</returns>
    [HttpGet("settings")]
    public IActionResult GetSettings()
    {
        var settings = new AutoApplySettingsDto
        {
            Enabled = _automationOptions.AutoApplyEnabled,
            MinimumConfidence = (float)_automationOptions.ConfidenceThreshold,
            IntervalMinutes = 15, // Default value, can be made configurable
            AccountIds = new List<Guid>(), // Empty = all accounts
            ExcludedCategoryIds = new List<Guid>()
        };

        return Ok(settings);
    }

    /// <summary>
    /// Updates auto-apply configuration settings.
    /// </summary>
    /// <param name="settings">New settings</param>
    /// <returns>Success result</returns>
    [HttpPost("settings")]
    public IActionResult UpdateSettings([FromBody] AutoApplySettingsDto settings)
    {
        if (settings == null)
        {
            return BadRequest(new
            {
                type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                title = "Bad Request",
                status = 400,
                detail = "Settings are required"
            });
        }

        if (settings.MinimumConfidence < 0.0f || settings.MinimumConfidence > 1.0f)
        {
            return BadRequest(new
            {
                type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                title = "Bad Request",
                status = 400,
                detail = "Minimum confidence must be between 0.0 and 1.0",
                errors = new Dictionary<string, string[]>
                {
                    ["MinimumConfidence"] = new[] { "Must be between 0.0 and 1.0" }
                }
            });
        }

        _logger.LogInformation(
            "Auto-apply settings updated: Enabled={Enabled}, Confidence={Confidence}",
            settings.Enabled,
            settings.MinimumConfidence);

        // Note: In production, persist settings to database or configuration file
        // For now, this is a placeholder that accepts but doesn't persist settings

        return Ok(new { success = true, message = "Settings updated successfully" });
    }

    /// <summary>
    /// Gets auto-apply history (last N transactions).
    /// </summary>
    /// <param name="limit">Number of history items to return (default: 50)</param>
    /// <returns>Auto-apply history</returns>
    [HttpGet("history")]
    public async Task<IActionResult> GetHistory([FromQuery] int limit = 50)
    {
        if (limit < 1 || limit > 500)
        {
            return BadRequest(new
            {
                type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                title = "Bad Request",
                status = 400,
                detail = "Limit must be between 1 and 500"
            });
        }

        var history = await _monitoringService.GetAutoApplyHistoryAsync(limit);
        return Ok(history);
    }

    /// <summary>
    /// Gets estimated number of auto-applies based on confidence threshold.
    /// </summary>
    /// <param name="confidence">Confidence threshold</param>
    /// <returns>Preview statistics</returns>
    [HttpGet("preview")]
    public async Task<IActionResult> GetPreviewStats([FromQuery] decimal confidence = 0.8m)
    {
        if (confidence < 0.0m || confidence > 1.0m)
        {
            return BadRequest(new
            {
                type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                title = "Bad Request",
                status = 400,
                detail = "Confidence must be between 0.0 and 1.0"
            });
        }

        var estimate = await _monitoringService.EstimateAutoApplyCountAsync((float)confidence);
        return Ok(new { estimatedAutoApplyCount = estimate });
    }
}
