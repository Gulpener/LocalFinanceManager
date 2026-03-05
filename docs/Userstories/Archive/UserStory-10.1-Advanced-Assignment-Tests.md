# UserStory-10.1: E2E Tests - Phase 2 Advanced Assignment

## Objective

Implement advanced E2E tests (19 tests) covering split assignment, bulk assignment, and integration workflows. Builds on UserStory-10 foundation to achieve 80% critical path coverage (39 tests total across UserStory-10 + UserStory-10.1).

## Requirements

- Implement 19 tests across 3 test suites: SplitAssignmentTests (9), BulkAssignmentTests (9), IntegrationWorkflowTests (1)
- Leverage PageObjectModels enhanced in UserStory-10 (TransactionsPageModel with bulk methods)
- Use existing PageObjectModels: SplitEditorPageModel, BulkAssignModalPageModel (already complete from US-5.1)
- Per-test cleanup strategy (same pattern as UserStory-10)
- Combined Phase 1 + Phase 2 execution time target <10 minutes
- CI parallel execution groups (Phase 1 tests, Phase 2 tests)
- Integration workflow test validates cross-feature data flow (import → assign → split → bulk)

## Patterns for Advanced E2E Testing

**When to Write Advanced E2E Tests:**

- After foundational tests (UserStory-10) complete and passing
- When testing complex multi-step workflows (split editor, bulk progress)
- When validating cross-feature integration (import + assignment + validation)

**Advanced Test Characteristics:**

- Build on foundation tests (import, basic assignment)
- Test edge cases (sum validation, partial failures, pagination persistence)
- Validate complex UI interactions (dynamic row management, progress tracking)
- Verify business rule enforcement across features

**Test Data Strategy:**

- Reuse SeedDataHelper patterns from UserStory-10
- Create larger datasets for bulk operations (20+ transactions)
- Test boundary conditions (sum tolerance ±0.01, pagination thresholds)

## Implementation Tasks

### 1. Split Assignment Tests (9 tests)

- [x] Create `SplitAssignmentTests.cs` test class in `LocalFinanceManager.E2E/Tests/`
- [x] Use `SeedDataHelper` to create account with categories and single €100 transaction
- [x] Test: Click "Split" button on transaction → Split editor modal opens
  - Use `TransactionsPageModel.ClickSplitButtonAsync(transactionId)`
  - Assert `SplitEditorPageModel.IsVisibleAsync()` returns true
- [x] Test: Add 3 splits (€40 Food + €35 Transport + €25 Entertainment = €100) → Real-time sum shows green checkmark
  - Use `SplitEditorPageModel.AddSplitRowAsync()` twice (starts with 2 rows)
  - Set amounts: `SetSplitAmountAsync(0, 40m)`, `SetSplitAmountAsync(1, 35m)`, `SetSplitAmountAsync(2, 25m)`
  - Select categories for each split
  - Assert `GetSumValidationStatusAsync()` returns "valid"
- [x] Test: Enter splits with sum mismatch (€40 + €35 + €20 = €95 ≠ €100) → Red warning shown, "Save" button disabled
  - Set amounts totaling €95
  - Assert validation status "invalid"
  - Assert save button disabled
- [x] Test: Adjust last split to match sum (€40 + €35 + €25 = €100) → Green checkmark, "Save" button enabled
  - Adjust third split to €25
  - Assert validation status "valid"
  - Assert save button enabled
- [x] Test: Save valid split → Transaction shows "Split" badge with tooltip showing breakdown
  - Click save button
  - Assert transaction row shows "Split" badge
  - Hover over badge, assert tooltip displays breakdown
- [x] Test: Remove split row → Remaining splits recalculated → Sum validation updates
  - Use `SplitEditorPageModel.RemoveSplitRowAsync(1)`
  - Assert sum validation updates
- [x] Test: Attempt split with category from different budget plan → Validation error for that split row
  - Seed second budget plan
  - Select mismatched category in one split
  - Assert error shown for that specific row
