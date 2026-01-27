# UserStory-9.2: E2E Tests - Phase 3 ML & Auto-Apply

## Objective

Implement ML-related E2E tests (estimated 25 tests) covering ML suggestions, auto-apply configuration, and monitoring dashboard. Completes comprehensive E2E test coverage (64 tests total: US-9: 20 + US-9.1: 19 + US-9.2: 25).

## Requirements

- Tests deferred until UserStory-7 (ML Suggestion & Auto-Apply) features fully implemented and stable
- Test count estimate (25) to be validated against actual US-7 implementation before story refinement
- Estimated test suites: MLSuggestionsTests (~8 tests), AutoApplyConfigTests (~8 tests), MonitoringDashboardTests (~9 tests)
- Leverage PageObjectModels from US-9/US-9.1 for transaction interactions
- Per-test cleanup strategy (same pattern as US-9/US-9.1)
- CI execution time target <15 minutes for complete Phase 1 + Phase 2 + Phase 3 suite

## Status

**Deferred - Pending UserStory-7 Completion**

This user story serves as a placeholder for Phase 3 E2E tests. Detailed implementation tasks, test scenarios, and acceptance criteria will be added during story refinement after US-7 ML features are production-ready.

## Implementation Tasks (Placeholder)

### 1. ML Suggestions Tests (~8 tests estimated)

- [ ] Create `MLSuggestionsTests.cs` test class in `LocalFinanceManager.E2E/Tests/`
- [ ] Test: Navigate to Transactions → Unassigned transactions show ML suggestion badges
  - **To be detailed after US-7 implementation**
- [ ] Test: ML badge displays confidence percentage (>80% green, 60-80% yellow, <60% gray)
  - **To be detailed after US-7 implementation**
- [ ] Test: Hover over ML badge → Tooltip shows top 3 feature importance factors
  - **To be detailed after US-7 implementation**
- [ ] Test: Click "Accept" on suggestion → Transaction assigned to suggested category
  - **To be detailed after US-7 implementation**
- [ ] Test: Click "Reject" on suggestion → Feedback recorded, badge remains
  - **To be detailed after US-7 implementation**
- [ ] Test: Filter transactions by "Has Suggestion" → Only transactions with ML suggestions shown
  - **To be detailed after US-7 implementation**
- [ ] Test: Sort by suggestion confidence → Highest confidence suggestions first
  - **To be detailed after US-7 implementation**
- [ ] Test: Navigate to ML model info page → Active model details displayed (version, training date, accuracy)
  - **To be detailed after US-7 implementation**

### 2. Auto-Apply Configuration Tests (~8 tests estimated)

- [ ] Create `AutoApplyConfigTests.cs` test class in `LocalFinanceManager.E2E/Tests/`
- [ ] Test: Navigate to Settings > Auto-Apply → Configuration page loads
  - **To be detailed after US-7 implementation**
- [ ] Test: Toggle "Enable Auto-Apply" → Setting saved and persisted
  - **To be detailed after US-7 implementation**
- [ ] Test: Adjust confidence threshold slider (60% → 80%) → Preview stats update dynamically
  - **To be detailed after US-7 implementation**
- [ ] Test: Select specific accounts for auto-apply → Only selected accounts processed
  - **To be detailed after US-7 implementation**
- [ ] Test: Add excluded categories → Transactions with excluded categories skipped
  - **To be detailed after US-7 implementation**
- [ ] Test: Save settings with invalid confidence (>100%) → Validation error shown
  - **To be detailed after US-7 implementation**
- [ ] Test: Trigger auto-apply job manually → Transactions auto-assigned based on threshold
  - **To be detailed after US-7 implementation**
- [ ] Test: Verify "Auto-applied" indicator in audit trail → Auto-assignments clearly marked
  - **To be detailed after US-7 implementation**

### 3. Monitoring Dashboard Tests (~9 tests estimated)

