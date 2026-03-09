using LocalFinanceManager.Services;
using LocalFinanceManager.DTOs.ML;
using LocalFinanceManager.Configuration;
using LocalFinanceManager.Data;
using LocalFinanceManager.Models;
using LocalFinanceManager.Services.Background;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using FluentValidation;
using System.Text.Json;

namespace LocalFinanceManager.Controllers;

/// <summary>
/// API controller for automation operations: undo auto-applied assignments and monitoring.
/// </summary>
[Authorize]
[ApiController]
[Route("api/automation")]
[IgnoreAntiforgeryToken]
public class AutomationController : ControllerBase
{
    private readonly IUndoService _undoService;
    private readonly IMonitoringService _monitoringService;
    private readonly IAutoApplyJobService _jobService;
    private readonly AppDbContext _dbContext;
    private readonly AutomationOptions _automationOptions;
    private readonly IValidator<AutoApplySettingsDto> _settingsValidator;
    private readonly IAutoApplySettingsProvider _settingsProvider;
    private readonly IUserContext _userContext;
    private readonly ILogger<AutomationController> _logger;

    public AutomationController(
        IUndoService undoService,
        IMonitoringService monitoringService,
        IAutoApplyJobService jobService,
        AppDbContext dbContext,
        IOptions<AutomationOptions> automationOptions,
        IValidator<AutoApplySettingsDto> settingsValidator,
        IAutoApplySettingsProvider settingsProvider,
        IUserContext userContext,
        ILogger<AutomationController> logger)
    {
        _undoService = undoService;
        _monitoringService = monitoringService;
        _jobService = jobService;
        _dbContext = dbContext;
        _automationOptions = automationOptions.Value;
        _settingsValidator = settingsValidator;
        _settingsProvider = settingsProvider;
        _userContext = userContext;
        _logger = logger;
    }

