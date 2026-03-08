# E2E Test Guide

## Scope

This guide covers the E2E tests in `LocalFinanceManager.E2E`:

**Phase 1 (UserStory-10):**
- `TransactionImportTests` (8 tests)
- `BasicAssignmentTests` (11 tests)
- `MultiAccountValidationTests` (1 test)

**Phase 2 (UserStory-10.1):**
- `SplitAssignmentTests` (9 tests)
- `BulkAssignmentTests` (9 tests)
- `IntegrationWorkflowTests` (1 test)

**Phase 3 (UserStory-10.2):**
- `MLSuggestionTests` (9 tests)
- `AutoApplyTests` (9 tests)
- `MonitoringDashboardTests` (10 tests)

Total Phase 1–3: **67 tests** (>90% critical path coverage).

## Prerequisites

On a fresh checkout, build the E2E project first so `playwright.ps1` is generated:

```powershell
dotnet build tests/LocalFinanceManager.E2E/LocalFinanceManager.E2E.csproj
```

Then install Playwright browsers (Chromium only):

```powershell
pwsh tests/LocalFinanceManager.E2E/bin/Debug/net10.0/playwright.ps1 install chromium
```

Alternative (using a local .NET tool manifest so no global tool is required).
Run the following commands from the **repo root**. The resulting `.config/dotnet-tools.json` file should be committed so all contributors share the same tool version:

```powershell
dotnet new tool-manifest            # creates .config/dotnet-tools.json (commit this file)
dotnet tool install Microsoft.Playwright.CLI --version 1.57.0
dotnet tool run playwright install chromium --project tests/LocalFinanceManager.E2E/LocalFinanceManager.E2E.csproj
```

## Run tests

Run all E2E tests:

```powershell
dotnet test tests/LocalFinanceManager.E2E/LocalFinanceManager.E2E.csproj
```

Run only Phase 1 (US-10) tests:

```powershell
dotnet test tests/LocalFinanceManager.E2E/LocalFinanceManager.E2E.csproj --filter "FullyQualifiedName~TransactionImportTests|FullyQualifiedName~BasicAssignmentTests|FullyQualifiedName~MultiAccountValidationTests"
```

Run only Phase 2 (US-10.1) tests:

```powershell
dotnet test tests/LocalFinanceManager.E2E/LocalFinanceManager.E2E.csproj --filter "FullyQualifiedName~SplitAssignmentTests|FullyQualifiedName~BulkAssignmentTests|FullyQualifiedName~IntegrationWorkflowTests"
```

Run only US-10 import tests:

```powershell
dotnet test tests/LocalFinanceManager.E2E/LocalFinanceManager.E2E.csproj --filter FullyQualifiedName~TransactionImportTests
```

Run only US-10 assignment tests:

```powershell
dotnet test tests/LocalFinanceManager.E2E/LocalFinanceManager.E2E.csproj --filter FullyQualifiedName~BasicAssignmentTests
```

Run only split assignment tests:

```powershell
dotnet test tests/LocalFinanceManager.E2E/LocalFinanceManager.E2E.csproj --filter FullyQualifiedName~SplitAssignmentTests
```

Run only bulk assignment tests:

```powershell
dotnet test tests/LocalFinanceManager.E2E/LocalFinanceManager.E2E.csproj --filter FullyQualifiedName~BulkAssignmentTests
```

Run only integration workflow test:

```powershell
dotnet test tests/LocalFinanceManager.E2E/LocalFinanceManager.E2E.csproj --filter FullyQualifiedName~IntegrationWorkflowTests
```

Run only multi-account validation:

```powershell
dotnet test tests/LocalFinanceManager.E2E/LocalFinanceManager.E2E.csproj --filter FullyQualifiedName~MultiAccountValidationTests
```

Run only Phase 3 (US-10.2) ML tests:

```powershell
dotnet test tests/LocalFinanceManager.E2E/LocalFinanceManager.E2E.csproj --filter "FullyQualifiedName~MLSuggestionTests|FullyQualifiedName~AutoApplyTests|FullyQualifiedName~MonitoringDashboardTests"
```

Run only ML suggestion tests:

```powershell
dotnet test tests/LocalFinanceManager.E2E/LocalFinanceManager.E2E.csproj --filter FullyQualifiedName~MLSuggestionTests
```

Run only auto-apply configuration tests:

```powershell
dotnet test tests/LocalFinanceManager.E2E/LocalFinanceManager.E2E.csproj --filter FullyQualifiedName~AutoApplyTests
```

Run only monitoring dashboard tests:

```powershell
dotnet test tests/LocalFinanceManager.E2E/LocalFinanceManager.E2E.csproj --filter FullyQualifiedName~MonitoringDashboardTests
```

## Phase 2 Test Organization

### SplitAssignmentTests (9 tests)

Tests the split editor workflow:

