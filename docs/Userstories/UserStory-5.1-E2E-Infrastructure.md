# UserStory-5.1: E2E Test Infrastructure Setup

## Objective

Establish comprehensive end-to-end test infrastructure using NUnit + Playwright to enable parallel test development for transaction assignment features (US-5, US-6, US-7). Provide reusable seed data helpers, PageObjectModel base classes, and test configuration to support automated UI testing across all assignment workflows.

## Requirements

- Enhance existing `TestWebApplicationFactory` with dedicated SQLite test database configuration
- Create reusable seed data helpers for accounts, categories, transactions, ML data, and auto-apply history
- Build PageObjectModel base classes for transaction/assignment UI components
- Configure screenshot/video capture on test failure for debugging
- Add `.runsettings` file for parallel test execution (max 4 parallel workers)
- Refactor existing E2E tests to use new seed helpers (reduce duplication)
- Provide smoke test validation for infrastructure readiness

## Pattern Adherence from Existing E2E Tests

This story **enhances** existing infrastructure in `LocalFinanceManager.E2E/`:

### Existing Infrastructure

- `TestWebApplicationFactory` - Already configured with SQLite test database
- Playwright browser automation - NUnit + Microsoft.Playwright installed
- Basic smoke tests - Infrastructure validation exists
- CRUD tests - Accounts/Categories/BudgetPlans test folders exist

### Enhancements Needed

- **Seed Helpers:** Centralized `SeedDataHelper.cs` with reusable methods
- **PageObjectModels:** Base classes for transaction/modal/editor interactions
- **Configuration:** Screenshot capture, video recording, parallel execution settings
- **Refactoring:** Update existing tests to use new seed helpers

### Test Organization (Existing Pattern)

- E2E tests in `LocalFinanceManager.E2E/` using NUnit + Playwright
- Follow AAA pattern (Arrange, Act, Assert)
- Use `WebApplicationFactory<Program>` with dedicated SQLite test database
- Database cleanup on teardown (fresh state per test)

## Implementation Tasks

### 1. Enhance TestWebApplicationFactory

- [ ] Review existing `TestWebApplicationFactory.cs` in `LocalFinanceManager.E2E/`
- [ ] Verify test database configuration (separate from dev: `localfinancemanager.test.db`)
- [ ] Add method to reset database state between test runs: `ResetDatabaseAsync()`
- [ ] Configure test logging output (ILogger integration with NUnit test context)
- [ ] Add environment-specific settings override (disable background jobs during tests)
- [ ] Document factory usage pattern in inline comments

### 2. Create SeedDataHelper Class

- [ ] Create `SeedDataHelper.cs` in `LocalFinanceManager.E2E/Helpers/`
- [ ] Add `SeedAccountAsync(string label, string iban, decimal initialBalance, string currency)` method
  - Returns created `Account` entity
  - Automatically creates linked `BudgetPlan` (current year, 12 months)
- [ ] Add `SeedCategoriesAsync(Guid budgetPlanId, int incomeCount, int expenseCount)` method
  - Creates specified number of Income/Expense categories
  - Returns `List<Category>` for test assertions
- [ ] Add `SeedTransactionsAsync(Guid accountId, int count, decimal minAmount, decimal maxAmount)` method
  - Creates transactions with random dates (last 90 days)
  - Random descriptions from predefined list ("Grocery Store", "Salary", "Fuel", etc.)
  - Returns `List<Transaction>` for test assertions
- [ ] Add `SeedMLDataAsync(Guid accountId, int labeledExamplesCount)` method
  - Creates `LabeledExample` entities for ML model training simulation
  - Links to specified account's categories
  - Returns `List<LabeledExample>` for ML tests
- [ ] Add `SeedAutoApplyHistoryAsync(Guid accountId, int totalCount, int undoCount)` method
  - Creates auto-applied transaction history
  - Simulates undo actions for specified count
  - Returns `List<TransactionAuditEntry>` for monitoring dashboard tests
- [ ] Add comprehensive XML documentation for all seed methods

### 3. Create PageObjectModel Base Classes

