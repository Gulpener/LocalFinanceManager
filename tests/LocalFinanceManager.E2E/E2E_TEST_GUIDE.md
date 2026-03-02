# E2E Test Guide

## Scope

This guide covers the UserStory-10 Phase 1 E2E foundation in `LocalFinanceManager.E2E`:

- `TransactionImportTests` (8 tests)
- `BasicAssignmentTests` (11 tests)
- `MultiAccountValidationTests` (1 test)

Total: **20 tests**.

## Prerequisites

On a fresh checkout, build the E2E project first so `playwright.ps1` is generated:

```powershell
dotnet build tests/LocalFinanceManager.E2E/LocalFinanceManager.E2E.csproj
```

Then install Playwright browsers (Chromium only):

```powershell
pwsh tests/LocalFinanceManager.E2E/bin/Debug/net10.0/playwright.ps1 install chromium
```

Alternative (using a local .NET tool manifest so no global tool is required):

```powershell
dotnet new tool-manifest            # creates .config/dotnet-tools.json if not present
dotnet tool install Microsoft.Playwright.CLI --version 1.57.0
dotnet tool run playwright install chromium --project tests/LocalFinanceManager.E2E/LocalFinanceManager.E2E.csproj
```

## Run tests

Run all E2E tests:

```powershell
dotnet test tests/LocalFinanceManager.E2E/LocalFinanceManager.E2E.csproj
```

Run only US-10 import tests:

```powershell
dotnet test tests/LocalFinanceManager.E2E/LocalFinanceManager.E2E.csproj --filter FullyQualifiedName~TransactionImportTests
```

Run only US-10 assignment tests:

```powershell
dotnet test tests/LocalFinanceManager.E2E/LocalFinanceManager.E2E.csproj --filter FullyQualifiedName~BasicAssignmentTests
```

Run only multi-account validation:

```powershell
dotnet test tests/LocalFinanceManager.E2E/LocalFinanceManager.E2E.csproj --filter FullyQualifiedName~MultiAccountValidationTests
```

## Debugging

- Use headed mode by setting Playwright launch options in the local debug profile.
- Use slower interactions for troubleshooting (`SlowMo=100`) in local debug launch.
- Screenshots are automatically captured on failures by `E2ETestBase`.

## Screenshot capture

- Failure screenshots: `test-results/screenshots/` (auto)
- US-10 manual screenshots (captured by the helper under `test-results/screenshots/` with timestamped filenames):
  - `import-preview_YYYYMMDD_HHMMSS.png`
  - `assignment-modal-open_YYYYMMDD_HHMMSS.png`
  - `multi-account-setup_YYYYMMDD_HHMMSS.png`
  - `multi-account-budget-line-filter_YYYYMMDD_HHMMSS.png`
  - `multi-account-validation-error_YYYYMMDD_HHMMSS.png`

## Test data and cleanup strategy

- Seed data is created via `SeedDataHelper`.
- Per-test isolation is enforced via `Factory.TruncateTablesAsync()` in US-10 test setup.
- Each fixture uses a dedicated SQLite database file and dynamic server port.

## CI notes

- Chromium-only execution is recommended for speed and deterministic results.
- Keep all E2E tests in one job, optionally split by class filter for parallel groups.
- Automatic migrations run during test host startup (no manual migration command needed).

## Naming convention

- Use `{Feature}Tests.cs` for test classes.
- Keep scenario names descriptive and workflow-oriented.

## Related phases

- Phase 2 extension: `UserStory-10.1-Advanced-Assignment-Tests.md`
- Phase 3 extension: `UserStory-10.2-ML-Tests.md`
