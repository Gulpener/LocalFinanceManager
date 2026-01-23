# UserStory-9: Integration Workflow Tests

## Objective

Deliver comprehensive integration workflow tests that validate complete end-to-end user journeys spanning multiple features (US-5, US-6, US-7). Focus on cross-feature workflows (import → suggestions → bulk/split → auto-apply → monitoring) and multi-account validation to ensure seamless integration between assignment components.

> **Note:** Feature-specific E2E tests have been redistributed to their respective implementation stories (US-5, US-6, US-7, US-8) for immediate feedback during development. This story focuses exclusively on integration testing.

## Requirements

- Implement 2 comprehensive integration workflow tests validating complete user journeys
- Use PageObjectModels and seed helpers from US-5.1 for consistent test structure
- Test cross-feature workflows (import → suggestions → bulk/split → auto-apply → monitoring)
- Test multi-account validation scenarios (ensure proper budget plan isolation)
- Provide centralized E2E test documentation (`E2E_TEST_GUIDE.md`) referencing all distributed tests
- Capture screenshots for complete workflow stages with specific filenames
- Configure CI integration for all E2E tests across stories (single unified job, Chromium-only)

## Patterns for Integration Testing

**When to Write Integration Tests:**
- After multiple features interact (US-5, US-6, US-7 complete)
- When validating cross-feature data flow (import → assign → monitor)
- When testing system-level constraints (budget plan isolation, UserStory-4 enforcement)

**Integration Test Structure:**
- Reuse PageObjectModels from feature stories (consistent UI interactions)
- Use SeedDataHelper for multi-account test data (no inline creation)
- Test complete user journeys, not isolated actions
- Validate business rules across feature boundaries (e.g., category filtering respects budget plan)

**Test Organization:**
- Feature tests: `{Feature}Tests.cs` (written immediately with feature implementation)
- Integration tests: `IntegratedWorkflowTests.cs` (written after all features complete)
- All tests run in single CI job (`LocalFinanceManager.E2E/**/*Tests.cs`)

**Test Data Isolation:**
- Each test creates isolated accounts/budget plans via SeedDataHelper
- Tests clean up after execution (in-memory database or explicit teardown)
- Avoid test interdependencies (each test runnable standalone)

## Implementation Tasks

### 1. E2E Tests - Cross-Feature Integration Workflows

- [ ] Create `IntegratedWorkflowTests.cs` test class in `LocalFinanceManager.E2E/Tests/`
- [ ] Test: End-to-end import workflow (complete user journey spanning all features):
  - **Import:** Import 50 transactions from CSV → Preview shown → Import confirmed
  - **Screenshot:** Capture `workflow-import-complete.png`
  - **Unassigned State:** Navigate to Transactions page → 50 unassigned transactions shown with warning badges
  - **Screenshot:** Capture `workflow-unassigned-transactions.png`
  - **ML Suggestions:** ML suggestions displayed for transactions with high confidence (>80%)
  - **Screenshot:** Capture `workflow-ml-suggestions.png`
  - **Accept Suggestions:** Accept 10 suggestions → 10 transactions assigned via ML
  - **Assertion:** Verify exactly 10 transactions have `AssignmentType = ML`, remaining 40 unassigned
  - **Bulk Assignment:** Select and bulk assign 20 transactions → Progress bar shown → 20 transactions assigned
  - **Screenshot:** Capture `workflow-bulk-assign-progress.png`
  - **Assertion:** Verify bulk assignment count: 20 transactions with `AssignmentType = Bulk`
  - **Split Assignment:** Split 5 transactions across multiple categories → Sum validation works → 5 transactions split
  - **Screenshot:** Capture `workflow-split-assignment.png`
  - **Assertion:** Verify 5 transactions have splits totaling original amount (sum validation)
  - **Auto-Apply:** Enable auto-apply with 80% threshold → Trigger job manually → Remaining 15 transactions auto-assigned
  - **Assertion:** Verify auto-apply count: 15 transactions with `AssignmentType = AutoApplied`
  - **Monitoring:** Navigate to monitoring dashboard → Stats show 15 auto-applied, acceptance rate displayed
  - **Screenshot:** Capture `workflow-monitoring-dashboard.png`
  - **Assertion:** Verify dashboard stats: `TotalAutoApplied = 15`, `AcceptanceRate >= 80%`
  - **Audit Trail:** Open audit trail for each transaction type → Verify all operations recorded (manual, bulk, split, ML, auto-applied)
  - **Assertion:** Verify audit trail contains exactly 50 entries: `ML:10, Bulk:20, Split:5, AutoApplied:15` (operation type counts)
