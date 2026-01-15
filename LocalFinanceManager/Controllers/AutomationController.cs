using LocalFinanceManager.Services;
using Microsoft.AspNetCore.Mvc;

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
    private readonly ILogger<AutomationController> _logger;

    public AutomationController(
        IUndoService undoService,
        IMonitoringService monitoringService,
        ILogger<AutomationController> logger)
    {
        _undoService = undoService;
        _monitoringService = monitoringService;
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
}