- [x] Test: Re-split already split transaction → Existing splits replaced with new splits
  - Split transaction once
  - Re-open split editor
  - Create new split configuration
  - Assert old splits replaced
- [x] Test: Navigate to audit trail → Split operation recorded with all split details
  - Open audit trail modal
  - Assert split operation entry shows breakdown
- [x] Add screenshots for split editor states (valid sum, invalid sum, saved split)

### 2. Bulk Assignment Tests (9 tests)

- [x] Create `BulkAssignmentTests.cs` test class in `LocalFinanceManager.E2E/Tests/`
- [x] Use `SeedDataHelper` to create 20 unassigned transactions
- [x] Test: Select 5 transactions via checkboxes → Bulk toolbar appears showing "5 transactions selected"
  - Use `TransactionsPageModel.SelectTransactionAsync(transactionId)` for 5 transactions
  - Assert bulk toolbar visible with correct count
- [x] Test: Click "Bulk Assign" button → Bulk modal opens
  - Use `TransactionsPageModel.ClickBulkAssignAsync()`
  - Assert `BulkAssignModalPageModel.IsVisibleAsync()` returns true
- [x] Test: Select category → Click "Assign All" → Progress bar shows 0-100% → Success summary: "5 assigned, 0 failed"
  - Select category in modal
  - Click "Assign All"
  - Poll `BulkAssignModalPageModel.GetProgressPercentageAsync()` until 100%
  - Assert `GetSuccessCountAsync()` returns 5
  - Assert `GetFailureCountAsync()` returns 0
- [x] Test: All-fail scenario via service layer (foreign budget line) → FailedCount == 5, AssignedCount == 0
- [x] Test: Expand error accordion in bulk modal → Shows per-transaction error details using year-mismatch
  - Use prior-year transactions with current-year budget line to trigger validation failure
  - Use `BulkAssignModalPageModel.ExpandErrorDetailsAsync()`
  - Assert error messages displayed for failed transactions
- [x] Test: Deselect all transactions → Bulk toolbar disappears
  - Use `TransactionsPageModel.DeselectAllAsync()`
  - Assert bulk toolbar not visible
- [x] Test: Select all via header checkbox → All transactions on page selected
  - Use `TransactionsPageModel.SelectAllOnPageAsync()`
  - Assert all visible transaction checkboxes checked
- [x] Test: Verify bulk-assigned transactions show category badge (no warning)
  - After bulk assignment, assert category badges visible
- [x] Test: Pagination preserves selections (select 3 on page 1, navigate to page 2, select 2 more → 5 total selected)
  - Select 3 transactions on page 1
  - Navigate to page 2
  - Select 2 transactions
  - Assert bulk toolbar shows "5 selected"
- [x] Add screenshots for bulk modal (progress bar, partial success, error details)

### 3. Integration Workflow Test (1 test)

- [x] Create `IntegrationWorkflowTests.cs` test class in `LocalFinanceManager.E2E/Tests/`
- [x] Test: Complete workflow validates cross-feature integration:
  - **Setup:** Create account with budget plan and 35 transactions via SeedDataHelper
  - **Basic Assignment:** Assign 10 transactions individually using assignment modal
  - **Assertion:** 10 transactions have category badges, audit trail shows 10 manual assignments
  - **Screenshot:** Capture `workflow-basic-assigned.png`
  - **Bulk Assignment:** Select and bulk assign 20 transactions
  - **Assertion:** Progress bar completes, 20 transactions assigned, audit trail shows bulk operation
  - **Screenshot:** Capture `workflow-bulk-assigned.png`
  - **Split Assignment:** Split 5 transactions across multiple categories
  - **Assertion:** 5 transactions show "Split" badge, audit trail records split details
  - **Screenshot:** Capture `workflow-split-assigned.png`
  - **Verify Totals:** 35 transactions assigned (10 manual + 20 bulk + 5 split), 0 unassigned remaining
  - **Assertion:** Transaction list filters work correctly (all 35 assigned, 0 unassigned)
  - **Audit Trail Verification:** Open audit trail for each assignment type
  - **Assertion:** Audit trail contains correct operation types: Manual (10), Bulk (20), Split (5)

