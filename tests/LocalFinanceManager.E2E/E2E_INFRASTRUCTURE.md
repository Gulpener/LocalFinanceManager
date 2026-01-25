# E2E Test Infrastructure Documentation

## Overview

This document describes the end-to-end (E2E) test infrastructure for LocalFinanceManager. The infrastructure provides reusable components, seed helpers, and page object models to enable efficient and maintainable E2E testing.

## Architecture

### Core Components

1. **TestWebApplicationFactory** - Web application factory for hosting the application in tests
2. **E2ETestBase** - Base class for all E2E tests with screenshot/video capture
3. **SeedDataHelper** - Reusable seed methods for creating test data
4. **PageObjectModels** - Abstractions for UI interactions
5. **.runsettings** - Configuration for parallel execution and test parameters

### Test Structure

```
LocalFinanceManager.E2E/
├── E2ETestBase.cs                    # Base test class
├── TestWebApplicationFactory.cs      # App factory for tests
├── .runsettings                      # Test configuration
├── Helpers/
│   └── SeedDataHelper.cs            # Seed data methods
├── Pages/
│   ├── PageObjectBase.cs            # Base page object model
│   ├── TransactionsPageModel.cs     # Transactions page interactions
│   ├── AssignmentModalPageModel.cs  # Assignment modal interactions
│   ├── SplitEditorPageModel.cs      # Split editor interactions
│   └── BulkAssignModalPageModel.cs  # Bulk assign modal interactions
├── Infrastructure/
│   └── SmokeTests.cs                # Infrastructure validation tests
├── Accounts/
│   └── AccountCrudTests.cs          # Account CRUD tests
├── Categories/
│   └── CategoryCrudTests.cs         # Category CRUD tests
└── BudgetPlans/
    └── BudgetPlanTests.cs           # Budget plan tests
```

## Getting Started

### Prerequisites

1. **.NET 10.0 SDK** installed
2. **Playwright browsers** installed

### Installing Playwright Browsers

Run the following command to install Playwright browsers:

```powershell
# From the E2E test project directory
cd tests/LocalFinanceManager.E2E
pwsh bin/Debug/net10.0/playwright.ps1 install
```

Or run it after building the project:

```powershell
dotnet build tests/LocalFinanceManager.E2E
pwsh tests/LocalFinanceManager.E2E/bin/Debug/net10.0/playwright.ps1 install
```

### Running Tests

#### Run All E2E Tests

```powershell
dotnet test tests/LocalFinanceManager.E2E
```

#### Run Tests with .runsettings Configuration

```powershell
dotnet test tests/LocalFinanceManager.E2E --settings tests/LocalFinanceManager.E2E/.runsettings
```

#### Run Specific Test Class

```powershell
dotnet test tests/LocalFinanceManager.E2E --filter "FullyQualifiedName~SmokeTests"
```

#### Run Tests in Headed Mode (See Browser)

```powershell
$env:HEADED="1"
dotnet test tests/LocalFinanceManager.E2E
```

#### Run Tests with Slow Motion (Debugging)

```powershell
$env:SLOWMO="500"  # 500ms delay between actions
dotnet test tests/LocalFinanceManager.E2E
```

#### Run Tests with Video Recording

```powershell
$env:CI="true"
dotnet test tests/LocalFinanceManager.E2E
```

Videos will be saved to `test-results/videos/`.

## Using SeedDataHelper

### Overview

`SeedDataHelper` provides reusable methods to create test data in the database. All methods are async and require an `AppDbContext` instance.

### Example: Seed Account with Budget Plan

```csharp
using var scope = Factory.CreateDbScope();
var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

var account = await SeedDataHelper.SeedAccountAsync(
    context,
    label: "Main Checking",
    iban: "DE89370400440532013000",
    initialBalance: 1000.00m,
    currency: "EUR");

// Account now has a linked BudgetPlan with 12 months
Assert.That(account.CurrentBudgetPlanId, Is.Not.Null);
```

### Example: Seed Categories

```csharp
var categories = await SeedDataHelper.SeedCategoriesAsync(
    context,
    budgetPlanId: account.CurrentBudgetPlanId.Value,
    incomeCount: 2,
    expenseCount: 5);

// Creates 2 Income categories and 5 Expense categories
Assert.That(categories.Count, Is.EqualTo(7));
```

### Example: Seed Transactions

```csharp
var transactions = await SeedDataHelper.SeedTransactionsAsync(
    context,
    accountId: account.Id,
    count: 50,
    minAmount: -200.00m,
    maxAmount: 500.00m);

// Creates 50 transactions with random dates (last 90 days) and amounts
Assert.That(transactions.Count, Is.EqualTo(50));
```

### Example: Seed ML Training Data

```csharp
// Requires existing transactions and categories
var labeledExamples = await SeedDataHelper.SeedMLDataAsync(
    context,
    accountId: account.Id,
    labeledExamplesCount: 20);

// Creates 20 labeled examples for ML training
Assert.That(labeledExamples.Count, Is.EqualTo(20));
```

### Example: Seed Auto-Apply History

