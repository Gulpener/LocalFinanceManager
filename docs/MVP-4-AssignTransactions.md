# MVP 4 — Koppel transacties aan budgetcategorieën

Doel

- UI en API om transacties aan budgetcategorieën of budgetlijnen te koppelen, inclusief split-transacties.

Acceptatiecriteria

- Single assign en bulk-assign flows.
- Splits: transacties kunnen in meerdere delen worden gesplitst met referentie naar budgetline/category.

Data model

- Transaction
  - Id: GUID
  - Amount: decimal
  - Date, Description, Counterparty
  - AssignedParts: collectie van `TransactionSplit`
- TransactionSplit
  - Id: GUID
  - TransactionId: GUID
  - BudgetLineId: GUID (of CategoryId)
  - Amount: decimal
  - Note

Business regels

- Sum(splits) must equal original Amount (allow rounding tolerance configurable).
- Bulk assign applies same BudgetLineId to many transactions; provide preview and undo.

API endpoints

- `POST /transactions/{id}/assign` body: `{ "budgetLineId":".." }`
- `POST /transactions/bulk-assign` body: `{ "transactionIds":[..], "budgetLineId":".." }`
- `POST /transactions/{id}/split` body: `{ "splits":[{"budgetLineId":"..","amount":100.00}, ...] }`

UI aanwijzingen

- Transaction detail: assign button with search/autocomplete for categories.
- Splits editor: inline rows, sum validation, save/cancel.
- Bulk UI: filter transactions → select → assign.

Audit & undo

- Persisteer `ChangedBy`, `ChangedAt`, en voor- en na-waarden; support undo last action per user.

Tests

- Splits sum validation, bulk assign atomicity, audit record generation.

Definition of Done

- API + UI assign/split working, validation, audit & undo, tests present.
