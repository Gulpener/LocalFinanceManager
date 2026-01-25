# UserStory-9: E2E Tests - Phase 1 Foundation

## Objective

Implement foundational E2E tests (20 tests) covering transaction import, basic assignment, and multi-account validation. Addresses technical debt from archived US-5 and deferred MVP-3 import tests. Establishes foundation for Phase 2 (US-9.1) and Phase 3 (US-9.2).

## Requirements

- Implement 20 tests across 3 test suites: TransactionImportTests (8), BasicAssignmentTests (11), MultiAccountValidationTests (1)
- Use PageObjectModels and SeedDataHelper from US-5.1 for consistent test structure
- Enhance TransactionListPage with missing methods for filters, audit trail, bulk operations
- Create ImportModalPageModel for CSV/JSON upload workflow
- Per-test cleanup strategy (prevent database bloat during test runs)
- CI execution time target <5 minutes for Phase 1 suite
- Capture screenshots for workflow stages with specific filenames
- Create E2E_TEST_GUIDE.md with test execution, debugging, and CI setup documentation

## Patterns for E2E Testing

**When to Write E2E Tests:**

- Immediately after feature implementation for fast feedback
- When validating complete user workflows (import → assign → validate)
- When testing system-level constraints (budget plan isolation, UserStory-4 enforcement)

**E2E Test Structure:**

- Use PageObjectModels for all UI interactions (no direct DOM queries)
- Use SeedDataHelper for test data creation (no inline test data)
- Per-test cleanup (fresh database state for each test)
- Test complete user actions, not isolated component behavior

**Test Organization:**

- Create dedicated Tests/ directory: `LocalFinanceManager.E2E/Tests/`
- One test class per feature area: `TransactionImportTests.cs`, `BasicAssignmentTests.cs`
- All tests run in single CI job with parallel execution groups

**Test Data Isolation:**

- Each test creates isolated accounts/budget plans via SeedDataHelper
- Per-test cleanup prevents test interdependencies
- Tests runnable standalone in any order

## Implementation Tasks

### 1. Prerequisites (Blocking)

- [ ] Create `tests/LocalFinanceManager.E2E/Tests/` directory for test files
- [ ] Enhance `TransactionListPage.cs` with missing methods:
  - [ ] `Task SelectFilterAsync(string filterType)` - Filter by "All", "Assigned", "Uncategorized"
  - [ ] `Task ClickAuditTrailAsync(Guid transactionId)` - Open audit trail modal
  - [ ] `Task SelectTransactionAsync(Guid transactionId)` - Select transaction checkbox for bulk operations
  - [ ] `Task SelectAllOnPageAsync()` - Select all visible transactions via header checkbox
  - [ ] `Task DeselectAllAsync()` - Clear all transaction selections
  - [ ] `Task ClickBulkAssignAsync()` - Open bulk assignment modal
- [ ] Create `ImportModalPageModel.cs` in `PageObjects/`:
  - [ ] `Task UploadFileAsync(string filePath)` - Upload CSV/JSON file
  - [ ] `Task<bool> IsPreviewVisibleAsync()` - Check if preview table shown
  - [ ] `Task<int> GetPreviewCountAsync()` - Get number of transactions in preview
  - [ ] `Task MapColumnAsync(string columnName, string targetField)` - Manual column mapping
  - [ ] `Task SelectDeduplicationModeAsync(string mode)` - Select "Exact", "Fuzzy", or "None"
  - [ ] `Task ClickImportAsync()` - Execute import
  - [ ] `Task<string> GetImportResultAsync()` - Get success/error message

### 2. Transaction Import Tests (8 tests)

- [ ] Create `TransactionImportTests.cs` test class in `LocalFinanceManager.E2E/Tests/`
- [ ] Use `SeedDataHelper` to create account and budget plan in test setup
- [ ] Test: Upload CSV file → Preview modal shows correct transaction count
  - Use `ImportModalPageModel.UploadFileAsync("test-transactions.csv")`
  - Assert `GetPreviewCountAsync()` returns expected count
  - Screenshot: `import-preview.png`
- [ ] Test: Column mapping automatically detected → Date, Amount, Description mapped correctly
  - Verify preview table shows mapped columns with sample data
  - Assert correct column headers displayed
- [ ] Test: Manual column mapping adjustment → Preview updates with adjusted mapping
  - Use `MapColumnAsync("Transaction Date", "Date")`
  - Assert preview refreshes with new mapping
- [ ] Test: Import with deduplication mode "Exact" → Duplicate transactions skipped
  - Upload CSV with duplicate ExternalId
  - Select "Exact" mode
  - Assert import result: "X new, Y duplicates skipped"
