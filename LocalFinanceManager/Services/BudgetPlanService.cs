using LocalFinanceManager.Data.Repositories;
using LocalFinanceManager.DTOs;
using LocalFinanceManager.Models;
using Microsoft.EntityFrameworkCore;

namespace LocalFinanceManager.Services;

/// <summary>
/// Service for managing budget plan operations.
/// </summary>
public class BudgetPlanService
{
    private readonly IBudgetPlanRepository _budgetPlanRepository;
    private readonly IBudgetLineRepository _budgetLineRepository;
    private readonly IAccountRepository _accountRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly ILogger<BudgetPlanService> _logger;

    public BudgetPlanService(
        IBudgetPlanRepository budgetPlanRepository,
        IBudgetLineRepository budgetLineRepository,
        IAccountRepository accountRepository,
        ICategoryRepository categoryRepository,
        ILogger<BudgetPlanService> logger)
    {
        _budgetPlanRepository = budgetPlanRepository;
        _budgetLineRepository = budgetLineRepository;
        _accountRepository = accountRepository;
        _categoryRepository = categoryRepository;
        _logger = logger;
    }

    /// <summary>
    /// Get all budget plans for a specific account.
    /// </summary>
    public async Task<List<BudgetPlanDto>> GetByAccountIdAsync(Guid accountId)
    {
        _logger.LogInformation("Retrieving budget plans for account: {AccountId}", accountId);
        var plans = await _budgetPlanRepository.GetByAccountIdAsync(accountId);
        return plans.Select(MapToDto).ToList();
    }

    /// <summary>
    /// Get a budget plan by ID.
    /// </summary>
    public async Task<BudgetPlanDto?> GetByIdAsync(Guid id)
    {
        _logger.LogInformation("Retrieving budget plan with ID: {BudgetPlanId}", id);
        var plan = await _budgetPlanRepository.GetByIdWithLinesAsync(id);
        return plan != null ? MapToDto(plan) : null;
    }

    /// <summary>
    /// Get a budget plan for a specific account and year.
    /// </summary>
    public async Task<BudgetPlanDto?> GetByAccountAndYearAsync(Guid accountId, int year)
    {
        _logger.LogInformation("Retrieving budget plan for account {AccountId}, year {Year}", accountId, year);
        var plan = await _budgetPlanRepository.GetByAccountAndYearAsync(accountId, year);
        return plan != null ? MapToDto(plan) : null;
    }

    /// <summary>
    /// Create a new budget plan.
    /// </summary>
    public async Task<BudgetPlanDto> CreateAsync(CreateBudgetPlanDto request)
    {
        _logger.LogInformation("Creating new budget plan for account {AccountId}, year {Year}", request.AccountId, request.Year);

        // Verify account exists
        var account = await _accountRepository.GetByIdAsync(request.AccountId);
        if (account == null)
        {
            throw new InvalidOperationException($"Account with ID {request.AccountId} not found.");
        }

        // Check if a plan already exists for this account and year
        var existingPlan = await _budgetPlanRepository.GetByAccountAndYearAsync(request.AccountId, request.Year);
        if (existingPlan != null)
        {
            throw new InvalidOperationException($"A budget plan already exists for account {request.AccountId} and year {request.Year}.");
        }

        var plan = new BudgetPlan
        {
            AccountId = request.AccountId,
            Year = request.Year,
            Name = request.Name,
            IsArchived = false
        };

        await _budgetPlanRepository.AddAsync(plan);

        _logger.LogInformation("Budget plan created with ID: {BudgetPlanId}", plan.Id);

        return MapToDto(plan);
    }

    /// <summary>
    /// Update an existing budget plan.
    /// </summary>
    public async Task<BudgetPlanDto?> UpdateAsync(Guid id, UpdateBudgetPlanDto request)
    {
        _logger.LogInformation("Updating budget plan with ID: {BudgetPlanId}", id);

        var plan = await _budgetPlanRepository.GetByIdWithLinesAsync(id);
        if (plan == null)
        {
            _logger.LogWarning("Budget plan not found with ID: {BudgetPlanId}", id);
            return null;
        }

        plan.Name = request.Name;
        plan.RowVersion = request.RowVersion;

        try
        {
            await _budgetPlanRepository.UpdateAsync(plan);
            _logger.LogInformation("Budget plan updated successfully: {BudgetPlanId}", id);
            return MapToDto(plan);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Concurrency conflict updating budget plan: {BudgetPlanId}", id);
            throw;
        }
    }

