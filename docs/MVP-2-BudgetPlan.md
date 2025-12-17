# MVP 2 — BudgetPlan per account (jaarlijks)

Doel

- Een budgetplan wordt opgesteld per kalenderjaar (Jan–Dec) per account; individuele budgetregels worden per maand ingesteld.

Acceptatiecriteria

- CRUD endpoints voor `BudgetPlan` en `BudgetLine`.
- UI: editor per account/jaar met verplicht per-maand invoer voor elke `BudgetLine`.

Data model

- BudgetPlan
  - Id: GUID
  - AccountId: GUID (FK)
  - Year: int (kalenderjaar, bijv. 2026 = januari–december 2026)
  - Name: string(150)
  - CreatedAt, UpdatedAt
- BudgetLine
  - Id: GUID
  - BudgetPlanId: GUID (FK)
  - CategoryId: GUID
  - MonthlyAmounts: decimal[12] (array van 12 waarden, Jan→Dec, decimal(18,2))
  - Notes: string?

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

- Unit: aggregatie berekeningen (sum MonthlyAmounts → YearTotal), CRUD voor plan/lines.
- Integration: end-to-end create plan + lines + read aggregations per maand.

Seed-data

- Voor account X, year 2026 met 5 budgetlines (huur, boodschappen, transport...) inclusief `MonthlyAmounts`.

Definition of Done

- API + UI editor werkend, per-maand invoer + aggregatie-test coverage, sample seed-data.
