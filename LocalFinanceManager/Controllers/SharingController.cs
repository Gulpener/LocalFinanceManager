using LocalFinanceManager.DTOs;
using LocalFinanceManager.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LocalFinanceManager.Controllers;

/// <summary>
/// API controller for managing account and budget plan sharing.
/// </summary>
[Authorize]
[ApiController]
public class SharingController : ControllerBase
{
    private readonly ISharingService _sharingService;
    private readonly IUserContext _userContext;
    private readonly ILogger<SharingController> _logger;

    public SharingController(ISharingService sharingService, IUserContext userContext, ILogger<SharingController> logger)
    {
        _sharingService = sharingService;
        _userContext = userContext;
        _logger = logger;
    }

    // --- Share ---

    [HttpPost("api/accounts/{id}/share")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AccountShareResponse>> ShareAccount(Guid id, [FromBody] ShareResourceRequest request)
    {
        var currentUserId = _userContext.GetCurrentUserId();
        if (currentUserId == Guid.Empty) return Unauthorized();

        try
        {
            var share = await _sharingService.ShareAccountAsync(id, request.Email, request.Permission, currentUserId);
            var response = AccountShareResponse.FromEntity(share);
            return CreatedAtAction(nameof(GetAccountShares), new { id }, response);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ProblemDetails { Status = 404, Title = "Not found", Detail = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ProblemDetails { Status = 400, Title = "Invalid operation", Detail = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error sharing account");
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails { Status = 500, Title = "Unexpected error" });
        }
    }

    [HttpPost("api/budgetplans/{id}/share")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BudgetPlanShareResponse>> ShareBudgetPlan(Guid id, [FromBody] ShareResourceRequest request)
    {
        var currentUserId = _userContext.GetCurrentUserId();
        if (currentUserId == Guid.Empty) return Unauthorized();

        try
        {
            var share = await _sharingService.ShareBudgetPlanAsync(id, request.Email, request.Permission, currentUserId);
            var response = BudgetPlanShareResponse.FromEntity(share);
            return CreatedAtAction(nameof(GetBudgetPlanShares), new { id }, response);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ProblemDetails { Status = 404, Title = "Not found", Detail = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ProblemDetails { Status = 400, Title = "Invalid operation", Detail = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error sharing budget plan");
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails { Status = 500, Title = "Unexpected error" });
        }
    }

    // --- Accept / Decline ---

    [HttpPost("api/shares/accounts/{id}/accept")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AcceptAccountShare(Guid id)
    {
        var currentUserId = _userContext.GetCurrentUserId();
        if (currentUserId == Guid.Empty) return Unauthorized();

        try
        {
            await _sharingService.AcceptAccountShareAsync(id, currentUserId);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ProblemDetails { Status = 404, Title = "Not found", Detail = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ProblemDetails { Status = 400, Title = "Invalid operation", Detail = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error accepting account share");
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails { Status = 500, Title = "Unexpected error" });
        }
    }

    [HttpPost("api/shares/accounts/{id}/decline")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeclineAccountShare(Guid id)
    {
        var currentUserId = _userContext.GetCurrentUserId();
        if (currentUserId == Guid.Empty) return Unauthorized();

        try
        {
            await _sharingService.DeclineAccountShareAsync(id, currentUserId);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ProblemDetails { Status = 404, Title = "Not found", Detail = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ProblemDetails { Status = 400, Title = "Invalid operation", Detail = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error declining account share");
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails { Status = 500, Title = "Unexpected error" });
        }
    }

    [HttpPost("api/shares/budgetplans/{id}/accept")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AcceptBudgetPlanShare(Guid id)
    {
        var currentUserId = _userContext.GetCurrentUserId();
        if (currentUserId == Guid.Empty) return Unauthorized();

        try
        {
            await _sharingService.AcceptBudgetPlanShareAsync(id, currentUserId);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ProblemDetails { Status = 404, Title = "Not found", Detail = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ProblemDetails { Status = 400, Title = "Invalid operation", Detail = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error accepting budget plan share");
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails { Status = 500, Title = "Unexpected error" });
        }
    }

    [HttpPost("api/shares/budgetplans/{id}/decline")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeclineBudgetPlanShare(Guid id)
    {
        var currentUserId = _userContext.GetCurrentUserId();
        if (currentUserId == Guid.Empty) return Unauthorized();

        try
        {
            await _sharingService.DeclineBudgetPlanShareAsync(id, currentUserId);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ProblemDetails { Status = 404, Title = "Not found", Detail = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ProblemDetails { Status = 400, Title = "Invalid operation", Detail = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error declining budget plan share");
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails { Status = 500, Title = "Unexpected error" });
        }
    }

    // --- Revoke ---

    [HttpDelete("api/shares/accounts/{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RevokeAccountShare(Guid id)
    {
        var currentUserId = _userContext.GetCurrentUserId();
        if (currentUserId == Guid.Empty) return Unauthorized();

        try
        {
            await _sharingService.RevokeAccountShareAsync(id, currentUserId);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ProblemDetails { Status = 404, Title = "Not found", Detail = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error revoking account share");
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails { Status = 500, Title = "Unexpected error" });
        }
    }

    [HttpDelete("api/shares/budgetplans/{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RevokeBudgetPlanShare(Guid id)
    {
        var currentUserId = _userContext.GetCurrentUserId();
        if (currentUserId == Guid.Empty) return Unauthorized();

        try
        {
            await _sharingService.RevokeBudgetPlanShareAsync(id, currentUserId);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ProblemDetails { Status = 404, Title = "Not found", Detail = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error revoking budget plan share");
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails { Status = 500, Title = "Unexpected error" });
        }
    }

    // --- Query ---

    [HttpGet("api/accounts/{id}/shares")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<AccountShareResponse>>> GetAccountShares(Guid id)
    {
        var currentUserId = _userContext.GetCurrentUserId();
        if (currentUserId == Guid.Empty) return Unauthorized();

        try
        {
            var shares = await _sharingService.GetAccountSharesAsync(id, currentUserId);
            return Ok(shares.Select(AccountShareResponse.FromEntity).ToList());
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails { Status = 403, Title = "Forbidden", Detail = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ProblemDetails { Status = 404, Title = "Not found", Detail = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error retrieving shares for account");
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails { Status = 500, Title = "Unexpected error" });
        }
    }

    [HttpGet("api/budgetplans/{id}/shares")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<BudgetPlanShareResponse>>> GetBudgetPlanShares(Guid id)
    {
        var currentUserId = _userContext.GetCurrentUserId();
        if (currentUserId == Guid.Empty) return Unauthorized();

        try
        {
            var shares = await _sharingService.GetBudgetPlanSharesAsync(id, currentUserId);
            return Ok(shares.Select(BudgetPlanShareResponse.FromEntity).ToList());
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails { Status = 403, Title = "Forbidden", Detail = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ProblemDetails { Status = 404, Title = "Not found", Detail = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error retrieving shares for budget plan");
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails { Status = 500, Title = "Unexpected error" });
        }
    }

    [HttpGet("api/shares/pending")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<PendingSharesResponse>> GetPendingShares()
    {
        var currentUserId = _userContext.GetCurrentUserId();
        if (currentUserId == Guid.Empty) return Unauthorized();

        try
        {
            var (accountShares, budgetPlanShares) = await _sharingService.GetPendingSharesForUserAsync(currentUserId);

            var response = new PendingSharesResponse
            {
                AccountShares = accountShares.Select(AccountShareResponse.FromEntity).ToList(),
                BudgetPlanShares = budgetPlanShares.Select(BudgetPlanShareResponse.FromEntity).ToList()
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error retrieving pending shares for user");
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails { Status = 500, Title = "Unexpected error" });
        }
    }
}