- [ ] Create `PageObjectBase.cs` in `LocalFinanceManager.E2E/Pages/`
- [ ] Add properties: `IPage Page`, `string BaseUrl`
- [ ] Add navigation helper: `NavigateToAsync(string path)`
- [ ] Add wait helper: `WaitForSelectorAsync(string selector, int timeoutMs = 5000)`
- [ ] Add screenshot helper: `TakeScreenshotAsync(string fileName)`
- [ ] Create `TransactionsPageModel.cs` extending `PageObjectBase`
  - Add selectors: Account filter dropdown, transaction table rows, pagination controls
  - Add methods: `SelectAccountFilterAsync(Guid accountId)`, `GetTransactionRowsAsync()`, `NavigateToPageAsync(int pageNumber)`
- [ ] Create `AssignmentModalPageModel.cs` extending `PageObjectBase`
  - Add selectors: Category dropdown, budget line dropdown, note input, assign button, cancel button
  - Add methods: `SelectCategoryAsync(Guid categoryId)`, `ClickAssignAsync()`, `ClickCancelAsync()`
- [ ] Create `SplitEditorPageModel.cs` extending `PageObjectBase`
  - Add selectors: Split rows, amount inputs, category selectors, add/remove buttons, sum validation indicator
  - Add methods: `AddSplitRowAsync()`, `RemoveSplitRowAsync(int index)`, `SetSplitAmountAsync(int index, decimal amount)`, `GetSumValidationStatusAsync()`
- [ ] Create `BulkAssignModalPageModel.cs` extending `PageObjectBase`
  - Add selectors: Progress bar, success summary, error accordion, close button
  - Add methods: `GetProgressPercentageAsync()`, `GetSuccessCountAsync()`, `GetFailureCountAsync()`, `ExpandErrorDetailsAsync()`

### 4. Configure Screenshot and Video Capture

- [ ] Add screenshot capture on test failure in `E2ETestBase` teardown
- [ ] Configure screenshot directory: `test-results/screenshots/`
- [ ] Add timestamp to screenshot filenames: `{TestName}_{DateTime}.png`
- [ ] Add video recording configuration (Playwright browser context options)
- [ ] Enable video only in CI environment (environment variable: `CI=true`)
- [ ] Configure video directory: `test-results/videos/`
- [ ] Add cleanup of old test results (keep last 10 runs)

### 5. Add .runsettings Configuration

- [ ] Create `.runsettings` file in `LocalFinanceManager.E2E/` root
- [ ] Configure parallel execution: `MaxCpuCount=4` (4 parallel workers)
- [ ] Configure test timeout: `TestTimeout=60000` (60 seconds per test)
- [ ] Configure test output directory: `ResultsDirectory=test-results/`
- [ ] Add test run parameters:
  ```xml
  <TestRunParameters>
    <Parameter name="BaseUrl" value="http://localhost:5000" />
    <Parameter name="Headless" value="true" />
    <Parameter name="SlowMo" value="0" />
  </TestRunParameters>
  ```
- [ ] Document `.runsettings` usage in E2E README

### 6. Refactor Existing E2E Tests

- [ ] Update tests in `LocalFinanceManager.E2E/Accounts/` to use `SeedDataHelper.SeedAccountAsync()`
  - Remove inline account creation code
  - Replace with seed helper calls
- [ ] Update tests in `LocalFinanceManager.E2E/Categories/` to use `SeedDataHelper.SeedCategoriesAsync()`
  - Remove inline category creation code
  - Replace with seed helper calls
- [ ] Update tests in `LocalFinanceManager.E2E/BudgetPlans/` to use seed helpers
  - Simplify test arrange sections
  - Improve test readability
- [ ] Verify all refactored tests pass with new seed helpers

### 7. Create Smoke Test for Infrastructure

- [ ] Create `SmokeTests.cs` in `LocalFinanceManager.E2E/Tests/`
- [ ] Test: Application starts successfully (`GET /` returns HTTP 200)
- [ ] Test: Database connection successful (query accounts table)
- [ ] Test: Playwright browser launches (navigate to home page)
- [ ] Test: SeedDataHelper creates account successfully
- [ ] Test: PageObjectModel navigation works (navigate to transactions page)
- [ ] Run smoke tests to verify infrastructure readiness