- [ ] Create `MonitoringDashboardTests.cs` test class in `LocalFinanceManager.E2E/Tests/`
- [ ] Test: Navigate to Monitoring dashboard → Dashboard loads with current stats
  - **To be detailed after US-7 implementation**
- [ ] Test: Verify metrics cards display (Total Auto-Applied, Undo Rate, Acceptance Rate)
  - **To be detailed after US-7 implementation**
- [ ] Test: Undo rate <10% → No alert banner, green status indicator
  - **To be detailed after US-7 implementation**
- [ ] Test: Undo rate >10% → Alert banner shown with warning message
  - **To be detailed after US-7 implementation**
- [ ] Test: Click "Undo Auto-Apply" on transaction → Confirmation modal → Undo successful
  - **To be detailed after US-7 implementation**
- [ ] Test: "Check if Can Undo" validation → Disabled for manual assignments, enabled for auto-applied
  - **To be detailed after US-7 implementation**
- [ ] Test: Auto-refresh works (30-second interval) → Stats update without page reload
  - **To be detailed after US-7 implementation**
- [ ] Test: History table shows last 50 auto-assigned transactions with timestamps
  - **To be detailed after US-7 implementation**
- [ ] Test: Undo rate trend chart displays correctly (7-day rolling average)
  - **To be detailed after US-7 implementation**

## Testing (Placeholder)

### Phase 3 Test Organization (Estimated)

**Test Suites:**

- **MLSuggestionsTests.cs** (~8 tests): Badge display, confidence scoring, accept/reject, feedback, filtering
- **AutoApplyConfigTests.cs** (~8 tests): Settings UI, threshold configuration, account/category selection, validation
- **MonitoringDashboardTests.cs** (~9 tests): Stats display, undo actions, alert banners, auto-refresh, trend charts

**Total: ~25 tests estimated** (subject to validation after US-7 implementation)

**Combined Coverage:** 64 tests total (US-9: 20 + US-9.1: 19 + US-9.2: 25) achieving >90% feature coverage

### Test Scenarios (To Be Refined)

Detailed test scenarios will be added during story refinement after US-7 features are implemented and stable. Scenarios will include:

- ML suggestion badge display and interaction patterns
- Auto-apply configuration edge cases (invalid thresholds, empty selections)
- Monitoring dashboard real-time updates and undo workflows
- Audit trail validation for auto-applied assignments
- Error handling when ML model unavailable

## Success Criteria (Tentative)

- ✅ 25 tests implemented and passing (8 ML + 8 config + 9 monitoring)
- ✅ Combined US-9 series: 64 tests achieving >90% feature coverage
- ✅ ML suggestion accuracy validated in E2E context
- ✅ Auto-apply configuration edge cases covered (invalid inputs, empty selections)
- ✅ Monitoring dashboard undo rate alerts tested (threshold <10% vs >10%)
- ✅ Auto-refresh functionality verified (30-second polling)
- ✅ Per-test cleanup verified (consistent with US-9/US-9.1 patterns)
- ✅ Test execution time <15 minutes for complete Phase 1+2+3 suite
- ✅ CI parallel execution configured (4 groups adding ML test group)
- ✅ All tests pass locally and in CI environment
- ✅ Code follows Implementation-Guidelines.md patterns

## Definition of Done (Tentative)

- [ ] Story refined with detailed test tasks after US-7 completion
- [ ] Test count validated (25 estimate confirmed or adjusted based on actual US-7 scope)
- [ ] MLSuggestionsTests.cs created with ~8 tests
- [ ] AutoApplyConfigTests.cs created with ~8 tests
- [ ] MonitoringDashboardTests.cs created with ~9 tests
- [ ] All tests passing locally and in CI
- [ ] Combined US-9 series: 64 tests passing
- [ ] E2E_TEST_GUIDE.md updated with Phase 3 test organization
- [ ] CI configured with 4th parallel execution group for ML tests
- [ ] Test execution time <15 minutes verified
- [ ] Code reviewed and merged to main branch

