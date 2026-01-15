using LocalFinanceManager.Models;

namespace LocalFinanceManager.DTOs;

/// <summary>
/// DTO for creating a new budget plan.
/// </summary>
public record CreateBudgetPlanDto
{
    public Guid AccountId { get; set; }
    public int Year { get; set; }
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// DTO for updating an existing budget plan.
/// </summary>
public record UpdateBudgetPlanDto
{
    public string Name { get; set; } = string.Empty;
    public byte[]? RowVersion { get; set; }
}

/// <summary>
/// DTO for budget plan responses.
/// </summary>
public record BudgetPlanDto
{
    public Guid Id { get; init; }
    public Guid AccountId { get; init; }
    public int Year { get; init; }
    public string Name { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public byte[]? RowVersion { get; init; }
    public List<BudgetLineDto> BudgetLines { get; init; } = new();
}

/// <summary>
/// DTO for creating a new budget line.
/// </summary>
public record CreateBudgetLineDto
{
    public Guid BudgetPlanId { get; set; }
    public Guid CategoryId { get; set; }
    public decimal[] MonthlyAmounts { get; set; } = Array.Empty<decimal>();
    public string? Notes { get; set; }
}

/// <summary>
/// DTO for updating an existing budget line.
/// </summary>
public record UpdateBudgetLineDto
{
    public Guid CategoryId { get; init; }
    public decimal[] MonthlyAmounts { get; init; } = Array.Empty<decimal>();
    public string? Notes { get; init; }
    public byte[]? RowVersion { get; init; }
}

/// <summary>
/// DTO for budget line responses.
/// </summary>
public record BudgetLineDto
{
    public Guid Id { get; init; }
    public Guid BudgetPlanId { get; init; }
    public Guid CategoryId { get; init; }
    public string CategoryName { get; init; } = string.Empty;
    public decimal[] MonthlyAmounts { get; init; } = Array.Empty<decimal>();
    public decimal YearTotal { get; init; }
    public string? Notes { get; init; }
    public byte[]? RowVersion { get; init; }
}

/// <summary>
/// DTO for category responses.
/// </summary>
public record CategoryDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public CategoryType Type { get; init; }
    public byte[]? RowVersion { get; init; }
}

/// <summary>
/// DTO for creating a category.
/// </summary>
public record CreateCategoryDto
{
    public string Name { get; init; } = string.Empty;
    public CategoryType Type { get; init; }
}

/// <summary>
/// DTO for updating a category.
/// </summary>
public record UpdateCategoryDto
{
    public string Name { get; init; } = string.Empty;
    public CategoryType Type { get; init; }
    public byte[]? RowVersion { get; init; }
}
