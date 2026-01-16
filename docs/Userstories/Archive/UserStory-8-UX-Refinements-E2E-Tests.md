# UserStory-8: Assignment UX Refinements & E2E Testing

> ⚠️ **ARCHIVED:** This story was split into three focused sub-stories on January 16, 2026:
>
> - **UserStory-5.1:** E2E Test Infrastructure Setup (~25 tasks, 1-2 days)
> - **UserStory-5.2:** E2E Tests for Assignment Flows (~42 tasks, 2-3 days)
> - **UserStory-5.3:** Assignment UX Enhancements (~24 tasks, 2-3 days)
>
> **Reason for Split:** Original story contained 86 tasks spanning infrastructure setup, comprehensive E2E testing, and UX enhancements—too large for a single sprint (estimated 10-15 days vs. target 2-4 days per story). Split enables:
>
> - **US-5.1** to run in parallel with US-5 (infrastructure ready before assignment UI completes)
> - **US-5.2** to be implemented incrementally after US-5/6/7 (test each feature as it's built)
> - **US-5.3** to be prioritized independently (UX enhancements can be deferred if needed)
>
> See [UserStory-Refinement-Recommendations.md](../UserStory-Refinement-Recommendations.md) for detailed split rationale.

## Objective

Deliver comprehensive end-to-end test coverage for all transaction assignment flows (manual, split, bulk, ML suggestions, auto-apply) using NUnit + Playwright, and implement UX enhancements (keyboard shortcuts, quick filters, recent category suggestions) to improve assignment efficiency and accessibility.

## Requirements

- Create comprehensive E2E test suite covering all assignment scenarios from US-5, US-6, and US-7
- Implement keyboard navigation shortcuts (Tab, Enter, Esc) for assignment workflows
- Add quick filter dropdowns (uncategorized, auto-applied, date ranges, category)
- Display recent/favorite category shortcuts for frequent assignments
- Conduct WCAG 2.1 AA accessibility audit with axe-core
- Optimize transaction list performance for 1000+ transactions (pagination)
- Provide detailed E2E test documentation and coverage reporting

## Pattern Adherence from US-5, US-6, US-7

This story **tests and refines** all artifacts from previous stories:

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

- E2E tests in `LocalFinanceManager.E2E/` using NUnit + Playwright
- Follow AAA pattern (Arrange, Act, Assert)
- Use `WebApplicationFactory` with dedicated SQLite test database
- Seed realistic test data (accounts, budget plans, categories, transactions)

## Implementation Tasks

### 1. E2E Test Infrastructure Setup

- [ ] Verify Playwright installation in `LocalFinanceManager.E2E` project
- [ ] Add `Microsoft.AspNetCore.Mvc.Testing` package for `WebApplicationFactory`
- [ ] Create `TestWebApplicationFactory` class extending `WebApplicationFactory<Program>`
- [ ] Configure test SQLite database (separate from dev database: `test_localfinancemanager.db`)
- [ ] Create `E2ETestBase` base class with setup/teardown methods
- [ ] Implement database seeding helper: `SeedTestDataAsync()` method
- [ ] Add Playwright browser context helpers (Chromium, Firefox, WebKit)
- [ ] Configure test logging and screenshot capture on failure
- [ ] Add `.runsettings` file for parallel test execution (max 4 parallel)

### 2. E2E Tests - Basic Assignment (US-5)

- [ ] Create `BasicAssignmentTests.cs` test class in `LocalFinanceManager.E2E/Tests/`
- [ ] Test: Navigate to Transactions page → Verify unassigned transactions show warning badges
- [ ] Test: Click "Assign" button on unassigned transaction → Modal opens with transaction details
- [ ] Test: Select category from `CategorySelector` dropdown → Click "Assign" → Transaction assigned successfully
- [ ] Test: Verify assigned transaction shows category badge (no warning)
- [ ] Test: Open assignment modal for assigned transaction → Shows current category → Re-assign to different category
- [ ] Test: Attempt to assign transaction with category from different budget plan → Validation error shown (HTTP 400)
- [ ] Test: Click "Audit Trail" link → Audit modal opens showing assignment history
- [ ] Test: Filter transactions by "Uncategorized" → Only unassigned transactions shown
- [ ] Test: Filter transactions by "Assigned" → Only assigned transactions shown
- [ ] Test: Pagination works correctly (50 transactions per page)
- [ ] Add screenshots for key UI states (modal open, validation error, success toast)

### 3. E2E Tests - Split Assignment (US-6)

- [ ] Create `SplitAssignmentTests.cs` test class in `LocalFinanceManager.E2E/Tests/`
- [ ] Test: Click "Split" button on transaction → Split editor modal opens
- [ ] Test: Add 3 splits (€40 Food + €35 Transport + €25 Entertainment = €100) → Real-time sum shows green checkmark
- [ ] Test: Enter splits with sum mismatch (€40 + €35 + €20 = €95 ≠ €100) → Red warning shown, "Save" button disabled
- [ ] Test: Adjust last split to match sum (€40 + €35 + €25 = €100) → Green checkmark, "Save" button enabled
- [ ] Test: Save valid split → Transaction shows "Split" badge with tooltip showing breakdown
- [ ] Test: Remove split row → Remaining splits recalculated → Sum validation updates
- [ ] Test: Attempt split with category from different budget plan → Validation error for that split row
- [ ] Test: Re-split already split transaction → Existing splits replaced with new splits
- [ ] Test: Navigate to audit trail → Split operation recorded with all split details
- [ ] Add screenshots for split editor states (valid sum, invalid sum, saved split)

### 4. E2E Tests - Bulk Assignment (US-6)

- [ ] Create `BulkAssignmentTests.cs` test class in `LocalFinanceManager.E2E/Tests/`
- [ ] Test: Select 5 transactions via checkboxes → Bulk toolbar appears showing "5 transactions selected"
- [ ] Test: Click "Bulk Assign" button → Bulk modal opens
- [ ] Test: Select category → Click "Assign All" → Progress bar shows 0-100% → Success summary: "5 assigned, 0 failed"
- [ ] Test: Select 10 transactions (5 valid, 5 with mismatched budget plan) → Bulk assign → Partial success: "5 assigned, 5 failed"
- [ ] Test: Expand error accordion in bulk modal → Shows per-transaction error details
- [ ] Test: Deselect all transactions → Bulk toolbar disappears
- [ ] Test: Select all via header checkbox → All transactions on page selected
- [ ] Test: Verify bulk-assigned transactions show category badge (no warning)
- [ ] Test: Pagination preserves selections (select 3 on page 1, navigate to page 2, select 2 more → 5 total selected)
- [ ] Add screenshots for bulk modal (progress bar, partial success, error details)

### 5. E2E Tests - ML Suggestions (US-7)

- [ ] Create `MLSuggestionTests.cs` test class in `LocalFinanceManager.E2E/Tests/`
- [ ] Seed 100+ LabeledExamples for ML model training (simulate trained model)
- [ ] Test: Navigate to Transactions page → Unassigned transactions with suggestions show `MLSuggestionBadge`
- [ ] Test: Hover over suggestion badge → Tooltip shows feature importance (top 3 features)
- [ ] Test: Click "Accept" button on suggestion → Transaction assigned to suggested category → Badge disappears
- [ ] Test: Click "Reject" button on suggestion → Feedback recorded → Badge remains (transaction still unassigned)
- [ ] Test: Filter transactions by "Has Suggestion" → Only transactions with ML suggestions shown
- [ ] Test: Sort transactions by suggestion confidence (highest first) → Order correct
- [ ] Test: Verify suggestion badge color coding (>80% green, 60-80% yellow, <60% gray)
- [ ] Test: Navigate to ML model info page → Shows active model details (version, accuracy, last trained)
- [ ] Add screenshots for suggestion badge states (high confidence, medium confidence, tooltip)

### 6. E2E Tests - Auto-Apply Configuration (US-7)

- [ ] Create `AutoApplyTests.cs` test class in `LocalFinanceManager.E2E/Tests/`
- [ ] Test: Navigate to "Settings > Auto-Apply" page → Settings page loads
- [ ] Test: Toggle "Enable Auto-Apply" switch → Setting saved
- [ ] Test: Adjust confidence threshold slider (60% → 85%) → Preview stats update: "Based on last 100 transactions, X would auto-apply"
- [ ] Test: Select specific accounts for auto-apply → Only selected accounts processed
- [ ] Test: Add excluded categories → Transactions for those categories skipped
- [ ] Test: Save settings with invalid confidence (e.g., 110%) → Validation error shown
- [ ] Test: Trigger auto-apply job manually (if API endpoint available) → Transactions auto-assigned
- [ ] Test: Verify auto-applied transactions show "Auto-applied" indicator in audit trail
- [ ] Add screenshots for settings page (toggle on, slider adjusted, validation error)

### 7. E2E Tests - Monitoring Dashboard (US-7)

- [ ] Create `MonitoringDashboardTests.cs` test class in `LocalFinanceManager.E2E/Tests/`
- [ ] Seed 100 auto-applied transactions (8 undone → 8% undo rate)
- [ ] Test: Navigate to "Settings > Auto-Apply Monitoring" page → Dashboard loads with stats
- [ ] Test: Verify metrics cards show correct values (Total: 100, Undo Rate: 8%)
- [ ] Test: Undo rate <10% → No alert shown → Green status indicator
- [ ] Seed additional undo (12 undone → 12% undo rate > 10% threshold)
- [ ] Test: Undo rate >10% → Alert banner shown: "⚠️ Undo rate exceeds threshold (12% > 10%)"
- [ ] Test: Click "Undo Auto-Apply" button on transaction row → Confirmation dialog → Undo successful
- [ ] Test: Verify "Check if Can Undo" validation (button disabled for manually assigned transactions)
- [ ] Test: Auto-refresh works (metrics update every 30 seconds without page reload)
- [ ] Test: Auto-apply history table shows last 50 transactions with status (accepted/undone)
- [ ] Add screenshots for dashboard (normal stats, alert shown, undo confirmation)

### 8. E2E Tests - Cross-Feature Workflows

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

### 9. UX Refinement - Keyboard Shortcuts

- [ ] Add keyboard shortcut documentation to UI (e.g., "?" key shows help modal)
- [ ] Implement keyboard shortcuts:
  - `Tab` - Navigate between form fields in modals
  - `Enter` - Submit assignment/split/bulk modal (when "Save" button focused)
  - `Esc` - Close modal without saving
  - `Space` - Toggle transaction checkbox selection
  - `Ctrl+A` / `Cmd+A` - Select all visible transactions
  - `Ctrl+D` / `Cmd+D` - Deselect all transactions
  - `/` - Focus search/filter input
- [ ] Add E2E tests for keyboard navigation:
  - Test: Press `Tab` in assignment modal → Focus moves through fields (category dropdown → budget line → save button)
  - Test: Press `Enter` when save button focused → Modal submits
  - Test: Press `Esc` in modal → Modal closes without saving
  - Test: Press `Space` on transaction row → Checkbox toggles
- [ ] Ensure keyboard shortcuts don't conflict with browser defaults

### 10. UX Refinement - Quick Filters

- [ ] Add quick filter dropdown bar above transaction list (collapsible)
- [ ] Implement filter options:
  - **Assignment Status:** All / Assigned / Unassigned / Split / Auto-Applied
  - **Suggestion Status:** All / Has Suggestion / No Suggestion
  - **Date Range:** Last 7 days / Last 30 days / Last 90 days / Custom range (date picker)
  - **Category:** Multi-select dropdown (show only assigned categories)
  - **Amount Range:** Min/Max amount inputs
  - **Account:** Dropdown (if multiple accounts exist)
- [ ] Add "Clear All Filters" button
- [ ] Persist filter state in browser localStorage (restore on page reload)
- [ ] Show active filter count badge: "3 filters active"
- [ ] Add E2E tests for filters:
  - Test: Apply "Unassigned" filter → Only unassigned transactions shown
  - Test: Apply date range filter → Transactions within range shown
  - Test: Apply multiple filters (unassigned + last 30 days) → Combined filter works
  - Test: Clear all filters → All transactions shown

### 11. UX Refinement - Recent Category Shortcuts

- [ ] Track recently used categories per user (store in browser localStorage or database)
- [ ] Display "Recent Categories" section in assignment modal (top 5 most recent)
- [ ] Show category name with usage count (e.g., "Food - Used 23 times")
- [ ] One-click assignment: Click recent category → Assign immediately without opening full selector
- [ ] Implement "Favorite Categories" feature (star icon to favorite)
- [ ] Show favorites at top of `CategorySelector` dropdown
- [ ] Add E2E tests:
  - Test: Assign transaction to "Food" category → Next assignment shows "Food" in recent categories
  - Test: Click recent category "Food" → Transaction assigned immediately
  - Test: Favorite "Food" category → Shows at top of category selector

### 12. UX Refinement - Performance Optimization

- [ ] Implement virtual scrolling OR pagination for transaction list (1000+ transactions)
- [ ] Use pagination with page size 50 (default) and page size selector (25/50/100/200)
- [ ] Add lazy loading for ML suggestions (fetch only for visible page)
- [ ] Optimize category selector loading (cache categories per account)
- [ ] Add loading skeletons for transaction list (better perceived performance)
- [ ] Add debouncing to search/filter inputs (300ms delay)
- [ ] Profile page load time: Target <500ms for 1000+ transactions (with pagination)
- [ ] Add E2E performance tests:
  - Test: Load transaction list with 1000 transactions → Measures load time <500ms
  - Test: Scroll through pages → Each page loads <200ms
  - Test: Apply filter to 1000 transactions → Filter applies <300ms

### 13. Accessibility Audit & Fixes

- [ ] Install axe-core accessibility testing library in E2E project
- [ ] Run axe-core audit on all transaction assignment pages/components
- [ ] Fix critical accessibility issues:
  - Add ARIA labels to all buttons/links (e.g., `aria-label="Assign transaction"`)
  - Ensure keyboard focus visible (outline on focused elements)
  - Add `role` attributes to modals (`role="dialog"`, `aria-modal="true"`)
  - Ensure color contrast ratio ≥4.5:1 (WCAG AA standard)
  - Add `alt` text to icons (or use `aria-hidden="true"` for decorative icons)
  - Ensure form inputs have associated labels (`<label for="...">`)
- [ ] Add screen reader testing:
  - Test with NVDA (Windows) or VoiceOver (Mac)
  - Verify modal announcements (e.g., "Assignment modal opened")
  - Verify success/error toast notifications announced
- [ ] Add E2E accessibility tests:
  - Test: Run axe-core on Transactions page → Zero critical violations
  - Test: Run axe-core on assignment modal → Zero critical violations
  - Test: Tab through assignment modal → Focus order logical
- [ ] Document accessibility features in README (keyboard shortcuts, screen reader support)

### 14. Test Documentation & Coverage Reporting

- [ ] Create `E2E_TEST_GUIDE.md` in `LocalFinanceManager.E2E/` with:
  - Test execution instructions (`dotnet test`)
  - Playwright browser setup (install browsers: `pwsh bin/Debug/net10.0/playwright.ps1 install`)
  - Test data seeding strategy
  - Screenshot/video capture configuration
  - Debugging tips (run headed mode, slow-mo)
- [ ] Configure test coverage reporting (Coverlet + ReportGenerator)
- [ ] Add coverage badge to README showing E2E test coverage
- [ ] Generate HTML coverage report (exclude E2E project itself, cover main app)
- [ ] Target E2E coverage: >90% for assignment-related endpoints/pages
- [ ] Add CI integration documentation (run E2E tests in GitHub Actions)

## Testing

### E2E Test Scenarios Summary

1. **Basic Assignment (US-5):**

   - 10 test cases covering assignment modal, category selection, validation errors, audit trail, filters, pagination

2. **Split Assignment (US-6):**

   - 9 test cases covering split editor, sum validation, split badge, re-splitting, validation errors

3. **Bulk Assignment (US-6):**

   - 9 test cases covering bulk selection, bulk modal, progress bar, partial success, error details, pagination persistence

4. **ML Suggestions (US-7):**

   - 8 test cases covering suggestion badges, accept/reject, tooltips, filters, sorting, color coding

5. **Auto-Apply Configuration (US-7):**

   - 8 test cases covering settings page, toggle, threshold slider, account selection, validation, auto-apply execution

6. **Monitoring Dashboard (US-7):**

   - 9 test cases covering metrics, alerts, undo functionality, auto-refresh, history table

7. **Cross-Feature Workflows:**

   - 2 comprehensive end-to-end workflows (import → assign → monitor, multi-account validation)

8. **Keyboard Shortcuts:**

   - 5 test cases covering Tab, Enter, Esc, Space, Ctrl+A navigation

9. **Quick Filters:**

   - 4 test cases covering filter options, combined filters, filter persistence, clear filters

10. **Recent Categories:**

    - 3 test cases covering recent category tracking, one-click assignment, favorites

11. **Performance:**

    - 3 test cases measuring load time, pagination performance, filter performance

12. **Accessibility:**
    - 4 test cases covering axe-core audit, keyboard focus, tab order, ARIA labels

**Total:** ~74 E2E test cases covering all assignment features

## Success Criteria

- ✅ E2E test coverage >90% for all assignment flows (manual, split, bulk, ML, auto-apply)
- ✅ All keyboard shortcuts functional (Tab, Enter, Esc, Space, Ctrl+A) with E2E tests
- ✅ Quick filters implemented with 6+ filter options (status, date, category, amount, account)
- ✅ Recent category shortcuts display top 5 most used categories with one-click assignment
- ✅ WCAG 2.1 AA compliance verified (axe-core audit shows zero critical violations)
- ✅ Transaction list loads <500ms with 1000+ transactions (via pagination)
- ✅ Pagination performs <200ms per page navigation
- ✅ E2E tests run in CI with screenshot capture on failure
- ✅ Test documentation comprehensive (setup, execution, debugging)
- ✅ All tests from US-11.1, US-11.2, US-11.3 integrated into E2E suite

## Definition of Done

- Comprehensive E2E test suite implemented in `LocalFinanceManager.E2E/` using NUnit + Playwright
- 74+ E2E test cases covering all assignment features (manual, split, bulk, ML, auto-apply)
- Keyboard shortcuts implemented (Tab, Enter, Esc, Space, Ctrl+A, /) with accessibility support
- Quick filter dropdown with 6+ filter options (assignment status, suggestions, date, category, amount, account)
- Recent category shortcuts with top 5 most used categories and one-click assignment
- Favorite categories feature (star icon, show at top of selector)
- Performance optimization: Pagination with <500ms load time for 1000+ transactions
- WCAG 2.1 AA accessibility compliance (axe-core audit passes, keyboard navigation functional, screen reader tested)
- Test documentation in `E2E_TEST_GUIDE.md` with setup, execution, debugging instructions
- Coverage reporting configured (>90% coverage for assignment endpoints)
- CI integration ready (tests run in GitHub Actions with screenshot/video artifacts)
- Code follows Implementation-Guidelines.md and patterns from US-11.1, US-11.2, US-11.3

## Dependencies

- **UserStory-5 (Basic Assignment UI):** ⚠️ **MUST complete before starting US-8.** Tests all artifacts from US-5.
- **UserStory-6 (Split/Bulk Assignment):** ⚠️ **MUST complete before starting US-8.** Tests all artifacts from US-6.
- **UserStory-7 (ML Suggestion Auto-Apply):** ⚠️ **MUST complete before starting US-8.** Tests all artifacts from US-7.
- **NUnit + Playwright Setup:** E2E test project already exists in `tests/LocalFinanceManager.E2E/`

## Estimated Effort

**2-3 days** (~20-25 implementation tasks)

## Notes

- E2E tests are critical for regression prevention: Run full suite before each release.
- Playwright supports Chromium, Firefox, WebKit: Run tests on all browsers to ensure cross-browser compatibility.
- Screenshot/video capture on failure invaluable for debugging CI test failures.
- Accessibility audit (axe-core) should be integrated into CI to prevent regressions.
- Keyboard shortcuts improve power user efficiency: Document prominently in UI (help modal triggered by `?` key).
- Recent category shortcuts reduce friction for frequent assignments: Track usage per user in localStorage or database.
- Performance optimization (pagination) essential for production use: Many users have 1000+ transactions per account.
- Filter persistence (localStorage) improves UX: Users don't lose filter state on page reload.
