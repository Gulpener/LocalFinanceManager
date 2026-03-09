using LocalFinanceManager.Data;
using LocalFinanceManager.Models;
using Microsoft.EntityFrameworkCore;

namespace LocalFinanceManager.Services;

public interface IDevelopmentUserSeedService
{
    Task SeedAsync();
    Task SeedForUserAsync(Guid userId);
}

public class DevelopmentUserSeedService : IDevelopmentUserSeedService
{
    private readonly AppDbContext _context;

    public DevelopmentUserSeedService(AppDbContext context)
    {
        _context = context;
    }

    public async Task SeedAsync()
    {
        if (!await _context.Users.AnyAsync())
        {
            var devUser = new User
            {
                Id = AppDbContext.SeedUserId,
                SupabaseUserId = "00000000-0000-0000-0000-000000000001",
                Email = AppDbContext.SeedUserEmail,
                DisplayName = "Dev User",
                EmailConfirmed = true,
                IsArchived = false
            };

            await _context.Users.AddAsync(devUser);
            await _context.SaveChangesAsync();
        }

        await SeedForUserAsync(AppDbContext.SeedUserId);

        var budgetPlanWithoutLines = await _context.BudgetPlans
            .Where(bp => bp.UserId == AppDbContext.SeedUserId && !bp.IsArchived)
            .Where(bp => !_context.BudgetLines.Any(bl => bl.BudgetPlanId == bp.Id && !bl.IsArchived))
            .FirstOrDefaultAsync();

        if (budgetPlanWithoutLines == null)
        {
            return;
        }

        var categories = await _context.Categories
            .Where(c => !c.IsArchived && c.BudgetPlanId == budgetPlanWithoutLines.Id)
            .ToListAsync();

        if (!categories.Any())
        {
            return;
        }

        var budgetLines = new List<BudgetLine>();

        static Category? FindCategory(IEnumerable<Category> list, params string[] names) =>
            list.FirstOrDefault(c => names.Contains(c.Name, StringComparer.OrdinalIgnoreCase));

        var housing = FindCategory(categories, "Huur", "Housing");
        if (housing != null)
        {
            budgetLines.Add(new BudgetLine
            {
                Id = Guid.NewGuid(),
                BudgetPlanId = budgetPlanWithoutLines.Id,
                CategoryId = housing.Id,
                MonthlyAmounts = Enumerable.Repeat(850m, 12).ToArray(),
                Notes = "Monthly rent",
                IsArchived = false
            });
        }

        var food = FindCategory(categories, "Boodschappen", "Food");
        if (food != null)
        {
            budgetLines.Add(new BudgetLine
            {
                Id = Guid.NewGuid(),
                BudgetPlanId = budgetPlanWithoutLines.Id,
                CategoryId = food.Id,
                MonthlyAmounts = Enumerable.Repeat(400m, 12).ToArray(),
                Notes = "Groceries and household items",
                IsArchived = false
            });
        }

        var transport = FindCategory(categories, "Transport", "Transportation");
        if (transport != null)
        {
            budgetLines.Add(new BudgetLine
            {
                Id = Guid.NewGuid(),
                BudgetPlanId = budgetPlanWithoutLines.Id,
                CategoryId = transport.Id,
                MonthlyAmounts = Enumerable.Repeat(150m, 12).ToArray(),
                Notes = "Public transport and fuel",
                IsArchived = false
            });
        }

        var entertainment = FindCategory(categories, "Entertainment");
        if (entertainment != null)
        {
            budgetLines.Add(new BudgetLine
            {
                Id = Guid.NewGuid(),
                BudgetPlanId = budgetPlanWithoutLines.Id,
                CategoryId = entertainment.Id,
                MonthlyAmounts = Enumerable.Repeat(200m, 12).ToArray(),
                Notes = "Movies, dining out, hobbies",
                IsArchived = false
            });
        }

        if (!budgetLines.Any())
        {
            return;
        }

        await _context.BudgetLines.AddRangeAsync(budgetLines);
        await _context.SaveChangesAsync();
    }

