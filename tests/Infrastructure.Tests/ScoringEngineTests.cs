using LocalFinanceManager.Application.Interfaces;
using LocalFinanceManager.Application.Services;
using LocalFinanceManager.Domain.Entities;
using LocalFinanceManager.Infrastructure;
using LocalFinanceManager.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Tests;

public class ScoringEngineTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly ScoringEngine _scoringEngine;
    private readonly EfCategoryLearningProfileRepository _profileRepository;
    private readonly EfCategoryRepository _categoryRepository;

    public ScoringEngineTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _profileRepository = new EfCategoryLearningProfileRepository(_context);
        _categoryRepository = new EfCategoryRepository(_context);
        _scoringEngine = new ScoringEngine(_profileRepository, _categoryRepository);

        SeedTestData();
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    private void SeedTestData()
    {
        // Create categories
        var groceriesCategory = new Category { Name = "Groceries", MonthlyBudget = 500m };
        var fuelCategory = new Category { Name = "Fuel", MonthlyBudget = 200m };
        _context.Categories.AddRange(groceriesCategory, fuelCategory);
        _context.SaveChanges();

        // Create learning profiles
        var groceriesProfile = new CategoryLearningProfile
        {
            CategoryId = groceriesCategory.Id,
            WordFrequency = new Dictionary<string, int>
            {
                { "albert", 15 },
                { "heijn", 15 },
                { "supermarket", 25 }
            },
            IbanFrequency = new Dictionary<string, int>
            {
                { "NL01RABO0123456789", 15 }
            },
            AmountBucketFrequency = new Dictionary<string, int>
            {
                { "25-50", 20 },
                { "50-100", 15 }
            }
        };

        var fuelProfile = new CategoryLearningProfile
        {
            CategoryId = fuelCategory.Id,
            WordFrequency = new Dictionary<string, int>
            {
                { "shell", 20 },
                { "gas", 15 },
                { "station", 10 }
            },
            IbanFrequency = new Dictionary<string, int>
            {
                { "NL02SHELL0011223344", 20 }
            },
            AmountBucketFrequency = new Dictionary<string, int>
            {
                { "50-100", 25 }
            }
        };

        _context.CategoryLearningProfiles.AddRange(groceriesProfile, fuelProfile);
        _context.SaveChanges();
    }

    [Fact]
    public async Task GetSuggestionsAsync_ShouldReturnSuggestionsOrderedByScore()
    {
        // Arrange
        var transaction = new Transaction
        {
            Description = "Albert Heijn supermarket",
            CounterAccount = "NL01RABO0123456789",
            Amount = -75m
        };

        // Act
        var suggestions = await _scoringEngine.GetSuggestionsAsync(transaction);

        // Assert
        Assert.NotEmpty(suggestions);
        Assert.Equal("Groceries", suggestions[0].CategoryName);
    }

    [Fact]
    public async Task GetSuggestionsAsync_ShouldScoreIbanMatch()
    {
        // Arrange
        var transaction = new Transaction
        {
            Description = "Payment",
            CounterAccount = "NL02SHELL0011223344",
            Amount = -60m
        };

        // Act
        var suggestions = await _scoringEngine.GetSuggestionsAsync(transaction);

        // Assert
        Assert.NotEmpty(suggestions);
        var fuelSuggestion = suggestions.FirstOrDefault(s => s.CategoryName == "Fuel");
        Assert.NotNull(fuelSuggestion);
        Assert.True(fuelSuggestion.Breakdown.IbanScore > 0);
    }

    [Fact]
    public async Task GetBestSuggestionAsync_ShouldReturnNull_WhenBelowThreshold()
    {
        // Arrange
        var transaction = new Transaction
        {
            Description = "Unknown vendor",
            CounterAccount = "NL99UNKNOWN0000000",
            Amount = -100m
        };

        // Act
        var suggestion = await _scoringEngine.GetBestSuggestionAsync(transaction, 0.5f);

        // Assert
        Assert.Null(suggestion);
    }

    [Fact]
    public void GetAmountBucket_ShouldReturnCorrectBucket()
    {
        Assert.Equal("0-10", ScoringEngine.GetAmountBucket(5m));
        Assert.Equal("10-25", ScoringEngine.GetAmountBucket(20m));
        Assert.Equal("25-50", ScoringEngine.GetAmountBucket(40m));
        Assert.Equal("50-100", ScoringEngine.GetAmountBucket(75m));
        Assert.Equal("100-250", ScoringEngine.GetAmountBucket(150m));
        Assert.Equal("1000+", ScoringEngine.GetAmountBucket(2000m));
    }
}
