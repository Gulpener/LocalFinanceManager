# MVP 4 — Koppel transacties aan budgetcategorieën

Doel

- UI en API om transacties aan budgetcategorieën of budgetlijnen te koppelen, inclusief split-transacties.

Acceptatiecriteria

- Single assign en bulk-assign flows.
- Splits: transacties kunnen in meerdere delen worden gesplitst met referentie naar budgetline/category.

Data model

- Transaction (extends `BaseEntity`)
  - Id: GUID (inherited from BaseEntity)
  - Amount: decimal
  - Date, Description, Counterparty
  - AssignedParts: collectie van `TransactionSplit` (optional; null/empty for unsplit transactions)
  - RowVersion: byte[] (inherited from BaseEntity, configured for optimistic concurrency)
- TransactionSplit (extends `BaseEntity`)
  - Id: GUID (inherited from BaseEntity)
  - TransactionId: GUID (FK)
  - BudgetLineId: GUID (of CategoryId)
  - Amount: decimal
  - Note: string?
  - RowVersion: byte[] (inherited from BaseEntity)

Business regels

- Sum(splits) must equal original Amount (±0.01 rounding tolerance).
- Bulk assign applies same BudgetLineId to many transactions; provide preview and undo within 30-day retention window.

API endpoints

- `POST /transactions/{id}/assign` body: `{ "budgetLineId":".." }` (returns 409 Conflict if RowVersion mismatch; client reloads + retries)
- `POST /transactions/bulk-assign` body: `{ "transactionIds":[..], "budgetLineId":".." }` (atomic operation; all succeed or all fail)
- `POST /transactions/{id}/split` body: `{ "splits":[{"budgetLineId":"...","amount":100.00}, ...] }` (validates sum and RowVersion)

UI aanwijzingen

- Transaction detail: assign button with search/autocomplete for categories.
- Splits editor: inline rows, sum validation, save/cancel.
- Bulk UI: filter transactions → select → assign.

Audit & undo

- Persisteer `ChangedBy`, `ChangedAt`, en voor- en na-waarden; support undo last action per user.

Tests

**Project Structure:** Shared test infrastructure from MVP-1 (`LocalFinanceManager.Tests` and `LocalFinanceManager.E2E`).

- **Unit tests** (`LocalFinanceManager.Tests`): Splits sum validation (including rounding tolerance), bulk assign logic, audit record generation, RowVersion conflict detection.
- **Integration tests** (`LocalFinanceManager.Tests`): end-to-end assign and split workflows with in-memory SQLite; verify 409 Conflict returned on RowVersion mismatch; test atomic bulk operations.
- **E2E tests** (`LocalFinanceManager.E2E`): assign UI workflow, split editor (add/remove rows, sum validation), bulk assign preview, undo workflow.

Definition of Done

- API + UI assign/split working, validation, audit & undo, tests present across separate test projects.
- RowVersion concurrency checks enforced; 409 Conflict handling enables "reload latest" + retry flow.
- Splits sum validation with configurable rounding tolerance; atomicity on bulk operations guaranteed.
- Audit trail captures ChangedBy, ChangedAt, before/after values; undo reverts last N actions per transaction.
- All transaction edits respect RowVersion; import-created transactions inherit RowVersion on first manual edit.