1. `ClickSplit_OpensSplitEditorModal` — click Split button → modal visible
2. `AddSplits_WithValidSum_ShowsGreenValidation` — 3 rows summing to transaction amount → valid indicator
3. `SumMismatch_ShowsInvalidValidation_AndDisablesSaveButton` — mismatched sum → invalid, save disabled
4. `AdjustSplitToMatchSum_EnablesSaveButton` — fix sum → valid, save enabled
5. `SaveValidSplit_ShowsSplitBadgeOnRow` — save → transaction row shows "Gesplitst" badge
6. `RemoveSplitRow_UpdatesSumValidation` — remove row → sum recalculated
7. `CrossBudgetPlanAssignment_RejectedByService` — cross-plan split → service rejects
8. `ResplitTransaction_ReplacesExistingSplits` — re-split → old splits replaced
9. `AuditTrail_RecordsSplitOperation` — split → audit trail records operation

### BulkAssignmentTests (9 tests)

Tests bulk transaction assignment:

1. `SelectTransactions_ShowsBulkToolbarWithCount` — select 5 → toolbar with count
2. `ClickBulkAssign_OpensBulkModal` — select all → click bulk assign → modal opens
3. `BulkAssign_AllTransactions_ShowsFullSuccess` — assign 5 → success count = 5, failure = 0
4. `BulkAssign_PartialFailure_ShowsMixedResults` — cross-plan bulk → failures recorded
5. `ExpandErrorDetails_ShowsFailureList` — failures present → expand accordion → error list visible
6. `DeselectAll_HidesBulkToolbar` — deselect all → toolbar disappears
7. `SelectAllOnPage_ChecksAllVisibleTransactions` — select all checkbox → all rows checked
8. `BulkAssigned_TransactionsShowCategoryBadge` — bulk assign → badge shown
9. `SelectionCount_DisplayedCorrectlyInToolbar` — cumulative selection → toolbar updates

### IntegrationWorkflowTests (1 test)

Validates cross-feature data flow:

1. `IntegrationWorkflow_AssignBulkSplit_ValidatesCrossFeatureFlow`
   — Setup 35 transactions → basic assign 10 → bulk assign 20 → split 5 → verify all assigned + audit trail

## Phase 3 Test Organization (UserStory-10.2)

ML & Auto-Apply E2E tests live in `tests/LocalFinanceManager.E2E/ML/`.

### MLSuggestionTests (9 tests)

Tests ML suggestion badge display and interaction:

1. `MLSuggestionBadge_DisplayedOnUnassignedTransactions` — transactions load → badge shown (or no-model badge)
2. `MLSuggestionBadge_ShowsFeatureImportanceTooltip` — hover over badge → tooltip with "Based on" text
3. `MLSuggestionBadge_DoesNotUseHtmlTitleTooltipAttributes` — badge has no `title`/`data-bs-toggle`/`data-bs-html` attributes (XSS guard)
4. `MLSuggestionBadge_AcceptButton_AssignsTransactionAndHidesBadge` — click Accept → badge disappears → split created in DB
5. `MLSuggestionBadge_RejectButton_RecordsFeedbackAndBadgeRemains` — click Reject → feedback recorded → transaction unassigned
6. `TransactionList_FilterBySuggestion_ShowsOnlyTransactionsWithSuggestions` — "Has Suggestion" filter → only badge rows shown
7. `TransactionList_SortByConfidence_OrdersCorrectly` — sort by confidence desc → `data-confidence` values descending
8. `MLSuggestionBadge_ColorCoding_MatchesConfidenceThreshold` — >80% → green, 60–80% → yellow, <60% → gray
9. `MLModelInfo_DisplaysActiveModelDetails` — `/admin/ml` → version, accuracy %, last-trained date shown

### AutoApplyTests (9 tests)

Tests the Auto-Apply Settings configuration page (`/admin/autoapply`):

1. `AutoApplySettings_PageLoads_WithCurrentSettings` — page loads, title contains "Auto-Apply" or "Settings"
2. `AutoApplySettings_ToggleEnable_SettingSaved` — toggle → save → reload → DB row `AutoApplyEnabled` persisted
3. `AutoApplySettings_AdjustConfidenceThreshold_PreviewStatsUpdate` — slider to 85% → preview shows "transact" text, display shows "85"
4. `AutoApplySettings_SelectSpecificAccounts_OnlySelectedAccountsProcessed` — select account → save → `AccountIdsJson` in DB contains account id
5. `AutoApplySettings_AddExcludedCategories_SettingSaved` — exclude category → save → `ExcludedCategoryIdsJson` in DB
6. `AutoApplySettings_InvalidConfidence_ValidationErrorShown` — inject >1.0 via JS → save → validation error or clamped value
7. `AutoApplySettings_SaveSettings_SuccessToastDisplayed` — change setting → save → success toast appears
8. `AutoApply_ManualTrigger_TransactionsAutoAssigned` — *(ignored: manual trigger endpoint not in MVP)*
9. `AutoApply_AuditTrail_ShowsAutoAppliedIndicator` — *(ignored: audit trail UI page not implemented)*

