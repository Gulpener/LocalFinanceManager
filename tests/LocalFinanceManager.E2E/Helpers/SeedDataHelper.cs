using LocalFinanceManager.Data;
using LocalFinanceManager.Models;
using Microsoft.EntityFrameworkCore;

namespace LocalFinanceManager.E2E.Helpers;

/// <summary>
/// Provides reusable seed data methods for E2E tests.
/// Simplifies test arrange sections and ensures consistent test data across test suites.
/// </summary>
public static class SeedDataHelper
{
    private static readonly Random _random = new();
    private static readonly string[] _transactionDescriptions = new[]
    {
        "Grocery Store", "Salary", "Fuel Station", "Restaurant", "Coffee Shop",
        "Online Shopping", "Utility Bill", "Rent Payment", "Insurance Premium",
        "Gym Membership", "Pharmacy", "Public Transport", "Internet Service",
        "Mobile Phone Bill", "Electricity Bill", "Water Bill", "Gas Bill",
        "Freelance Income", "Bonus Payment", "Investment Return", "Gift Received"
    };

    /// <summary>
    /// Seeds a new account with specified parameters.
    /// Automatically creates a linked BudgetPlan for the current year (12 months).
    /// </summary>
    /// <param name="context">Database context for saving entities.</param>
    /// <param name="label">User-friendly account label (e.g., "Main Checking Account").</param>
    /// <param name="iban">International Bank Account Number (stored without spaces).</param>
    /// <param name="initialBalance">Starting balance for the account.</param>
    /// <param name="currency">ISO-4217 currency code (e.g., "EUR", "USD"). Defaults to "EUR".</param>
    /// <returns>The created Account entity with linked BudgetPlan.</returns>
    public static async Task<Account> SeedAccountAsync(
        AppDbContext context,
        string label,
        string iban,
        decimal initialBalance,
        string currency = "EUR")
    {
        var account = new Account
        {
            Id = Guid.NewGuid(),
            Label = label,
            IBAN = iban,
            StartingBalance = initialBalance,
            Currency = currency,
            Type = AccountType.Checking,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Accounts.Add(account);

        // Save account first to avoid circular dependency
        await context.SaveChangesAsync();

        // Create linked BudgetPlan for current year
        var budgetPlan = new BudgetPlan
        {
            Id = Guid.NewGuid(),
            Name = $"{label} Budget {DateTime.UtcNow.Year}",
            Year = DateTime.UtcNow.Year,
            AccountId = account.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Link the budget plan to the account before saving to avoid an extra round-trip
        account.CurrentBudgetPlanId = budgetPlan.Id;

        context.BudgetPlans.Add(budgetPlan);
        await context.SaveChangesAsync();
        return account;
    }

    /// <summary>
    /// Seeds categories for a budget plan with specified counts for Income and Expense types.
    /// </summary>
    /// <param name="context">Database context for saving entities.</param>
    /// <param name="budgetPlanId">ID of the BudgetPlan to associate categories with.</param>
    /// <param name="incomeCount">Number of Income categories to create.</param>
    /// <param name="expenseCount">Number of Expense categories to create.</param>
    /// <returns>List of created Category entities (Income categories first, then Expense categories).</returns>
    public static async Task<List<Category>> SeedCategoriesAsync(
        AppDbContext context,
        Guid budgetPlanId,
        int incomeCount,
        int expenseCount)
    {
        var categories = new List<Category>();

        // Create Income categories
        for (int i = 1; i <= incomeCount; i++)
        {
            var category = new Category
            {
                Id = Guid.NewGuid(),
                Name = $"Income {i}",
                Type = CategoryType.Income,
                BudgetPlanId = budgetPlanId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            categories.Add(category);
            context.Categories.Add(category);
        }

        // Create Expense categories
        for (int i = 1; i <= expenseCount; i++)
        {
            var category = new Category
            {
                Id = Guid.NewGuid(),
                Name = $"Expense {i}",
                Type = CategoryType.Expense,
                BudgetPlanId = budgetPlanId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            categories.Add(category);
            context.Categories.Add(category);
        }

        await context.SaveChangesAsync();
        return categories;
    }

    /// <summary>
    /// Seeds transactions for an account with random dates, amounts, and descriptions.
    /// Transactions are created with dates within the last 90 days.
    /// </summary>
    /// <param name="context">Database context for saving entities.</param>
    /// <param name="accountId">ID of the Account to associate transactions with.</param>
    /// <param name="count">Number of transactions to create.</param>
    /// <param name="minAmount">Minimum transaction amount (can be negative for expenses).</param>
    /// <param name="maxAmount">Maximum transaction amount.</param>
    /// <returns>List of created Transaction entities ordered by date descending.</returns>
    public static async Task<List<Transaction>> SeedTransactionsAsync(
        AppDbContext context,
        Guid accountId,
        int count,
        decimal minAmount,
        decimal maxAmount)
    {
        var transactions = new List<Transaction>();
        var startDate = DateTime.UtcNow.AddDays(-90);

        for (int i = 0; i < count; i++)
        {
            var amount = (decimal)(_random.NextDouble() * (double)(maxAmount - minAmount) + (double)minAmount);
            var daysOffset = _random.Next(0, 90);
            var description = _transactionDescriptions[_random.Next(_transactionDescriptions.Length)];

            var transaction = new Transaction
            {
                Id = Guid.NewGuid(),
                AccountId = accountId,
                Amount = Math.Round(amount, 2),
                Date = startDate.AddDays(daysOffset),
                Description = description,
                Counterparty = $"Counterparty {i + 1}",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            transactions.Add(transaction);
            context.Transactions.Add(transaction);
        }

        await context.SaveChangesAsync();
        return transactions.OrderByDescending(t => t.Date).ToList();
    }

    /// <summary>
    /// Seeds labeled examples for ML model training simulation.
    /// Creates training data linking transactions to categories with confidence scores.
    /// </summary>
    /// <param name="context">Database context for saving entities.</param>
    /// <param name="accountId">ID of the Account to fetch transactions from.</param>
    /// <param name="labeledExamplesCount">Number of labeled examples to create.</param>
    /// <returns>List of created LabeledExample entities.</returns>
    /// <remarks>
    /// Requires existing transactions and categories in the database.
    /// Will randomly select transactions and categories for labeling.
    /// </remarks>
    public static async Task<List<LabeledExample>> SeedMLDataAsync(
        AppDbContext context,
        Guid accountId,
        int labeledExamplesCount)
    {
        // Fetch existing transactions and categories
        var transactions = await context.Transactions
            .Where(t => t.AccountId == accountId)
            .Take(labeledExamplesCount)
            .ToListAsync();

        var account = await context.Accounts
            .Include(a => a.CurrentBudgetPlan)
            .FirstOrDefaultAsync(a => a.Id == accountId);

        if (account?.CurrentBudgetPlanId == null)
        {
            throw new InvalidOperationException($"Account {accountId} has no budget plan.");
        }

        var categories = await context.Categories
            .Where(c => c.BudgetPlanId == account.CurrentBudgetPlanId.Value)
            .ToListAsync();

        if (!categories.Any())
        {
            throw new InvalidOperationException($"No categories found for account {accountId}.");
        }

        var labeledExamples = new List<LabeledExample>();

        foreach (var transaction in transactions.Take(labeledExamplesCount))
        {
            // Randomly select a category (prefer Expense categories for negative amounts, Income for positive)
            var categoryType = transaction.Amount >= 0 ? CategoryType.Income : CategoryType.Expense;
            var eligibleCategories = categories.Where(c => c.Type == categoryType).ToList();

            if (!eligibleCategories.Any())
            {
                eligibleCategories = categories; // Fallback to any category
            }

            var category = eligibleCategories[_random.Next(eligibleCategories.Count)];

            var labeledExample = new LabeledExample
            {
                Id = Guid.NewGuid(),
                TransactionId = transaction.Id,
                CategoryId = category.Id,
                WasAutoApplied = _random.NextDouble() > 0.5,
                AcceptedSuggestion = _random.NextDouble() > 0.3,
                SuggestionConfidence = (float)(_random.NextDouble() * 0.5 + 0.5), // 0.5-1.0
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            labeledExamples.Add(labeledExample);
            context.LabeledExamples.Add(labeledExample);
        }

        await context.SaveChangesAsync();
        return labeledExamples;
    }

    /// <summary>
    /// Seeds auto-apply history audit entries for monitoring dashboard tests.
    /// Creates transaction audit entries simulating auto-applied assignments with optional undo actions.
    /// </summary>
    /// <param name="context">Database context for saving entities.</param>
    /// <param name="accountId">ID of the Account to fetch transactions from.</param>
    /// <param name="totalCount">Total number of audit entries to create.</param>
    /// <param name="undoCount">Number of audit entries marked as undone.</param>
    /// <returns>List of created TransactionAudit entities.</returns>
    /// <remarks>
    /// Requires existing transactions in the database.
    /// Creates both auto-apply and undo audit entries.
    /// </remarks>
    public static async Task<List<TransactionAudit>> SeedAutoApplyHistoryAsync(
        AppDbContext context,
        Guid accountId,
        int totalCount,
        int undoCount)
    {
        // Fetch existing transactions
        var transactions = await context.Transactions
            .Where(t => t.AccountId == accountId)
            .Take(totalCount)
            .ToListAsync();

        if (transactions.Count < totalCount)
        {
            throw new InvalidOperationException(
                $"Not enough transactions found. Required: {totalCount}, Found: {transactions.Count}");
        }

        var auditEntries = new List<TransactionAudit>();

        for (int i = 0; i < totalCount; i++)
        {
            var transaction = transactions[i];
            var isUndone = i < undoCount;

            var auditEntry = new TransactionAudit
            {
                Id = Guid.NewGuid(),
                TransactionId = transaction.Id,
                ActionType = isUndone ? "Undo" : "AutoAssign",
                ChangedBy = "AutoApplyService",
                ChangedAt = DateTime.UtcNow.AddMinutes(-i),
                IsAutoApplied = true,
                AutoAppliedBy = "AutoApplyService",
                AutoAppliedAt = DateTime.UtcNow.AddMinutes(-i),
                Confidence = (float)(_random.NextDouble() * 0.3 + 0.7), // 0.7-1.0 for auto-apply
                ModelVersion = 1,
                BeforeState = isUndone ? "{\"assigned\":true}" : "{\"assigned\":false}",
                AfterState = isUndone ? "{\"assigned\":false}" : "{\"assigned\":true}",
                Reason = isUndone ? "User requested undo" : "Auto-applied by ML model",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            auditEntries.Add(auditEntry);
            context.TransactionAudits.Add(auditEntry);
        }

        await context.SaveChangesAsync();
        return auditEntries;
    }
}
