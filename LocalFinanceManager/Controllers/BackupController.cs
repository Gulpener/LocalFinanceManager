using System.Text;
using System.Text.Json;
using LocalFinanceManager.DTOs;
using LocalFinanceManager.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LocalFinanceManager.Controllers;

[Authorize]
[ApiController]
[Route("api/backup")]
public class BackupController : ControllerBase
{
    private readonly IBackupService _backupService;
    private readonly IUserContext _userContext;
    private readonly ILogger<BackupController> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public BackupController(
        IBackupService backupService,
        IUserContext userContext,
        ILogger<BackupController> logger)
    {
        _backupService = backupService;
        _userContext = userContext;
        _logger = logger;
    }

    /// <summary>
    /// Export all user data as a JSON backup file.
    /// </summary>
    [HttpGet("export")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Export()
    {
        var userId = _userContext.GetCurrentUserId();
        if (userId == Guid.Empty)
            return Unauthorized();

        _logger.LogInformation("Backup export requested by user {UserId}", userId);

        var backup = await _backupService.CreateBackupAsync(userId);
        var json = JsonSerializer.Serialize(backup, JsonOptions);
        var bytes = Encoding.UTF8.GetBytes(json);
        var fileName = $"backup-{DateTime.UtcNow:yyyyMMdd}.json";

        return File(bytes, "application/json", fileName);
    }

    /// <summary>
    /// Validate a backup without committing any changes.
    /// </summary>
    [HttpPost("validate")]
    [RequestSizeLimit(10_485_760)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<BackupValidationResultDto>> Validate([FromBody] BackupData backup)
    {
        var userId = _userContext.GetCurrentUserId();
        if (userId == Guid.Empty)
            return Unauthorized();

        var result = await _backupService.ValidateBackupAsync(backup, userId);
        return Ok(result);
    }

    /// <summary>
    /// Restore data from a backup using the specified conflict resolution strategy.
    /// </summary>
    [HttpPost("restore")]
    [RequestSizeLimit(10_485_760)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<BackupRestoreResultDto>> Restore([FromBody] RestoreRequest request)
    {
        var userId = _userContext.GetCurrentUserId();
        if (userId == Guid.Empty)
            return Unauthorized();

        _logger.LogInformation("Backup restore requested by user {UserId} with strategy {Strategy}", userId, request.Strategy);

        var result = await _backupService.RestoreBackupAsync(userId, request.Backup, request.Strategy);

        if (!result.Success)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Restore failed",
                Detail = string.Join("; ", result.Errors),
                Extensions = { ["errors"] = result.Errors }
            });
        }

        return Ok(result);
    }
}