```csharp
// Requires existing transactions
var auditEntries = await SeedDataHelper.SeedAutoApplyHistoryAsync(
    context,
    accountId: account.Id,
    totalCount: 15,
    undoCount: 3);

// Creates 15 audit entries (12 auto-assigns, 3 undos)
Assert.That(auditEntries.Count, Is.EqualTo(15));
Assert.That(auditEntries.Count(ae => ae.ActionType == "Undo"), Is.EqualTo(3));
```

## Using PageObjectModels

### Overview

Page Object Models (POMs) encapsulate UI interactions for specific pages or components. They provide clean, reusable methods for test actions.

### Example: TransactionsPageModel

```csharp
[Test]
public async Task FilterTransactionsByAccount()
{
    // Arrange - Seed data
    var account = await SeedDataHelper.SeedAccountAsync(context, ...);
    await SeedDataHelper.SeedTransactionsAsync(context, account.Id, 10, ...);

    // Act - Use page object model
    var transactionsPage = new TransactionsPageModel(Page, BaseUrl);
    await transactionsPage.NavigateAsync();
    await transactionsPage.SelectAccountFilterAsync(account.Id);

    // Assert
    var transactionCount = await transactionsPage.GetTransactionCountAsync();
    Assert.That(transactionCount, Is.EqualTo(10));
}
```

### Example: AssignmentModalPageModel

```csharp
[Test]
public async Task AssignTransactionToCategory()
{
    // Arrange
    var transactionsPage = new TransactionsPageModel(Page, BaseUrl);
    await transactionsPage.NavigateAsync();
    await transactionsPage.ClickAssignButtonForRowAsync(0);

    // Act - Use assignment modal
    var assignmentModal = new AssignmentModalPageModel(Page, BaseUrl);
    await assignmentModal.WaitForModalAsync();
    await assignmentModal.SelectCategoryAsync(categoryId);
    await assignmentModal.EnterNoteAsync("Test assignment");
    await assignmentModal.ClickAssignAsync();

    // Assert
    var isModalVisible = await assignmentModal.IsModalVisibleAsync();
    Assert.That(isModalVisible, Is.False);
}
```

### Example: SplitEditorPageModel

```csharp
[Test]
public async Task SplitTransactionIntoMultipleCategories()
{
    // Arrange
    var splitEditor = new SplitEditorPageModel(Page, BaseUrl);
    await splitEditor.WaitForModalAsync();

    // Act - Add split rows and configure
    await splitEditor.AddSplitRowAsync();
    await splitEditor.SetSplitAmountAsync(0, 50.00m);
    await splitEditor.SelectSplitCategoryAsync(0, category1Id);

    await splitEditor.AddSplitRowAsync();
    await splitEditor.SetSplitAmountAsync(1, 50.00m);
    await splitEditor.SelectSplitCategoryAsync(1, category2Id);

    // Assert
    var isSumValid = await splitEditor.IsSumValidAsync();
    Assert.That(isSumValid, Is.True);

    await splitEditor.ClickSaveAsync();
}
```

### Example: BulkAssignModalPageModel

```csharp
[Test]
public async Task BulkAssignMonitorsProgress()
{
    // Arrange
    var bulkModal = new BulkAssignModalPageModel(Page, BaseUrl);
    await bulkModal.WaitForModalAsync();

    // Act - Wait for completion
    await bulkModal.WaitForCompletionAsync();

    // Assert
    var successCount = await bulkModal.GetSuccessCountAsync();
    var failureCount = await bulkModal.GetFailureCountAsync();

    Assert.That(successCount, Is.GreaterThan(0));
    Assert.That(failureCount, Is.EqualTo(0));
}
```

## Screenshot and Video Capture

### Automatic Screenshot on Failure

E2ETestBase automatically captures screenshots when tests fail. Screenshots are saved to:

```
test-results/screenshots/{TestName}_{Timestamp}.png
```

### Manual Screenshot Capture

You can manually capture screenshots using the PageObjectBase:

```csharp
var transactionsPage = new TransactionsPageModel(Page, BaseUrl);
await transactionsPage.TakeScreenshotAsync("transaction-list-before-filter");
```

### Video Recording

Video recording is enabled automatically in CI environments (when `CI=true` environment variable is set). Videos are saved to:

```
test-results/videos/{TestName}.webm
```

To enable video recording locally:

```powershell
$env:CI="true"
dotnet test tests/LocalFinanceManager.E2E
```

## Parallel Execution

### Configuration

The `.runsettings` file configures 4 parallel test workers:

```xml
<MaxCpuCount>4</MaxCpuCount>
<NumberOfTestWorkers>4</NumberOfTestWorkers>
```

### Running Tests in Parallel

```powershell
dotnet test tests/LocalFinanceManager.E2E --settings tests/LocalFinanceManager.E2E/.runsettings
```

### Test Isolation

Each test runs with a fresh database instance:

- `TestWebApplicationFactory` creates a dedicated SQLite test database
- Database is recreated on factory initialization (clean state)
- Database is deleted on factory disposal (cleanup)

## Debugging Test Failures

