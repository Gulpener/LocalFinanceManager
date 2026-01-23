using LocalFinanceManager.Data;
using LocalFinanceManager.Models;
using LocalFinanceManager.Tests.Fixtures;
using Microsoft.EntityFrameworkCore;

namespace LocalFinanceManager.Tests.Integration;

/// <summary>
/// Integration tests for automatic account budget plan assignment.
/// </summary>
[TestFixture]
public class AccountBudgetPlanAssignmentTests
{
    private TestDbContextFactory _factory = null!;
    private AppDbContext _context = null!;

    [SetUp]
    public void Setup()
    {
        _factory = new TestDbContextFactory();
        _context = _factory.CreateContext();
    }

    [TearDown]
    public void TearDown()
    {
        _context?.Dispose();
        _factory?.Dispose();
    }

    [Test]
    public async Task UpdateAccountBudgetPlanReferences_AssignsMostRecentBudgetPlan()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var account = new Account
        {
            Id = accountId,
            Label = "Test Account",
            Type = AccountType.Checking,
            IBAN = "NL91ABNA0417164300",
            Currency = "EUR",
            StartingBalance = 1000m,
            CurrentBudgetPlanId = null // No budget plan assigned yet
        };
        await _context.Accounts.AddAsync(account);

        var budgetPlan2023 = new BudgetPlan
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            Year = 2023,
            Name = "Budget 2023",
            IsArchived = false
        };

        var budgetPlan2026 = new BudgetPlan
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            Year = 2026,
            Name = "Budget 2026",
            IsArchived = false
        };

        await _context.BudgetPlans.AddRangeAsync(budgetPlan2023, budgetPlan2026);
        await _context.SaveChangesAsync();

        // Act - Simulate the update logic from Program.cs
        var accountsWithoutBudgetPlan = await _context.Accounts
            .Where(a => !a.IsArchived && a.CurrentBudgetPlanId == null)
            .ToListAsync();

        foreach (var acc in accountsWithoutBudgetPlan)
        {
            var latestBudgetPlan = await _context.BudgetPlans
                .Where(bp => bp.AccountId == acc.Id && !bp.IsArchived)
                .OrderByDescending(bp => bp.Year)
                .FirstOrDefaultAsync();

            if (latestBudgetPlan != null)
            {
                acc.CurrentBudgetPlanId = latestBudgetPlan.Id;
            }
        }

        await _context.SaveChangesAsync();

        // Assert
        var updatedAccount = await _context.Accounts.FindAsync(accountId);
        Assert.That(updatedAccount, Is.Not.Null);
        Assert.That(updatedAccount!.CurrentBudgetPlanId, Is.EqualTo(budgetPlan2026.Id));
    }

    [Test]
    public async Task UpdateAccountBudgetPlanReferences_IgnoresArchivedBudgetPlans()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var account = new Account
        {
            Id = accountId,
            Label = "Test Account",
            Type = AccountType.Checking,
            IBAN = "NL91ABNA0417164300",
            Currency = "EUR",
            StartingBalance = 1000m,
            CurrentBudgetPlanId = null
        };
        await _context.Accounts.AddAsync(account);

        var budgetPlan2025 = new BudgetPlan
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            Year = 2025,
            Name = "Budget 2025",
            IsArchived = false
        };

        var budgetPlan2026Archived = new BudgetPlan
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            Year = 2026,
            Name = "Budget 2026 (Archived)",
            IsArchived = true // This should be ignored
        };

        await _context.BudgetPlans.AddRangeAsync(budgetPlan2025, budgetPlan2026Archived);
        await _context.SaveChangesAsync();

        // Act
        var accountsWithoutBudgetPlan = await _context.Accounts
            .Where(a => !a.IsArchived && a.CurrentBudgetPlanId == null)
            .ToListAsync();

        foreach (var acc in accountsWithoutBudgetPlan)
        {
            var latestBudgetPlan = await _context.BudgetPlans
                .Where(bp => bp.AccountId == acc.Id && !bp.IsArchived)
                .OrderByDescending(bp => bp.Year)
                .FirstOrDefaultAsync();

            if (latestBudgetPlan != null)
            {
                acc.CurrentBudgetPlanId = latestBudgetPlan.Id;
            }
        }

        await _context.SaveChangesAsync();

        // Assert
        var updatedAccount = await _context.Accounts.FindAsync(accountId);
        Assert.That(updatedAccount!.CurrentBudgetPlanId, Is.EqualTo(budgetPlan2025.Id));
    }

    [Test]
    public async Task UpdateAccountBudgetPlanReferences_DoesNotUpdateAccountsWithExistingBudgetPlan()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var existingBudgetPlanId = Guid.NewGuid();

        var account = new Account
        {
            Id = accountId,
            Label = "Test Account",
            Type = AccountType.Checking,
            IBAN = "NL91ABNA0417164300",
            Currency = "EUR",
            StartingBalance = 1000m,
            CurrentBudgetPlanId = null // Set after budget plan is saved
        };
        await _context.Accounts.AddAsync(account);
        await _context.SaveChangesAsync(); // Save account first

        var budgetPlan = new BudgetPlan
        {
            Id = existingBudgetPlanId,
            AccountId = accountId,
            Year = 2023,
            Name = "Budget 2023",
            IsArchived = false
        };

        var newerBudgetPlan = new BudgetPlan
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            Year = 2026,
            Name = "Budget 2026",
            IsArchived = false
        };

        await _context.BudgetPlans.AddRangeAsync(budgetPlan, newerBudgetPlan);
        await _context.SaveChangesAsync(); // Save budget plans

        // Now set CurrentBudgetPlanId
        account.CurrentBudgetPlanId = existingBudgetPlanId;
        await _context.SaveChangesAsync();

        // Act
        var accountsWithoutBudgetPlan = await _context.Accounts
            .Where(a => !a.IsArchived && a.CurrentBudgetPlanId == null)
            .ToListAsync();

        // Assert - Should not find any accounts to update
        Assert.That(accountsWithoutBudgetPlan, Is.Empty);

        var unchangedAccount = await _context.Accounts.FindAsync(accountId);
        Assert.That(unchangedAccount!.CurrentBudgetPlanId, Is.EqualTo(existingBudgetPlanId));
    }

    [Test]
    public async Task UpdateAccountBudgetPlanReferences_HandlesAccountWithNoBudgetPlans()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var account = new Account
        {
            Id = accountId,
            Label = "Test Account",
            Type = AccountType.Checking,
            IBAN = "NL91ABNA0417164300",
            Currency = "EUR",
            StartingBalance = 1000m,
            CurrentBudgetPlanId = null
        };
        await _context.Accounts.AddAsync(account);
        await _context.SaveChangesAsync();

        // Act
        var accountsWithoutBudgetPlan = await _context.Accounts
            .Where(a => !a.IsArchived && a.CurrentBudgetPlanId == null)
            .ToListAsync();

        foreach (var acc in accountsWithoutBudgetPlan)
        {
            var latestBudgetPlan = await _context.BudgetPlans
                .Where(bp => bp.AccountId == acc.Id && !bp.IsArchived)
                .OrderByDescending(bp => bp.Year)
                .FirstOrDefaultAsync();

            if (latestBudgetPlan != null)
            {
                acc.CurrentBudgetPlanId = latestBudgetPlan.Id;
            }
        }

        await _context.SaveChangesAsync();

        // Assert - Account should still have null CurrentBudgetPlanId
        var updatedAccount = await _context.Accounts.FindAsync(accountId);
        Assert.That(updatedAccount!.CurrentBudgetPlanId, Is.Null);
    }
}
