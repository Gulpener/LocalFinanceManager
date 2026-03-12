# Plan: Fix 4 Remaining Flaky E2E Tests

**Date:** March 12, 2026  
**Target:** 107/107 eligible tests passing across 3 consecutive full-suite runs  
**Files changed:** 3  
**Total edits:** 7

---

## Background

After all fixes in `TROUBLESHOOT-E2E.md`, one test still fails non-deterministically per run:

- `AssignTransaction_UpdatesRowToAssigned`
- `MonitoringDashboard_LowUndoRate_NoAlertShown`
- `IntegrationWorkflow_AssignBulkSplit_ValidatesCrossFeatureFlow`
- `AuditTrailButton_OpensHistoryAfterAssignment`

All pass in isolation. Root cause: missing Playwright _wait guards_ at specific Blazor state-transition points, not timing pressure per se.

---

## Root Causes

| Test                                           | Root Cause                                                                                                                                                               |
| ---------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| `AssignTransaction_UpdatesRowToAssigned`       | No `ToBeEnabledAsync` before save click; default 5 s `Not.ToBeVisibleAsync` timeout; `TextContentAsync` snapshot before re-render                                        |
| `AuditTrailButton_OpensHistoryAfterAssignment` | Audit button clicked before `@if (transaction.IsAssigned)` re-render completes; no `ToBeVisibleAsync` guard                                                              |
| `MonitoringDashboard_LowUndoRate_NoAlertShown` | Threshold read from `Factory.Services` (test host) — differs from real Kestrel server's `Factory.HostServices`; `IsAlertBannerVisibleAsync()` is a single-frame snapshot |
| `IntegrationWorkflow_AssignBulkSplit_...`      | Split badge `CountAsync()` fires before badge `OnInitializedAsync` completes; `SelectFilterAsync` called while audit modal Bootstrap fade-out is still running           |

---

## Changes

### 1. `AssignTransaction_UpdatesRowToAssigned`

**File:** `tests/LocalFinanceManager.E2E/Tests/BasicAssignmentTests.cs`

```diff
  await Page.SelectOptionAsync("#budgetLineSelect", _budgetLineFood.ToString());
+ await Expect(Page.Locator("#assignSaveButton")).ToBeEnabledAsync(new LocatorAssertionsToBeEnabledOptions { Timeout = 5_000 });
  await Page.ClickAsync("#assignSaveButton");

- await Expect(Page.Locator("#transactionAssignModal")).Not.ToBeVisibleAsync();
- var rowText = await Page.Locator("tr[data-testid='transaction-row']:has-text('Unassigned Tx A')").TextContentAsync();
- Assert.That(rowText, Does.Contain("Food"));
+ await Expect(Page.Locator("#transactionAssignModal")).Not.ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 30_000 });
+ await Expect(Page.Locator("tr[data-testid='transaction-row']:has-text('Unassigned Tx A')"))
+     .ToContainTextAsync("Food", new LocatorAssertionsToContainTextOptions { Timeout = 15_000 });
```

**Why `ToContainTextAsync` instead of `TextContentAsync` + Assert:**  
Playwright's `Expect` API retries internally until the condition is met or timeout. A snapshot read would require a manual retry loop to achieve the same reliability.

---

### 2. `AuditTrailButton_OpensHistoryAfterAssignment`

**File:** `tests/LocalFinanceManager.E2E/Tests/BasicAssignmentTests.cs`

```diff
  var row = Page.Locator("tr[data-testid='transaction-row']:has-text('Unassigned Tx A')").First;
- await row.Locator("button[title='Bekijk toewijzingsgeschiedenis']").ClickAsync();
+ var auditBtn = row.Locator("button[title='Bekijk toewijzingsgeschiedenis']");
+ await Expect(auditBtn).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 15_000 });
+ await auditBtn.ClickAsync();
```

**Why explicit `ToBeVisibleAsync`:**  
The button only renders inside `@if (transaction.IsAssigned)` in `Transactions.razor`. After modal-close, `OnAssignmentSuccess` calls `LoadTransactionsAsync()` + `StateHasChanged()` — the button does not exist in the DOM until this round-trip completes.

---

### 3. `MonitoringDashboard_LowUndoRate_NoAlertShown` — threshold host

**File:** `tests/LocalFinanceManager.E2E/ML/MonitoringDashboardTests.cs`

