# Copilot instructions

Doel

- Geef Copilot precies genoeg informatie om elk MVP te implementeren zonder extra details.

Algemene regels

- Gebruik EF Core voor persistence en Blazor (pages) voor UI.
- **Database Migrations:** Code-first EF Core met automatic migrations. Migrations worden automatisch toegepast bij app startup via `Database.MigrateAsync()` in `Program.cs`. Geen handmatige CLI-stappen vereist tijdens development.
- Tests: unit tests + integration tests met in-memory SQLite + **e2e tests met NUnit + Microsoft.Playwright**.
- Valuta: ISO-4217 (3 letters). Geldwaarden als decimal(18,2).
- **Technical Decisions:** Alle technische keuzes voor logging, error handling, configuration, async patterns, DI conventions, en database setup zijn vastgesteld in `docs/Implementation-Guidelines.md`. Raadpleeg dit document voor gedetailleerde implementatie-patronen.
- Copilot moet **altijd** voorbeeld CLI-commando's opnemen om .NET solutions en projecten aan te maken en te koppelen (bijv. `dotnet new`, `dotnet sln add`, `dotnet new blazorserver`, `dotnet add package`).
- **Concurrency Control:** Implementeer optimistic concurrency met `RowVersion` (byte[]) op veelvuldig bewerkte entities (Account, BudgetPlan, Transaction, BudgetLine); configureer EF Core met `.IsRowVersion()`. Handle `DbUpdateConcurrencyException` met last-write-wins reload strategie; return HTTP 409 Conflict met huidigte entity state.
- **Soft-delete filtering:** Implementeer `IsArchived` property; alle queries moeten expliciet filteren via `.Where(x => !x.IsArchived)`. Encapsuleer filtering in `IRepository<T>` pattern om foutrisico's te minimaliseren.
- **BaseEntity inheritance:** Alle entities erven van `BaseEntity` met `Guid Id` en `byte[] RowVersion` properties. CreatedAt/UpdatedAt automatisch ingesteld door EF Core value converters of interceptors.

Project Structuur

- `LocalFinanceManager/` — Main Blazor Server app + API controllers + services
- `LocalFinanceManager.Tests/` — Unit + integration tests (in-memory SQLite)
- `LocalFinanceManager.E2E/` — NUnit + Playwright end-to-end tests
- `LocalFinanceManager.ML/` — Class library voor ML.NET model training/inference (separate van main app)
- `LocalFinanceManager.ML.Tests/` — ML model training validation + fixture models

Test Framework Specificatie

- **Unit/Integration:** xUnit of NUnit in `LocalFinanceManager.Tests/` met `TestDbContextFactory` voor in-memory SQLite (`:memory:` database).
- **E2E:** **NUnit** + **Microsoft.Playwright** in `LocalFinanceManager.E2E/` tegen `WebApplicationFactory` met dedicated SQLite test database.
- **ML Tests:** NUnit in `LocalFinanceManager.ML.Tests/` met pre-trained fixture models (`.bin` files) voor snelle <100ms startup.

Machine Learning

- Gebruik **Microsoft.ML** (ML.NET) voor model training/inference in `LocalFinanceManager.ML` class library (separate van main app).
- Store getrainde modellen als `MLModel` entities (byte[] ModelBytes + JSON metadata) in database; enable versioning zonder filesystem dependencies.
- Fixture models: pre-trained `.bin` files committed in `LocalFinanceManager.ML.Tests/fixtures/models/`; CI job retrains monthly van labeled data.
- Minimum labeled examples threshold (e.g., 10 per category) enforced voor auto-assignment consideration.

## Implementation Guidelines Reference

- **Technical Decisions:** All code must follow decisions specified in `docs/Implementation-Guidelines.md`:
  - **.NET Version:** net10.0
  - **Logging:** Built-in Microsoft.Extensions.Logging (ILogger)
  - **Error Responses:** RFC 7231 Problem Details format
  - **Configuration:** appsettings.json + environment-specific files with IOptions<T>
  - **CORS:** Not needed (same-origin Blazor Server)
  - **Async Patterns:** Async all the way (all I/O operations async/await)
  - **DI Conventions:** Scoped services with I<Name> interfaces
  - **Database:** SQLite file (localfinancemanager.db in project root)
  - **Validation Errors:** Standard RFC 7231 format with property errors
  - **Code Style:** Nullable reference types enabled, warnings not-as-errors

For code examples and detailed guidance, refer to `docs/Implementation-Guidelines.md`.

Verplichte CLI-gebruik (NOODZAKELIJK)

- Copilot MAG NOOIT handmatig `.csproj`-bestanden aanmaken of volledig handmatig projectbestanden scaffolden.
- Voor het aanmaken of wijzigen van projecten moet Copilot eerst de exacte `dotnet`-CLI-commando's tonen die lokale ontwikkelaars kunnen uitvoeren.
- Als Copilot aanpassingen aan projectbestanden nodig acht, moet het eerst de corresponderende `dotnet`-CLI-aanpak voorstellen (bijv. `dotnet new`, `dotnet add package`, `dotnet sln add`). Alleen wanneer een CLI-oplossing niet mogelijk is, mag Copilot aanvullende handmatige wijzigingen voorstellen — en dan altijd met een duidelijke motivatie.