### 8. Documentation

- [ ] Create `E2E_INFRASTRUCTURE.md` in `LocalFinanceManager.E2E/` with:
  - Overview of test infrastructure components
  - SeedDataHelper usage examples
  - PageObjectModel usage examples
  - How to run tests locally (headed/headless mode)
  - How to debug test failures (screenshots, videos, slow-mo)
  - Parallel execution configuration
  - CI integration notes
- [ ] Add setup instructions for Playwright browsers: `pwsh bin/Debug/net10.0/playwright.ps1 install`
- [ ] Document test database cleanup strategy

## Testing

### Validation Scenarios

1. **Seed Helpers:**

   - SeedAccountAsync creates account + linked budget plan
   - SeedCategoriesAsync creates correct count of Income/Expense categories
   - SeedTransactionsAsync creates transactions with specified parameters
   - SeedMLDataAsync creates labeled examples for ML training
   - SeedAutoApplyHistoryAsync creates auto-apply history with undo records

2. **PageObjectModels:**

   - TransactionsPageModel navigates to transactions page successfully
   - AssignmentModalPageModel selects category from dropdown
   - SplitEditorPageModel adds/removes split rows
   - BulkAssignModalPageModel retrieves progress percentage

3. **Configuration:**

   - Screenshot captured on test failure (saved to test-results/screenshots/)
   - Video recorded in CI environment only
   - Parallel execution runs 4 tests concurrently
   - Test timeout enforced (60 seconds)

4. **Refactored Tests:**
   - All existing E2E tests pass with new seed helpers
   - Test execution time improved (reduced setup duplication)
   - Test readability improved (cleaner arrange sections)

## Success Criteria

- ✅ `SeedDataHelper.cs` provides 5 reusable seed methods with XML documentation
- ✅ PageObjectModel base classes cover 4 key UI areas (transactions, modals, editors)
- ✅ Screenshot capture on failure functional (test-results/screenshots/ directory populated)
- ✅ `.runsettings` file enables 4 parallel test workers
- ✅ Existing E2E tests refactored to use seed helpers (0 inline creation code)
- ✅ Smoke tests validate infrastructure readiness (5 tests pass)
- ✅ `E2E_INFRASTRUCTURE.md` documentation comprehensive (setup, usage, debugging)
- ✅ All tests pass locally and in CI environment

## Definition of Done

- `SeedDataHelper.cs` class implemented with 5 seed methods in `LocalFinanceManager.E2E/Helpers/`
- PageObjectModel base classes implemented: `PageObjectBase`, `TransactionsPageModel`, `AssignmentModalPageModel`, `SplitEditorPageModel`, `BulkAssignModalPageModel`
- Screenshot/video capture configured in `E2ETestBase` teardown
- `.runsettings` file created with parallel execution and test parameters
- Existing tests in `Accounts/`, `Categories/`, `BudgetPlans/` refactored to use seed helpers
- Smoke tests implemented and passing in `SmokeTests.cs`
- `E2E_INFRASTRUCTURE.md` documentation complete
- Code follows Implementation-Guidelines.md (.NET 10.0, async/await, ILogger)
- Infrastructure ready for US-9 (Integration Tests) and US-8 (UX Enhancements) implementation

## Dependencies

- **Existing E2E Project:** `LocalFinanceManager.E2E/` already exists with NUnit + Playwright installed
- **TestWebApplicationFactory:** Already implemented and functional
- **No blocking dependencies:** Can start immediately in parallel with UserStory-5

## Estimated Effort

**1-2 days** (~25 implementation tasks)

## Notes

- This story **enables** parallel test development: Complete before US-5 finishes to allow US-9 (Integration Tests) to start immediately after US-5/6/7.
- Seed helpers designed for reusability: All future E2E tests (US-9, US-8) will use these helpers.
- PageObjectModels follow DRY principle: Avoid selector duplication across tests.
- Screenshot/video capture invaluable for debugging CI test failures: Always enable in CI pipeline.
- Parallel execution reduces total test suite runtime: Critical for fast feedback loops.
- Refactoring existing tests ensures consistency: All tests follow same patterns.
