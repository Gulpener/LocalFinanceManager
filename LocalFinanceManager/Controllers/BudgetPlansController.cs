using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LocalFinanceManager.Services;
using LocalFinanceManager.DTOs;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace LocalFinanceManager.Controllers;

/// <summary>
/// API controller for managing budget plans and budget lines.
/// </summary>
[Authorize]
[ApiController]
[Route("api/[controller]")]
public class BudgetPlansController : ControllerBase
{
    private readonly BudgetPlanService _budgetPlanService;
    private readonly IValidator<CreateBudgetPlanDto> _createPlanValidator;
    private readonly IValidator<UpdateBudgetPlanDto> _updatePlanValidator;
    private readonly IValidator<CreateBudgetLineDto> _createLineValidator;
    private readonly IValidator<UpdateBudgetLineDto> _updateLineValidator;
    private readonly ILogger<BudgetPlansController> _logger;

    public BudgetPlansController(
        BudgetPlanService budgetPlanService,
        IValidator<CreateBudgetPlanDto> createPlanValidator,
        IValidator<UpdateBudgetPlanDto> updatePlanValidator,
        IValidator<CreateBudgetLineDto> createLineValidator,
        IValidator<UpdateBudgetLineDto> updateLineValidator,
        ILogger<BudgetPlansController> logger)
    {
        _budgetPlanService = budgetPlanService;
        _createPlanValidator = createPlanValidator;
        _updatePlanValidator = updatePlanValidator;
        _createLineValidator = createLineValidator;
        _updateLineValidator = updateLineValidator;
        _logger = logger;
    }

    /// <summary>
    /// Get all budget plans for a specific account.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<BudgetPlanDto>>> GetByAccount([FromQuery] Guid accountId)
    {
        if (accountId == Guid.Empty)
        {
            return BadRequest(new { title = "Account ID is required", status = 400 });
        }

        var plans = await _budgetPlanService.GetByAccountIdAsync(accountId);
        return Ok(plans);
    }

    /// <summary>
    /// Get a budget plan by ID.
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<BudgetPlanDto>> GetById(Guid id)
    {
        var plan = await _budgetPlanService.GetByIdAsync(id);
        if (plan == null)
        {
            return NotFound(new { title = "Budget plan not found", status = 404 });
        }
        return Ok(plan);
    }

    /// <summary>
    /// Get a budget plan for a specific account and year.
    /// </summary>
    [HttpGet("account/{accountId}/year/{year}")]
    public async Task<ActionResult<BudgetPlanDto>> GetByAccountAndYear(Guid accountId, int year)
    {
        var plan = await _budgetPlanService.GetByAccountAndYearAsync(accountId, year);
        if (plan == null)
        {
            return NotFound(new { title = "Budget plan not found", status = 404 });
        }
        return Ok(plan);
    }

    /// <summary>
    /// Create a new budget plan.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<BudgetPlanDto>> Create([FromBody] CreateBudgetPlanDto request)
    {
        var validationResult = await _createPlanValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            return BadRequest(new
            {
                title = "Validation failed",
                status = 400,
                errors = validationResult.Errors.ToDictionary(e => e.PropertyName, e => e.ErrorMessage)
            });
        }

        try
        {
            var plan = await _budgetPlanService.CreateAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = plan.Id }, plan);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation while creating budget plan.");
            return BadRequest(new { title = "Invalid operation", status = 400, detail = "The budget plan request could not be processed." });
        }
    }

    /// <summary>
    /// Update an existing budget plan.
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<BudgetPlanDto>> Update(Guid id, [FromBody] UpdateBudgetPlanDto request)
    {
        var validationResult = await _updatePlanValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            return BadRequest(new
            {
                title = "Validation failed",
                status = 400,
                errors = validationResult.Errors.ToDictionary(e => e.PropertyName, e => e.ErrorMessage)
            });
        }

        try
        {
            var plan = await _budgetPlanService.UpdateAsync(id, request);
            if (plan == null)
            {
                return NotFound(new { title = "Budget plan not found", status = 404 });
            }
            return Ok(plan);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized attempt to update budget plan {Id}", id);
            return StatusCode(403, new { title = "Forbidden", status = 403, detail = ex.Message });
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Concurrency conflict updating budget plan {Id}", id);
            return Conflict(new { title = "Concurrency conflict", status = 409, detail = "The budget plan has been modified by another user." });
        }
    }

    /// <summary>
    /// Archive a budget plan (soft delete).
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Archive(Guid id)
    {
        try
        {
            var success = await _budgetPlanService.ArchiveAsync(id);
            if (!success)
            {
                return NotFound(new { title = "Budget plan not found", status = 404 });
            }
            return NoContent();
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized attempt to archive budget plan {Id}", id);
            return StatusCode(403, new { title = "Forbidden", status = 403, detail = ex.Message });
        }
    }

    /// <summary>
    /// Create a new budget line.
    /// </summary>
    [HttpPost("lines")]
    public async Task<ActionResult<BudgetLineDto>> CreateLine([FromBody] CreateBudgetLineDto request)
    {
        var validationResult = await _createLineValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            return BadRequest(new
            {
                title = "Validation failed",
                status = 400,
                errors = validationResult.Errors.ToDictionary(e => e.PropertyName, e => e.ErrorMessage)
            });
        }

        try
        {
            var line = await _budgetPlanService.CreateLineAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = line.BudgetPlanId }, line);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized attempt to create budget line for plan {PlanId}", request.BudgetPlanId);
            return StatusCode(403, new { title = "Forbidden", status = 403, detail = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation while creating budget line.");
            return BadRequest(new { title = "Invalid operation", status = 400, detail = "The budget line request could not be processed." });
        }
    }

    /// <summary>
    /// Update an existing budget line.
    /// </summary>
    [HttpPut("lines/{id}")]
    public async Task<ActionResult<BudgetLineDto>> UpdateLine(Guid id, [FromBody] UpdateBudgetLineDto request)
    {
        var validationResult = await _updateLineValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            return BadRequest(new
            {
                title = "Validation failed",
                status = 400,
                errors = validationResult.Errors.ToDictionary(e => e.PropertyName, e => e.ErrorMessage)
            });
        }

        try
        {
            var line = await _budgetPlanService.UpdateLineAsync(id, request);
            if (line == null)
            {
                return NotFound(new { title = "Budget line not found", status = 404 });
            }
            return Ok(line);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized attempt to update budget line {Id}", id);
            return StatusCode(403, new { title = "Forbidden", status = 403, detail = ex.Message });
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Concurrency conflict updating budget line {Id}", id);
            return Conflict(new { title = "Concurrency conflict", status = 409, detail = "The budget line has been modified by another user." });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation while updating budget line {Id}", id);
            return BadRequest(new { title = "Invalid operation", status = 400, detail = "The budget line request could not be processed." });
        }
    }

    /// <summary>
    /// Archive a budget line (soft delete).
    /// </summary>
    [HttpDelete("lines/{id}")]
    public async Task<IActionResult> ArchiveLine(Guid id)
    {
        try
        {
            var success = await _budgetPlanService.ArchiveLineAsync(id);
            if (!success)
            {
                return NotFound(new { title = "Budget line not found", status = 404 });
            }
            return NoContent();
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized attempt to archive budget line {Id}", id);
            return StatusCode(403, new { title = "Forbidden", status = 403, detail = ex.Message });
        }
    }
}
