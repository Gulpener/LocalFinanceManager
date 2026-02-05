using LocalFinanceManager.Services;
using LocalFinanceManager.DTOs.ML;
using LocalFinanceManager.Configuration;
using LocalFinanceManager.Data;
using LocalFinanceManager.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

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
    private readonly AppDbContext _dbContext;
    private readonly AutomationOptions _automationOptions;
    private readonly ILogger<AutomationController> _logger;

    public AutomationController(
        IUndoService undoService,
        IMonitoringService monitoringService,
        AppDbContext dbContext,
        IOptions<AutomationOptions> automationOptions,
        ILogger<AutomationController> logger)
    {
        _undoService = undoService;
        _monitoringService = monitoringService;
        _dbContext = dbContext;
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
    public async Task<IActionResult> GetSettings()
    {
        // Try to load from database first, fallback to appsettings.json
        var dbSettings = await _dbContext.AppSettings.FindAsync(1);

        if (dbSettings != null)
        {
            var accountIds = string.IsNullOrEmpty(dbSettings.AccountIdsJson)
                ? new List<Guid>()
                : JsonSerializer.Deserialize<List<Guid>>(dbSettings.AccountIdsJson) ?? new List<Guid>();

            var excludedCategoryIds = string.IsNullOrEmpty(dbSettings.ExcludedCategoryIdsJson)
                ? new List<Guid>()
                : JsonSerializer.Deserialize<List<Guid>>(dbSettings.ExcludedCategoryIdsJson) ?? new List<Guid>();

            var settings = new AutoApplySettingsDto
            {
                Enabled = dbSettings.AutoApplyEnabled,
                MinimumConfidence = dbSettings.MinimumConfidence,
                IntervalMinutes = dbSettings.IntervalMinutes,
                AccountIds = accountIds,
                ExcludedCategoryIds = excludedCategoryIds
            };

            return Ok(settings);
        }

        // Fallback to appsettings.json defaults
        var defaultSettings = new AutoApplySettingsDto
        {
            Enabled = _automationOptions.AutoApplyEnabled,
            MinimumConfidence = (float)_automationOptions.ConfidenceThreshold,
            IntervalMinutes = 15,
            AccountIds = new List<Guid>(),
            ExcludedCategoryIds = new List<Guid>()
        };

        return Ok(defaultSettings);
    }

    /// <summary>
    /// Updates auto-apply configuration settings.
    /// </summary>
    /// <param name="settings">New settings</param>
    /// <returns>Success result</returns>
    [HttpPost("settings")]
    public async Task<IActionResult> UpdateSettings([FromBody] AutoApplySettingsDto settings)
    {
        if (!TryValidateModel(settings))
        {
            return ValidationProblem(ModelState);
        }
        try
        {
            // Load or create settings record
            var dbSettings = await _dbContext.AppSettings.FindAsync(1);
            if (dbSettings == null)
            {
                dbSettings = new AppSettings { Id = 1 };
                _dbContext.AppSettings.Add(dbSettings);
            }

            // Update settings
            dbSettings.AutoApplyEnabled = settings.Enabled;
            dbSettings.MinimumConfidence = settings.MinimumConfidence;
            dbSettings.IntervalMinutes = settings.IntervalMinutes;
            dbSettings.AccountIdsJson = settings.AccountIds.Any()
                ? JsonSerializer.Serialize(settings.AccountIds)
                : null;
            dbSettings.ExcludedCategoryIdsJson = settings.ExcludedCategoryIds.Any()
                ? JsonSerializer.Serialize(settings.ExcludedCategoryIds)
                : null;
            dbSettings.UpdatedAt = DateTime.UtcNow;
            dbSettings.UpdatedBy = "System"; // TODO: Replace with actual user when auth is implemented

            await _dbContext.SaveChangesAsync();

            _logger.LogInformation(
                "Auto-apply settings saved: Enabled={Enabled}, Confidence={Confidence}, Accounts={Accounts}, ExcludedCategories={ExcludedCategories}",
                settings.Enabled,
                settings.MinimumConfidence,
                settings.AccountIds.Count,
                settings.ExcludedCategoryIds.Count);

            return Ok(new { success = true, message = "Settings updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save auto-apply settings");
            return StatusCode(500, new
            {
                type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                title = "Internal Server Error",
                status = 500,
                detail = "Failed to save settings. Please try again."
            });
        }
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
