<!--
Sync Impact Report

Version change: template (unversioned) -> 1.0.0

Modified principles:
- Clean Architecture (NON-NEGOTIABLE)
- Local-First Privacy & Data Ownership
- Test-First for Critical Engines (RuleEngine & BudgetEngine)
- Preserve Original Data & Import Integrity
- Dutch UI, Simplicity, Observability & Semantic Versioning

Added sections:
- Technical Constraints & Stack
- Development Workflow & Quality Gates

Removed sections:
- None

Templates requiring updates:
- .specify/templates/plan-template.md: ⚠ pending (Constitution Check must reference new gates)
- .specify/templates/spec-template.md: ⚠ pending (Testing & success criteria must cite constitution rules)
- .specify/templates/tasks-template.md: ⚠ pending (Tasks must include mandatory test tasks for engines)

Follow-up TODOs:
- RATIFICATION_DATE must be set (TODO(RATIFICATION_DATE))
- Update the three templates above to explicitly reference this constitution's gates and test mandates
-->

# LocalFinanceManager Constitution

## Core Principles

### Clean Architecture (NON-NEGOTIABLE)

All code and features MUST follow Clean Architecture layering: `Domain`, `Application`, `Infrastructure`, `Presentation`. Services MUST be named and organized (e.g., `TransactionService`, `RuleEngineService`, `BudgetService`). Business rules MUST reside in the `Domain`/`Application` layers; persistence and UI details MUST remain in `Infrastructure`/`Presentation`. Changes that break this layering are disallowed without an explicit migration plan and a MAJOR version bump.

### Local-First Privacy & Data Ownership

User data MUST be stored and processed locally by default. Network connections to third-party services or cloud providers are PROHIBITED unless explicitly opt-in and documented with a privacy rationale. Encryption-at-rest (AES-256) is SUPPORTED and SHOULD be offered as an opt-in. Data export/import features MUST clearly document data flows and user consent.

### Test-First for Critical Engines

Critical engines (notably the `RuleEngine` and `BudgetEngine`) MUST follow a test-first approach: unit tests that define expected behavior MUST be present before implementation, and CI MUST enforce that these tests pass. All new features that affect scoring, categorization, or budgets MUST include unit tests; integration tests with SQLite are REQUIRED for persistence-related changes.

### Preserve Original Data & Import Integrity

All imports MUST preserve original source strings (e.g., `Transaction.OriginalCsv`). Import processes MUST be idempotent and perform deduplication. Importers MUST NOT alter original data without recording the transformation, and MUST provide a clear audit trail for any automated fixes or parsing heuristics.

### Dutch UI, Simplicity, Observability & Semantic Versioning

The user-facing UI and all user-visible text MUST be presented in Dutch. Internal code, comments, and documentation MUST remain in English. Systems MUST favor simplicity and explicitness: prefer readable code over clever shortcuts. Structured logging and sufficient local observability (logs, local metrics) MUST be provided for debugging. Versioning follows semantic versioning: MAJOR for breaking governance or API/DB changes, MINOR for feature additions, PATCH for fixes and wording clarifications.

## Technical Constraints & Stack

The project MUST target the following stack unless a documented exception is approved:

- Backend: .NET 10 / ASP.NET Core (MUST)
- Frontend: Blazor Server (recommended); alternative frontends require justification (SHOULD)
- Database: SQLite via EF Core (MUST support local SQLite; integration tests SHALL use SQLite instances)
- Migrations: EF Core migrations MUST be used (`dotnet ef migrations add`, `dotnet ef database update`)
- Dependency Injection: All services MUST be registered via DI (`builder.Services.AddScoped<...>`)

## Development Workflow & Quality Gates

- Every PR that changes behavior MUST include tests covering the change. Critical areas (scoring, categorization, budgets) MUST include unit tests and an integration test against SQLite.
- Code review: PRs require at least two approvals, one of which MUST be a project maintainer.
- Breaking changes that affect data layout, imports, or governance MUST accompany a migration plan and increment the MAJOR version.
- All PRs MUST reference this constitution in their description when they affect any principle (e.g., "Affects: Preserve Original Data").
- The `Constitution Check` entry in the plan template MUST be updated to name this file and list the gating rules (see follow-ups).

## Governance

Amendments to this constitution MUST follow this procedure:

1. Open a documented PR titled `docs: amend constitution to vX.Y.Z` with a clear rationale and migration plan for any operational changes.
2. Include specific test/automation updates required by the amendment.
3. Obtain at least two approvals, one from a maintainer.
4. After merge, update the `Last Amended` date and publish release notes describing the impact.

Versioning policy:

- MAJOR: Backward-incompatible governance, data model, or import/DB layout changes.
- MINOR: Addition of new principles, new mandatory requirements, or expansions of scope.
- PATCH: Wording clarifications, typos, and non-substantive refinements.

Compliance review: A compliance check SHOULD be performed annually to ensure practices match the constitution. Any deviations MUST be documented and remediated with a timeboxed plan.

**Version**: 1.0.0 | **Ratified**: TODO(RATIFICATION_DATE) | **Last Amended**: 2025-12-14
