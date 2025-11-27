# Project Plan — LocalFinanceManager

This document lists prioritized epics and ordered user stories to build the application incrementally. Each story includes a short acceptance criteria and suggested sprint order. Implement features top-to-bottom.

## Milestones
1. Core data & persistence
2. Import, dedupe & manual entry
3. Categorization & rules engine
4. Budgets & envelopes
5. UI, dashboards & reporting
6. Privacy, backup & ops
7. Tests, CI and documentation

---

## Epic A — Core data & persistence (Sprint 1)
Goal: establish database, domain model, repository layer and basic CRUD for accounts and transactions.

- A.1 — Initialize solution & projects  
  Acceptance: Solution skeleton with projects: Domain, Application, Infrastructure, Web. DI wired in `Program.cs`.  
- A.2 — Implement EF Core models & migrations (Accounts, Transactions, Category, Envelope, Rule)  
  Acceptance: EF models match datamodel in README. `InitialCreate` migration applied to local SQLite.  
- A.3 — Repository interfaces + EF implementations  
  Acceptance: `IAccountRepository`, `ITransactionRepository` and implementations with unit tests for basic CRUD.  
- A.4 — Transaction service + add/get endpoints (Blazor pages or minimal APIs)  
  Acceptance: Can create, read, update, delete transactions via UI/API; transactions persist in SQLite.

---

## Epic B — Import & manual entry (Sprint 2)
Goal: support importing CSV/MT940/JSON, preserve original CSV string, and deduplicate.

- B.1 — CSV/TSV importer (pluggable)  
  Acceptance: Import pipeline reads rows to `Transaction` with `OriginalCsv` stored.  
- B.2 — MT940 + JSON import adapters (priority: CSV then JSON then MT940)  
  Acceptance: Adapters convert respective formats into domain transactions.  
- B.3 — Deduplication strategy & UI feedback  
  Acceptance: Duplicate detection (hash on Date+Amount+Description+Account) with configurable dedupe threshold and preview before commit.  
- B.4 — Manual transaction entry UI/validation  
  Acceptance: Form for adding transactions, validation via FluentValidation.

---

## Epic C — Categorization & learning engine (Sprint 3)
Goal: implement score-based automatic categorization and learning from corrections.

- C.1 — Category learning profile storage & update APIs  
  Acceptance: Word/IBAN/amount-bucket frequencies persist per category.  
- C.2 — Scoring engine (description, contra-account, amount buckets, recurrence)  
  Acceptance: Given a transaction, engine returns category suggestions with scores.  
- C.3 — Uncertainty threshold + UI suggestion flow  
  Acceptance: If top score < threshold, UI prompts for confirmation; else auto-assign.  
- C.4 — Learning from manual corrections (incremental update)  
  Acceptance: When user changes category, learning profile updates and affects future scores.

---

## Epic D — Rules, priorities & split transactions (Sprint 4)
Goal: deterministic rules and the ability to split a transaction into multiple categories/envelopes.

- D.1 — Rule engine with priorities (pattern matching, regex, IBAN)  
  Acceptance: Rules evaluated before/after scoring; highest priority rule applies.  
- D.2 — UI for creating/editing rules and priority ordering  
  Acceptance: CRUD for rules and preview of affected transactions.  
- D.3 — Transaction splitting UI + accounting adjustments  
  Acceptance: Split a transaction into N parts with categories/envelopes; persisted correctly.

---

## Epic E — Budgets & envelopes (Sprint 5)
Goal: monthly budgets per category and envelope allocations.

- E.1 — Budget model + monthly calculation service  
  Acceptance: Store monthly budgets; service computes spent vs budget for month.  
- E.2 — Envelopes (potjes) allocation tasks & recurring allocations  
  Acceptance: Create envelopes, set monthly allocation, run allocation job.  
- E.3 — UI for budgets & envelope management + progress bars  
  Acceptance: Visuals showing budget consumption and envelope balances.

---

## Epic F — Dashboards & reporting (Sprint 6)
Goal: visual, exportable summaries and trends.

- F.1 — Monthly/Yearly overview components (charts)  
  Acceptance: Pie/bar/line charts for income vs expense and per-category breakdown.  
- F.2 — Export CSV/PDF for periods and categories  
  Acceptance: Export selected reports.  
- F.3 — Advanced filters and saved reports  
  Acceptance: Save and load report filters.

---

## Epic G — Privacy, backup & local operations (Sprint 7)
Goal: ensure local-only storage, optional encryption and backups.

- G.1 — Local SQLite file handling & backup/restore UI  
  Acceptance: Create manual backups and restore with validation.  
- G.2 — Optional AES-256 DB encryption at rest  
  Acceptance: Toggle encryption; keys stored locally; clear doc for key recovery.  
- G.3 — Automatic local backups (configurable cadence)  
  Acceptance: Scheduled backups written to configured folder.

---

## Epic H — Tests, CI, scaffolding & docs (parallel, ongoing)
Goal: product-quality processes.

- H.1 — Unit tests for RuleEngine, ScoringEngine, BudgetEngine (target 80%+ on core)  
  Acceptance: CI runs tests on PR and master.  
- H.2 — Integration tests with in-memory or transient SQLite  
  Acceptance: End-to-end flow tests for import → categorize → budget impact.  
- H.3 — CI pipeline (GitHub Actions): build, test, static analysis  
  Acceptance: PRs gated by passing pipeline.  
- H.4 — Documentation: README extensions, usage guide, and API docs  
  Acceptance: Developer guide for running locally and release notes.

---

## Prioritization & Implementation notes
- Implement Epics A→C first to deliver basic usable product (accounts, transactions, import + categorization).
- Rules (Epic D) and Budgets (Epic E) follow to improve automation and user value.
- Keep each story small and testable; prefer iterative demos after each sprint.
- Use feature toggles for advanced features (encryption, scheduled tasks) to allow progressive rollout.
- Maintain backward-compatible migrations for local SQLite.

---

## Example User Story Template (use for all stories)
Title: Short title  
As a: [role]  
I want: [capability]  
So that: [value]  
Acceptance criteria: bullet list (data, UI, tests)

---

End of Project Plan