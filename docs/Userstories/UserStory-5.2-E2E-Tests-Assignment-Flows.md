# UserStory-5.2: E2E Tests for Assignment Flows

## Objective

Deliver comprehensive end-to-end test coverage for all transaction assignment workflows (manual single assignment, split assignment, bulk assignment, ML suggestions, auto-apply) using NUnit + Playwright with PageObjectModels and seed helpers from US-5.1. Ensure 100% coverage of critical user journeys with accessibility validation and performance testing.

## Requirements

- Create comprehensive E2E test suite covering all assignment scenarios from US-5, US-6, and US-7
- Use PageObjectModels and seed helpers from US-5.1 for consistent test structure
- Implement cross-feature workflow tests (import → assign → monitor)
- Conduct WCAG 2.1 AA accessibility audit with axe-core
- Add performance tests for transaction list loading (1000+ transactions)
- Provide detailed E2E test documentation (`E2E_TEST_GUIDE.md`)
- Configure coverage reporting targeting >90% for assignment endpoints

## Pattern Adherence from US-5, US-6, US-7

This story **tests** all artifacts from previous stories:

### Components Tested

- `CategorySelector.razor` from US-5
- `TransactionAssignModal.razor` from US-5
- `AssignmentAuditTrail.razor` from US-5
- `SplitEditor.razor` from US-6
- `BulkAssignModal.razor` from US-6
- `MLSuggestionBadge.razor` from US-7
- `AutoApplySettings.razor` from US-7
- `MonitoringDashboard.razor` from US-7
- Updated `Transactions.razor` from all stories

### Services Tested

- `ITransactionAssignmentService` from US-5 (extended in US-6)
- `IAutoApplyService` from US-7
- `AutoApplyBackgroundJob` from US-7

### Test Organization (Same Structure)

- E2E tests in `LocalFinanceManager.E2E/Tests/` using NUnit + Playwright
- Follow AAA pattern (Arrange, Act, Assert)
- Use PageObjectModels from US-5.1 for UI interactions
- Use SeedDataHelper from US-5.1 for test data setup
- Screenshot capture on failure (configured in US-5.1)

## Implementation Tasks

### 1. E2E Tests - Basic Assignment (US-5)

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

### 2. E2E Tests - Split Assignment (US-6)

- [ ] Create `SplitAssignmentTests.cs` test class in `LocalFinanceManager.E2E/Tests/`
- [ ] Use `SeedDataHelper` to create account with categories and single €100 transaction
- [ ] Test: Click "Split" button on transaction → Split editor modal opens
  - Use `TransactionsPageModel.ClickSplitButtonAsync(transactionId)`
  - Assert `SplitEditorPageModel.IsVisibleAsync()` returns true
- [ ] Test: Add 3 splits (€40 Food + €35 Transport + €25 Entertainment = €100) → Real-time sum shows green checkmark
  - Use `SplitEditorPageModel.AddSplitRowAsync()` twice (starts with 2 rows)
  - Set amounts: `SetSplitAmountAsync(0, 40m)`, `SetSplitAmountAsync(1, 35m)`, `SetSplitAmountAsync(2, 25m)`
  - Select categories for each split
  - Assert `GetSumValidationStatusAsync()` returns "valid"
- [ ] Test: Enter splits with sum mismatch (€40 + €35 + €20 = €95 ≠ €100) → Red warning shown, "Save" button disabled
  - Set amounts totaling €95
  - Assert validation status "invalid"
  - Assert save button disabled
- [ ] Test: Adjust last split to match sum (€40 + €35 + €25 = €100) → Green checkmark, "Save" button enabled
  - Adjust third split to €25
  - Assert validation status "valid"
  - Assert save button enabled
- [ ] Test: Save valid split → Transaction shows "Split" badge with tooltip showing breakdown
  - Click save button
  - Assert transaction row shows "Split" badge
  - Hover over badge, assert tooltip displays breakdown
- [ ] Test: Remove split row → Remaining splits recalculated → Sum validation updates
  - Use `SplitEditorPageModel.RemoveSplitRowAsync(1)`
  - Assert sum validation updates
- [ ] Test: Attempt split with category from different budget plan → Validation error for that split row
  - Seed second budget plan
  - Select mismatched category in one split
  - Assert error shown for that specific row
