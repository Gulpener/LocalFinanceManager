# BugReport-1: Currency Symbol Incorrect

## Status

- [x] Resolved

## Summary

Amounts are displayed with the generic `¤` symbol (e.g. `¤2.60`) instead of the currency symbol matching the account's currency setting (e.g. `€2.60` for EUR, `$2.60` for USD).

## Steps to Reproduce

1. Log in and open any page that displays transaction amounts or account balances.
2. Observe that amounts are shown as `¤2.60` instead of the expected currency symbol.

## Expected Behaviour

The currency symbol corresponds to the ISO-4217 currency code stored on the account (e.g. `EUR` → `€`, `USD` → `$`).

## Actual Behaviour

The generic Unicode currency placeholder `¤` is rendered for all amounts.

## Root Cause (suspected)

`ToString("C")` or the number formatter is called with a `CultureInfo` that does not resolve the correct symbol from the account's currency code.

## Fix

Derive a `CultureInfo` from the ISO-4217 currency code stored on the account and use it when formatting monetary values, so the correct symbol is always shown.

## Tasks

- [x] Find all call sites of `ToString("C")` and currency formatting helpers
- [x] Ensure the culture/locale is resolved from the account's ISO-4217 currency code (e.g. `EUR` → `€`)
- [x] Add unit test: formatting `2.60` with currency `EUR` returns `€2.60`

## Solution

**Root cause confirmed:** All `ToString("C2")` calls used the server process's ambient `CultureInfo`, which has a generic `¤` currency symbol when no locale is configured.

**Files changed:**

- `LocalFinanceManager/Helpers/CurrencyFormatter.cs` *(new)*  
  Static helper that builds a lookup from ISO-4217 codes to `CultureInfo` objects by iterating all specific cultures and their `RegionInfo.ISOCurrencySymbol`. `Format(amount, currencyCode)` uses the matched culture so `.ToString("C2")` renders the correct symbol (e.g. `€`, `$`, `£`).

- `LocalFinanceManager/DTOs/TransactionDTOs.cs`  
  Added `AccountCurrency` (`string?`) to `TransactionDto`.

- `LocalFinanceManager/DTOs/ML/AutoApplySettingsDto.cs`  
  Added `AccountCurrency` (`string?`) to `AutoApplyHistoryDto`.

- `LocalFinanceManager/Services/TransactionAssignmentService.cs`  
  `MapToDto` now copies `transaction.Account?.Currency` into `AccountCurrency`.

- `LocalFinanceManager/Services/MonitoringService.cs`  
  `GetAutoApplyHistoryAsync` now copies `audit.Transaction.Account?.Currency` into `AccountCurrency`.

- `LocalFinanceManager/Controllers/TransactionsController.cs`  
  DTO mapping now copies `t.Account?.Currency` into `AccountCurrency`.

- `LocalFinanceManager/Components/Pages/Transactions.razor`  
  Replaced all `ToString("C2")` and `:C2` interpolations with `CurrencyFormatter.Format(…)`. Added `SelectedAccountCurrency` computed property for the filtered total row.

- `LocalFinanceManager/Components/Pages/TransactionAudit.razor`  
  Replaced `ToString("C2")` with `CurrencyFormatter.Format(…)`.

- `LocalFinanceManager/Components/Pages/TransactionImport.razor`  
  Added `SelectedImportAccountCurrency` computed property; replaced `ToString("C2")` in the preview table.

- `LocalFinanceManager/Components/Pages/Admin/Monitoring.razor`  
  Replaced `ToString("C2")` with `CurrencyFormatter.Format(…)`.

- `LocalFinanceManager/Components/Shared/TransactionAssignModal.razor`  
  Replaced `ToString("C2")` with `CurrencyFormatter.Format(…)`.

- `LocalFinanceManager/Components/Shared/SplitEditor.razor`  
  Replaced all four `ToString("C2")` occurrences with `CurrencyFormatter.Format(…)`.

- `LocalFinanceManager/Components/_Imports.razor`  
  Added `@using LocalFinanceManager.Helpers` so all Razor components can use `CurrencyFormatter` without per-file using directives.

- `tests/LocalFinanceManager.Tests/Unit/CurrencyFormatterTests.cs` *(new)*  
  13 unit tests covering EUR/USD/GBP symbol rendering, case insensitivity, null/empty/unknown currency codes, and negative amounts. All pass.

