# Copilot instructions for LocalFinanceManager

This file gives focused, actionable guidance for AI coding agents working in this repository.

- **Big picture:** LocalFinanceManager is an offline, locally-hosted personal finance webapp built with ASP.NET Core 10 and Blazor Server. Data is persisted to SQLite via EF Core. The project follows a Clean Architecture pattern (Domain → Application → Infrastructure → Presentation) described in the repository `readme.md`.

- **Where to look first:**

  - `readme.md` — high-level architecture, data model examples, and developer commands.
  - `.github/prompts/*` — existing prompt templates the project uses; follow their style for tasks.
  - `.specify/scripts/.../update-agent-context.ps1` — automation that references agent files; note it expects agent files at `.github/agents/*.md` and a copilot file at `.github/copilot-instructions.md`.

- **Project conventions discovered here:**

  - Clean Architecture layering is expected; name services like `TransactionService`, `RuleEngineService`, `BudgetService`.
  - Transactions must store the original import string: `Transaction.OriginalCsv`.
  - Budgets can be scoped to `Category`, `Envelope`, or `Account`; prefer explicit property names like `AccountId`, `CategoryId`, `EnvelopeId`.
  - The scoring/categorization model is incremental and learns from user corrections — keep model updates idempotent and fast.

  - **Language:** All code, inline comments, and repository documentation must be written in English. The user-facing UI and all visible text in the application should be presented in Dutch. Do not produce non-English identifiers or translations in source code or internal docs; language-specific examples are allowed only for test data where necessary and must be clearly labeled.

- **Build / run / DB workflow (documented and reproducible):**

  - Build: `dotnet build` in the project folder.
  - Run: `dotnet run` from the webapp project directory.
  - EF migrations: `dotnet ef migrations add <Name>` and `dotnet ef database update` (the readme shows this pattern).

- **Patterns & examples to follow when editing code:**

  - Use DI registrations (`builder.Services.AddScoped<...>`) for services.
  - Data models use plain POCOs with collections, e.g. `Transaction` includes `List<string> Tags` and `string OriginalCsv` — preserve these fields when refactoring imports.
  - Rules carry `Priority` and optional `TargetCategoryId`/`TargetEnvelopeId` — prefer explicit, nullable IDs instead of magic values.

- **Agent-specific integration notes:**

  - The repository has `.github/agents/*` agent descriptions and `.github/prompts/*` prompts; align any generated content with their style and titles.
  - `.specify/scripts/...` references `.github/agents` and `.github/agents/copilot-instructions.md` — keep or add the same content in both `.github/copilot-instructions.md` and `.github/agents/copilot-instructions.md` if automation expects it.

- **When generating code:**

  - Produce minimal, testable changes that respect the Clean Architecture separation (don’t mix UI and persistence logic).
  - If adding DB migrations, include the EF `Migration` name and update instructions in the commit message.
  - Prefer explicit unit tests for `RuleEngine` and `BudgetEngine` behavior (readme lists these as critical test targets).

- **Edge cases / known gaps:**
  - `src/` is currently empty in the workspace snapshot; scaffolding or project folders may be created via `dotnet new` as described in `readme.md`.
  - There are prompts and agent files under `.github/prompts` and `.github/agents` that show the project's voice — reuse their terser, checklist-oriented style.

If any of the above assumptions are incorrect or you want alternative wording/coverage (more examples, stricter linting rules, or additional workflow commands), tell me which area to expand and I will iterate.