    public async Task SeedForUserAsync(Guid userId)
    {
        if (userId == Guid.Empty)
        {
            return;
        }

        var userExists = await _context.Users.AnyAsync(u => !u.IsArchived && u.Id == userId);
        if (!userExists)
        {
            return;
        }

        if (await _context.Accounts.AnyAsync(a => !a.IsArchived && a.UserId == userId))
        {
            return;
        }

        var accounts = new[]
        {
            new Account
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Label = "Betaalrekening",
                Type = AccountType.Checking,
                IBAN = "NL91ABNA0417164300",
                Currency = "EUR",
                StartingBalance = 1000.00m,
                IsArchived = false
            },
            new Account
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Label = "Spaarrekening",
                Type = AccountType.Savings,
                IBAN = "NL20INGB0001234567",
                Currency = "EUR",
                StartingBalance = 2500.00m,
                IsArchived = false
            },
            new Account
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Label = "Credit Card",
                Type = AccountType.Credit,
                IBAN = "NL39RABO0300065264",
                Currency = "EUR",
                StartingBalance = 0.00m,
                IsArchived = false
            }
        };

        await _context.Accounts.AddRangeAsync(accounts);
        await _context.SaveChangesAsync();

        var firstAccount = accounts[0];
        var budgetPlan = new BudgetPlan
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            AccountId = firstAccount.Id,
            Year = DateTime.Now.Year,
            Name = $"{DateTime.Now.Year} Household Budget",
            IsArchived = false
        };

        await _context.BudgetPlans.AddAsync(budgetPlan);
        await _context.SaveChangesAsync();

        var personalCategories = CategoryTemplates.Templates["Personal"];
        foreach (var (name, type) in personalCategories)
        {
            _context.Categories.Add(new Category
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Name = name,
                Type = type,
                BudgetPlanId = budgetPlan.Id,
                IsArchived = false
            });
        }

        await _context.SaveChangesAsync();

        var categoryMap = await _context.Categories
            .Where(c => !c.IsArchived && c.BudgetPlanId == budgetPlan.Id)
            .ToDictionaryAsync(c => c.Name, c => c.Id);

        var budgetLines = new List<BudgetLine>();

        if (categoryMap.TryGetValue("Huur", out var huurCategoryId))
        {
            budgetLines.Add(new BudgetLine
            {
                Id = Guid.NewGuid(),
                BudgetPlanId = budgetPlan.Id,
                CategoryId = huurCategoryId,
                MonthlyAmounts = Enumerable.Repeat(850m, 12).ToArray(),
                Notes = "Monthly rent",
                IsArchived = false
            });
        }

        if (categoryMap.TryGetValue("Boodschappen", out var boodschappenCategoryId))
        {
            budgetLines.Add(new BudgetLine
            {
                Id = Guid.NewGuid(),
                BudgetPlanId = budgetPlan.Id,
                CategoryId = boodschappenCategoryId,
                MonthlyAmounts = Enumerable.Repeat(400m, 12).ToArray(),
                Notes = "Groceries and household items",
                IsArchived = false
            });
        }

        if (budgetLines.Any())
        {
            await _context.BudgetLines.AddRangeAsync(budgetLines);
            await _context.SaveChangesAsync();
        }

        var transactions = new[]
        {
            new Transaction
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                AccountId = firstAccount.Id,
                Amount = -45.50m,
                Date = DateTime.UtcNow.AddDays(-5),
                Description = "Grocery Store Purchase",
                Counterparty = "Albert Heijn",
                ExternalId = "TRX001",
                IsArchived = false
            },
            new Transaction
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                AccountId = firstAccount.Id,
                Amount = -12.30m,
                Date = DateTime.UtcNow.AddDays(-4),
                Description = "Coffee Shop",
                Counterparty = "Starbucks",
                ExternalId = "TRX002",
                IsArchived = false
            },
            new Transaction
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                AccountId = firstAccount.Id,
                Amount = 2500.00m,
                Date = DateTime.UtcNow.AddDays(-3),
                Description = "Salary Payment",
                Counterparty = "Employer Corp",
                ExternalId = "TRX003",
                IsArchived = false
            },
            new Transaction
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                AccountId = firstAccount.Id,
                Amount = -850.00m,
                Date = DateTime.UtcNow.AddDays(-2),
                Description = "Rent Payment",
                Counterparty = "Landlord",
                ExternalId = "TRX004",
                IsArchived = false
            }
        };

        await _context.Transactions.AddRangeAsync(transactions);
        await _context.SaveChangesAsync();
    }
}