### 1. Run Tests in Headed Mode

See the browser in action:

```powershell
$env:HEADED="1"
dotnet test tests/LocalFinanceManager.E2E --filter "FullyQualifiedName~MyFailingTest"
```

### 2. Enable Slow Motion

Add delays between actions for easier observation:

```powershell
$env:SLOWMO="1000"  # 1 second delay
dotnet test tests/LocalFinanceManager.E2E --filter "FullyQualifiedName~MyFailingTest"
```

### 3. Review Screenshots

Check the screenshot captured on failure:

```
test-results/screenshots/{TestName}_{Timestamp}.png
```

### 4. Review Videos (CI)

If running in CI or with `CI=true`, review the recorded video:

```
test-results/videos/{TestName}.webm
```

### 5. Add Breakpoints

Use Visual Studio or VS Code debugger with breakpoints in test code.

## Database Cleanup Strategy

### Automatic Cleanup

- Each test gets a fresh database (via `TestWebApplicationFactory`)
- Database file is deleted on factory disposal
- No manual cleanup needed

### Manual Database Reset

If you need to reset the database mid-test:

```csharp
await Factory.ResetDatabaseAsync();
```

### Test Database Location

The test database is created as:

```
localfinancemanager.test.db
```

This file is automatically ignored by Git (see `.gitignore`).

## CI Integration

### GitHub Actions Example

```yaml
name: E2E Tests

on: [push, pull_request]

jobs:
  e2e:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3

      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: "10.0.x"

      - name: Install Playwright browsers
        run: |
          dotnet build tests/LocalFinanceManager.E2E
          pwsh tests/LocalFinanceManager.E2E/bin/Debug/net10.0/playwright.ps1 install

      - name: Run E2E tests
        env:
          CI: true
        run: dotnet test tests/LocalFinanceManager.E2E --settings tests/LocalFinanceManager.E2E/.runsettings

      - name: Upload test results
        if: always()
        uses: actions/upload-artifact@v3
        with:
          name: test-results
          path: test-results/
```

## Best Practices

### 1. Use SeedDataHelper for Test Data

❌ **Don't** create entities inline in tests:

```csharp
var account = new Account { Label = "Test", IBAN = "...", ... };
context.Accounts.Add(account);
await context.SaveChangesAsync();
```

✅ **Do** use SeedDataHelper:

```csharp
var account = await SeedDataHelper.SeedAccountAsync(context, "Test", "...", 1000m);
```

### 2. Use PageObjectModels for UI Interactions

❌ **Don't** interact with selectors directly in tests:

```csharp
await Page.ClickAsync("button[data-action='assign']");
await Page.FillAsync("#category-select", categoryId.ToString());
```

✅ **Do** use PageObjectModels:

```csharp
var assignmentModal = new AssignmentModalPageModel(Page, BaseUrl);
await assignmentModal.SelectCategoryAsync(categoryId);
await assignmentModal.ClickAssignAsync();
```

### 3. Isolate Test Data

Each test should create its own data using SeedDataHelper. Don't rely on data from previous tests.

### 4. Use Descriptive Test Names

Follow the pattern: `MethodUnderTest_Scenario_ExpectedOutcome`

```csharp
[Test]
public async Task AssignTransaction_ValidCategory_AssignsSuccessfully()
```

### 5. Follow AAA Pattern

Structure tests with Arrange, Act, Assert sections:

```csharp
[Test]
public async Task TestName()
{
    // Arrange
    var account = await SeedDataHelper.SeedAccountAsync(...);
    var transactionsPage = new TransactionsPageModel(Page, BaseUrl);

    // Act
    await transactionsPage.NavigateAsync();
    var count = await transactionsPage.GetTransactionCountAsync();

    // Assert
    Assert.That(count, Is.EqualTo(0));
}
```

## Troubleshooting

### "Playwright browser not found"

Install Playwright browsers:

```powershell
pwsh tests/LocalFinanceManager.E2E/bin/Debug/net10.0/playwright.ps1 install
```

### "Database locked" errors

Ensure you're disposing the DbContext scope properly:

```csharp
using var scope = Factory.CreateDbScope();
var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
// ... use context ...
// Scope automatically disposes at end of using block
```

### Tests fail only in CI

- Enable video recording locally: `$env:CI="true"`
- Review CI artifacts for screenshots/videos
- Check for timing issues (add waits if needed)

### Parallel tests interfering with each other

Each test should have an isolated database. If issues persist:

- Check for shared resources (files, ports, etc.)
- Ensure tests don't modify global state

## Summary

The E2E test infrastructure provides:

- ✅ **TestWebApplicationFactory** for isolated test environments
- ✅ **SeedDataHelper** for consistent test data creation
- ✅ **PageObjectModels** for maintainable UI interactions
- ✅ **Automatic screenshot capture** on test failures
- ✅ **Video recording** in CI environments
- ✅ **Parallel execution** for fast feedback
- ✅ **Comprehensive documentation** for developer onboarding

For questions or issues, refer to the test code examples in the Infrastructure/SmokeTests.cs file.
