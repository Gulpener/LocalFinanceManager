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
- Capture screenshots for complete workflow stages
- Configure CI integration for all E2E tests across stories (single unified job)

## Test Distribution Strategy

Feature-specific E2E tests have been **redistributed** to implementation stories for immediate feedback:

### Tests Now in UserStory-5 (11 tests)

- Basic assignment modal, category selection, validation errors
- Audit trail, filters, pagination
- **Write these tests immediately after implementing US-5 components**

### Tests Now in UserStory-6 (18 tests)

- Split assignment: 9 tests (split editor, sum validation, split badge)
- Bulk assignment: 9 tests (bulk modal, progress tracking, partial success)
- **Write these tests immediately after implementing US-6 components**

### Tests Now in UserStory-7 (25 tests)

- ML suggestions: 8 tests (suggestion badges, accept/reject, confidence scoring)
- Auto-apply config: 8 tests (settings page, threshold slider, preview stats)
- Monitoring dashboard: 9 tests (metrics, alerts, undo functionality)
- **Write these tests immediately after implementing US-7 components**

### Tests Now in UserStory-5.3 (7 tests)

- Accessibility: 4 tests (axe-core audit, keyboard navigation, focus order)
- Performance: 3 tests (1000+ transaction load time, pagination, filter performance)
- **Write these tests immediately after implementing US-5.3 components**

### Remaining in UserStory-9 (2 tests - THIS STORY)

- **Integration workflows:** Cross-feature end-to-end user journeys
- **Multi-account validation:** Budget plan isolation across accounts

## Pattern Adherence from US-5, US-6, US-7

This story **validates integration** of all components from previous stories:
Cross-Feature Integration Workflows

### Components Tested (Integration Only)

All components from US-5, US-6, US-7, US-8 tested together in complete workflows

### Test Organization (Same Structure)

- Integration workflow tests in `LocalFinanceManager.E2E/Tests/IntegratedWorkflowTests.cs`
- Follow AAA pattern (Arrange, Act, Assert)
- Use PageObjectModels from US-5.1 for UI interactions
- Use SeedDataHelper from US-5.1 for test data setup
- Screenshot capture on failure (configured in US-5.1)

## Implementation Tasks

### 1. E2E Tests - Cross-Feature Integration Workflows

- [ ] Create `IntegratedWorkflowTests.cs` test class in `LocalFinanceManager.E2E/Tests/`
- [ ] Test: End-to-end import workflow (complete user journey spanning all features):
  - **Import:** Import 50 transactions from CSV → Preview shown → Import confirmed
  - **Unassigned State:** Navigate to Transactions page → 50 unassigned transactions shown with warning badges
  - **ML Suggestions:** ML suggestions displayed for transactions with high confidence (>80%)
  - **Accept Suggestions:** Accept 10 suggestions → 10 transactions assigned via ML
  - **Bulk Assignment:** Select and bulk assign 20 transactions → Progress bar shown → 20 transactions assigned
  - **Split Assignment:** Split 5 transactions across multiple categories → Sum validation works → 5 transactions split
  - **Auto-Apply:** Enable auto-apply with 80% threshold → Trigger job manually → Remaining 15 transactions auto-assigned
  - **Monitoring:** Navigate to monitoring dashboard → Stats show 15 auto-applied, acceptance rate displayed
  - **Audit Trail:** Open audit trail for each transaction type → Verify all operations recorded (manual, bulk, split, ML, auto-applied)
- [ ] Test: Multi-account workflow (validates budget plan isolation):
  - **Setup:** Create 2 accounts with separate budget plans (Account A with Budget Plan 1, Account B with Budget Plan 2)
  - **Categories:** Create categories for each budget plan (Food in Plan 1, Entertainment in Plan 2)
  - **Import:** Import transactions for each account
  - **Filtering:** Verify `CategorySelector` in assignment modal filters categories by account's budget plan
  - **Validation:** Attempt to assign transaction from Account A with category from Budget Plan 2 → Validation error (HTTP 400)
  - **Cross-Verify:** Verify transaction list shows only relevant categories per account in filters
  - **Audit:** Verify audit trail records validation errors correctly
