# MVP 1 — Accounts (CRUD)

Doel

- Basis CRUD voor bank-/spaarrekeningen: aanmaken, bewerken, weergeven, archiveren.

Acceptatiecriteria

- API: `GET /accounts`, `GET /accounts/{id}`, `POST /accounts`, `PUT /accounts/{id}`, `DELETE /accounts/{id}` (archive flow: soft-delete / `IsArchived`).
- Blazor UI: lijst, create, edit, archive/unarchive, eenvoudige saldo-overzicht.
- Validatie: `Label` required, `IBAN` required (valide IBAN), `Currency` ISO-4217 code, `StartingBalance` numeric, `Type` enum (Checking,Savings,Credit).
  Data model (voor Copilot implementatie)

- Account (extends `BaseEntity`)
  - Id: GUID (inherited from BaseEntity)
  - Label: string(100), not null (gebruikersvriendelijke omschrijving, bv. "Hoofdrekening")
  - Type: enum {Checking, Savings, Credit, Other}
  - Currency: string(3) ISO-4217
  - IBAN: string(34), not null (bewaar als genormaliseerde IBAN zonder spaties)
  - StartingBalance: decimal(18,2)
  - IsArchived: bool (default false)
  - CreatedAt: DateTime
  - UpdatedAt: DateTime
  - RowVersion: byte[] (inherited from BaseEntity, configured for optimistic concurrency)

DB / EF details

- DbContext registratie voorbeeld: `DbSet<Account> Accounts`.
- **Migrations:** Automatic migrations applied on app startup via `Database.MigrateAsync()` in `Program.cs`—no manual CLI steps during development.
- Migratie seed: 3 sample accounts (EUR Checking, USD Savings, EUR Credit) applied in `AppDbContext.SeedAsync()` method called only in Development environment.
- **Concurrency:** `RowVersion` byte[] configured with EF Core `.IsRowVersion()` for optimistic concurrency; `DbUpdateConcurrencyException` handled with last-write-wins reload strategy returning HTTP 409 Conflict.
- **IBAN Validation:** Use `IbanNet` NuGet package for IBAN validation in FluentValidation rules.
- **Soft-delete filtering:** All Account queries must explicitly include `.Where(a => !a.IsArchived)`; encapsulated via `IAccountRepository` pattern to reduce error risk.

API contract details

- Request/Response JSON schema voorbeelden:
  - POST body: `{ "label":"Spaarrekening","iban":"NL91ABNA0417164300","type":"Savings","currency":"EUR","startingBalance":0.00 }` (IBAN is verplicht)
  - Response: full Account object with `id` and `rowVersion`.
- Statuscodes: 200/201/204/400/404/409 Conflict (when RowVersion mismatch detected).
- **409 Conflict handling:** Return current entity state; client UI displays "reload latest" prompt (MVP-1-3 simple resolution; advanced merge UI deferred to MVP-4+).

UI aanwijzingen (Blazor)

- Pages: `/accounts` (lijst), `/accounts/new`, `/accounts/{id}/edit`.
- Lijst toont: Label, Type, Currency, CurrentBalance (derived; initially StartingBalance), Actions(edit, archive).
- Form validatie client-side + server-side; gebruik FluentValidation of DataAnnotations.

Business/edge-cases

- Archiveren betekent verbergen in standaardlijsten; transacties moeten historisch blijven.
  - **Implementation note:** All queries explicitly filter archived accounts (`.Where(a => !a.IsArchived)`); no global soft-delete filter configured.
- Niet toestaan van duplicate labels per gebruiker (configurable).

Tests

**Project Structure:** Separate test projects per `LocalFinanceManager.Tests` (unit + integration) and `LocalFinanceManager.E2E` (Playwright).

- **Unit tests** (`LocalFinanceManager.Tests`): repository CRUD ops, DTO mapping, validator rules (including IbanNet validation), RowVersion conflict resolution logic.
- **Integration tests** (`LocalFinanceManager.Tests`): in-memory SQLite (`:memory:` database) tests for DbContext + automatic migrations using `TestDbContextFactory`.
  - Each test receives fresh in-memory context via `CreateContext()` for complete isolation.
  - Use shared `TestDataBuilder` for seed data (3 sample accounts) across test suite.
- **E2E tests** (`LocalFinanceManager.E2E`): Playwright tests for account CRUD workflows (create, edit, archive, list) against `WebApplicationFactory` with dedicated test SQLite database.

Deployment/env

- Connection strings via `ASPNETCORE_ConnectionStrings__Default`.
- Local dev: sample seed toggled behind `ASPNETCORE_ENVIRONMENT == Development`.
- **Automatic migrations:** At app startup, `Database.MigrateAsync()` is called to ensure latest schema is applied without manual intervention.

Voorbeeld seed-data

- { "Label": "Betaalrekening", "IBAN": "NL91ABNA0417164300", "Type":"Checking", "Currency":"EUR", "StartingBalance": 1000.00 }
- { "Label": "Spaarrekening", "IBAN": "NL20INGB0001234567", "Type":"Savings", "Currency":"EUR", "StartingBalance": 2500.00 }

Definition of Done

- End-to-end: create account via API + UI, edit, archive, list (excluding archived), unit & integration tests present across separate test projects.
- Automatic migrations applied on app startup without manual CLI steps; seed data loaded in Development only.
- IBAN validation via IbanNet working in both API and Blazor forms.
- Archived accounts excluded from all standard list queries via `IAccountRepository` pattern.
- RowVersion optimistic concurrency implemented; 409 Conflict returned and UI prompts "reload latest".
- E2E tests for account CRUD workflows passing; Playwright tests cover create, edit, archive flows.
- Solution scaffolded with four projects: `LocalFinanceManager` (main), `LocalFinanceManager.Tests`, `LocalFinanceManager.E2E`, `LocalFinanceManager.ML`.