    /// <summary>
    /// Archive a budget plan.
    /// </summary>
    public async Task<bool> ArchiveAsync(Guid id)
    {
        _logger.LogInformation("Archiving budget plan with ID: {BudgetPlanId}", id);

        var plan = await _budgetPlanRepository.GetByIdAsync(id);
        if (plan == null)
        {
            _logger.LogWarning("Budget plan not found with ID: {BudgetPlanId}", id);
            return false;
        }

        await _budgetPlanRepository.ArchiveAsync(id);
        _logger.LogInformation("Budget plan archived successfully: {BudgetPlanId}", id);
        return true;
    }

    /// <summary>
    /// Create a new budget line.
    /// </summary>
    public async Task<BudgetLineDto> CreateLineAsync(CreateBudgetLineDto request)
    {
        _logger.LogInformation("Creating new budget line for plan {BudgetPlanId}, category {CategoryId}",
            request.BudgetPlanId, request.CategoryId);

        // Verify budget plan exists
        var plan = await _budgetPlanRepository.GetByIdAsync(request.BudgetPlanId);
        if (plan == null)
        {
            throw new InvalidOperationException($"Budget plan with ID {request.BudgetPlanId} not found.");
        }

        // Verify category exists
        var category = await _categoryRepository.GetByIdAsync(request.CategoryId);
        if (category == null)
        {
            throw new InvalidOperationException($"Category with ID {request.CategoryId} not found.");
        }

        var line = new BudgetLine
        {
            BudgetPlanId = request.BudgetPlanId,
            CategoryId = request.CategoryId,
            MonthlyAmounts = request.MonthlyAmounts,
            Notes = request.Notes,
            IsArchived = false
        };

        await _budgetLineRepository.AddAsync(line);

        _logger.LogInformation("Budget line created with ID: {BudgetLineId}", line.Id);

        return MapLineToDto(line, category);
    }

    /// <summary>
    /// Update an existing budget line.
    /// </summary>
    public async Task<BudgetLineDto?> UpdateLineAsync(Guid id, UpdateBudgetLineDto request)
    {
        _logger.LogInformation("Updating budget line with ID: {BudgetLineId}", id);

        var line = await _budgetLineRepository.GetByIdAsync(id);
        if (line == null)
        {
            _logger.LogWarning("Budget line not found with ID: {BudgetLineId}", id);
            return null;
        }

        // Verify category exists
        var category = await _categoryRepository.GetByIdAsync(request.CategoryId);
        if (category == null)
        {
            throw new InvalidOperationException($"Category with ID {request.CategoryId} not found.");
        }

        line.CategoryId = request.CategoryId;
        line.MonthlyAmounts = request.MonthlyAmounts;
        line.Notes = request.Notes;
        line.RowVersion = request.RowVersion;

        try
        {
            await _budgetLineRepository.UpdateAsync(line);
            _logger.LogInformation("Budget line updated successfully: {BudgetLineId}", id);
            return MapLineToDto(line, category);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Concurrency conflict updating budget line: {BudgetLineId}", id);
            throw;
        }
    }

    /// <summary>
    /// Archive a budget line.
    /// </summary>
    public async Task<bool> ArchiveLineAsync(Guid id)
    {
        _logger.LogInformation("Archiving budget line with ID: {BudgetLineId}", id);

        var line = await _budgetLineRepository.GetByIdAsync(id);
        if (line == null)
        {
            _logger.LogWarning("Budget line not found with ID: {BudgetLineId}", id);
            return false;
        }

        await _budgetLineRepository.ArchiveAsync(id);
        _logger.LogInformation("Budget line archived successfully: {BudgetLineId}", id);
        return true;
    }

    private BudgetPlanDto MapToDto(BudgetPlan plan)
    {
        return new BudgetPlanDto
        {
            Id = plan.Id,
            AccountId = plan.AccountId,
            Year = plan.Year,
            Name = plan.Name,
            CreatedAt = plan.CreatedAt,
            UpdatedAt = plan.UpdatedAt,
            RowVersion = plan.RowVersion,
            BudgetLines = plan.BudgetLines.Select(bl => MapLineToDto(bl, bl.Category)).ToList()
        };
    }

    private BudgetLineDto MapLineToDto(BudgetLine line, Category category)
    {
        return new BudgetLineDto
        {
            Id = line.Id,
            BudgetPlanId = line.BudgetPlanId,
            CategoryId = line.CategoryId,
            CategoryName = category.Name,
            MonthlyAmounts = line.MonthlyAmounts,
            YearTotal = line.YearTotal,
            Notes = line.Notes,
            RowVersion = line.RowVersion
        };
    }
}