- [ ] Add screenshots for complete workflow stages (import, suggestions, bulk, split, auto-apply, monitoring)

### 2. Test Documentation

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

### E2E Test Distribution Summary

**Feature-Specific Tests (Redistributed to Implementation Stories):**

1. **UserStory-5:** 11 tests (basic assignment modal, category selection, validation, audit trail, filters, pagination)
2. **UserStory-6:** 18 tests (9 split + 9 bulk: sum validation, progress tracking, partial success, pagination persistence)
3. **UserStory-7:** 25 tests (8 suggestions + 8 config + 9 monitoring: badges, accept/reject, settings, alerts, undo)
4. **UserStory-8:** 7 tests (4 accessibility + 3 performance: axe-core, keyboard navigation, load time benchmarks)

**Integration Tests (This Story - US-9):**

5. **Cross-Feature Workflows:** 2 comprehensive integration workflows:
   - **Complete User Journey:** Import → ML suggestions → bulk/split assignment → auto-apply → monitoring (validates seamless integration across US-5, US-6, US-7)
   - **Multi-Account Validation:** Budget plan isolation, category filtering, cross-account validation errors (validates UserStory-4 enforcement)

**Total:** 63 E2E tests (61 feature-specific + 2 integration workflows)
2 integration workflow tests implemented and passing (complete user journey + multi-account validation)

- ✅ Cross-feature workflow validates seamless integration across US-5, US-6, US-7 (import → suggestions → bulk/split → auto-apply → monitoring)
- ✅ Multi-account workflow validates UserStory-4 budget plan isolation (category filtering, validation errors)
- ✅ PageObjectModels from US-5.1 used consistently for UI interactions
- ✅ SeedDataHelper from US-5.1 used for test data setup (multi-account, transactions, labeled examples)
- ✅ Screenshots captured for all workflow stages (import, suggestions, bulk, split, auto-apply, monitoring)
- ✅ `E2E_TEST_GUIDE.md` comprehensive documentation (setup, execution, debugging, test distribution across stories)
- ✅ CI integration configured (single unified job runs all 63 E2E tests across US-5, US-6, US-7, US-8, US-9)
- ✅ Test distribution documented in README (61 feature tests + 2 integration tests = 63 total)
- ✅ All integration tests pass locally and in CI environment
  - Validates UserStory-4 enforcement (Category.BudgetPlanId must match Account.CurrentBudgetPlanId)
  - Ensures CategorySelector properly filters categories by account
  - Verifies validation errors display correctly for cross-account assignment attempts

## Success Criteria

- ✅ E2E test coverage >90% for all assignment flows (manual, split, bulk, ML, auto-apply)
- ✅ All 63 E2E test cases implemented and passing
- ✅ PageObjectModels from US-5.1 used consistently across all tests
- ✅ SeedDataHelper from US-5.1 used for test data setup (no inline creation)
- ✅ WCAG 2.1 AA compliance verified (axe-core audit shows zero critical violations)
- ✅ Performance tests validate transaction list loads <500ms with 1000+ transactions
  - **Test Execution:** Basic commands (`dotnet test`, `dotnet test --filter FullyQualifiedName~IntegratedWorkflowTests`)
  - **Playwright Setup:** Browser installation (`pwsh bin/Debug/net10.0/playwright.ps1 install`)
  - **Test Data Seeding:** Strategy using `SeedDataHelper` from US-5.1
  - **Screenshot/Video Capture:** Configuration inherited from US-5.1 (auto-capture on failure)
  - **Debugging Tips:** Run headed mode (`Headless=false`), slow-mo (`SlowMo=100`), browser DevTools
  - **Test Organization:** Document distributed test structure:
    - Feature-specific tests in US-5, US-6, US-7, US-8 (write immediately after implementation)
    - Integration workflow tests in US-9 (run after all features complete)
  - **CI Integration:** Single unified E2E test job runs all `LocalFinanceManager.E2E/**/*Tests.cs`
  - **Parallel Execution:** Configure max parallel workers (default: CPU cores / 2)
  - **Test Naming Conventions:** `{Feature}{Action}Tests.cs` (e.g., `BasicAssignmentTests.cs`, `IntegratedWorkflowTests.cs`)
