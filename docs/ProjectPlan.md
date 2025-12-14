# Project Plan — LocalFinanceManager (Agent Task List)

This version of the project plan converts epics and stories into explicit, actionable tasks the agent can perform. Each task includes the exact work steps, commands to run, target files to create/change, and acceptance criteria so tasks can be executed and verified automatically.

Status tracking

- Tasks are represented as GitHub-style markdown checkboxes. The agent will mark a task as completed by changing its checkbox from `- [ ]` to `- [x]` and will include the commit/PR reference in the commit message or PR description when doing so.

Note: the agent must strictly follow `.editorconfig` and `CONTRIBUTING.md` rules; if those files are missing the agent will create them as the first step.

---

## Bootstrapping (preliminary tasks)

- [ ] Task 0.1 — Create `.editorconfig` and `CONTRIBUTING.md`  
       Owner: agent  
       Steps:

- Create `.editorconfig` with project formatting rules (indentation, newline, C# conventions).
- Create `CONTRIBUTING.md` with branch/PR/style rules and testing requirements.
  Acceptance:
- Files exist at repo root and validate with `dotnet format` (or linter step).

- [ ] Task 0.2 — Initialize solution & projects (Epic A.1)  
       Owner: agent  
       Steps:

- Run `dotnet new sln -n LocalFinanceManager` in repo root
- Create `src/` and `tests/` folders
- `dotnet new classlib -n LocalFinanceManager.Domain -o src/Domain`
- `dotnet new classlib -n LocalFinanceManager.Application -o src/Application`
- `dotnet new classlib -n LocalFinanceManager.Infrastructure -o src/Infrastructure`
- `dotnet new blazorserver -n LocalFinanceManager.Web -o src/Web`
- `dotnet sln add src/**/*.csproj`
- Create minimal `Program.cs` in `Web` with DI registrations placeholder.
  Files:
- `LocalFinanceManager.sln`, `src/Domain/*.csproj`, `src/Application/*.csproj`, `src/Infrastructure/*.csproj`, `src/Web/*.csproj`, `src/Web/Program.cs`
  Acceptance:
- Solution builds (`dotnet build`).
- `Program.cs` registers DI container and sample services.

- [ ] Task 0.3 — Create test data folder structure  
       Owner: agent  
       Steps:
- Create `tests/TestData/` with sample CSV/JSON files
  Files:
- `tests/TestData/sample-transactions.csv`
- `tests/TestData/sample-transactions.json`
  Acceptance:
- Folders and sample files exist; solution builds.

---

## Epic A — Core data & persistence (Sprint 1) -> Tasks

- [ ] Task A.1 — Add EF Core packages and SQLite provider  
       Owner: agent  
       Steps:

- Add EF Core packages to `Infrastructure` and `Web` projects.
  Commands:
- `dotnet add src/Infrastructure package Microsoft.EntityFrameworkCore`
- `dotnet add src/Infrastructure package Microsoft.EntityFrameworkCore.Sqlite`
- `dotnet add src/Web package Microsoft.EntityFrameworkCore.Design`
  Acceptance:
- Packages referenced; project restores successfully.

- [ ] Task A.2 — Implement EF Core entity models (A.2)  
       **Depends on:** Task 0.2  
       Owner: agent  
       Steps:

- Create classes in `Domain/Entities` matching `readme.md` datamodel (`Account`, `Transaction`, `Category`, `Envelope`, `Rule`, `CategoryLearningProfile`).
  Files:
- `src/Domain/Entities/*.cs`
  Acceptance:
- Entities compile and follow naming / formatting from `.editorconfig`.

- [ ] Task A.3 — Create `ApplicationDbContext` & mapping (A.2)  
       Owner: agent  
       Steps:

- Create `ApplicationDbContext` in `Infrastructure` with DbSet properties for each entity.
- Configure value conversions for `List<string>` and `Dictionary<,>` as JSON columns.
  Files:
- `src/Infrastructure/ApplicationDbContext.cs`
  Acceptance:
- `ApplicationDbContext` compiles and can be registered in DI.

- [ ] Task A.4 — Add initial EF migration and apply to local SQLite (A.2)  
       Owner: agent  
       Steps:

- Add migration `InitialCreate` and update local SQLite DB in dev folder (`App_Data/local.db`).
  Commands:
- `dotnet ef migrations add InitialCreate --project src/Infrastructure --startup-project src/Web`
- `dotnet ef database update --project src/Infrastructure --startup-project src/Web`
  Acceptance:

````markdown
# Project Plan — LocalFinanceManager (Agent Task List)

This version of the project plan converts epics and stories into explicit, actionable tasks the agent can perform. Each task includes the exact work steps, commands to run, target files to create/change, and acceptance criteria so tasks can be executed and verified automatically.

Status tracking

- Tasks are represented as GitHub-style markdown checkboxes. The agent will mark a task as completed by changing its checkbox from `- [ ]` to `- [x]` and will include the commit/PR reference in the commit message or PR description when doing so.

Note: the agent must strictly follow `.editorconfig` and `CONTRIBUTING.md` rules; if those files are missing the agent will create them as the first step.

---

## Bootstrapping (preliminary tasks)

- [ ] Task 0.1 — Create `.editorconfig` and `CONTRIBUTING.md`  
       Owner: agent  
       Steps:

- Create `.editorconfig` with project formatting rules (indentation, newline, C# conventions).
- Create `CONTRIBUTING.md` with branch/PR/style rules and testing requirements.
  Acceptance:
- Files exist at repo root and validate with `dotnet format` (or linter step).

- [ ] Task 0.2 — Initialize solution & projects (Epic A.1)  
       Owner: agent  
       Steps:

- Run `dotnet new sln -n LocalFinanceManager` in repo root
- Create `src/` and `tests/` folders
- `dotnet new classlib -n LocalFinanceManager.Domain -o src/Domain`
- `dotnet new classlib -n LocalFinanceManager.Application -o src/Application`
- `dotnet new classlib -n LocalFinanceManager.Infrastructure -o src/Infrastructure`
- `dotnet new blazorserver -n LocalFinanceManager.Web -o src/Web`
- `dotnet sln add src/**/*.csproj`
- Create minimal `Program.cs` in `Web` with DI registrations placeholder.
  Files:
- `LocalFinanceManager.sln`, `src/Domain/*.csproj`, `src/Application/*.csproj`, `src/Infrastructure/*.csproj`, `src/Web/*.csproj`, `src/Web/Program.cs`
  Acceptance:
- Solution builds (`dotnet build`).
- `Program.cs` registers DI container and sample services.

- [ ] Task 0.3 — Create test data folder structure  
       Owner: agent  
       Steps:
- Create `tests/TestData/` with sample CSV/JSON files
  Files:
- `tests/TestData/sample-transactions.csv`
- `tests/TestData/sample-transactions.json`
  Acceptance:
- Folders and sample files exist; solution builds.

---

## Epic A — Core data & persistence (Sprint 1) -> Tasks

- [ ] Task A.1 — Add EF Core packages and SQLite provider  
       Owner: agent  
       Steps:

- Add EF Core packages to `Infrastructure` and `Web` projects.
  Commands:
- `dotnet add src/Infrastructure package Microsoft.EntityFrameworkCore`
- `dotnet add src/Infrastructure package Microsoft.EntityFrameworkCore.Sqlite`
- `dotnet add src/Web package Microsoft.EntityFrameworkCore.Design`
  Acceptance:
- Packages referenced; project restores successfully.

- [ ] Task A.2 — Implement EF Core entity models (A.2)  
       **Depends on:** Task 0.2  
       Owner: agent  
       Steps:

- Create classes in `Domain/Entities` matching `readme.md` datamodel (`Account`, `Transaction`, `Category`, `Envelope`, `Rule`, `CategoryLearningProfile`).
  Files:
- `src/Domain/Entities/*.cs`
  Acceptance:
- Entities compile and follow naming / formatting from `.editorconfig`.

- [ ] Task A.3 — Create `ApplicationDbContext` & mapping (A.2)  
       Owner: agent  
       Steps:

- Create `ApplicationDbContext` in `Infrastructure` with DbSet properties for each entity.
- Configure value conversions for `List<string>` and `Dictionary<,>` as JSON columns.
  Files:
- `src/Infrastructure/ApplicationDbContext.cs`
  Acceptance:
- `ApplicationDbContext` compiles and can be registered in DI.

---

## Epic B — Import & manual entry (Sprint 2) -> Tasks

- [ ] Task B.1 — Scaffolding: Import pipeline & interfaces (B.1)  
       Owner: agent  
       Steps:

- Create `ITransactionImporter` interface and pipeline in `Application`.
- Add `CsvTransactionImporter` implementation class in `Infrastructure`.
  Files:
- `src/Application/Interfaces/ITransactionImporter.cs`, `src/Infrastructure/Import/CsvTransactionImporter.cs`
  Acceptance:
- Importer registered in DI and can be called by a CLI or UI endpoint.

- [ ] Task B.2 — CSV/TSV importer implementation (B.1)  
       Owner: agent  
       Steps:

- Parse CSV rows, map to `Transaction` entity preserving `OriginalCsv`.
- Allow configurable delimiter and header mapping.
  Commands:
- Unit tests with sample CSV files under `tests/TestData/*.csv`
  Files:
- `src/Infrastructure/Import/CsvTransactionImporter.cs`, `tests/ImporterTests.cs`
  Acceptance:
- Imported transactions contain `OriginalCsv` equal to source row; tests validate parsing.

- [ ] Task B.3 — JSON and MT940 adapters (B.2)  
       Owner: agent  
       Steps:

- Add `JsonTransactionImporter` and `Mt940TransactionImporter` skeletons, wire to pipeline (priority order: CSV -> JSON -> MT940).
  Files:
- `src/Infrastructure/Import/JsonTransactionImporter.cs`, `Mt940TransactionImporter.cs`
  Acceptance:
- JSON importer handles sample JSON; MT940 adapter skeleton ready for future parsing rules.

- [ ] Task B.4 — Deduplication implementation & preview (B.3)  
       Owner: agent  
       Steps:

- Implement dedupe logic: compute hash on Date+Amount+Description+Account.
- Add configurable threshold and preview endpoint/UI to show duplicates before commit.
  Files:
- `src/Application/Services/DeduplicationService.cs`, `src/Web/Pages/ImportPreview.razor`
  Acceptance:
- Preview returns duplicates and unique candidates; tests validate duplicate detection.

- [ ] Task B.5 — Manual entry UI + FluentValidation (B.4)  
       Owner: agent  
       Steps:
- Create transaction entry form with validation rules using FluentValidation.
  Files:
- `src/Web/Pages/Transactions/Add.razor`, `src/Application/Validators/TransactionValidator.cs`
  Acceptance:
- Form validates input and persists valid transactions; tests cover validator.

---

## Epic C — Categorization & learning engine (Sprint 3) -> Tasks

- [ ] Task C.1 — Category learning profile persistence (C.1)  
       Owner: agent  
       Steps:

- Ensure `CategoryLearningProfile` EF mapping persists dictionaries as JSON.
- Add repository methods to read/update profiles.
  Files:
- `src/Infrastructure/Repositories/CategoryLearningProfileRepository.cs`
  Acceptance:
- Profiles saved and retrieved correctly in DB; unit tests validate frequency updates.

- [ ] Task C.2 — Implement ScoringEngine (C.2)  
       Owner: agent  
       Steps:

- Create `ScoringEngine` in `Application` that computes scores from description words, IBANs, amount buckets, recurrence signals.
- Return list of category suggestions with score floats.
  Files:
- `src/Application/Services/ScoringEngine.cs`
  Acceptance:
- Given sample transactions, engine returns ranked suggestions; unit tests assert expected scores.

- [ ] Task C.3 — Uncertainty threshold and UI flow (C.3)  
       Owner: agent  
       Steps:

- Add configurable uncertainty threshold in app settings.
- Wire UI to prompt user when top score < threshold; auto-assign otherwise.
  Files:
- `src/Web/Pages/Transactions/AutoCategorize.razor`, `appsettings.Development.json`
  Acceptance:
- UI behaves per threshold; integration test verifies flow.

- [ ] Task C.4 — Learning update on manual corrections (C.4)  
       Owner: agent  
       Steps:
- When user changes category, update `CategoryLearningProfile` frequencies incrementally.
- Add tests that show learning impacts future scoring.
  Files:
- `src/Application/Services/LearningService.cs`
  Acceptance:
- After correction, subsequent scoring reflects updated profile.

---

## Epic D — Rules, priorities & split transactions (Sprint 4) -> Tasks

- [ ] Task D.1 — Rule engine core (D.1)  
       Owner: agent  
       Steps:

- Implement `RuleEngine` that evaluates rules (pattern, regex, IBAN) with priority ordering.
- Rules can apply before scoring or as override.
  Files:
- `src/Application/Services/RuleEngine.cs`
  Acceptance:
- Engine applies highest-priority matching rule; unit tests cover match types.

- [ ] Task D.2 — Rules CRUD UI + priority ordering (D.2)  
       Owner: agent  
       Steps:

- Add pages to create/edit/delete rules and reorder priorities; show preview of affected transactions.
  Files:
- `src/Web/Pages/Rules/*`
  Acceptance:
- CRUD works and preview lists impacted transactions.

- [ ] Task D.3 — Transaction splitting feature (D.3)  
       Owner: agent  
       Steps:
- Implement split model and persist parts as linked records with accounting adjustments.
- UI to create N-part splits and reassign categories/envelopes.
  Files:
- `src/Domain/Entities/TransactionSplit.cs`, `src/Web/Pages/Transactions/Split.razor`
  Acceptance:
- Splits persist and transaction totals reconcile; tests validate correctness.

---

## Epic E — Budgets & envelopes (Sprint 5) -> Tasks

- [ ] Task E.1 — Budget model & monthly calculation service (E.1)  
       Owner: agent  
       Steps:

- Implement `Budget` entity if needed and `BudgetService` that computes spent vs budget for a month.
  Files:
- `src/Application/Services/BudgetService.cs`
  Acceptance:
- Service returns monthly budget summary; unit tests validate calculations.

- [ ] Task E.1 — Budget model & monthly calculation service (E.1)  
       Owner: agent  
       Steps:

- Ensure `Budget` entity includes optional `AccountId` to support account-scoped budgets (see `readme.md`).
- Update EF Core mappings in `ApplicationDbContext` to persist `AccountId` and add composite unique index `IX_Budget_AccountId_Month`.
- Add migration `AddAccountToBudget` (or include in next schema migration) and apply to development SQLite DB.
  Commands:
- `dotnet ef migrations add AddAccountToBudget --project src/Infrastructure --startup-project src/Web`
- `dotnet ef database update --project src/Infrastructure --startup-project src/Web`
- Update `BudgetService` to compute planned/actual amounts for flexible scopes:
  - Category (all accounts)
  - Envelope (allocations)
  - Account (all transactions for one account)
  - Combined scope rules (e.g., Account + Category) — service should accept optional `accountId` and/or `categoryId`/`envelopeId` filters.
- Update API/UI: add account selector when creating/editing budgets and show account-level summaries and progress bars.
  Files:
- `src/Application/Services/BudgetService.cs`
- `src/Infrastructure/Migrations/*AddAccountToBudget*`
- `src/Web/Pages/Budgets/*`
  Acceptance:
- DB schema contains `AccountId` column and index `IX_Budget_AccountId_Month`.
- `BudgetService` returns correct values for account-scoped budgets; unit/integration tests added.
- UI allows creating account-scoped budgets and displays per-account progress bars.

- [ ] Task E.1.1 — Backfill / Data Migration (optional)
      Steps:

- If previous budgets should be associated to accounts, provide a one-time migration or admin import to convert defaults.
- Provide a CLI tool or admin UI to assign account scope to existing budget entries.
  Files:
- `src/Tools/Backfill/AssignBudgetAccount.cs` (optional)
  Acceptance:
- Backfill tool documented and tested on sample DB.

- [ ] Task E.2 — Envelopes allocation job (E.2)  
       Owner: agent  
       Steps:

- Implement envelope allocation logic and an optional scheduled job or manual run endpoint.
  Files:
- `src/Application/Jobs/EnvelopeAllocator.cs`, `src/Web/Api/EnvelopeJobController.cs`
  Acceptance:
- Job executes allocations and updates envelope balances.

- [ ] Task E.3 — UI for budgets & envelopes (E.3)  
       Owner: agent  
       Steps:
- Add pages with progress bars showing budget consumption and envelope balances (use chosen chart lib).
  Files:
- `src/Web/Pages/Budgets/*`, `src/Web/Pages/Envelopes/*`
  Acceptance:
- UI displays accurate values for a test dataset.

---

## Epic F — Dashboards & reporting (Sprint 6) -> Tasks

- [ ] Task F.1 — Overview components & charts (F.1)  
       Owner: agent  
       Steps:

- Integrate Chart.js or Blazor chart component; add monthly/yearly charts.
  Files:
- `src/Web/Components/Charts/*`
  Acceptance:
- Charts render with sample data.

- [ ] Task F.2 — Export CSV/PDF endpoints (F.2)  
       Owner: agent  
       Steps:

- Add export endpoints for period/category as CSV and PDF (PDF via server-side library).
  Files:
- `src/Web/Api/ExportController.cs`
  Acceptance:
- Exports produce correct files for sample input.

- [ ] Task F.3 — Advanced filters & saved reports (F.3)  
       Owner: agent  
       Steps:
- Implement UI for filters and ability to save/load report presets.
  Files:
- `src/Web/Pages/Reports/*`
  Acceptance:
- Saved reports persist and restore filters.

---

## Epic G — Privacy, backup & operations (Sprint 7) -> Tasks

- [ ] Task G.1 — Backup/restore UI (G.1)  
       Owner: agent  
       Steps:

- Implement manual backup endpoint that copies `local.db` to configured folder with validation.
  Files:
- `src/Web/Api/BackupController.cs`
  Acceptance:
- Backup and restore validated by checksum and file existence.

- [ ] Task G.2 — Optional AES-256 DB encryption (G.2)  
       Owner: agent  
       Steps:

- Add a feature toggle and scaffolding for AES-256 encryption at rest (local key management docs).
  Files:
- `src/Infrastructure/Security/EncryptionService.cs`, `docs/encryption.md`
  Acceptance:
- Encryption toggle exists and documentation explains key recovery; actual encryption implementation gated behind feature flag.

- [ ] Task G.3 — Scheduled backups (G.3)  
       Owner: agent  
       Steps:
- Add optional scheduled backup job (Hangfire Lite or hosted service) with configurable cadence.
  Files:
- `src/Application/Jobs/ScheduledBackupService.cs`
  Acceptance:
- Scheduled backup can be simulated in tests.

---

## Epic H — Tests, CI & docs (parallel, ongoing) -> Tasks

- [ ] Task H.1 — Unit tests for core engines (H.1)  
       Owner: agent  
       Steps:

- Add tests for `RuleEngine`, `ScoringEngine`, `BudgetService` targeting >=80% for core logic.
  Files:
- `tests/*.Tests`
  Acceptance:
- Tests run in CI and meet coverage targets.

- [ ] Task H.2 — Integration tests with transient SQLite (H.2)  
       Owner: agent  
       Steps:

- Add integration tests that cover import → categorize → budget impact end-to-end.
  Files:
- `tests/Integration/*.cs`
  Acceptance:
- Tests pass locally and in CI.

- [ ] Task H.3 — GitHub Actions pipeline (H.3)  
       Owner: agent  
       Steps:

- Create `.github/workflows/ci.yml` pipeline: build, test, dotnet-format check, run migrations in ephemeral DB.
  Files:
- `.github/workflows/ci.yml`
  Acceptance:
- Pipeline runs on PR and passes.

- [ ] Task H.4 — Documentation & developer guide (H.4)  
       Owner: agent  
       Steps:
- Update `README.md` with run instructions, sample data, and migration steps; add developer guide to `docs/`.
  Files:
- `README.md`, `docs/developer-guide.md`
  Acceptance:
- Developer can clone, run `dotnet ef database update` and start the app with documented steps.

---

## Implementation notes for the agent

- Break each task into small commits and create feature branches per task.
- Create unit/integration tests before finalizing feature commits where practical.
- Use `App_Data/local.db` for the default SQLite path in development.
- Persist `OriginalCsv` on every imported transaction.
- Expose feature flags via `appsettings.json` and `IConfiguration`.
- Prefer JSON columns for `List<string>` and `Dictionary<,>` storage using EF Core value converters.
- Always run `dotnet format` and `dotnet build` before opening PR; CI will run the same checks.

---

## Error handling for the agent

- If a `dotnet` command fails, capture stderr and report it in task comments
- If tests fail, attach test output and mark task blocked
- If migration fails, verify connection string in `appsettings.Development.json`

---

End of task-oriented project plan.

## Improvements (Agent suggestions)

- [ ] Task I.1 — Improve structured logging & observability  
       Owner: agent  
       Steps:

  - Add `Serilog` and configure sinks (console, file, optional Seq/OTel).
  - Add correlation IDs and request logging middleware.
  - Add basic metrics (request counts, error counts) via OpenTelemetry.  
    Files:
  - `src/Infrastructure/Logging/SerilogExtensions.cs`, `src/Web/Program.cs`  
    Acceptance:
  - Structured logs include correlation IDs and key fields; metrics visible in dev output.

- [ ] Task I.2 — Feature flags & runtime configuration  
       Owner: agent  
       Steps:

  - Add `Microsoft.FeatureManagement` package and scaffold feature flags.
  - Expose flags in `appsettings.{Environment}.json` and provide an admin UI for toggling.  
    Files:
  - `src/Web/Pages/Admin/FeatureFlags.razor`, `appsettings.Development.json`  
    Acceptance:
  - Flags toggle features at runtime without redeploy; tests exercise toggled behavior.

- [ ] Task I.3 — CI improvements: security scanning & coverage  
       Owner: agent  
       Steps:

  - Extend GitHub Actions to run code scanning (e.g., `dotnet format`, `dotnet restore` + static analyzers) and to upload coverage artifacts.
  - Add caching for NuGet restore and build outputs.  
    Files:
  - `.github/workflows/ci.yml`  
    Acceptance:
  - CI pipeline includes formatting check, static analyzers, and publishes test coverage artifacts.

- [ ] Task I.4 — Accessibility & UI polish  
       Owner: agent  
       Steps:

  - Run an accessibility audit on core pages (transactions, import, budgets).
  - Fix issues: labels, ARIA attributes, contrast, keyboard navigation.  
    Files:
  - `src/Web/Pages/*`  
    Acceptance:
  - Core pages pass automated a11y checks (axe or similar) and keyboard navigation works.

- [ ] Task I.5 — Internationalization (i18n) support  
       Owner: agent  
       Steps:

  - Add resource files and localize UI strings; move hard-coded strings to resources.
  - Provide culture-aware date/number formatting and a locale selector in UI.  
    Files:
  - `src/Web/Resources/*.resx`, `src/Web/Program.cs`  
    Acceptance:
  - Application supports at least two locales and formats dates/numbers accordingly.

---

### Appendix A — `.editorconfig` template

```ini
root = true

[*.cs]
indent_style = space
indent_size = 4
dotnet_sort_system_directives_first = true
csharp_new_line_before_open_brace = all
```
````

```

```