- [ ] Test: Import with deduplication mode "Fuzzy" → Similar transactions detected
  - Upload CSV with similar amount/date/description
  - Select "Fuzzy" mode
  - Assert fuzzy duplicates detected and skipped
- [ ] Test: Import with errors → Partial import with per-row error details shown
  - Upload CSV with invalid date formats in some rows
  - Assert partial success message
  - Assert error accordion shows row numbers and error messages
- [ ] Test: Import JSON format → Successful import with correct structure
  - Use `UploadFileAsync("test-transactions.json")`
  - Assert import succeeds with JSON structure
- [ ] Test: Navigate to transactions after import → Imported transactions visible in list
  - After successful import, navigate to Transactions page
  - Assert imported transactions displayed with correct data

### 3. Basic Assignment Tests (11 tests)

- [ ] Create `BasicAssignmentTests.cs` test class in `LocalFinanceManager.E2E/Tests/`
- [ ] Use `SeedDataHelper` to create account, categories, and transactions in test setup
- [ ] Test: Navigate to Transactions page → Verify unassigned transactions show warning badges
  - Use `TransactionsPageModel.NavigateToAsync()`
  - Assert warning badge visible for unassigned transactions
- [ ] Test: Click "Assign" button on unassigned transaction → Modal opens with transaction details
  - Use `TransactionsPageModel.ClickAssignButtonAsync(transactionId)`
  - Assert `AssignmentModalPageModel.IsVisibleAsync()` returns true
- [ ] Test: Select category from `CategorySelector` dropdown → Click "Assign" → Transaction assigned successfully
  - Use `AssignmentModalPageModel.SelectCategoryAsync(categoryId)`
  - Use `AssignmentModalPageModel.ClickAssignAsync()`
  - Assert transaction shows category badge (no warning)
- [ ] Test: Verify assigned transaction shows category badge (no warning)
  - Assert badge text matches category name
- [ ] Test: Open assignment modal for assigned transaction → Shows current category → Re-assign to different category
  - Verify current category pre-selected in dropdown
  - Select new category and save
  - Assert category badge updated
- [ ] Test: Attempt to assign transaction with category from different budget plan → Validation error shown (HTTP 400)
  - Seed second account with different budget plan
  - Attempt mismatched assignment
  - Assert error message displayed in modal
- [ ] Test: Click "Audit Trail" link → Audit modal opens showing assignment history
  - Use `TransactionsPageModel.ClickAuditTrailAsync(transactionId)`
  - Assert audit entries displayed with timestamps
- [ ] Test: Filter transactions by "Uncategorized" → Only unassigned transactions shown
  - Use `TransactionsPageModel.SelectFilterAsync("Uncategorized")`
  - Assert only unassigned transactions visible
- [ ] Test: Filter transactions by "Assigned" → Only assigned transactions shown
  - Use `TransactionsPageModel.SelectFilterAsync("Assigned")`
  - Assert only assigned transactions visible
- [ ] Test: Pagination works correctly (50 transactions per page)
  - Seed 150 transactions
  - Assert page 1 shows 50 transactions
  - Navigate to page 2, assert next 50 transactions visible
- [ ] Add screenshots for key UI states (modal open, validation error, success toast)

### 4. Multi-Account Validation Test (1 test)

- [ ] Create `MultiAccountValidationTests.cs` test class in `LocalFinanceManager.E2E/Tests/`
- [ ] Test: Multi-account workflow validates budget plan isolation (UserStory-4 enforcement):
  - **Setup:** Create 2 accounts with separate budget plans (Account A with Budget Plan 1, Account B with Budget Plan 2) via SeedDataHelper
  - **Categories:** Create categories for each budget plan (Food in Plan 1, Entertainment in Plan 2)
  - **Screenshot:** Capture `multi-account-setup.png` showing both accounts
  - **Import:** Import transactions for each account (25 transactions per account)
  - **Filtering:** Verify `CategorySelector` in assignment modal filters categories by account's budget plan
  - **Assertion:** When assigning transaction from Account A → Only categories from Budget Plan 1 visible in dropdown (Food, not Entertainment)
  - **Screenshot:** Capture `multi-account-category-filter.png`
  - **Validation:** Attempt to assign transaction from Account A with category from Budget Plan 2 → Validation error (HTTP 400)
  - **Assertion:** Error message: `"Category 'Entertainment' belongs to Budget Plan 2, but Account A uses Budget Plan 1"`
  - **Screenshot:** Capture `multi-account-validation-error.png`
  - **Cross-Verify:** Verify transaction list shows only relevant categories per account in filters
  - **Assertion:** Transaction list filter dropdown for Account A → Only Budget Plan 1 categories shown
  - **Audit:** Verify audit trail records validation errors correctly
  - **Assertion:** Audit trail contains entry: `Operation = ValidationFailed, Error = CrossAccountCategoryAssignment`