### 4. CI Configuration

- [x] Update GitHub Actions workflow with sequential execution phases:
  - Phase 1: Basic Assignment + UX tests
  - Phase 2: Split + Bulk Assignment (UserStory-10.1)
  - Phase 3: Integration Workflow + other suites
- [x] Configure CI timeout: `timeout-minutes: 10` per phase (3 phases; effective upper bound ~30 min wall-clock, expected <15 min)
- [x] Verify screenshot/video artifact upload on failure
- [x] E2E_TEST_GUIDE.md updated with Phase 2 test organization and per-phase timeout info

## Testing

### Phase 2 Test Organization

**Test Suites:**

- **SplitAssignmentTests.cs** (9 tests): Split editor, sum validation, row management, validation errors, audit trail
- **BulkAssignmentTests.cs** (9 tests): Bulk selection, progress tracking, partial failures, error accordion, pagination persistence
- **IntegrationWorkflowTests.cs** (1 test): Cross-feature workflow (import → basic → bulk → split)

**Total: 19 tests** completing Phase 2 (combined with UserStory-10: 39 tests achieving 80% critical path coverage)

### Test Scenarios

**1. Split Assignment (9 tests):**

- Split editor modal opens
- Real-time sum validation (green checkmark for valid, red warning for invalid)
- Dynamic row management (add/remove splits)
- Save button state (disabled when invalid, enabled when valid)
- Split badge display with tooltip breakdown
- Cross-budget-plan validation errors
- Re-split existing transactions
- Audit trail records split operations

**2. Bulk Assignment (9 tests):**

- Transaction checkbox selection
- Bulk toolbar display with selection count
- Progress bar 0-100% tracking
- Partial success handling (some succeed, some fail)
- Error accordion with per-transaction details
- Deselect all functionality
- Select all on page via header checkbox
- Pagination preserves selections across pages

**3. Integration Workflow (1 test):**

- Cross-feature validation: import (UserStory-10) → basic (UserStory-10) → bulk → split
- Audit trail comprehensive validation
- Transaction list filter accuracy (assigned vs unassigned counts)
- Badge display consistency across assignment types

### Per-Test Cleanup Strategy

- Same pattern as UserStory-10: fresh database state for each test
- No test interdependencies (tests runnable in any order)
- Isolated test data via SeedDataHelper

## Success Criteria

- ✅ 19 tests implemented and passing (9 split + 9 bulk + 1 integration)
- ✅ Combined with UserStory-10: 39 tests total achieving 80% critical path coverage
- ✅ Split editor tests validate sum validation (±0.01 tolerance)
- ✅ Bulk assignment tests validate progress tracking and partial failures
- ✅ Integration workflow test validates cross-feature data flow
- ✅ SplitEditorPage and BulkOperationPage PageObjectModels used consistently
- ✅ Per-test cleanup verified (no database bloat)
- ✅ Test execution time <10 minutes for combined Phase 1 + Phase 2
- ✅ CI parallel execution configured (3 groups: import+basic, split+bulk, integration+validation)
- ✅ Screenshots captured for split states, bulk progress, integration stages
- ✅ All tests pass locally and in CI environment (no flaky tests)
- ✅ Audit trail validation comprehensive across all assignment types
- ✅ Code follows Implementation-Guidelines.md patterns (async/await, error handling)

## Definition of Done

- [x] SplitAssignmentTests.cs created with 9 tests (editor, sum validation, row management, audit)
- [x] BulkAssignmentTests.cs created with 9 tests (selection, progress, all-fail service-level, error accordion)
- [x] IntegrationWorkflowTests.cs created with 1 test (basic → bulk → split workflow; all 35 assigned)
- [x] All 19 tests passing locally (headless mode) and in CI (Chromium-only)
- [x] Combined UserStory-10 + UserStory-10.1: 39 tests passing with 80% critical path coverage
- [x] CI sequential phases configured (3 phases, `timeout-minutes: 10` each)
- [x] Per-test cleanup verified (tests runnable in any order without failures)
- [x] Screenshots captured for split editor states, bulk progress, integration workflow stages
- [x] Test execution time <10 minutes verified in CI
- [x] E2E_TEST_GUIDE.md updated with Phase 2 test organization and per-phase timeout info
- [x] No manual migrations required (automatic via `Database.MigrateAsync()`)
- [x] Code reviewed and merged to main branch
- [x] Phase 3 (UserStory-10.2) ready to be refined once US-7 ML features implemented