## Dependencies

- **UserStory-7 (ML Suggestion & Auto-Apply) REQUIRED (blocking):** ML features must be production-ready before implementing E2E tests. US-7 provides:
  - MLSuggestionBadge component
  - AutoApplySettings page
  - MonitoringDashboard page
  - ML model training and inference infrastructure
  - Auto-apply background service
- **UserStory-9 (Phase 1 Foundation):** Provides Tests/ directory, TransactionListPage, ImportModalPageModel, E2E_TEST_GUIDE.md
- **UserStory-9.1 (Phase 2 Advanced):** Provides integration test patterns, CI parallel execution configuration

## Estimated Effort

**TBD (~4-5 days estimated based on 25-test scope analysis)**

To be refined during story refinement after US-7 implementation review.

**Preliminary Breakdown:**

- ML Suggestions Tests: ~1.5 days (8 tests with badge interaction complexity)
- Auto-Apply Config Tests: ~1.5 days (8 tests with settings validation)
- Monitoring Dashboard Tests: ~1.5 days (9 tests with real-time updates and undo workflows)
- CI Configuration: ~0.25 days (add 4th parallel group)
- Documentation Updates: ~0.25 days (E2E_TEST_GUIDE.md Phase 3 section)

## Implementation Status

> **Deferred - Pending US-7:** This story is a placeholder for Phase 3 ML E2E tests. Detailed refinement will occur after UserStory-7 (ML Suggestion & Auto-Apply) features are fully implemented and stable in production.

**Refinement Required:**

- Validate 25-test estimate against actual US-7 implementation scope
- Add detailed test task checklists (similar to US-9/US-9.1 specificity)
- Define PageObjectModel requirements (e.g., MLSuggestionBadgePage, AutoApplySettingsPage, MonitoringDashboardPage)
- Document ML-specific test data seeding strategies (labeled examples, trained models)
- Specify confidence threshold edge cases to test
- Define undo rate alert threshold testing approach

**Prerequisites for Refinement:**

- US-7 features implemented and deployed
- ML model training pipeline stable
- Auto-apply background service operational
- Monitoring dashboard UI finalized

## Pattern Adherence from UserStory-9 and UserStory-9.1

**Reuses Established Patterns:**

- Per-test cleanup strategy (fresh database state)
- SeedDataHelper for test data creation
- PageObjectModel-only UI interactions
- Screenshot capture for key workflow stages
- Chromium-only browser testing
- CI parallel execution groups

**Extends for ML Context:**

- ML model seeding (pre-trained test models, labeled examples)
- Polling patterns for background job completion (auto-apply service)
- Real-time update testing (dashboard auto-refresh)
- Confidence threshold boundary testing (79.9% vs 80.0%)

## Notes

- **Test Count Validation Required:** 25-test estimate based on original US-9 gap analysis; actual count will be validated against implemented US-7 features during story refinement before implementation
- **ML Test Data Complexity:** Tests will require pre-trained ML models and labeled examples; consider using fixture models (similar to LocalFinanceManager.ML.Tests patterns)
- **Background Job Testing:** Auto-apply tests may require triggering background jobs; consider test-specific job scheduling or manual triggering endpoints
- **Real-Time Updates:** Dashboard auto-refresh tests may require polling with timeout; verify CI environment handles 30-second waits gracefully
- **Undo Rate Alerts:** Test threshold boundaries (9.9% vs 10.1%) to validate alert trigger logic
- **Phase 3 Completes Coverage:** Combined 64 tests (20+19+25) achieve comprehensive E2E coverage for all implemented features
- **Defer Until Stable:** ML features often require iteration; defer test implementation until US-7 is production-stable to avoid rework
- **PageObjectModel Creation:** US-9.2 may require new PageObjectModels (MLSuggestionBadgePage, AutoApplySettingsPage, MonitoringDashboardPage) not present in US-5.1