### 5. Test Documentation

- [ ] Create `E2E_TEST_GUIDE.md` in `LocalFinanceManager.E2E/` with:
  - **Test Execution:** Basic commands (`dotnet test`, `dotnet test --filter FullyQualifiedName~BasicAssignmentTests`)
  - **Playwright Setup:** Browser installation (`pwsh bin/Debug/net10.0/playwright.ps1 install`)
  - **Test Data Seeding:** Strategy using `SeedDataHelper` from US-5.1 (accounts, categories, transactions)
  - **Screenshot Capture:** Configuration inherited from US-5.1 (auto-capture on failure, manual capture with filenames)
  - **Debugging Tips:** Run headed mode (`Headless=false`), slow-mo (`SlowMo=100`), browser DevTools
  - **Per-Test Cleanup:** Document cleanup strategy (fresh database state for each test)
  - **CI Integration:** GitHub Actions workflow configuration with parallel execution groups
  - **Browser Configuration:** Chromium-only (rationale: fastest execution, sufficient for internal finance app)
  - **Test Naming Conventions:** `{Feature}Tests.cs` (e.g., `TransactionImportTests.cs`, `BasicAssignmentTests.cs`)
  - **Running Specific Tests:** Filter examples (`--filter "FullyQualifiedName~Import"`)
- [ ] Document Phase 1 test organization: 20 tests across 3 suites (8 + 11 + 1)
- [ ] Add note referencing Phase 2 (US-9.1) and Phase 3 (US-9.2) for complete test coverage

## Testing

### Phase 1 Test Organization

**Test Suites:**

- **TransactionImportTests.cs** (8 tests): CSV/JSON upload, preview, column mapping, deduplication modes, error handling
- **BasicAssignmentTests.cs** (11 tests): Modal interaction, category selection, validation, audit trail, filters, pagination
- **MultiAccountValidationTests.cs** (1 test): UserStory-4 budget plan isolation enforcement

**Total: 20 tests** establishing foundation for Phase 2 (US-9.1: 19 tests) and Phase 3 (US-9.2: 25 tests estimated)

### Test Scenarios

**1. Transaction Import (8 tests):**

- CSV upload → preview modal displays
- Automatic column detection
- Manual column mapping adjustment
- Deduplication modes (Exact, Fuzzy, None)
- Partial import with errors
- JSON format import
- Post-import transaction list verification
- ExternalId uniqueness validation

**2. Basic Assignment (11 tests):**

- Unassigned warning badges display
- Assignment modal opens with transaction details
- Category selection and assignment
- Re-assignment to different category
- Cross-budget-plan validation error (HTTP 400)
- Audit trail displays history
- Filter by Uncategorized/Assigned
- Pagination (50 per page, 150 total test)
- Screenshot capture for key states

**3. Multi-Account Validation (1 test):**

- 2 accounts with separate budget plans
- CategorySelector filters by budget plan
- Cross-account assignment blocked (HTTP 400)
- Validation error messages displayed
- Audit trail records validation failures

### Per-Test Cleanup Strategy

- Each test method creates fresh test data via SeedDataHelper
- Test database state reset after each test (no cross-test contamination)
- Tests runnable in any order without dependencies

## Success Criteria

- ✅ 20 tests implemented and passing (8 import + 11 basic + 1 multi-account)
- ✅ Tests/ directory created in LocalFinanceManager.E2E project
- ✅ TransactionListPage enhanced with 6 missing methods (filters, audit, bulk operations)
- ✅ ImportModalPageModel created for CSV/JSON upload workflow
- ✅ Tests validate transaction import with multiple formats (CSV, JSON)
- ✅ Tests validate basic assignment workflow (modal, categories, validation, audit trail)
- ✅ Multi-account test enforces UserStory-4 budget plan isolation rules
- ✅ Per-test cleanup strategy implemented (fresh database state for each test)
- ✅ PageObjectModels from US-5.1 used consistently for all UI interactions
- ✅ SeedDataHelper from US-5.1 used for test data setup
- ✅ Test execution time <5 minutes for Phase 1 suite
- ✅ Screenshots captured for key workflow stages
- ✅ `E2E_TEST_GUIDE.md` created with comprehensive documentation
- ✅ CI execution configured (Chromium-only, parallel execution)
- ✅ All tests pass locally and in CI environment (no flaky tests)
- ✅ Foundation ready for Phase 2 (US-9.1) implementation
- ✅ Code follows Implementation-Guidelines.md patterns (async/await, error handling, DI conventions)