- [ ] Test: Re-split already split transaction → Existing splits replaced with new splits
  - Split transaction once
  - Re-open split editor
  - Create new split configuration
  - Assert old splits replaced
- [ ] Test: Navigate to audit trail → Split operation recorded with all split details
  - Open audit trail modal
  - Assert split operation entry shows breakdown
- [ ] Add screenshots for split editor states (valid sum, invalid sum, saved split)

### 3. E2E Tests - Bulk Assignment (US-6)

- [ ] Create `BulkAssignmentTests.cs` test class in `LocalFinanceManager.E2E/Tests/`
- [ ] Use `SeedDataHelper` to create 20 unassigned transactions
- [ ] Test: Select 5 transactions via checkboxes → Bulk toolbar appears showing "5 transactions selected"
  - Use `TransactionsPageModel.SelectTransactionAsync(transactionId)` for 5 transactions
  - Assert bulk toolbar visible with correct count
- [ ] Test: Click "Bulk Assign" button → Bulk modal opens
  - Use `TransactionsPageModel.ClickBulkAssignAsync()`
  - Assert `BulkAssignModalPageModel.IsVisibleAsync()` returns true
- [ ] Test: Select category → Click "Assign All" → Progress bar shows 0-100% → Success summary: "5 assigned, 0 failed"
  - Select category in modal
  - Click "Assign All"
  - Poll `BulkAssignModalPageModel.GetProgressPercentageAsync()` until 100%
  - Assert `GetSuccessCountAsync()` returns 5
  - Assert `GetFailureCountAsync()` returns 0
- [ ] Test: Select 10 transactions (5 valid, 5 with mismatched budget plan) → Bulk assign → Partial success: "5 assigned, 5 failed"
  - Seed 5 transactions from different account/budget plan
  - Select all 10 transactions
  - Bulk assign to category from first account
  - Assert partial success (5 succeeded, 5 failed)
- [ ] Test: Expand error accordion in bulk modal → Shows per-transaction error details
  - Use `BulkAssignModalPageModel.ExpandErrorDetailsAsync()`
  - Assert error messages displayed for failed transactions
- [ ] Test: Deselect all transactions → Bulk toolbar disappears
  - Use `TransactionsPageModel.DeselectAllAsync()`
  - Assert bulk toolbar not visible
- [ ] Test: Select all via header checkbox → All transactions on page selected
  - Use `TransactionsPageModel.SelectAllOnPageAsync()`
  - Assert all visible transaction checkboxes checked
- [ ] Test: Verify bulk-assigned transactions show category badge (no warning)
  - After bulk assignment, assert category badges visible
- [ ] Test: Pagination preserves selections (select 3 on page 1, navigate to page 2, select 2 more → 5 total selected)
  - Select 3 transactions on page 1
  - Navigate to page 2
  - Select 2 transactions
  - Assert bulk toolbar shows "5 selected"
- [ ] Add screenshots for bulk modal (progress bar, partial success, error details)

### 4. E2E Tests - ML Suggestions (US-7)

- [ ] Create `MLSuggestionTests.cs` test class in `LocalFinanceManager.E2E/Tests/`
- [ ] Use `SeedDataHelper.SeedMLDataAsync()` to create 100+ LabeledExamples (simulate trained model)
- [ ] Test: Navigate to Transactions page → Unassigned transactions with suggestions show `MLSuggestionBadge`
  - Seed transactions with patterns matching labeled examples
  - Assert suggestion badges visible on unassigned transactions
- [ ] Test: Hover over suggestion badge → Tooltip shows feature importance (top 3 features)
  - Hover over badge
  - Assert tooltip displays feature weights
- [ ] Test: Click "Accept" button on suggestion → Transaction assigned to suggested category → Badge disappears
  - Click accept button on suggestion badge
  - Assert transaction assigned to suggested category
  - Assert suggestion badge no longer visible
- [ ] Test: Click "Reject" button on suggestion → Feedback recorded → Badge remains (transaction still unassigned)
  - Click reject button
  - Assert feedback recorded (check via API or audit trail)
  - Assert transaction still unassigned
- [ ] Test: Filter transactions by "Has Suggestion" → Only transactions with ML suggestions shown
  - Use `TransactionsPageModel.SelectFilterAsync("Has Suggestion")`
  - Assert only transactions with suggestion badges visible
