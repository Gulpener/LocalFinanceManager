# MVP 1 — Accounts (CRUD)

Doel

- Basis CRUD voor bank-/spaarrekeningen: aanmaken, bewerken, weergeven, archiveren.

Acceptatiecriteria

- API: `GET /accounts`, `GET /accounts/{id}`, `POST /accounts`, `PUT /accounts/{id}`, `DELETE /accounts/{id}` (archive flow: soft-delete / `IsArchived`).
- Blazor UI: lijst, create, edit, archive/unarchive, eenvoudige saldo-overzicht.
- Validatie: `Name` required, `Currency` ISO-4217 code, `StartingBalance` numeric, `Type` enum (Checking,Savings,Credit).

Data model (voor Copilot implementatie)

- Account
  - Id: GUID
  - Name: string(100), not null
  - Type: enum {Checking, Savings, Credit, Other}
  - Currency: string(3) ISO-4217
  - StartingBalance: decimal(18,2)
  - IsArchived: bool (default false)
  - CreatedAt: DateTime
  - UpdatedAt: DateTime

DB / EF details

- DbContext registratie voorbeeld: `DbSet<Account> Accounts`.
- Migratie seed: 3 sample accounts (EUR Checking, USD Savings, EUR Credit).
- Concurrency: optionele `RowVersion` byte[] voor optimistic concurrency.

API contract details

- Request/Response JSON schema voorbeelden:
  - POST body: `{ "name":"Spaar","type":"Savings","currency":"EUR","startingBalance":0.00 }`
  - Response: full Account object with `id`.
- Statuscodes: 200/201/204/400/404/409 (conflict for concurrency)

UI aanwijzingen (Blazor)

- Pages: `/accounts` (lijst), `/accounts/new`, `/accounts/{id}/edit`.
- Lijst toont: Name, Type, Currency, CurrentBalance (derived; initially StartingBalance), Actions(edit, archive).
- Form validatie client-side + server-side; gebruik FluentValidation of DataAnnotations.

Business/edge-cases

- Archiveren betekent verbergen in standaardlijsten; transacties moeten historisch blijven.
- Niet toestaan van duplicate names per gebruiker (configurable).

Tests

- Unit: repository CRUD ops, DTO mapping, validator rules.
- Integration: in-memory SQLite tests for DbContext + migrations.

Deployment/env

- Connection strings via `ASPNETCORE_ConnectionStrings__Default`.
- Local dev: sample seed toggled behind `ASPNETCORE_ENVIRONMENT == Development`.

Voorbeeld seed-data

- { "Name": "Betaalrekening", "Type":"Checking", "Currency":"EUR", "StartingBalance": 1000.00 }
- { "Name": "Spaarrekening", "Type":"Savings", "Currency":"EUR", "StartingBalance": 2500.00 }

Definition of Done

- End-to-end: create account via API + UI, edit, archive, unit & integration tests present.
