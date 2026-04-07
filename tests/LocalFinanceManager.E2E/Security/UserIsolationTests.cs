using LocalFinanceManager.Data;
using LocalFinanceManager.Data.Repositories;
using LocalFinanceManager.Models;
using LocalFinanceManager.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Playwright;

namespace LocalFinanceManager.E2E.Security;

/// <summary>
/// Security tests that verify user data isolation.
/// Each user must only be able to see and access their own accounts, transactions,
/// and budget plans — never another user's data.
/// </summary>
[TestFixture]
public class UserIsolationTests : E2ETestBase
{
    // User A is the existing seed user; User B is a second, distinct user
    private static readonly Guid UserAId = AppDbContext.SeedUserId;
    private static readonly Guid UserBId = new Guid("00000000-0000-0000-0000-000000000099");

    /// <summary>
    /// Runs before every test: truncate financial data for a predictable baseline
    /// and ensure User B exists in the Users table (FK dependency).
    /// </summary>
    [SetUp]
    public async Task IsolationSetUpAsync()
    {
        // Remove all financial records so each test owns exactly what it seeds
        await Factory!.TruncateTablesAsync();

        using var scope = Factory.CreateDbScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // User A (SeedUserId) is created by SeedAsync() at startup and is not truncated.
        // User B must be created once per fixture lifetime; guard against duplicates.
        if (!context.Users.Any(u => u.Id == UserBId))
        {
            context.Users.Add(new User
            {
                Id = UserBId,
                SupabaseUserId = "00000000-0000-0000-0000-000000000099",
                Email = "userb@test.local",
                DisplayName = "User B",
                EmailConfirmed = true,
                IsArchived = false
            });
            await context.SaveChangesAsync();
        }
    }

    // -------------------------------------------------------------------------
    // Repository-level isolation: Accounts
    // -------------------------------------------------------------------------