Volledige Scaffolding Voorbeeld (vereist voor Project Initialisatie)

```powershell
# Create solution and main Blazor project
dotnet new sln -n LocalFinanceManager
dotnet new blazorserver -n LocalFinanceManager -o LocalFinanceManager
dotnet sln add LocalFinanceManager/LocalFinanceManager.csproj

# Add main project NuGet packages
dotnet add LocalFinanceManager package Microsoft.EntityFrameworkCore.Sqlite
dotnet add LocalFinanceManager package Microsoft.EntityFrameworkCore.Design
dotnet add LocalFinanceManager package FluentValidation.AspNetCore
dotnet add LocalFinanceManager package IbanNet
dotnet add LocalFinanceManager package Swashbuckle.AspNetCore

# Create unit/integration test project
dotnet new nunit -n LocalFinanceManager.Tests -o tests/LocalFinanceManager.Tests
dotnet sln add tests/LocalFinanceManager.Tests/LocalFinanceManager.Tests.csproj
dotnet add tests/LocalFinanceManager.Tests package Microsoft.EntityFrameworkCore.Sqlite
dotnet add tests/LocalFinanceManager.Tests project LocalFinanceManager/LocalFinanceManager.csproj

# Create E2E test project (NUnit + Playwright)
dotnet new nunit -n LocalFinanceManager.E2E -o tests/LocalFinanceManager.E2E
dotnet sln add tests/LocalFinanceManager.E2E/LocalFinanceManager.E2E.csproj
dotnet add tests/LocalFinanceManager.E2E package Microsoft.Playwright
dotnet add tests/LocalFinanceManager.E2E package Microsoft.Playwright.NUnit
dotnet add tests/LocalFinanceManager.E2E project LocalFinanceManager/LocalFinanceManager.csproj

# Create ML.NET class library
dotnet new classlib -n LocalFinanceManager.ML -o LocalFinanceManager.ML
dotnet sln add LocalFinanceManager.ML/LocalFinanceManager.ML.csproj
dotnet add LocalFinanceManager.ML package Microsoft.ML
dotnet add LocalFinanceManager.ML package Microsoft.ML.FastTree
dotnet add LocalFinanceManager.ML project LocalFinanceManager/LocalFinanceManager.csproj

# Create ML test project
dotnet new nunit -n LocalFinanceManager.ML.Tests -o tests/LocalFinanceManager.ML.Tests
dotnet sln add tests/LocalFinanceManager.ML.Tests/LocalFinanceManager.ML.Tests.csproj
dotnet add tests/LocalFinanceManager.ML.Tests package Microsoft.ML
dotnet add tests/LocalFinanceManager.ML.Tests project LocalFinanceManager.ML/LocalFinanceManager.ML.csproj
```

Definition of Done (kort)

- Voor elk MVP: werkende API + minimale Blazor UI, unit + integration tests (in-memory SQLite), e2e tests, voorbeeld seed-data.
- Automatic migrations toegepast bij app startup zonder handmatige CLI-stappen.
- **TODO Tracking:** Na voltooiing van elke taak/stap in een MVP, markeert Copilot de overeenkomstige taak in `docs/TODO.md` als voltooid (`[x]`). Dit handelt Copilot automatisch af na implementatie.

Repository & Entity Patterns

**BaseEntity:**

```csharp
public abstract class BaseEntity
{
    public Guid Id { get; set; }
    public byte[] RowVersion { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

**Soft-Delete Repository Pattern:**

```csharp
public interface IRepository<T> where T : BaseEntity
{
    Task<T?> GetByIdAsync(Guid id);
    Task<List<T>> GetActiveAsync();
    Task AddAsync(T entity);
    Task UpdateAsync(T entity);
}

public class Repository<T> : IRepository<T> where T : BaseEntity
{
    // All queries automatically filter: .Where(x => !x.IsArchived)
    // Handles DbUpdateConcurrencyException with last-write-wins reload
}
```

**EF Core Configuration:**

- Configure RowVersion: `builder.Property(e => e.RowVersion).IsRowVersion();`
- Configure Timestamps: Use value converters or interceptors to auto-set CreatedAt/UpdatedAt
- Connection String: `ASPNETCORE_ConnectionStrings__Default` (environment variable)

**Seed Data Strategy:**

- Seed only in Development environment via `AppDbContext.SeedAsync()` called from `Program.cs`
- Check existing data to prevent duplicate seeds on re-run
- Example: `if (!context.Accounts.Any()) { /* seed */ }`

Waar nodig voor implementatie: raadpleeg de gedetailleerde MVP-documenten in `docs/`.
