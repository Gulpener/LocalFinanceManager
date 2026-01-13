# MVP 2 — BudgetPlan per account (jaarlijks)

Doel

- Een budgetplan wordt opgesteld per kalenderjaar (Jan–Dec) per account; individuele budgetregels worden per maand ingesteld.

Acceptatiecriteria

- CRUD endpoints voor `BudgetPlan` en `BudgetLine`.
- UI: editor per account/jaar met verplicht per-maand invoer voor elke `BudgetLine`.

Data model

- BudgetPlan (extends `BaseEntity`)
  - Id: GUID (inherited from BaseEntity)
  - AccountId: GUID (FK)
  - Year: int (kalenderjaar, bijv. 2026 = januari–december 2026)
  - Name: string(150)
  - CreatedAt, UpdatedAt
  - RowVersion: byte[] (inherited from BaseEntity, configured for optimistic concurrency)
- BudgetLine (extends `BaseEntity`)
  - Id: GUID (inherited from BaseEntity)
  - BudgetPlanId: GUID (FK)
  - CategoryId: GUID
  - MonthlyAmounts: decimal[12] stored as JSON array (Jan→Dec, decimal(18,2) values per month)
  - Notes: string?
  - RowVersion: byte[] (inherited from BaseEntity)

Business regels

- Budgetregels moeten per maand worden ingevuld. Het jaarbedrag is de som van `MonthlyAmounts`.
- UI mag een uniforme maandwaarde ondersteunen (vul alle 12 maanden met dezelfde waarde), maar opslag moet per-maand zijn.
- Valuta: gebruikt account currency; geen cross-currency sums tenzij expliciet gevraagd.

API contract & voorbeelden

- `GET /accounts/{accountId}/budgetplans` — lijst per jaar
- `POST /accounts/{accountId}/budgetplans` — body: `{ "year":2026, "name":"Jaarplan" }`
- `POST /budgetplans/{id}/lines` — body: `{ "categoryId":"...","monthlyAmounts":[100.00,100.00,100.00,100.00,100.00,100.00,100.00,100.00,100.00,100.00,100.00,100.00] }`

UI aanwijzingen

- Editor: tabel met rijen per categorie en kolommen per maand (Jan–Dec) en een kolom `Jaar-totaal` (soma van 12 maanden). Per-maand invoer is de primaire invoer.
- Bulk import/export CSV per budgetplan: required 12 maand-kolommen of één uniform maand-kolom die naar 12 maanden wordt gekopieerd.

Edgecases

- Wijzigingen in verledenjaar: hoe behandelen van reeds geboekte transacties (configurable: retroactief / historische rapportage onveranderd).
- Ontbrekende maanden: default 0 (UI valideren op 12 maanden of invuloptie).

Tests

**Project Structure:** Shared test infrastructure from MVP-1 (`LocalFinanceManager.Tests` and `LocalFinanceManager.E2E`).

- **Unit tests** (`LocalFinanceManager.Tests`): aggregatie berekeningen (sum MonthlyAmounts JSON → YearTotal), CRUD voor plan/lines, RowVersion conflict handling on budget edits.
- **Integration tests** (`LocalFinanceManager.Tests`): end-to-end create plan + lines + read aggregations per maand using in-memory SQLite; verify JSON array storage/retrieval.
- **E2E tests** (`LocalFinanceManager.E2E`): budget editor UI workflows, per-maand entry, bulk uniform value assignment, persistence verification.

Seed-data

- Voor account X, year 2026 met 5 budgetlines (huur, boodschappen, transport...) inclusief `MonthlyAmounts`.

Definition of Done

- API + UI editor werkend, per-maand invoer + aggregatie-test coverage, sample seed-data.
