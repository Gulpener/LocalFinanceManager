# MVP 2 — BudgetPlan per account (jaarlijks)

Doel

- Beheer van jaarlijkse budgetplannen per account met individuele budgetregels per categorie.

Acceptatiecriteria

- CRUD endpoints voor `BudgetPlan` en `BudgetLine`.
- UI: editor per account/jaar met maand- of jaaroverzicht.

Data model

- BudgetPlan
  - Id: GUID
  - AccountId: GUID (FK)
  - Year: int
  - Name: string(150)
  - CreatedAt, UpdatedAt
- BudgetLine
  - Id: GUID
  - BudgetPlanId: GUID (FK)
  - CategoryId: GUID
  - AmountYearly: decimal(18,2)
  - Notes: string?

Business regels

- Aggregatie: `AmountYearly` kan worden weergegeven per maand (divide by 12) of custom monthly values indien UI ondersteunt.
- Valuta: gebruikt account currency; geen cross-currency sums tenzij expliciet gevraagd.

API contract & voorbeelden

- `GET /accounts/{accountId}/budgetplans` — lijst per jaar
- `POST /accounts/{accountId}/budgetplans` — body: `{ "year":2026, "name":"Jaarplan" }`
- `POST /budgetplans/{id}/lines` — body: `{ "categoryId":"...","amountYearly":1200.00 }`

UI aanwijzingen

- Editor: tabel met rijen per categorie en kolommen per maand + jaar-totaal of alleen jaarbedrag met per-maand fallback.
- Bulk import/export CSV per budgetplan.

Edgecases

- Wijzigingen in verledenjaar: hoe behandelen van reeds geboekte transacties (configurable: retroactief / historische rapportage onveranderd).
- Ontbrekende maanden: default 0.

Tests

- Unit: aggregatie berekeningen (jaarlijks → maandelijks), CRUD voor plan/lines.
- Integration: end-to-end create plan + lines + read aggregations.

Seed-data

- Voor account X, year 2026 met 5 budgetlines (huur, boodschappen, transport...).

Definition of Done

- API + UI editor werkend, aggregatie-test coverage, sample seed-data.