- [ ] Test: Multi-account workflow (validates budget plan isolation):
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
- [ ] Add Playwright screenshot capture on test failure (inherited from US-5.1 configuration)
- [ ] Verify all 11 implementation task checkboxes completed before marking story done

### 2. Test Documentation

- [ ] Create `E2E_TEST_GUIDE.md` in `LocalFinanceManager.E2E/` with:
  - **Test Execution:** Basic commands (`dotnet test`, `dotnet test --filter FullyQualifiedName~IntegratedWorkflowTests`)
  - **Playwright Setup:** Browser installation (`pwsh bin/Debug/net10.0/playwright.ps1 install`)
  - **Test Data Seeding:** Strategy using `SeedDataHelper` from US-5.1 (multi-account, transactions, labeled examples)
  - **Screenshot Capture:** Configuration inherited from US-5.1 (auto-capture on failure, manual capture with filenames)
  - **Debugging Tips:** Run headed mode (`Headless=false`), slow-mo (`SlowMo=100`), browser DevTools
  - **Test Organization:** Document distributed test structure (see Testing section below for complete breakdown)
  - **CI Integration:** Single unified E2E test job runs all `LocalFinanceManager.E2E/**/*Tests.cs`
  - **Browser Configuration:** Chromium-only (rationale: fastest execution, sufficient cross-browser coverage for internal finance app)
  - **Test Naming Conventions:** `{Feature}{Action}Tests.cs` (e.g., `BasicAssignmentTests.cs`, `IntegratedWorkflowTests.cs`)
  - **Running Specific Tests:** Filter examples (`--filter "FullyQualifiedName~MultiAccount"`)
- [ ] Configure test coverage reporting (Coverlet + ReportGenerator)
- [ ] Add coverage badge to README showing E2E test coverage (target: >90% for `TransactionsController`, `CategoriesController`)
- [ ] Generate HTML coverage report (exclude E2E project itself, cover main app)
- [ ] Target E2E coverage: >90% for assignment-related endpoints
- [ ] Document CI integration in README:
  - GitHub Actions workflow runs all E2E tests on every PR
  - Screenshot/video artifacts uploaded on failure
  - Test results summary published as PR comment

## Testing

### Test Distribution Across Stories

**Feature-Specific Tests (Redistributed to Implementation Stories):**
- **UserStory-5:** 11 tests (basic assignment modal, category selection, validation, audit trail, filters, pagination)
- **UserStory-6:** 18 tests (9 split + 9 bulk: sum validation, progress tracking, partial success, pagination persistence)
- **UserStory-7:** 25 tests (8 suggestions + 8 config + 9 monitoring: badges, accept/reject, settings, alerts, undo)
- **UserStory-8:** 7 tests (4 accessibility + 3 performance: axe-core, keyboard navigation, load time benchmarks)

**Integration Tests (This Story - US-9):**
- **UserStory-9:** 2 comprehensive workflow tests (complete user journey + multi-account validation)

**Total:** 63 E2E tests across all stories (validated count: 11+18+25+7+2=63) ✓

### Integration Test Scenarios