- [ ] Document E2E test distribution in README:
  - US-5: 11 tests (basic assignment)
  - US-6: 18 tests (split + bulk)
  - US-7: 25 tests (ML + auto-apply)
  - US-8: 7 tests (accessibility + performance)
  - US-9: 2 tests (integration workflows)
  - **Total:** 63 E2E tests across all stories
- [ ] Add CI integration documentation:
  - Configure GitHub Actions workflow to run all E2E tests
  - Upload screenshot/video artifacts on failure
  - Generate and publish test results summarys)
- Performance tests validate load times (<500ms for 1000+ transactions)
- Cross-feature workflow tests cover import → assign → monitor flows
- Test documentation in `E2E_TEST_GUIDE.md` with setup, execution, debugging instructions
- Coverage reporting configured (>90% coverage for `TransactionsController` and `CategoriesController`)
- CI integration ready (tests run in GitHub Actions with screenshot/video artifacts)
- Code follows Implementation-Guidelines.md patterns
- All tests pass locally and in CI environment

## Dependencies

- **UserStory-5.1 (E2E Infrastructure):** REQUIRED - Must complete before starting US-9. Provides PageObjectModels, SeedDataHelper, screenshot/video configuration.
- **UserStory-5 (Basic Assignment UI):** REQUIRED - Integration workflows test basic assignment components (CategorySelector, TransactionAssignModal, audit trail).
- **UserStory-6 (Split/Bulk Assignment):** REQUIRED - Integration workflows test split/bulk components (SplitEditor, BulkAssignModal, bulk selection).
- **UserStory-7 (ML Suggestion Auto-Apply):** REQUIRED - Integration workflows test ML components (MLSuggestionBadge, AutoApplySettings, MonitoringDashboard).
- **UserStory-8 (UX Enhancements):** OPTIONAL - Can test keyboard shortcuts and filters in integration workflows if US-8 completed first.

## Estimated Effort

0.5-1 day\*\* (~2 implementation tasks: 2 integration workflow tests + documentation)

> **Note:** Effort reduced from original 2-3 days because feature-specific tests (61 tests) redistributed to US-5, US-6, US-7, US-8 for immediate feedback during implementation.
> **2-3 days** (~42 implementation tasks)
> **Test Distribution Strategy:** Feature-specific E2E tests redistributed to implementation stories (US-5, US-6, US-7, US-8) for immediate feedback. This story focuses on integration testing only.

- **Incremental Testing Benefits:** Writing E2E tests immediately after implementing features provides faster feedback, easier debugging, and prevents regression introduction.
- **Integration Tests Purpose:** These 2 workflow tests validate seamless integration between features. They're the highest confidence tests simulating real user journeys.
- **CI Strategy:** Single unified E2E test job runs all 63 tests (`LocalFinanceManager.E2E/**/*Tests.cs`) regardless of distribution across stories.
- **Screenshot/Video Artifacts:** Configured in US-5.1, invaluable for debugging CI failures. Automatically captured on test failure.
- **Cross-Browser Testing:** Playwright supports Chromium, Firefox, WebKit. Consider running integration tests on all browsers in CI for maximum coverage.
- **Maintenance:** Integration workflows may require updates when new features added. Keep workflows aligned with current feature set.
- **Documentation Central:** `E2E_TEST_GUIDE.md` serves as single source of truth for all E2E tests across storiesradation.
- Cross-feature workflow tests provide highest confidence: They simulate real user journeys.
- E2E tests are critical for regression prevention: Run full suite before each release.
- Playwright supports Chromium, Firefox, WebKit: Consider running tests on all browsers in CI.