    [Test]
    [Description("User A should only retrieve their own accounts, never User B's.")]
    public async Task Accounts_UserA_CannotSee_UserB_Accounts()
    {
        // Arrange
        using var scope = Factory!.CreateDbScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var accountA = BuildAccount(UserAId, "Account A", "NL91ABNA0417164300");
        var accountB = BuildAccount(UserBId, "Account B", "NL20INGB0001234567");
        context.Accounts.AddRange(accountA, accountB);
        await context.SaveChangesAsync();

        // Act
        var accountsA = await CreateAccountRepo(context, UserAId).GetAllActiveAsync();
        var accountsB = await CreateAccountRepo(context, UserBId).GetAllActiveAsync();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(accountsA.Select(a => a.Id), Does.Contain(accountA.Id),
                "User A must see their own account");
            Assert.That(accountsA.Select(a => a.Id), Does.Not.Contain(accountB.Id),
                "User A must NOT see User B's account");

            Assert.That(accountsB.Select(a => a.Id), Does.Contain(accountB.Id),
                "User B must see their own account");
            Assert.That(accountsB.Select(a => a.Id), Does.Not.Contain(accountA.Id),
                "User B must NOT see User A's account");
        });
    }

    [Test]
    [Description("Fetching an account by ID as User A must not return User B's account.")]
    public async Task Account_GetById_UserA_CannotRetrieve_UserB_Account()
    {
        // Arrange
        using var scope = Factory!.CreateDbScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var accountB = BuildAccount(UserBId, "Account B", "NL20INGB0001234567");
        context.Accounts.Add(accountB);
        await context.SaveChangesAsync();

        // Act – User A tries to fetch User B's account by its ID
        var result = await CreateAccountRepo(context, UserAId).GetByIdAsync(accountB.Id);

        // Assert
        Assert.That(result, Is.Null, "User A must not retrieve User B's account by ID");
    }

    // -------------------------------------------------------------------------
    // Repository-level isolation: Transactions
    // -------------------------------------------------------------------------

    [Test]
    [Description("User A should only retrieve their own transactions, never User B's.")]
    public async Task Transactions_UserA_CannotSee_UserB_Transactions()
    {
        // Arrange
        using var scope = Factory!.CreateDbScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var accountA = BuildAccount(UserAId, "Account A", "NL91ABNA0417164300");
        var accountB = BuildAccount(UserBId, "Account B", "NL20INGB0001234567");
        context.Accounts.AddRange(accountA, accountB);
        await context.SaveChangesAsync();

        var txA = BuildTransaction(UserAId, accountA.Id, "Tx A");
        var txB = BuildTransaction(UserBId, accountB.Id, "Tx B");
        context.Transactions.AddRange(txA, txB);
        await context.SaveChangesAsync();

        // Act
        var txsA = await CreateTransactionRepo(context, UserAId).GetActiveAsync();
        var txsB = await CreateTransactionRepo(context, UserBId).GetActiveAsync();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(txsA.Select(t => t.Id), Does.Contain(txA.Id),
                "User A must see their own transaction");
            Assert.That(txsA.Select(t => t.Id), Does.Not.Contain(txB.Id),
                "User A must NOT see User B's transaction");

            Assert.That(txsB.Select(t => t.Id), Does.Contain(txB.Id),
                "User B must see their own transaction");
            Assert.That(txsB.Select(t => t.Id), Does.Not.Contain(txA.Id),
                "User B must NOT see User A's transaction");
        });
    }

    [Test]
    [Description("Querying transactions by account ID as User A must not return User B's transactions.")]
    public async Task Transactions_GetByAccountId_UserA_CannotSee_UserB_Transactions()
    {
        // Arrange
        using var scope = Factory!.CreateDbScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Both accounts exist and User A somehow learns accountB's ID
        var accountA = BuildAccount(UserAId, "Account A", "NL91ABNA0417164300");
        var accountB = BuildAccount(UserBId, "Account B", "NL20INGB0001234567");
        context.Accounts.AddRange(accountA, accountB);
        await context.SaveChangesAsync();

        var txB = BuildTransaction(UserBId, accountB.Id, "Tx B");
        context.Transactions.Add(txB);
        await context.SaveChangesAsync();

        // Act – User A queries transactions for accountB's ID
        var result = await CreateTransactionRepo(context, UserAId).GetByAccountIdAsync(accountB.Id);

        // Assert
        Assert.That(result, Is.Empty,
            "User A must not retrieve any transactions from User B's account");
    }

    // -------------------------------------------------------------------------
    // Repository-level isolation: Budget plans
    // -------------------------------------------------------------------------

    [Test]
    [Description("Querying budget plans by account ID as User A must not return User B's plans.")]
    public async Task BudgetPlans_GetByAccountId_UserA_CannotSee_UserB_Plans()
    {
        // Arrange
        using var scope = Factory!.CreateDbScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var accountA = BuildAccount(UserAId, "Account A", "NL91ABNA0417164300");
        var accountB = BuildAccount(UserBId, "Account B", "NL20INGB0001234567");
        context.Accounts.AddRange(accountA, accountB);
        await context.SaveChangesAsync();

        var bpA = BuildBudgetPlan(UserAId, accountA.Id, "Plan A");
        var bpB = BuildBudgetPlan(UserBId, accountB.Id, "Plan B");
        context.BudgetPlans.AddRange(bpA, bpB);
        await context.SaveChangesAsync();

        // Act – User A queries the budget plans linked to accountA and accountB
        var plansForAccountA = await CreateBudgetPlanRepo(context, UserAId).GetByAccountIdAsync(accountA.Id);
        var plansForAccountB = await CreateBudgetPlanRepo(context, UserAId).GetByAccountIdAsync(accountB.Id);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(plansForAccountA.Select(p => p.Id), Does.Contain(bpA.Id),
                "User A must see their own budget plan");
            Assert.That(plansForAccountB, Is.Empty,
                "User A must not see User B's budget plan even when querying by User B's accountId");
        });
    }

    [Test]
    [Description("Fetching a budget plan by ID as User A must not return User B's plan.")]
    public async Task BudgetPlan_GetByIdWithLines_UserA_CannotRetrieve_UserB_Plan()
    {
        // Arrange
        using var scope = Factory!.CreateDbScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var accountB = BuildAccount(UserBId, "Account B", "NL20INGB0001234567");
        context.Accounts.Add(accountB);
        await context.SaveChangesAsync();

        var bpB = BuildBudgetPlan(UserBId, accountB.Id, "Plan B");
        context.BudgetPlans.Add(bpB);
        await context.SaveChangesAsync();

        // Act – User A attempts to load User B's budget plan by ID
        var result = await CreateBudgetPlanRepo(context, UserAId).GetByIdWithLinesAsync(bpB.Id);

        // Assert
        Assert.That(result, Is.Null, "User A must not retrieve User B's budget plan by ID");
    }

    // -------------------------------------------------------------------------
    // Blazor routing: unauthenticated access is redirected to /login
    // -------------------------------------------------------------------------

    [Test]
    [Description("Visiting /accounts without a JWT in sessionStorage must redirect to /login.")]
    public async Task UnauthenticatedBrowser_Accounts_RedirectsToLogin()
    {
        await AssertRedirectsToLogin("/accounts");
    }

    [Test]
    [Description("Visiting /transactions without a JWT in sessionStorage must redirect to /login.")]
    public async Task UnauthenticatedBrowser_Transactions_RedirectsToLogin()
    {
        await AssertRedirectsToLogin("/transactions");
    }

    [Test]
    [Description("Visiting /budgets without a JWT in sessionStorage must redirect to /login.")]
    public async Task UnauthenticatedBrowser_BudgetPlans_RedirectsToLogin()
    {
        await AssertRedirectsToLogin("/budgets");
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private async Task AssertRedirectsToLogin(string path)
    {
        // Use a fresh browser context so sessionStorage is completely empty.
        // Without the e2e-auth-token cookie, E2ECookieAuthenticationHandler returns NoResult
        // and UseAuthorization challenges anonymous access to [Authorize] pages. The overridden
        // HandleChallengeAsync in the test handler issues an HTTP 302 redirect to /login
        // instead of the default 401 (which UseStatusCodePagesWithReExecute would convert to 404).
        await using var context = await Browser.NewContextAsync(ContextOptions());
        var page = await context.NewPageAsync();

        await page.GotoAsync($"{BaseUrl}{path}", new PageGotoOptions
        {
            WaitUntil = WaitUntilState.NetworkIdle,
            Timeout = 30_000
        });

        await Assertions.Expect(page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex("/login"), new() { Timeout = 20_000 });

        Assert.That(page.Url, Does.Contain("/login"),
            $"Unauthenticated access to {path} must redirect to /login");
    }

    private static Account BuildAccount(Guid userId, string label, string iban) => new()
    {
        Id = Guid.NewGuid(),
        UserId = userId,
        Label = label,
        IBAN = iban,
        Currency = "EUR",
        StartingBalance = 100m,
        Type = AccountType.Checking
    };

    private static Transaction BuildTransaction(Guid userId, Guid accountId, string description) => new()
    {
        Id = Guid.NewGuid(),
        UserId = userId,
        AccountId = accountId,
        Amount = -10m,
        Date = DateTime.UtcNow,
        Description = description
    };

    private static BudgetPlan BuildBudgetPlan(Guid userId, Guid accountId, string name) => new()
    {
        Id = Guid.NewGuid(),
        UserId = userId,
        AccountId = accountId,
        Name = name,
        Year = DateTime.UtcNow.Year
    };

    private static AccountRepository CreateAccountRepo(AppDbContext context, Guid userId) =>
        new(context, NullLogger<Repository<Account>>.Instance, new StaticUserContext(userId));

    private static TransactionRepository CreateTransactionRepo(AppDbContext context, Guid userId) =>
        new(context, NullLogger<TransactionRepository>.Instance, new StaticUserContext(userId));

    private static BudgetPlanRepository CreateBudgetPlanRepo(AppDbContext context, Guid userId) =>
        new(context, NullLogger<Repository<BudgetPlan>>.Instance, new StaticUserContext(userId));

    /// <summary>
    /// A test-only <see cref="IUserContext"/> that always returns a fixed user ID.
    /// </summary>
    private sealed class StaticUserContext : IUserContext
    {
        private readonly Guid _userId;
        public StaticUserContext(Guid userId) => _userId = userId;
        public Guid GetCurrentUserId() => _userId;
        public string GetCurrentUserEmail() => string.Empty;
        public bool IsAuthenticated() => _userId != Guid.Empty;
    }
}
