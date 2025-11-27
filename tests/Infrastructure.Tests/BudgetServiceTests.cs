using LocalFinanceManager.Application.Services;
using LocalFinanceManager.Domain.Entities;
using LocalFinanceManager.Infrastructure;
using LocalFinanceManager.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Tests;

public class BudgetServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly BudgetService _budgetService;
    private readonly EfBudgetRepository _budgetRepository;
    private readonly EfTransactionRepository _transactionRepository;
    private readonly Account _testAccount;
    private readonly Category _testCategory;

    public BudgetServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _budgetRepository = new EfBudgetRepository(_context);
        _transactionRepository = new EfTransactionRepository(_context);
        _budgetService = new BudgetService(_budgetRepository, _transactionRepository);

        // Seed required entities
        _testAccount = new Account { Name = "Test Account", AccountType = "bank", InitialBalance = 1000m, IsActive = true };
        _testCategory = new Category { Name = "Groceries", MonthlyBudget = 500m };
        _context.Accounts.Add(_testAccount);
        _context.Categories.Add(_testCategory);
        _context.SaveChanges();
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task GetMonthlySummaryAsync_ShouldReturnBudgetSummary()
    {
        // Arrange
        var month = new DateTime(2025, 1, 1);
        var budget = new Budget
        {
            CategoryId = _testCategory.Id,
            Month = month,
            PlannedAmount = 500m
        };
        await _budgetRepository.AddAsync(budget);

        // Add some transactions
        var transaction1 = new Transaction
        {
            AccountId = _testAccount.Id,
            CategoryId = _testCategory.Id,
            Date = new DateTime(2025, 1, 15),
            Amount = -100m,
            Description = "Groceries 1"
        };
        var transaction2 = new Transaction
        {
            AccountId = _testAccount.Id,
            CategoryId = _testCategory.Id,
            Date = new DateTime(2025, 1, 20),
            Amount = -150m,
            Description = "Groceries 2"
        };
        await _transactionRepository.AddAsync(transaction1);
        await _transactionRepository.AddAsync(transaction2);

        // Act
        var summaries = await _budgetService.GetMonthlySummaryAsync(month);

        // Assert
        Assert.Single(summaries);
        var summary = summaries[0];
        Assert.Equal(500m, summary.PlannedAmount);
        Assert.Equal(-250m, summary.ActualAmount);
        Assert.Equal(250m, summary.RemainingAmount);
        Assert.Equal(50m, summary.PercentageUsed);
        Assert.False(summary.IsExceeded);
    }

    [Fact]
    public async Task GetMonthlySummaryAsync_ShouldIndicateExceededBudget()
    {
        // Arrange
        var month = new DateTime(2025, 1, 1);
        var budget = new Budget
        {
            CategoryId = _testCategory.Id,
            Month = month,
            PlannedAmount = 100m
        };
        await _budgetRepository.AddAsync(budget);

        var transaction = new Transaction
        {
            AccountId = _testAccount.Id,
            CategoryId = _testCategory.Id,
            Date = new DateTime(2025, 1, 15),
            Amount = -150m,
            Description = "Over budget"
        };
        await _transactionRepository.AddAsync(transaction);

        // Act
        var summaries = await _budgetService.GetMonthlySummaryAsync(month);

        // Assert
        Assert.Single(summaries);
        Assert.True(summaries[0].IsExceeded);
        Assert.True(summaries[0].PercentageUsed > 100);
    }

    [Fact]
    public async Task GetCategoryBudgetSummaryAsync_ShouldReturnCorrectSummary()
    {
        // Arrange
        var month = new DateTime(2025, 1, 1);
        var budget = new Budget
        {
            CategoryId = _testCategory.Id,
            Month = month,
            PlannedAmount = 500m
        };
        await _budgetRepository.AddAsync(budget);

        // Act
        var summary = await _budgetService.GetCategoryBudgetSummaryAsync(_testCategory.Id, month);

        // Assert
        Assert.NotNull(summary);
        Assert.Equal(_testCategory.Id, summary.Budget.CategoryId);
    }

    [Fact]
    public async Task GetAccountBudgetSummaryAsync_ShouldFilterByAccount()
    {
        // Arrange
        var month = new DateTime(2025, 1, 1);
        var budget = new Budget
        {
            AccountId = _testAccount.Id,
            Month = month,
            PlannedAmount = 1000m
        };
        await _budgetRepository.AddAsync(budget);

        var transaction = new Transaction
        {
            AccountId = _testAccount.Id,
            CategoryId = _testCategory.Id,
            Date = new DateTime(2025, 1, 15),
            Amount = -200m,
            Description = "Account expense"
        };
        await _transactionRepository.AddAsync(transaction);

        // Act
        var summary = await _budgetService.GetAccountBudgetSummaryAsync(_testAccount.Id, month);

        // Assert
        Assert.NotNull(summary);
        Assert.Equal(_testAccount.Id, summary.Budget.AccountId);
        Assert.Equal(-200m, summary.ActualAmount);
    }
}