- [ ] Test: Sort transactions by suggestion confidence (highest first) → Order correct
  - Sort by confidence
  - Assert transactions ordered by confidence score descending
- [ ] Test: Verify suggestion badge color coding (>80% green, 60-80% yellow, <60% gray)
  - Seed transactions with varying confidence scores
  - Assert badge colors match confidence thresholds
- [ ] Test: Navigate to ML model info page → Shows active model details (version, accuracy, last trained)
  - Navigate to ML settings/info page
  - Assert model metadata displayed
- [ ] Add screenshots for suggestion badge states (high confidence, medium confidence, tooltip)

### 5. E2E Tests - Auto-Apply Configuration (US-7)

- [ ] Create `AutoApplyTests.cs` test class in `LocalFinanceManager.E2E/Tests/`
- [ ] Test: Navigate to "Settings > Auto-Apply" page → Settings page loads
  - Navigate to auto-apply settings
  - Assert settings page renders
- [ ] Test: Toggle "Enable Auto-Apply" switch → Setting saved
  - Toggle switch to enabled
  - Reload page, assert switch remains enabled
- [ ] Test: Adjust confidence threshold slider (60% → 85%) → Preview stats update: "Based on last 100 transactions, X would auto-apply"
  - Drag slider to 85%
  - Assert preview statistics update
- [ ] Test: Select specific accounts for auto-apply → Only selected accounts processed
  - Seed multiple accounts
  - Select subset for auto-apply
  - Assert settings saved correctly
- [ ] Test: Add excluded categories → Transactions for those categories skipped
  - Add category to exclusion list
  - Assert setting saved
- [ ] Test: Save settings with invalid confidence (e.g., 110%) → Validation error shown
  - Attempt to set confidence >100%
  - Assert validation error displayed
- [ ] Test: Trigger auto-apply job manually (if API endpoint available) → Transactions auto-assigned
  - Trigger job via UI button or API
  - Wait for job completion
  - Assert transactions auto-assigned
- [ ] Test: Verify auto-applied transactions show "Auto-applied" indicator in audit trail
  - Open audit trail for auto-applied transaction
  - Assert "Auto-applied" source displayed
- [ ] Add screenshots for settings page (toggle on, slider adjusted, validation error)

### 6. E2E Tests - Monitoring Dashboard (US-7)

- [ ] Create `MonitoringDashboardTests.cs` test class in `LocalFinanceManager.E2E/Tests/`
- [ ] Use `SeedDataHelper.SeedAutoApplyHistoryAsync()` to create 100 auto-applied transactions (8 undone → 8% undo rate)
- [ ] Test: Navigate to "Settings > Auto-Apply Monitoring" page → Dashboard loads with stats
  - Navigate to monitoring dashboard
  - Assert page renders with metrics
- [ ] Test: Verify metrics cards show correct values (Total: 100, Undo Rate: 8%)
  - Assert total count card shows 100
  - Assert undo rate card shows 8%
- [ ] Test: Undo rate <10% → No alert shown → Green status indicator
  - Assert no alert banner visible
  - Assert status indicator green
- [ ] Seed additional undo (12 undone → 12% undo rate > 10% threshold)
  - Use seed helper to add more undone transactions
- [ ] Test: Undo rate >10% → Alert banner shown: "⚠️ Undo rate exceeds threshold (12% > 10%)"
  - Reload dashboard
  - Assert alert banner visible with warning message
- [ ] Test: Click "Undo Auto-Apply" button on transaction row → Confirmation dialog → Undo successful
  - Click undo button on transaction
  - Confirm in dialog
  - Assert transaction reverted to unassigned
- [ ] Test: Verify "Check if Can Undo" validation (button disabled for manually assigned transactions)
  - Seed manually assigned transaction
  - Assert undo button disabled or not visible
- [ ] Test: Auto-refresh works (metrics update every 30 seconds without page reload)
  - Seed new auto-applied transaction
  - Wait 30 seconds
  - Assert metrics updated without page reload
- [ ] Test: Auto-apply history table shows last 50 transactions with status (accepted/undone)
  - Assert history table displays last 50 entries
  - Assert status column shows "Accepted" or "Undone"