```diff
- using var scope = Factory!.Services.CreateScope();
+ using var scope = Factory!.HostServices.CreateScope();
```

**Why `HostServices`:**  
`Factory.Services` is the WebApplicationFactory's _test_ DI container. The Kestrel server running Blazor uses `Factory.HostServices`. If `appsettings` layering or environment-variable overrides differ between the two, `UndoRateAlertThreshold` resolves to a different value — causing `GetUndoCountBelowThreshold()` to seed data that actually exceeds the real threshold.

---

### 4. `MonitoringDashboard_LowUndoRate_NoAlertShown` — polling assertion

**File:** `tests/LocalFinanceManager.E2E/ML/MonitoringDashboardTests.cs`

```diff
- var isAlertVisible = await _dashboardPage.IsAlertBannerVisibleAsync();
- Assert.That(isAlertVisible, Is.False,
-     $"No alert should be shown when undo rate is below configured threshold ({_undoRateAlertThresholdPercent:0.0}%)");
+ await Expect(Page.Locator("[data-testid='alert-banner']"))
+     .Not.ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 8_000 });
```

**Why 8 s:**  
`MonitoringRefreshIntervalSeconds=2` means the component auto-refreshes every 2 s. 8 s covers one full refresh cycle plus Blazor render time with a 3× safety margin. `IsAlertBannerVisibleAsync()` calls `Page.IsVisibleAsync()` — a single-frame snapshot that can land mid-render.

---

### 5. `IntegrationWorkflow_AssignBulkSplit_...` — badge spinner wait

**File:** `tests/LocalFinanceManager.E2E/Tests/IntegrationWorkflowTests.cs`

```diff
  // Verify split badges appear for split transactions
  await transactionsPage.NavigateAsync();
  await transactionsPage.SelectAccountFilterAsync(accountId);
+ await Page.WaitForFunctionAsync(
+     "() => !document.querySelector(\"tr[data-testid='transaction-row'] .badge .spinner-border\")",
+     new PageWaitForFunctionOptions { Timeout = 15_000 });
  var splitBadges = await Page.Locator("tr[data-testid='transaction-row'] .badge.bg-info[aria-label='Gesplitst']").CountAsync();
```

**Why:** `SelectAccountFilterAsync` resolves when `data-loaded-account` matches, but individual badge components each fire their own `OnInitializedAsync` (fetching `/api/suggestions/{id}`). `CountAsync()` is a snapshot — it fires before all per-row badge spinners have resolved.

---

### 6. `IntegrationWorkflow_AssignBulkSplit_...` — Escape → SelectFilter race

**File:** `tests/LocalFinanceManager.E2E/Tests/IntegrationWorkflowTests.cs`

```diff
  await Page.Keyboard.PressAsync("Escape");
+ await Expect(Page.Locator("#auditTrailModal")).Not.ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 10_000 });

  // Check audit trail for one split transaction
  await transactionsPage.SelectFilterAsync("Assigned");
```

**Why:** Bootstrap's modal `fade` CSS animation takes ~300 ms after `Escape`. If `SelectFilterAsync` fires during the animation, its stable-window JS check can observe the partially-visible modal backdrop shifting the DOM — triggering a false "not stable" condition.

---

## Verification

Run the four tests in isolation first:

```powershell
dotnet test tests/LocalFinanceManager.E2E --no-build --filter "AssignTransaction_UpdatesRowToAssigned"
dotnet test tests/LocalFinanceManager.E2E --no-build --filter "AuditTrailButton_OpensHistoryAfterAssignment"
dotnet test tests/LocalFinanceManager.E2E --no-build --filter "MonitoringDashboard_LowUndoRate_NoAlertShown"
dotnet test tests/LocalFinanceManager.E2E --no-build --filter "IntegrationWorkflow_AssignBulkSplit_ValidatesCrossFeatureFlow"
```

Then run the full suite 3 times to confirm non-determinism is eliminated:

```powershell
1..3 | ForEach-Object {
    Write-Host "=== Run $_ ==="
    dotnet test tests/LocalFinanceManager.E2E --no-build 2>&1 | Select-String "Passed|Failed|Skipped" | Select-Object -Last 3
}
```

**Expected result:** `107 passed, 0 failed, 5 skipped` on all 3 runs.
