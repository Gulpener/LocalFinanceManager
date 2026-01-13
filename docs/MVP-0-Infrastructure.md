# MVP 0 — Infrastructure Setup (Foundation)

Doel

- Scaffold complete .NET solution with all projects, configure EF Core, establish test infrastructure, and setup ML.NET foundation.
- This is a **prerequisite phase** that must be completed before any feature MVP (MVP 1-6) implementation begins.

Acceptatiecriteria

- All 4 projects created via CLI: `LocalFinanceManager`, `LocalFinanceManager.Tests`, `LocalFinanceManager.E2E`, `LocalFinanceManager.ML`, `LocalFinanceManager.ML.Tests`
- `BaseEntity` abstract class with Id, RowVersion, CreatedAt, UpdatedAt implemented
- `AppDbContext` configured with automatic migration on startup
- Test infrastructure working: `TestDbContextFactory`, `PlaywrightFixture`, `TestDataBuilder`
- ML.NET project structure in place with ML.Tests fixtures directory
- All projects compile without errors
- Development environment ready for MVP-1 implementation

Solution Scaffolding

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

Core Infrastructure Components

**BaseEntity**

- Abstract base class for all domain entities
- Properties: `Guid Id`, `byte[] RowVersion`, `DateTime CreatedAt`, `DateTime UpdatedAt`
- All entities inherit from `BaseEntity`
- Created in: `LocalFinanceManager/Models/BaseEntity.cs`

**AppDbContext**

- EF Core DbContext for all entities
- Configure automatic migration on startup via `Database.MigrateAsync()` in `Program.cs`
- RowVersion property configured with `.IsRowVersion()`
- Timestamps (CreatedAt/UpdatedAt) auto-set via value converters or interceptors
- Seed data via `AppDbContext.SeedAsync()` method (Development-only)
- Connection string: `ASPNETCORE_ConnectionStrings__Default` (environment variable)

**Repository Pattern**

- Generic `IRepository<T>` interface for data access
- `Repository<T>` base class with soft-delete filtering (`.Where(x => !x.IsArchived)`)
- Encapsulates concurrency exception handling (`DbUpdateConcurrencyException` → last-write-wins reload)
- Returns HTTP 409 Conflict when RowVersion mismatch detected
- Created in: `LocalFinanceManager/Data/Repositories/`

**Test Infrastructure**

- **TestDbContextFactory:** Creates fresh in-memory SQLite contexts (`:memory:`) for isolation
- **PlaywrightFixture:** Base class for E2E tests with `WebApplicationFactory` + test SQLite database
- **TestDataBuilder:** Factory for shared seed data (3 sample accounts, categories, transactions)
- All placed in `LocalFinanceManager.Tests/` with utilities available to `LocalFinanceManager.E2E/`

**ML.NET Structure**

- `LocalFinanceManager.ML/` class library for model training/inference (separate from main app)
- `MLModel` entity for database storage of trained models (byte[] ModelBytes + JSON metadata)
- `LocalFinanceManager.ML.Tests/fixtures/models/` directory for pre-trained `.bin` files
- Create placeholder fixture structure; models populated during MVP-5

Folder Structure (Post-Scaffolding)

```
LocalFinanceManager/
├── LocalFinanceManager.sln
├── LocalFinanceManager/                    # Main Blazor Server app
│   ├── Models/
│   │   ├── BaseEntity.cs                  # Abstract base for all entities
│   │   └── Category.cs                    # Placeholder for MVP-3 onward
│   ├── Data/
│   │   ├── AppDbContext.cs
│   │   └── Repositories/
│   │       ├── IRepository.cs
│   │       └── Repository.cs
│   ├── Services/
│   ├── Pages/
│   ├── Program.cs                          # Configure DbContext, migrations, DI
│   ├── appsettings.json
│   └── appsettings.Development.json
├── LocalFinanceManager.ML/                 # ML.NET library
│   ├── IMLService.cs                      # Service interface (implementation TBD)
│   ├── Models/                             # Feature engineering + model definitions
│   └── LocalFinanceManager.ML.csproj
├── tests/
│   ├── LocalFinanceManager.Tests/          # Unit + Integration tests
│   │   ├── Fixtures/
│   │   │   ├── TestDataBuilder.cs
│   │   │   └── TestDbContextFactory.cs
│   │   └── LocalFinanceManager.Tests.csproj
│   ├── LocalFinanceManager.E2E/            # Playwright E2E tests
│   │   ├── PlaywrightFixture.cs
│   │   └── LocalFinanceManager.E2E.csproj
│   └── LocalFinanceManager.ML.Tests/       # ML model tests
│       ├── fixtures/
│       │   └── models/                     # Pre-trained .bin files (populate in MVP-5)
│       └── LocalFinanceManager.ML.Tests.csproj
└── docs/
    ├── MVP-0-Infrastructure.md             # This file
    ├── MVP-1-Accounts.md
    └── TODO.md
```

Configuration Details

**Program.cs Setup**

- Register `AppDbContext` with SQLite connection string
- Call `Database.MigrateAsync()` on startup (before app runs)
- Register repositories via DI (e.g., `services.AddScoped<IRepository<Account>>()`)
- Call `AppDbContext.SeedAsync()` only if `ASPNETCORE_ENVIRONMENT == "Development"`

**appsettings.json**

```json
{
  "ConnectionStrings": {
    "Default": "Data Source=localfinancemanager.db"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  }
}
```

**appsettings.Development.json**

```json
{
  "ConnectionStrings": {
    "Default": "Data Source=localfinancemanager.db"
  }
}
```

EF Core Configuration Example

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);
    
    // Configure RowVersion
    modelBuilder.Entity<Account>()
        .Property(e => e.RowVersion)
        .IsRowVersion();
    
    // Configure CreatedAt/UpdatedAt (example with interceptor approach)
    // Details depend on chosen timestamp strategy
}
```

Tests

- **No feature tests yet** — only infrastructure validation tests
- Unit test: Verify `TestDbContextFactory` creates isolated contexts
- Unit test: Verify `TestDataBuilder` creates expected seed data
- Integration test: Verify `AppDbContext` migrations run on startup
- E2E test (smoke): Verify `WebApplicationFactory` spins up correctly

Definition of Done

- All 4 projects compile without warnings/errors
- Solution structure matches documented folder layout
- `BaseEntity` implemented and inheritable by all domain entities
- `AppDbContext` configured with automatic migrations on startup
- `IRepository<T>` pattern working with soft-delete filtering
- `TestDbContextFactory` creates fresh in-memory SQLite contexts
- `PlaywrightFixture` creates `WebApplicationFactory` + test database
- `TestDataBuilder` provides shared seed data (3 sample accounts)
- ML.NET class libraries created and referenced correctly
- `MLModel` entity created (placeholder, no storage logic yet)
- ML test fixtures directory structure in place
- Development environment ready to start MVP-1 implementation
- All core infrastructure tests passing