## Dependencies

- **UserStory-10 REQUIRED (blocking):** Must complete Phase 1 before starting Phase 2. UserStory-10 provides:
  - Tests/ directory structure
  - Enhanced TransactionsPageModel with bulk operation methods
  - ImportModalPageModel for transaction creation
  - E2E_TEST_GUIDE.md foundation
  - 20 passing foundation tests
- **UserStory-5.1 (E2E Infrastructure):** REQUIRED - Provides SplitEditorPage, BulkOperationPage, SeedDataHelper, E2ETestBase

## Estimated Effort

**4 days** (~30 implementation tasks: 9 split + 9 bulk + 1 integration + 4 CI configuration + 7 screenshots/documentation)

**Breakdown:**

- Split Assignment Tests: 1.5 days (9 tests with sum validation complexity)
- Bulk Assignment Tests: 1.5 days (9 tests with progress tracking and error handling)
- Integration Workflow Test: 0.5 days (1 comprehensive cross-feature test)
- CI Configuration: 0.25 days (parallel execution groups, timeout, artifacts)
- Documentation Updates: 0.25 days (E2E_TEST_GUIDE.md updates)

## Implementation Status

> **Phase 2 Context:** This story implements advanced assignment tests (19) building on UserStory-10 Phase 1 foundation (20 tests). Combined coverage: 39 tests achieving 80% critical path coverage. Phase 3 ML tests (25 estimated) deferred to UserStory-10.2 pending US-7 completion.

**Prerequisites from UserStory-10:**

- ✅ Tests/ directory created
- ✅ TransactionsPageModel enhanced with bulk methods (SelectTransactionAsync, DeselectAllAsync, etc.)
- ✅ ImportModalPageModel available for transaction import
- ✅ E2E_TEST_GUIDE.md foundation established

**UserStory-10.1 Adds:**

- 9 split assignment tests (sum validation, row management)
- 9 bulk assignment tests (progress tracking, partial failures)
- 1 integration workflow test (cross-feature validation)
- CI parallel execution configuration

## Pattern Adherence from UserStory-10

**Reuses UserStory-10 Patterns:**

- Per-test cleanup strategy (fresh database state)
- SeedDataHelper for test data creation
- PageObjectModel-only UI interactions
- Screenshot capture for key workflow stages
- Chromium-only browser testing

**Extends UserStory-10 Patterns:**

- Advanced PageObjectModel usage (SplitEditorPage, BulkOperationPage)
- Progress polling patterns for async operations
- Error accordion expansion for partial failures
- Pagination state preservation across page navigation

## Notes

- **Phase 1 Dependency:** UserStory-10 must complete first; import/basic tests validate foundation for advanced tests
- **Phase 3 Continuation:** UserStory-10.2 implements ML tests (25 estimated) after US-7 ML features complete
- **CI Parallel Execution:** 3 groups reduce total execution time (<10 minutes vs sequential ~15 minutes)
- **Integration Test Value:** Validates that import, basic, bulk, and split features work seamlessly together
- **Screenshot Artifacts:** Critical for debugging split sum validation failures and bulk progress issues
- **Sum Validation Testing:** ±0.01 tolerance tested explicitly (€100.00 vs €99.99 valid, €100.00 vs €99.98 invalid)
- **Partial Failure Patterns:** Bulk tests validate graceful handling of mixed success/failure scenarios
- **Pagination Complexity:** Preserving selections across pages requires careful state management testing
- **Audit Trail Completeness:** Integration test verifies all assignment types (manual, bulk, split) recorded correctly