## Definition of Done

- [ ] Tests/ directory created: `LocalFinanceManager.E2E/Tests/`
- [ ] TransactionListPage.cs enhanced with 6 methods: SelectFilterAsync, ClickAuditTrailAsync, SelectTransactionAsync, SelectAllOnPageAsync, DeselectAllAsync, ClickBulkAssignAsync
- [ ] ImportModalPageModel.cs created with upload, preview, mapping, import methods
- [ ] TransactionImportTests.cs created with 8 tests (CSV/JSON, preview, mapping, deduplication, errors)
- [ ] BasicAssignmentTests.cs created with 11 tests (modal, categories, validation, audit, filters, pagination)
- [ ] MultiAccountValidationTests.cs created with 1 test (UserStory-4 enforcement)
- [ ] All 20 tests passing locally (headless mode) and in CI (Chromium-only)
- [ ] Per-test cleanup verified (no database bloat, tests runnable in any order)
- [ ] E2E_TEST_GUIDE.md created with test execution, debugging, CI setup documentation
- [ ] Screenshots captured for import preview, assignment modal, validation errors, multi-account setup
- [ ] Test execution time <5 minutes verified in CI
- [ ] No manual migrations required (automatic via `Database.MigrateAsync()`)
- [ ] Code reviewed and merged to main branch
- [ ] Phase 2 (US-9.1) unblocked for implementation

## Dependencies

- **UserStory-5.1 (E2E Infrastructure):** REQUIRED - Must complete before starting US-9. Provides PageObjectModels, SeedDataHelper, screenshot configuration, E2ETestBase, TestWebApplicationFactory.

## Estimated Effort

**4 days** (~45 implementation tasks: 7 prerequisites + 8 import + 11 basic + 1 multi-account + 3 documentation + 15 PageObjectModel enhancements)

**Breakdown:**

- Prerequisites: 0.5 days (create Tests/ directory, enhance PageObjectModels)
- Transaction Import Tests: 1 day (8 tests with CSV/JSON handling)
- Basic Assignment Tests: 1.5 days (11 tests covering full assignment workflow)
- Multi-Account Validation: 0.5 days (1 comprehensive test)
- Documentation: 0.5 days (E2E_TEST_GUIDE.md, CI configuration)

## Implementation Status

> **Technical Debt Context:** US-5 and US-6 were archived in January 2026 with E2E tests marked "write immediately after feature implementation", but 29 tests were never completed. Additionally, 8 transaction import tests were deferred from MVP-3 as technical debt. This story consolidates Phase 1 foundation tests (20 total), with Phase 2 advanced tests (19) deferred to US-9.1 and Phase 3 ML tests (25 estimated) deferred to US-9.2 to maintain reasonable story size.

**Original Scope:**

- US-9 initially planned for 2 integration tests (0.5-1 day)
- Redistributed test strategy assumed features would implement their own E2E tests

**Current Reality:**

- US-5 archived: 11 basic assignment tests never implemented
- US-6 active: 18 split/bulk tests not yet implemented
- MVP-3 import tests: 8 tests deferred as technical debt
- Total missing: 39 critical tests (20 in US-9 + 19 in US-9.1)

**US-9 Scope (Phase 1):**

- 8 transaction import tests (addresses MVP-3 technical debt)
- 11 basic assignment tests (addresses US-5 technical debt)
- 1 multi-account validation test (UserStory-4 enforcement)
- **Total: 20 tests establishing foundation for Phase 2**

## Notes

- **Phase 2 Continuation:** UserStory-9.1 implements advanced assignment tests (split, bulk, integration workflows - 19 tests)
- **Phase 3 Deferred:** UserStory-9.2 implements ML tests pending US-7 completion (25 tests estimated)
- **Per-Test Cleanup:** Each test creates fresh database state; no test interdependencies
- **CI Parallel Execution:** Phase 1 tests run in parallel groups (import+basic, multi-account) for <5 minute execution
- **Screenshot Artifacts:** Invaluable for debugging CI failures; captured automatically on test failure
- **PageObjectModel Enhancements:** TransactionListPage additions unblock both Phase 1 and Phase 2 tests
- **Import Tests Foundation:** Transaction import tests enable all subsequent test phases (can't test assignment without transactions)
- **Chromium-Only Browser Testing:** Fastest execution, sufficient for internal finance app (no public-facing requirements)
- **Test-First Strategy:** Write Phase 1 tests first to validate import/assignment features before building advanced workflows