**1. Complete User Journey (Cross-Feature Integration):**
- **Setup:** Import 50 transactions across 3 accounts with 2 budget plans
- **Workflow Steps:**
  1. Import 50 transactions → Verify preview modal shows correct count
  2. ML suggestions display for high-confidence matches (>80%) → Verify suggestion badges visible
  3. Accept 10 ML suggestions → **Assert:** Exactly 10 transactions `AssignmentType = ML`
  4. Bulk assign 20 transactions → **Assert:** Bulk count = 20, `AssignmentType = Bulk`
  5. Split 5 transactions → **Assert:** Split sums equal original amounts (validation passes)
  6. Enable auto-apply (80% threshold) → Trigger job → **Assert:** Remaining 15 auto-assigned
  7. Monitor dashboard → **Assert:** Stats: `TotalAutoApplied = 15`, `AcceptanceRate >= 80%`
  8. Audit trail → **Assert:** 50 entries total: `ML:10, Bulk:20, Split:5, AutoApplied:15`
- **Validation:** No orphaned records, all assignments respect budget plan rules, seamless transitions between features
- **Expected Outcome:** All 50 transactions fully assigned with correct audit trail entries
- **Screenshots:** 6 workflow stage captures (`workflow-*.png`)

**2. Multi-Account Budget Plan Isolation:**
- **Setup:** Account A (Budget Plan 1 with "Food" category), Account B (Budget Plan 2 with "Entertainment" category)
- **Workflow Steps:**
  1. Import 25 transactions per account (50 total)
  2. Open assignment modal for Account A transaction
  3. **Assert:** Category dropdown shows only "Food" (Budget Plan 1), NOT "Entertainment" (Budget Plan 2)
  4. Attempt API call: Assign Account A transaction to "Entertainment" category → **Assert:** HTTP 400 error
  5. **Assert:** Error message: `"Category 'Entertainment' belongs to Budget Plan 2, but Account A uses Budget Plan 1"`
  6. Verify transaction list filters → **Assert:** Account A filter shows only Budget Plan 1 categories
  7. Check audit trail → **Assert:** Validation error logged: `Operation = ValidationFailed, Error = CrossAccountCategoryAssignment`
- **Validation:** UserStory-4 enforcement verified (Category.BudgetPlanId must match Account.CurrentBudgetPlanId)
- **Expected Outcome:** Cross-account category assignment blocked, clear validation messages, audit trail complete
- **Screenshots:** 3 captures (`multi-account-*.png`)

## Success Criteria

- ✅ 2 integration workflow tests implemented and passing (complete user journey + multi-account validation)
- ✅ Tests validate seamless integration across US-5, US-6, US-7 components (no feature isolation issues)
- ✅ Multi-account test enforces UserStory-4 budget plan isolation rules (cross-account assignment blocked)
- ✅ PageObjectModels from US-5.1 used consistently for all UI interactions (no direct DOM queries)
- ✅ SeedDataHelper from US-5.1 used for test data setup (no inline test data creation)
- ✅ Test execution time <2 minutes for both integration tests (performance threshold for CI)
- ✅ Screenshots captured for 9 workflow stages with specific filenames (`workflow-*.png`, `multi-account-*.png`)
- ✅ `E2E_TEST_GUIDE.md` comprehensive documentation (setup, execution, debugging, test distribution)
- ✅ CI integration configured (single job runs all 63 E2E tests, Chromium-only browser)
- ✅ Test distribution documented in README (61 feature tests + 2 integration tests = 63 total)
- ✅ Coverage reporting configured (>90% for `TransactionsController`, `CategoriesController`)
- ✅ All tests pass locally and in CI environment (no flaky tests)
- ✅ Code follows Implementation-Guidelines.md patterns (async/await, error handling, DI conventions)

## Definition of Done