- [ ] Add screenshots for dashboard (normal stats, alert shown, undo confirmation)

### 7. E2E Tests - Cross-Feature Workflows

- [ ] Create `IntegratedWorkflowTests.cs` test class in `LocalFinanceManager.E2E/Tests/`
- [ ] Test: End-to-end import workflow:
  - Import 50 transactions from CSV → Preview shown → Import confirmed
  - Navigate to Transactions page → 50 unassigned transactions shown with warnings
  - ML suggestions displayed for transactions with high confidence
  - Accept 10 suggestions → 10 transactions assigned
  - Bulk assign 20 transactions → 20 transactions assigned
  - Split 5 transactions → 5 transactions split
  - Enable auto-apply with 80% threshold → Job runs → Remaining 15 transactions auto-assigned
  - Navigate to monitoring dashboard → Stats show 15 auto-applied
- [ ] Test: Multi-account workflow:
  - Create 2 accounts with separate budget plans
  - Import transactions for each account
  - Verify `CategorySelector` filters categories by account's budget plan
  - Attempt to assign transaction from Account A with category from Account B's budget plan → Validation error
- [ ] Add screenshots for complete workflow stages

### 8. E2E Tests - Accessibility Validation

- [ ] Install `Deque.AxeCore.Playwright` NuGet package in `LocalFinanceManager.E2E`
- [ ] Create `AccessibilityTests.cs` test class in `LocalFinanceManager.E2E/Tests/`
- [ ] Test: Run axe-core on Transactions page → Zero critical violations
  - Navigate to transactions page
  - Run axe-core scan
  - Assert no critical accessibility violations
- [ ] Test: Run axe-core on assignment modal → Zero critical violations
  - Open assignment modal
  - Run axe-core scan
  - Assert no critical violations
- [ ] Test: Tab through assignment modal → Focus order logical
  - Open assignment modal
  - Press Tab repeatedly
  - Assert focus moves in logical order (category → budget line → note → save → cancel)
- [ ] Test: Keyboard navigation works (Enter submits, Esc closes)
  - Open modal, press Enter (assert submits if valid)
  - Open modal, press Esc (assert closes without saving)
- [ ] Document accessibility features in README (keyboard shortcuts, screen reader support)

### 9. E2E Performance Tests

- [ ] Create `PerformanceTests.cs` test class in `LocalFinanceManager.E2E/Tests/`
- [ ] Test: Load transaction list with 1000 transactions → Measures load time <500ms
  - Use `SeedDataHelper.SeedTransactionsAsync()` to create 1000 transactions
  - Start stopwatch
  - Navigate to transactions page
  - Stop stopwatch
  - Assert elapsed time <500ms
- [ ] Test: Scroll through pages → Each page loads <200ms
  - Seed 500 transactions (10 pages)
  - Measure time to navigate to page 2
  - Assert page load time <200ms
- [ ] Test: Apply filter to 1000 transactions → Filter applies <300ms
  - Load 1000 transactions
  - Start stopwatch
  - Apply "Uncategorized" filter
  - Stop stopwatch
  - Assert elapsed time <300ms

### 10. Test Documentation

- [ ] Create `E2E_TEST_GUIDE.md` in `LocalFinanceManager.E2E/` with:
  - Test execution instructions (`dotnet test`)
  - Playwright browser setup (install browsers: `pwsh bin/Debug/net10.0/playwright.ps1 install`)
  - Test data seeding strategy (using `SeedDataHelper` from US-5.1)
  - Screenshot/video capture configuration (inherited from US-5.1)
  - Debugging tips (run headed mode: `Headless=false`, slow-mo: `SlowMo=100`)
  - How to run specific test classes or methods
  - CI integration notes (headless mode, parallel execution)
- [ ] Configure test coverage reporting (Coverlet + ReportGenerator)
- [ ] Add coverage badge to README showing E2E test coverage
- [ ] Generate HTML coverage report (exclude E2E project itself, cover main app)
- [ ] Target E2E coverage: >90% for assignment-related endpoints (`TransactionsController`, `CategoriesController`)
- [ ] Add CI integration documentation (run E2E tests in GitHub Actions)

## Testing

### E2E Test Scenarios Summary