### MonitoringDashboardTests (10 tests)

Tests the Monitoring Dashboard page (`/admin/monitoring`):

1. `MonitoringDashboard_PageLoads_WithStatsDisplayed` — page loads, title contains "Monitoring" or "Dashboard", total ≥ 0
2. `MonitoringDashboard_LowUndoRate_NoAlertShown` — undo count below threshold → no alert, green status indicator
3. `MonitoringDashboard_HighUndoRate_AlertBannerShown` — *(ignored: Blazor rendering timing issue)*
4. `MonitoringDashboard_MetricsCardsShowCorrectValues` — seed 100/8 undone → total=100, undo rate=8%, accepted=92
5. `MonitoringDashboard_UndoButton_RevertsAutoAppliedTransaction` — *(ignored: undo page refresh timing)*
6. `MonitoringDashboard_UndoButtonDisabled_ForAlreadyUndoneTransactions` — already-undone row has no enabled undo button
7. `MonitoringDashboard_HistoryTable_ShowsLast50Transactions` — seed 100 → history table ≤ 50 rows
8. `MonitoringDashboard_HistoryTable_ShowsStatusColumn` — rows show "Geaccepteerd" or "Ongedaan gemaakt"
9. `MonitoringDashboard_AutoRefresh_UpdatesMetricsWithoutPageReload` — add data while page open → metrics update within timeout *(slow)*
10. `MonitoringDashboard_ConfirmationDialog_AppearsBeforeUndo` — *(ignored: browser native confirm() not consistently interceptable)*

## Debugging

- Use headed mode by setting Playwright launch options in the local debug profile.
- Use slower interactions for troubleshooting (`SlowMo=100`) in local debug launch.
- Screenshots are automatically captured on failures by `E2ETestBase`.

## Screenshot capture

- Failure screenshots: `test-results/screenshots/` (auto)
- Phase 1 manual screenshots (captured under `test-results/screenshots/` with timestamped filenames):
  - `import-preview_YYYYMMDD_HHMMSS.png`
  - `assignment-modal-open_YYYYMMDD_HHMMSS.png`
  - `multi-account-setup_YYYYMMDD_HHMMSS.png`
  - `multi-account-budget-line-filter_YYYYMMDD_HHMMSS.png`
  - `multi-account-validation-error_YYYYMMDD_HHMMSS.png`
- Phase 2 manual screenshots:
  - `split-editor-open_YYYYMMDD_HHMMSS.png`
  - `split-editor-valid-sum_YYYYMMDD_HHMMSS.png`
  - `split-editor-invalid-sum_YYYYMMDD_HHMMSS.png`
  - `split-editor-saved_YYYYMMDD_HHMMSS.png`
  - `bulk-modal-open_YYYYMMDD_HHMMSS.png`
  - `bulk-assign-complete_YYYYMMDD_HHMMSS.png`
  - `bulk-error-details_YYYYMMDD_HHMMSS.png`
  - `bulk-assigned-badges_YYYYMMDD_HHMMSS.png`
  - `bulk-toolbar-count_YYYYMMDD_HHMMSS.png`
  - `workflow-start_YYYYMMDD_HHMMSS.png`
  - `workflow-basic-assigned_YYYYMMDD_HHMMSS.png`
  - `workflow-bulk-assigned_YYYYMMDD_HHMMSS.png`
  - `workflow-split-assigned_YYYYMMDD_HHMMSS.png`
  - `workflow-complete_YYYYMMDD_HHMMSS.png`

## Test data and cleanup strategy

- Seed data is created via `SeedDataHelper`.
- Per-test isolation is enforced via `Factory.TruncateTablesAsync()` in each test suite's SetUp.
- Each fixture uses a dedicated SQLite database file and dynamic server port.
- Integration workflow test cleans up at the start of the test, not in a shared SetUp.

## CI notes

- Chromium-only execution is recommended for speed and deterministic results.
- All E2E tests run in a single CI workflow, split into four sequential phases (Phase 1–4).
- Parallel execution groups can be configured using NUnit's `--worker` flag or `filter` options.
- Automatic migrations run during test host startup (no manual migration command needed).
- CI timeouts: each phase has `timeout-minutes: 10` (effective upper bound ~40 minutes wall-clock; expected total runtime remains <15 minutes including overhead).
- Phase 4 (ML) includes one slow test (`MonitoringDashboard_AutoRefresh`) tagged `[Category("Slow")]`; it waits up to 10 seconds for auto-refresh.

## Naming convention

- Use `{Feature}Tests.cs` for test classes.
- Keep scenario names descriptive and workflow-oriented.

## Related phases

- Phase 1 foundation: `UserStory-10-Integration-Workflow-Tests.md` (archived)
- Phase 2 advanced: `UserStory-10.1-Advanced-Assignment-Tests.md` (archived on completion)
- Phase 3 extension: `UserStory-10.2-ML-Tests.md` (archived on completion)
