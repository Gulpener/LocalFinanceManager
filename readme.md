# 🏦 Personal Finance Manager – Lokaal WebApp (.NET 10)

**Beschrijving:**  
LocalFinanceManager is een lokaal draaiende webapplicatie (ASP.NET Core 10, Blazor Server) voor het beheren van persoonlijke financiën. De applicatie ondersteunt meerdere rekeningen, budgetten, potjes/enveloppen, import van transacties en score-based automatische categorisatie. Alle data blijft lokaal opgeslagen in SQLite.

**Doelgroep:** Iedereen die persoonlijke financiën overzichtelijk wil beheren zonder cloud-afhankelijkheid.

**Technologie:** .NET 10 / Blazor Server / EF Core 10 / SQLite

---

## MVP Strategie (prioriteit)

De ontwikkeling wordt uitgevoerd in een set van opeenvolgende, volledige MVP's. Elk MVP is een werkende milestone die gebruikerswaarde levert.

MVP 1 — Accounts (CRUD)

- Creëer en beheer rekeningen (CRUD).
- Beginbalans instellen.
- Zichtbare saldi en eenvoudige rekeningenlijst.

MVP 2 — Budgetplan per account (jaarlijks)

- Voeg per rekening een budgetplan toe.
- Vul geschatte budgetbedragen (jaarlijks) per categorie.
- Basisweergave van geplande vs. werkelijke bedragen.

MVP 3 — Import transacties

- Importer functionaliteit: CSV (basis), JSON.
- Deduplicatie op ingestelde regels (hash/veld-match).
- Opslaan van originele import-string.

MVP 4 — Koppel transacties aan budgetcategorieën

- Handmatig toewijzen van transacties aan budgetcategorieën.
- Splitsen van transacties over meerdere categorieën.

MVP 5 — Leerfunctie (categorisatie)

- Score-based categorisatie op basis van omschrijving, tegenrekening, bedrag en patronen.
- Interface om correcties te doen en model te laten bijleren.

MVP 6 — Automatisering bij hoge zekerheid

- Automatisch toewijzen wanneer score boven drempel ligt.
- Bevestiging/undo-UX voor automatische toewijzingen.

---

## Development roadmap

1. Voltooi MVP 1 → release alfa (lokale gebruikers).
2. Voltooi MVP 2 → budget-analytics basis.
3. Voltooi MVP 3 → import-stroom en persistente transacties.
4. Voltooi MVP 4 → koppeling transacties → budgetplan.
5. Voltooi MVP 5 → leren en verbeteren categorisatie.
6. Voltooi MVP 6 → veilige automatisering en policies.

Zie docs/TODO.md voor de gedetailleerde backlog per MVP.

---

## 🎯 Functionaliteiten

### 1. Rekeningbeheer

- Meerdere rekeningen (bank, spaar, creditcard, contant)
- Beginbalans + transacties
- Saldo automatisch berekend
- Archiveren/verbergen van rekeningen

### 2. Transactiebeheer

- Handmatig toevoegen van transacties
- Importeren van transacties via CSV, TSV, MT940 of JSON
- Deduplicatie bij import
- Splitsen van transacties in meerdere categorieën/potjes
- **Originele CSV-string opslaan bij elke transactie**

### 3. Score-based automatische categorisatie

- Leer van eerder handmatig gecategoriseerde transacties
- Scoring gebaseerd op:

  - Woorden in omschrijving
  - Tegenrekening / IBAN
  - Bedrag clusters
  - Herhalende patronen (maandelijks/wekelijkse betalingen)

- Score-formule bepaalt de meest waarschijnlijke categorie
- Gebruiker kan correcties doen → model leert bij
- Onzekerheidsdrempel: als score te laag is, vraagt systeem gebruiker om bevestiging

### 4. Budgetten

- Maandelijkse budgetten per categorie en optioneel per account
- Vergelijking: geplande vs. werkelijke uitgaven (per scope: category / envelope / account)
- Visuele voortgangsbalken per categorie of per account
- Budgetten kunnen worden aangemaakt met scope: `Category`, `Envelope` of `Account`.
  - **Category budgets** volgen uitgaven per categorie over alle rekeningen.
  - **Envelope budgets** volgen toewijzingen naar potjes/enveloppen.
  - **Account budgets** volgen alle transacties voor een specifieke rekening (optioneel gefilterd op categorie).
- Weergave en prioriteit: wanneer zowel account- als category-budgets bestaan, toont de UI beide overzichten; account-budgets geven inzicht in per-rekening limieten terwijl category-budgets helpen bij categorie-gestuurde planning.

### 5. Potjes / Enveloppen

- Aanmaken van potjes: “Leefgeld”, “Boodschappen”, “Vakantie”, etc.
- Maandelijkse automatische toewijzing
- Transacties koppelen aan potjes
- Dashboard met resterende saldo per potje

### 6. Dashboards & Rapportages

- Maandelijks overzicht inkomsten / uitgaven
- Jaaroverzicht met trends
- Grafieken: pie chart, bar chart, lijn grafieken
- Categorie-verdeling & potjesstatus

### 7. Lokale opslag & privacy

- SQLite database lokaal opgeslagen
- Optionele AES-256 encryptie
- Geen cloudverbindingen
- Automatische back-ups naar lokale bestanden

---

## 🏛️ Architectuur

### Backend

- ASP.NET Core 10
- EF Core 10 + SQLite provider
- Clean Architecture:

  - **Domain:** Entities, Aggregates
  - **Application:** Services (TransactionService, CategoryService, RuleEngineService, BudgetService)
  - **Infrastructure:** Repositories, EF Core migrations
  - **Presentation:** Blazor UI

### Frontend

- Blazor Server (aanbevolen)
- Alternatief: Razor Pages of React + minimal APIs

### Dependency Injection

- Alle services via DI container (`builder.Services.AddScoped<...>`)

---

## 🔧 Technologie & Libraries

- **.NET 10 / ASP.NET Core**
- **Blazor Server / Razor Components**
- **Entity Framework Core 10** + SQLite
- **AutoMapper**
- **FluentValidation**
- **MediatR** (optioneel)
- **Chart.js** integratie of Blazor Charts component
- **Hangfire Lite** (optioneel voor geplande taken)

---

## 🔨 Scaffolding via dotnet CLI

- `dotnet new webapp -n LocalFinanceManager`
- `dotnet new classlib` (voor services en domain)
- `dotnet ef migrations add InitialCreate`
- `dotnet ef database update`
- `dotnet new razorcomponent` (UI scaffolding)

Copilot kan hier scaffolding templates voor genereren.

---

## 🧪 Teststrategie

- Unit tests voor RuleEngine & BudgetEngine
- Integration tests met SQLite + EF Core
- UI tests via Playwright of BUnit voor Blazor

---

## 🚀 Toekomstige uitbreidingen

- PSD2 koppeling (lokaal token)
- Export naar Excel/PDF
- Multi-user (offline auth)
- Offline-first PWA optie
- Plugin systeem voor extra functionaliteit
