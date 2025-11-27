using LocalFinanceManager.Application.Services;
using LocalFinanceManager.Domain.Entities;
using LocalFinanceManager.Infrastructure;
using LocalFinanceManager.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Tests;

public class RuleEngineTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly RuleEngine _ruleEngine;
    private readonly EfRuleRepository _ruleRepository;
    private readonly EfTransactionRepository _transactionRepository;
    private readonly Account _testAccount;
    private readonly Category _testCategory;

    public RuleEngineTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _ruleRepository = new EfRuleRepository(_context);
        _transactionRepository = new EfTransactionRepository(_context);
        _ruleEngine = new RuleEngine(_ruleRepository, _transactionRepository);

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
    public async Task ApplyRulesAsync_ShouldMatchContainsRule()
    {
        // Arrange
        var rule = new Rule
        {
            MatchType = "contains",
            Pattern = "albert heijn",
            TargetCategoryId = _testCategory.Id,
            Priority = 100
        };
        await _ruleRepository.AddAsync(rule);

        var transaction = new Transaction
        {
            AccountId = _testAccount.Id,
            Description = "Albert Heijn supermarket payment",
            Amount = -50m
        };

        // Act
        var result = await _ruleEngine.ApplyRulesAsync(transaction);

        // Assert
        Assert.True(result.HasMatch);
        Assert.Equal(_testCategory.Id, result.SuggestedCategoryId);
    }

    [Fact]
    public async Task ApplyRulesAsync_ShouldMatchIbanRule()
    {
        // Arrange
        var rule = new Rule
        {
            MatchType = "iban",
            Pattern = "NL01RABO0123456789",
            TargetCategoryId = _testCategory.Id,
            Priority = 100
        };
        await _ruleRepository.AddAsync(rule);

        var transaction = new Transaction
        {
            AccountId = _testAccount.Id,
            Description = "Some payment",
            CounterAccount = "NL01RABO0123456789",
            Amount = -50m
        };

        // Act
        var result = await _ruleEngine.ApplyRulesAsync(transaction);

        // Assert
        Assert.True(result.HasMatch);
    }

    [Fact]
    public async Task ApplyRulesAsync_ShouldMatchRegexRule()
    {
        // Arrange
        var rule = new Rule
        {
            MatchType = "regex",
            Pattern = @"SHELL\s+\d+",
            TargetCategoryId = _testCategory.Id,
            Priority = 100
        };
        await _ruleRepository.AddAsync(rule);

        var transaction = new Transaction
        {
            AccountId = _testAccount.Id,
            Description = "SHELL 12345 gas purchase",
            Amount = -60m
        };

        // Act
        var result = await _ruleEngine.ApplyRulesAsync(transaction);

        // Assert
        Assert.True(result.HasMatch);
    }

    [Fact]
    public async Task ApplyRulesAsync_ShouldApplyHigherPriorityFirst()
    {
        // Arrange
        var category2 = new Category { Name = "Fuel", MonthlyBudget = 200m };
        _context.Categories.Add(category2);
        _context.SaveChanges();

        var lowPriorityRule = new Rule
        {
            MatchType = "contains",
            Pattern = "shell",
            TargetCategoryId = _testCategory.Id,
            Priority = 10
        };

        var highPriorityRule = new Rule
        {
            MatchType = "contains",
            Pattern = "shell",
            TargetCategoryId = category2.Id,
            Priority = 100
        };

        await _ruleRepository.AddAsync(lowPriorityRule);
        await _ruleRepository.AddAsync(highPriorityRule);

        var transaction = new Transaction
        {
            AccountId = _testAccount.Id,
            Description = "Shell gas station",
            Amount = -60m
        };

        // Act
        var result = await _ruleEngine.ApplyRulesAsync(transaction);

        // Assert
        Assert.True(result.HasMatch);
        Assert.Equal(category2.Id, result.SuggestedCategoryId);
    }

    [Fact]
    public async Task ApplyRulesAsync_ShouldReturnNoMatch_WhenNoRulesMatch()
    {
        // Arrange
        var rule = new Rule
        {
            MatchType = "contains",
            Pattern = "xyz123",
            TargetCategoryId = _testCategory.Id,
            Priority = 100
        };
        await _ruleRepository.AddAsync(rule);

        var transaction = new Transaction
        {
            AccountId = _testAccount.Id,
            Description = "Regular payment",
            Amount = -50m
        };

        // Act
        var result = await _ruleEngine.ApplyRulesAsync(transaction);

        // Assert
        Assert.False(result.HasMatch);
    }
}
