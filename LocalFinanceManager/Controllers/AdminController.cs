using LocalFinanceManager.DTOs;
using LocalFinanceManager.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LocalFinanceManager.Controllers;

/// <summary>
/// API controller for admin-only management operations.
/// All endpoints require the "AdminPolicy" authorization policy, which enforces <c>IsAdmin = true</c>
/// in the database. Non-admins receive HTTP 403.
/// </summary>
[ApiController]
[Route("api/admin")]
[Authorize(Policy = "AdminPolicy")]
public class AdminController : ControllerBase
{
    private readonly IAdminService _adminService;
    private readonly IUserContext _userContext;
    private readonly ILogger<AdminController> _logger;

    public AdminController(IAdminService adminService, IUserContext userContext, ILogger<AdminController> logger)
    {
        _adminService = adminService;
        _userContext = userContext;
        _logger = logger;
    }

    /// <summary>
    /// Returns all registered (non-archived) users with account and share counts.
    /// </summary>
    [HttpGet("users")]
    [ProducesResponseType(typeof(List<UserSummaryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<List<UserSummaryResponse>>> GetUsers(CancellationToken ct)
    {
        try
        {
            var users = await _adminService.GetAllUsersAsync(ct);
            return Ok(users);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error retrieving all users for admin");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ProblemDetails { Status = 500, Title = "Unexpected error" });
        }
    }

    /// <summary>
    /// Returns all active shares given by the specified user.
    /// Unknown users currently return empty share lists.
    /// </summary>
    [HttpGet("users/{id:guid}/shares")]
    [ProducesResponseType(typeof(UserSharesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<UserSharesResponse>> GetUserShares(Guid id, CancellationToken ct)
    {
        try
        {
            var shares = await _adminService.GetUserSharesAsync(id, ct);
            return Ok(shares);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error retrieving shares for user {UserId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ProblemDetails { Status = 500, Title = "Unexpected error" });
        }
    }

    /// <summary>
    /// Toggles the <c>IsAdmin</c> flag of the target user.
    /// Returns HTTP 400 when the requesting admin attempts to change their own role.
    /// </summary>
    [HttpPost("users/{id:guid}/toggle-admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ToggleAdmin(Guid id, CancellationToken ct)
    {
        var requestingUserId = _userContext.GetCurrentUserId();
        if (requestingUserId == Guid.Empty) return Unauthorized();

        try
        {
            await _adminService.ToggleAdminAsync(id, requestingUserId, ct);
            return Ok();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ProblemDetails { Status = 400, Title = "Bad request", Detail = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ProblemDetails { Status = 404, Title = "Not found", Detail = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error toggling admin for user {UserId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ProblemDetails { Status = 500, Title = "Unexpected error" });
        }
    }
}
