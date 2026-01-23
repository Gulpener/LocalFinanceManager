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

    [Test]
    public async Task UpdateAccountBudgetPlanReferences_MultipleAccountsCanReferenceTheirOwnBudgetPlans()
    {
        // Arrange
        var account1Id = Guid.NewGuid();
        var account2Id = Guid.NewGuid();
        var account3Id = Guid.NewGuid();

        var account1 = new Account
        {
            Id = account1Id,
            Label = "Checking Account",
            Type = AccountType.Checking,
            IBAN = "NL91ABNA0417164300",
            Currency = "EUR",
            StartingBalance = 1000m,
            CurrentBudgetPlanId = null
        };

        var account2 = new Account
        {
            Id = account2Id,
            Label = "Savings Account",
            Type = AccountType.Savings,
            IBAN = "NL91ABNA0417164301",
            Currency = "EUR",
            StartingBalance = 5000m,
            CurrentBudgetPlanId = null
        };

        var account3 = new Account
        {
            Id = account3Id,
            Label = "Investment Account",
            Type = AccountType.Investment,
            IBAN = "NL91ABNA0417164302",
            Currency = "USD",
            StartingBalance = 10000m,
            CurrentBudgetPlanId = null
        };

        await _context.Accounts.AddRangeAsync(account1, account2, account3);

        // Create budget plans for each account
        var budgetPlan1 = new BudgetPlan
        {
            Id = Guid.NewGuid(),
            AccountId = account1Id,
            Year = 2025,
            Name = "Checking Budget 2025",
            IsArchived = false
        };

        var budgetPlan2 = new BudgetPlan
        {
            Id = Guid.NewGuid(),
            AccountId = account2Id,
            Year = 2025,
            Name = "Savings Budget 2025",
            IsArchived = false
        };

        var budgetPlan3 = new BudgetPlan
        {
            Id = Guid.NewGuid(),
            AccountId = account3Id,
            Year = 2025,
            Name = "Investment Budget 2025",
            IsArchived = false
        };

        await _context.BudgetPlans.AddRangeAsync(budgetPlan1, budgetPlan2, budgetPlan3);
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

        // Assert - Each account should reference its own budget plan
        var updatedAccount1 = await _context.Accounts.FindAsync(account1Id);
        var updatedAccount2 = await _context.Accounts.FindAsync(account2Id);
        var updatedAccount3 = await _context.Accounts.FindAsync(account3Id);

        Assert.That(updatedAccount1!.CurrentBudgetPlanId, Is.EqualTo(budgetPlan1.Id));
        Assert.That(updatedAccount2!.CurrentBudgetPlanId, Is.EqualTo(budgetPlan2.Id));
        Assert.That(updatedAccount3!.CurrentBudgetPlanId, Is.EqualTo(budgetPlan3.Id));

        // Verify all three accounts have different budget plan references
        Assert.That(updatedAccount1.CurrentBudgetPlanId, Is.Not.EqualTo(updatedAccount2.CurrentBudgetPlanId));
        Assert.That(updatedAccount1.CurrentBudgetPlanId, Is.Not.EqualTo(updatedAccount3.CurrentBudgetPlanId));
        Assert.That(updatedAccount2.CurrentBudgetPlanId, Is.Not.EqualTo(updatedAccount3.CurrentBudgetPlanId));
    }
}
