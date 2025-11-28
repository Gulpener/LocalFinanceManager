using LocalFinanceManager.Application.Interfaces;
using LocalFinanceManager.Application.Services;
using LocalFinanceManager.Domain.Entities;
using LocalFinanceManager.Infrastructure;
using LocalFinanceManager.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Tests;

public class AutoCategorizeTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly ScoringEngine _scoringEngine;
    private readonly EfCategoryLearningProfileRepository _profileRepository;
    private readonly EfCategoryRepository _categoryRepository;
    private readonly EfTransactionRepository _transactionRepository;
    private readonly TransactionService _transactionService;
    private int _accountId;

    public AutoCategorizeTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _profileRepository = new EfCategoryLearningProfileRepository(_context);
        _categoryRepository = new EfCategoryRepository(_context);
        _transactionRepository = new EfTransactionRepository(_context);
        _transactionService = new TransactionService(_transactionRepository);
        _scoringEngine = new ScoringEngine(_profileRepository, _categoryRepository);

        SeedTestData();
    }



    public void Dispose()
    {
        _context.Dispose();
    }

    private void SeedTestData()
    {
        var groceries = new Category { Name = "Groceries", MonthlyBudget = 300m };
        var utilities = new Category { Name = "Utilities", MonthlyBudget = 150m };
        _context.Categories.AddRange(groceries, utilities);
        _context.SaveChanges();

        var groceriesProfile = new CategoryLearningProfile
        {
            CategoryId = groceries.Id,
            WordFrequency = new Dictionary<string, int>
            {
                { "albert", 10 },
                { "heijn", 10 },
                { "supermarket", 20 }
            },
            IbanFrequency = new Dictionary<string, int>
            {
                { "NL01RABO0123456789", 10 }
            },
            AmountBucketFrequency = new Dictionary<string, int>
            {
                { "50-100", 15 }
            }
        };

        var utilitiesProfile = new CategoryLearningProfile
        {
            CategoryId = utilities.Id,
            WordFrequency = new Dictionary<string, int>
            {
                { "energy", 15 },
                { "electric", 10 }
            },
            IbanFrequency = new Dictionary<string, int>
            {
                { "NL09UTIL0000112233", 12 }
            },
            AmountBucketFrequency = new Dictionary<string, int>
            {
                { "100-250", 10 }
            }
        };

        _context.CategoryLearningProfiles.AddRange(groceriesProfile, utilitiesProfile);
        _context.SaveChanges();

        var account = new Account { Name = "Checking", AccountType = "Checking" };
        _context.Accounts.Add(account);
        _context.SaveChanges();
        _accountId = account.Id;

        // Transaction likely matching groceries (should be assigned when threshold low enough)
        var t1 = new Transaction
        {
            AccountId = account.Id,
            Date = DateTime.Today,
            Description = "Albert Heijn supermarket purchase",
            CounterAccount = "NL01RABO0123456789",
            Amount = -75m,
            CategoryId = 0
        };

        // Transaction unlikely to match profiles (should remain uncategorized when threshold high)
        var t2 = new Transaction
        {
            AccountId = account.Id,
            Date = DateTime.Today,
            Description = "Random vendor",
            CounterAccount = "NL99UNKNOWN0000000",
            Amount = -13m,
            CategoryId = 0
        };

        _context.Transactions.AddRange(t1, t2);
        _context.SaveChanges();
    }

    [Fact]
    public async Task AutoCategorize_AssignsCategory_WhenScoreAtOrAboveThreshold()
    {
        // Arrange: pick the transaction that should match the groceries profile
        var transactions = await _context.Transactions.Where(t => t.AccountId == _accountId).ToListAsync();
        var tx = transactions.First(t => t.Description.Contains("Albert Heijn"));

        // Act: get best suggestion with a modest threshold and apply it
        var best = await _scoringEngine.GetBestSuggestionAsync(tx, 0.5f);
        Assert.NotNull(best);
        tx.CategoryId = best!.CategoryId;
        await _transactionRepository.UpdateAsync(tx);

        // Reload directly from context and assert the CategoryId was persisted
        var reloaded = await _context.Transactions.FindAsync(tx.Id);
        Assert.NotNull(reloaded);
        Assert.Equal(best.CategoryId, reloaded!.CategoryId);
    }
}