- [ ] `IntegratedWorkflowTests.cs` created with 2 comprehensive workflow tests
- [ ] Complete user journey test validates seamless integration: import → ML suggestions → bulk/split → auto-apply → monitoring
- [ ] Multi-account test validates UserStory-4 enforcement: category filtering, cross-account validation errors
- [ ] PageObjectModels from US-5.1 used for all UI interactions (consistent test structure)
- [ ] SeedDataHelper from US-5.1 used for test data setup (multi-account, transactions, categories)
- [ ] Screenshots captured for 9 workflow stages with specific filenames (auto-capture on failure)
- [ ] `E2E_TEST_GUIDE.md` created with comprehensive documentation (setup, execution, debugging, browser config)
- [ ] Test coverage >90% for assignment-related endpoints (`TransactionsController`, `CategoriesController`)
- [ ] CI integration configured: GitHub Actions runs all 63 E2E tests, uploads screenshot artifacts on failure
- [ ] All tests pass locally (headless mode) and in CI environment (Chromium-only)
- [ ] Test execution time <2 minutes (performance threshold verified)
- [ ] No manual migrations required (automatic via `Database.MigrateAsync()`)
- [ ] Code reviewed and merged to main branch

## Dependencies

- **UserStory-5.1 (E2E Infrastructure):** REQUIRED - Must complete before starting US-9. Provides PageObjectModels, SeedDataHelper, screenshot/video configuration.
- **UserStory-5 (Basic Assignment UI):** REQUIRED - Integration workflows test basic assignment components (CategorySelector, TransactionAssignModal, audit trail).
- **UserStory-6 (Split/Bulk Assignment):** REQUIRED - Integration workflows test split/bulk components (SplitEditor, BulkAssignModal, bulk selection).
- **UserStory-7 (ML Suggestion Auto-Apply):** REQUIRED - Integration workflows test ML components (MLSuggestionBadge, AutoApplySettings, MonitoringDashboard).
- **UserStory-8 (UX Enhancements):** OPTIONAL - Can test keyboard shortcuts and filters in integration workflows if US-8 completed first.

## Estimated Effort

**0.5-1 day** (~11 implementation tasks: 2 workflow tests + 9 documentation tasks)

> **Note:** Effort reduced from original 3-4 days because 61 feature-specific E2E tests redistributed to US-5, US-6, US-7, US-8 for immediate feedback during implementation. This story focuses exclusively on 2 integration workflows validating cross-feature interactions.

## Implementation Status

> **Scope Change (Effective: January 16, 2026):** Feature-specific E2E tests redistributed to implementation stories for faster feedback loops. Original scope included 63 tests in single story; refined scope focuses on 2 integration workflows only.

**Feature Tests Redistributed:**
- 11 tests → UserStory-5 (write immediately after US-5 implementation)
- 18 tests → UserStory-6 (write immediately after US-6 implementation)
- 25 tests → UserStory-7 (write immediately after US-7 implementation)
- 7 tests → UserStory-8 (write immediately after US-8 implementation)

**Integration Tests (This Story):**
- 2 tests validating cross-feature workflows and system-level constraints

## Notes

- **Incremental Testing Strategy:** Feature tests provide fast feedback during development; integration tests validate complete system behavior after features complete
- **CI Execution:** Single unified E2E test job runs all 63 tests (`LocalFinanceManager.E2E/**/*Tests.cs`) regardless of distribution across stories
- **Screenshot Artifacts:** Invaluable for debugging CI failures; captured automatically on test failure with specific filenames for workflow stages
- **Test Maintenance:** Integration workflows require updates when new features added to user journey; keep workflows aligned with current feature set
- **Chromium-Only Browser Testing:** Provides fastest execution (<2 minutes) and sufficient cross-browser coverage for internal finance app (no public-facing requirements)
- **Performance Threshold:** <2 minutes execution time ensures fast CI feedback loop; avoid heavy test data (e.g., limit to 50 transactions per test)
- **Cross-Feature Workflow Tests:** Highest confidence tests simulating real user journeys; validate that features integrate seamlessly without isolation issues
- **Documentation Central:** `E2E_TEST_GUIDE.md` serves as single source of truth for all E2E tests across stories; update when test patterns change