    /// <summary>
    /// Manually triggers the auto-apply job immediately (does not wait for scheduled interval).
    /// Reads current persisted settings; returns a 503 if auto-apply is disabled.
    /// </summary>
    [HttpPost("run-now")]
    public async Task<IActionResult> RunNow(CancellationToken cancellationToken)
    {
        var currentUserId = _userContext.GetCurrentUserId();
        if (currentUserId == Guid.Empty)
        {
            return Unauthorized();
        }

        _logger.LogInformation("Manual auto-apply trigger requested");

        // Read settings directly from the database (bypasses the in-memory cache) so that
        // a manual trigger always reflects the latest persisted state without racing against
        // the background service's cache population.
        var dbSettings = await _dbContext.AppSettings
            .AsNoTracking()
            .Where(s => !s.IsArchived && s.UserId == currentUserId)
            .FirstOrDefaultAsync(cancellationToken);

        bool enabled = dbSettings?.AutoApplyEnabled ?? _automationOptions.AutoApplyEnabled;

        if (!enabled)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                type = "https://tools.ietf.org/html/rfc7231#section-6.6.4",
                title = "Service Unavailable",
                status = 503,
                detail = "Auto-apply is currently disabled. Enable it in settings before triggering manually."
            });
        }

        // Build runtime settings from the fresh DB row (falling back to config defaults)
        var runtimeSettings = dbSettings != null
            ? new AutoApplyRuntimeSettings
            {
                Enabled = dbSettings.AutoApplyEnabled,
                MinimumConfidence = dbSettings.MinimumConfidence,
                IntervalMinutes = dbSettings.IntervalMinutes,
                AccountIds = AutoApplySettingsProvider.DeserializeGuidList(dbSettings.AccountIdsJson),
                ExcludedCategoryIds = AutoApplySettingsProvider.DeserializeGuidList(dbSettings.ExcludedCategoryIdsJson).ToHashSet()
            }
            : new AutoApplyRuntimeSettings
            {
                Enabled = true,
                MinimumConfidence = (float)_automationOptions.ConfidenceThreshold,
                IntervalMinutes = 15
            };

        var result = await _jobService.ExecuteJobAsync(runtimeSettings, cancellationToken);

        return Ok(new
        {
            success = true,
            appliedCount = result.AppliedCount,
            skippedCount = result.SkippedCount,
            averageConfidence = result.AverageConfidence
        });
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
        var currentUserId = _userContext.GetCurrentUserId();
        if (currentUserId == Guid.Empty)
        {
            return Unauthorized();
        }

        // Try to load from database first, fallback to appsettings.json
        var dbSettings = await _dbContext.AppSettings
            .Where(s => !s.IsArchived && s.UserId == currentUserId)
            .FirstOrDefaultAsync();

        if (dbSettings != null)
        {
            return Ok(ToDto(dbSettings));
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
    public async Task<IActionResult> UpdateSettings([FromBody] AutoApplySettingsDto? settings)
    {
        var currentUserId = _userContext.GetCurrentUserId();
        if (currentUserId == Guid.Empty)
        {
            return Unauthorized();
        }

        if (settings == null)
        {
            return ValidationProblem(new ValidationProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "One or more validation errors occurred.",
                Errors = new Dictionary<string, string[]>
                {
                    ["settings"] = new[] { "Settings payload is required." }
                }
            });
        }

        var validationResult = await _settingsValidator.ValidateAsync(settings);
        if (!validationResult.IsValid)
        {
            return ValidationProblem(new ValidationProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "One or more validation errors occurred.",
                Errors = validationResult.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())
            });
        }


        try
        {
            var accountIds = settings.AccountIds ?? new List<Guid>();
            var excludedCategoryIds = settings.ExcludedCategoryIds ?? new List<Guid>();
            var accountIdsJson = accountIds.Any()
                ? JsonSerializer.Serialize(accountIds)
                : null;
            var excludedCategoryIdsJson = excludedCategoryIds.Any()
                ? JsonSerializer.Serialize(excludedCategoryIds)
                : null;

            var existingSettings = await _dbContext.AppSettings
                .FirstOrDefaultAsync(s => !s.IsArchived && s.UserId == currentUserId);

            if (existingSettings == null)
            {
                existingSettings = new AppSettings
                {
                    UserId = currentUserId,
                    IsArchived = false
                };
                await _dbContext.AppSettings.AddAsync(existingSettings);
            }

            existingSettings.AutoApplyEnabled = settings.Enabled;
            existingSettings.MinimumConfidence = settings.MinimumConfidence;
            existingSettings.IntervalMinutes = settings.IntervalMinutes;
            existingSettings.AccountIdsJson = accountIdsJson;
            existingSettings.ExcludedCategoryIdsJson = excludedCategoryIdsJson;
            existingSettings.UpdatedBy = _userContext.GetCurrentUserEmail();
            existingSettings.IsArchived = false;

            await _dbContext.SaveChangesAsync();

            _settingsProvider.Invalidate(currentUserId);

            _logger.LogInformation(
                "Auto-apply settings saved: Enabled={Enabled}, Confidence={Confidence}, Accounts={Accounts}, ExcludedCategories={ExcludedCategories}",
                settings.Enabled,
                settings.MinimumConfidence,
                accountIds.Count,
                excludedCategoryIds.Count);

            return Ok(new { success = true, message = "Settings updated successfully" });
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Concurrency conflict while saving auto-apply settings");

            var currentSettings = await _dbContext.AppSettings
                .AsNoTracking()
                .Where(s => !s.IsArchived && s.UserId == currentUserId)
                .FirstOrDefaultAsync();

            var currentState = TryMapToDtoForConflict(currentSettings);

            return Conflict(new
            {
                title = "Concurrency conflict",
                status = 409,
                detail = "The settings were modified by another user. Please reload and try again.",
                currentState
            });
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

    private static AutoApplySettingsDto ToDto(AppSettings dbSettings)
    {
        return new AutoApplySettingsDto
        {
            Enabled = dbSettings.AutoApplyEnabled,
            MinimumConfidence = dbSettings.MinimumConfidence,
            IntervalMinutes = dbSettings.IntervalMinutes,
            AccountIds = DeserializeGuidList(dbSettings.AccountIdsJson),
            ExcludedCategoryIds = DeserializeGuidList(dbSettings.ExcludedCategoryIdsJson)
        };
    }

    private AutoApplySettingsDto? TryMapToDtoForConflict(AppSettings? dbSettings)
    {
        if (dbSettings == null)
        {
            return null;
        }

        try
        {
            return ToDto(dbSettings);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to parse persisted automation settings JSON while building concurrency conflict response. Returning sanitized fallback values.");

            return new AutoApplySettingsDto
            {
                Enabled = dbSettings.AutoApplyEnabled,
                MinimumConfidence = dbSettings.MinimumConfidence,
                IntervalMinutes = dbSettings.IntervalMinutes,
                AccountIds = new List<Guid>(),
                ExcludedCategoryIds = new List<Guid>()
            };
        }
    }

    private static List<Guid> DeserializeGuidList(string? json)
    {
        if (string.IsNullOrEmpty(json))
        {
            return new List<Guid>();
        }

        try
        {
            return JsonSerializer.Deserialize<List<Guid>>(json) ?? new List<Guid>();
        }
        catch (JsonException)
        {
            return new List<Guid>();
        }
    }
}
