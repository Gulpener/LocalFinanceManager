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

Total: **39 tests** (80% critical path coverage).

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
- All E2E tests run in a single CI workflow, split into three sequential phases (Phase 1–3).
- Parallel execution groups can be configured using NUnit's `--worker` flag or `filter` options.
- Automatic migrations run during test host startup (no manual migration command needed).
- CI timeouts: each phase has `timeout-minutes: 10` (effective upper bound ~30 minutes wall-clock; expected total runtime remains <15 minutes including overhead).

## Naming convention

- Use `{Feature}Tests.cs` for test classes.
- Keep scenario names descriptive and workflow-oriented.

## Related phases

- Phase 1 foundation: `UserStory-10-Integration-Workflow-Tests.md` (archived)
- Phase 2 advanced: `UserStory-10.1-Advanced-Assignment-Tests.md` (archived on completion)
- Phase 3 extension: `UserStory-10.2-ML-Tests.md`
