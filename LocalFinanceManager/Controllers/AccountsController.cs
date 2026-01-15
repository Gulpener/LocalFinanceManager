using FluentValidation;
using LocalFinanceManager.DTOs;
using LocalFinanceManager.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LocalFinanceManager.Controllers;

/// <summary>
/// API controller for account management.
/// </summary>
[ApiController]
[Route("api/accounts")]
public class AccountsController : ControllerBase
{
    private readonly AccountService _accountService;
    private readonly IValidator<CreateAccountRequest> _createValidator;
    private readonly IValidator<UpdateAccountRequest> _updateValidator;
    private readonly ILogger<AccountsController> _logger;

    public AccountsController(
        AccountService accountService,
        IValidator<CreateAccountRequest> createValidator,
        IValidator<UpdateAccountRequest> updateValidator,
        ILogger<AccountsController> logger)
    {
        _accountService = accountService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _logger = logger;
    }

    /// <summary>
    /// Get all active accounts.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<AccountResponse>>> GetAll()
    {
        var accounts = await _accountService.GetAllActiveAsync();
        return Ok(accounts);
    }

    /// <summary>
    /// Get an account by ID.
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AccountResponse>> GetById(Guid id)
    {
        var account = await _accountService.GetByIdAsync(id);
        if (account == null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Account not found",
                Detail = $"Account with ID {id} was not found."
            });
        }

        return Ok(account);
    }

    /// <summary>
    /// Create a new account.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AccountResponse>> Create([FromBody] CreateAccountRequest request)
    {
        var validationResult = await _createValidator.ValidateAsync(request);
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

        var account = await _accountService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = account.Id }, account);
    }

    /// <summary>
    /// Update an existing account.
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AccountResponse>> Update(Guid id, [FromBody] UpdateAccountRequest request)
    {
        var validationResult = await _updateValidator.ValidateAsync(request);
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
            var account = await _accountService.UpdateAsync(id, request);
            if (account == null)
            {
                return NotFound(new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Account not found",
                    Detail = $"Account with ID {id} was not found."
                });
            }

            return Ok(account);
        }
        catch (DbUpdateConcurrencyException)
        {
            // Reload the current state
            var currentAccount = await _accountService.GetByIdAsync(id);
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Concurrency conflict",
                Detail = "The account was modified by another user. Please reload and try again.",
                Extensions = { ["currentState"] = currentAccount }
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid operation",
                Detail = ex.Message
            });
        }
    }

    /// <summary>
    /// Archive (soft-delete) an account.
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Archive(Guid id)
    {
        try
        {
            var result = await _accountService.ArchiveAsync(id);
            if (!result)
            {
                return NotFound(new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Account not found",
                    Detail = $"Account with ID {id} was not found."
                });
            }

            return NoContent();
        }
        catch (DbUpdateConcurrencyException)
        {
            var currentAccount = await _accountService.GetByIdAsync(id);
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Concurrency conflict",
                Detail = "The account was modified by another user. Please reload and try again.",
                Extensions = { ["currentState"] = currentAccount }
            });
        }
    }

    /// <summary>
    /// Unarchive (restore) an account.
    /// </summary>
    [HttpPost("{id}/unarchive")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Unarchive(Guid id)
    {
        try
        {
            var result = await _accountService.UnarchiveAsync(id);
            if (!result)
            {
                return NotFound(new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Account not found",
                    Detail = $"Account with ID {id} was not found."
                });
            }

            return NoContent();
        }
        catch (DbUpdateConcurrencyException)
        {
            var currentAccount = await _accountService.GetByIdAsync(id);
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Concurrency conflict",
                Detail = "The account was modified by another user. Please reload and try again.",
                Extensions = { ["currentState"] = currentAccount }
            });
        }
    }
}
