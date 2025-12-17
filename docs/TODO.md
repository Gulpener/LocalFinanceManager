# TODO Backlog — MVP-gebaseerde roadmap

Laatste update: 2025-12-17

## MVP 1 — Accounts (CRUD) [High Priority]

- [ ] Scaffold project basis (Program.cs, csproj, DI)
- [ ] Entity: Account (Id, Name, Type, Currency, StartingBalance, IsArchived)
- [ ] Repositories + EF DbContext registratie
- [ ] Account CRUD API / Blazor pages (lijst, create, edit, archive)
- [ ] Unit tests voor Account CRUD
- [ ] UI: eenvoudige accountslijst met saldo-overzicht

## MVP 2 — Budgetplan per account (jaarlijks) [High Priority]

- [ ] Entity: BudgetPlan (Id, AccountId, Year (kalenderjaar, Jan–Dec), Name)
- [ ] Entity: BudgetLine (BudgetPlanId, CategoryId, MonthlyAmounts[12])
- [ ] CRUD voor BudgetPlan en BudgetLine
- [ ] UI: Budgetplan editor per account (verplichte per-maand invoer voor budgetlijnen)
- [ ] Aggregatie: geplande vs. werkelijke bedragen (per maand/jaar)
- [ ] Unit tests voor BudgetEngine basis

## MVP 3 — Import transacties [High Priority]

- [ ] Import pipeline basis en UI
- [ ] CSV parser (configurable kolommen)
- [ ] JSON import
- [ ] Deduplicatie strategie implementatie (configurabele matcher)
- [ ] Opslaan originele import-string per transactie
- [ ] Tests: import + dedupe flows + edgecases

## MVP 4 — Koppel transacties aan budgetcategorieën [Medium Priority]

- [ ] UI: toewijzen transacties aan budgetcategorie (bulk + enkele)
- [ ] Splits-transacties ondersteunen
- [ ] Persistente relatie Transaction → BudgetLine/Category
- [ ] Rapportage: transacties per budgetcategorie
- [ ] Tests voor splitsen en koppelen

## MVP 5 — Leerfunctie (categorisatie) [Medium Priority]

- [ ] RuleEngine prototype (woordscore, tegenrekening, bedragclustering)
- [ ] Persistente opslag van user-correcties
- [ ] Training/update flow: leer van handmatige toewijzingen
- [ ] Unit tests voor scoreberekeningen

## MVP 6 — Automatisering bij voldoende zekerheid [Medium Priority]

- [ ] Drempelconfiguratie voor automatische toewijzing
- [ ] Background job / realtime proces om hoge-score transacties automatisch te taggen
- [ ] Undo / bevestiging UI en audit-log
- [ ] Veiligheidsbeleid en rollout (wat wordt automatisch, wat niet)

## Post-MVP / Nice-to-have [Low Priority]

- [ ] MT940 parser
- [ ] Encrypted SQLite ondersteuning (AES-256 key management)
- [ ] Backups en restore UI
- [ ] Charts & dashboards
- [ ] CI/CD templates (GitHub Actions)
- [ ] Playwright / BUnit UI-tests