1. **Basic Assignment (US-5):** 11 test cases covering assignment modal, category selection, validation errors, audit trail, filters, pagination
2. **Split Assignment (US-6):** 9 test cases covering split editor, sum validation, split badge, re-splitting, validation errors
3. **Bulk Assignment (US-6):** 9 test cases covering bulk selection, bulk modal, progress bar, partial success, error details, pagination persistence
4. **ML Suggestions (US-7):** 8 test cases covering suggestion badges, accept/reject, tooltips, filters, sorting, color coding
5. **Auto-Apply Configuration (US-7):** 8 test cases covering settings page, toggle, threshold slider, account selection, validation, auto-apply execution
6. **Monitoring Dashboard (US-7):** 9 test cases covering metrics, alerts, undo functionality, auto-refresh, history table
7. **Cross-Feature Workflows:** 2 comprehensive end-to-end workflows (import → assign → monitor, multi-account validation)
8. **Accessibility:** 4 test cases covering axe-core audit, keyboard focus, tab order, keyboard navigation
9. **Performance:** 3 test cases measuring load time, pagination performance, filter performance

**Total:** ~63 E2E test cases covering all assignment features

## Success Criteria

- ✅ E2E test coverage >90% for all assignment flows (manual, split, bulk, ML, auto-apply)
- ✅ All 63 E2E test cases implemented and passing
- ✅ PageObjectModels from US-5.1 used consistently across all tests
- ✅ SeedDataHelper from US-5.1 used for test data setup (no inline creation)
- ✅ WCAG 2.1 AA compliance verified (axe-core audit shows zero critical violations)
- ✅ Performance tests validate transaction list loads <500ms with 1000+ transactions
- ✅ Cross-feature workflow tests cover complete user journeys
- ✅ `E2E_TEST_GUIDE.md` documentation comprehensive (setup, execution, debugging)
- ✅ Coverage reporting configured (>90% coverage for assignment endpoints)
- ✅ All tests run successfully in CI with screenshot capture on failure

## Definition of Done

- Comprehensive E2E test suite implemented in `LocalFinanceManager.E2E/Tests/` using NUnit + Playwright
- 63+ E2E test cases covering all assignment features (manual, split, bulk, ML, auto-apply)
- PageObjectModels from US-5.1 used for all UI interactions
- SeedDataHelper from US-5.1 used for all test data setup
- Accessibility tests implemented with axe-core (zero critical violations)
- Performance tests validate load times (<500ms for 1000+ transactions)
- Cross-feature workflow tests cover import → assign → monitor flows
- Test documentation in `E2E_TEST_GUIDE.md` with setup, execution, debugging instructions
- Coverage reporting configured (>90% coverage for `TransactionsController` and `CategoriesController`)
- CI integration ready (tests run in GitHub Actions with screenshot/video artifacts)
- Code follows Implementation-Guidelines.md patterns
- All tests pass locally and in CI environment

## Dependencies

- **UserStory-5.1 (E2E Infrastructure):** REQUIRED - Must complete before starting US-5.2. Provides PageObjectModels, SeedDataHelper, screenshot/video configuration.
- **UserStory-5 (Basic Assignment UI):** REQUIRED - Tests require CategorySelector, TransactionAssignModal, assignment UI components.
- **UserStory-6 (Split/Bulk Assignment):** REQUIRED - Tests require SplitEditor, BulkAssignModal, bulk selection UI.
- **UserStory-7 (ML Suggestion Auto-Apply):** REQUIRED - Tests require MLSuggestionBadge, AutoApplySettings, MonitoringDashboard components.

## Estimated Effort

**2-3 days** (~42 implementation tasks)

## Notes

- This story can be implemented **incrementally**: Write US-5 tests after US-5 completes, US-6 tests after US-6 completes, etc.
- Screenshot/video capture (configured in US-5.1) invaluable for debugging CI test failures.
- Accessibility tests (axe-core) should be run on every new UI component to prevent regressions.
- Performance tests establish baseline metrics: Re-run periodically to detect performance degradation.
- Cross-feature workflow tests provide highest confidence: They simulate real user journeys.
- E2E tests are critical for regression prevention: Run full suite before each release.
- Playwright supports Chromium, Firefox, WebKit: Consider running tests on all browsers in CI.
