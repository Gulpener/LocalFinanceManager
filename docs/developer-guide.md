# Developer Guide — LocalFinanceManager

This guide provides instructions for developers who want to contribute to or extend the LocalFinanceManager application.

## Prerequisites

- .NET 10 SDK
- SQLite (included with .NET)
- A code editor (Visual Studio, VS Code, or Rider recommended)

## Getting Started

### 1. Clone the Repository

```bash
git clone https://github.com/your-org/LocalFinanceManager.git
cd LocalFinanceManager
```

### 2. Restore Dependencies

```bash
dotnet restore
```

### 3. Apply Database Migrations

```bash
dotnet new tool-manifest # if you don't already have .config/dotnet-tools.json
dotnet tool install dotnet-ef --version 10.0.0
dotnet tool restore
```

```bash
dotnet ef database update --project src/Infrastructure --startup-project src/Web
```

This creates the SQLite database at `src/Web/App_Data/local.db`.

### 4. Run the Application

```bash
dotnet run --project src/Web
```

The application will be available at `http://localost:5000` (or as configured in `launchSettings.json`).

### 5. Run Tests

```bash
dotnet test
```

## Project Structure

```
LocalFinanceManager/
├── src/
│   ├── Domain/                    # Core entities (no dependencies)
│   │   └── Entities/             # Account, Transaction, Category, etc.
│   ├── Application/              # Business logic
│   │   ├── Interfaces/           # Repository and service interfaces
│   │   ├── Services/             # TransactionService, BudgetService, etc.
│   │   └── Validators/           # FluentValidation validators
│   ├── Infrastructure/           # Data access layer
│   │   ├── Repositories/         # EF Core repository implementations
│   │   ├── Import/               # CSV, JSON, MT940 importers
│   │   ├── Migrations/           # EF Core migrations
│   │   └── ApplicationDbContext.cs
│   └── Web/                      # Blazor Server UI
│       ├── Components/
│       │   ├── Layout/           # Main layout, nav menu
│       │   └── Pages/            # Razor pages
│       └── Program.cs            # DI configuration and startup
├── tests/
│   ├── Infrastructure.Tests/     # Repository and importer tests
│   └── TestData/                 # Sample CSV/JSON files
└── docs/                         # Documentation
```

## Architecture

The application follows Clean Architecture principles:

1. **Domain**: Contains entities with no external dependencies
2. **Application**: Contains business logic, service interfaces, and validators
3. **Infrastructure**: Implements data access using EF Core and SQLite
4. **Web**: Blazor Server UI that wires everything together

### Dependency Flow

```
Domain ← Application ← Infrastructure ← Web
```

- Domain has no dependencies
- Application depends only on Domain
- Infrastructure depends on Domain and Application
- Web depends on all layers and configures DI

## Key Components

### Entity Framework Core

The application uses EF Core with SQLite. Key configurations:

- Connection string: `Data Source=App_Data/local.db`
- JSON columns for `List<string>` and `Dictionary<,>` properties
- Value converters and comparers for complex types

### Services

| Service                | Purpose                                         |
| ---------------------- | ----------------------------------------------- |
| `TransactionService`   | CRUD operations for transactions                |
| `BudgetService`        | Budget calculations and summaries               |
| `ScoringEngine`        | Auto-categorization scoring                     |
| `LearningService`      | Updates learning profiles from user corrections |
| `RuleEngine`           | Applies user-defined categorization rules       |
| `DeduplicationService` | Detects duplicate transactions                  |

### Importers

| Importer                   | File Types       |
| -------------------------- | ---------------- |
| `CsvTransactionImporter`   | `.csv`, `.tsv`   |
| `JsonTransactionImporter`  | `.json`          |
| `Mt940TransactionImporter` | `.mt940`, `.sta` |

## Database Migrations

### Create a new migration

```bash
dotnet ef migrations add MigrationName --project src/Infrastructure --startup-project src/Web
```

### Update database

```bash
dotnet ef database update --project src/Infrastructure --startup-project src/Web
```

### Remove last migration (if not applied)

```bash
dotnet ef migrations remove --project src/Infrastructure --startup-project src/Web
```

## Testing

### Running Tests

```bash
# Run all tests
dotnet test

# Run with verbose output
dotnet test --logger "console;verbosity=normal"

# Run specific test project
dotnet test tests/Infrastructure.Tests
```

### Test Conventions

- Use xUnit for all tests
- Name tests: `MethodName_Scenario_ExpectedBehavior`
- Use in-memory database for repository tests
- Target ≥80% coverage for core services

## Code Style

- Follow `.editorconfig` rules
- Run `dotnet format` before committing
- Use XML documentation for public APIs
- Keep methods focused and concise

## Adding New Features

### 1. Add a new entity

1. Create entity class in `src/Domain/Entities/`
2. Add DbSet to `ApplicationDbContext`
3. Configure entity in `OnModelCreating`
4. Create migration

### 2. Add a new service

1. Create interface in `src/Application/Interfaces/`
2. Create implementation in `src/Application/Services/`
3. Register in `src/Web/Program.cs`
4. Add unit tests

### 3. Add a new page

1. Create Razor component in `src/Web/Components/Pages/`
2. Add `@page` directive with route
3. Inject required services
4. Add navigation link in `NavMenu.razor`

## Troubleshooting

### Database Issues

If you encounter database errors:

1. Delete `src/Web/App_Data/local.db`
2. Run `dotnet ef database update`

### Build Errors

1. Ensure .NET 10 SDK is installed
2. Run `dotnet restore`
3. Check for package version conflicts

### Test Failures

1. Run tests individually to isolate failures
2. Check database state for repository tests
3. Ensure test data fixtures are present

## Troubleshooting: `dotnet ef database update` failures

If the command

```
dotnet ef database update --project src/Infrastructure --startup-project src/Web
```

fails, follow these checks in order:

1. Tool availability

   - If you use a tool manifest: dotnet tool restore
   - Or install globally: dotnet tool install --global dotnet-ef

2. Run from solution root and enable verbose output

   - dotnet build src/Web
   - dotnet ef database update --project src/Infrastructure --startup-project src/Web --verbose
   - The --verbose output often shows why the design-time host or DbContext could not be created.

3. Ensure startup project is executable and builds

   - The startup project (src/Web) must be runnable (e.g., a Blazor Server app) and reference the Infrastructure assembly (directly or via solution dependency).
   - Run dotnet build src/Web and resolve any build errors first.

4. Ensure EF Design package and references

   - Add Microsoft.EntityFrameworkCore.Design to src/Infrastructure if it's missing:
     dotnet add src/Infrastructure package Microsoft.EntityFrameworkCore.Design

5. Design-time DbContext creation failures

   - Common error: "Unable to create an object of type 'ApplicationDbContext'. For the different patterns of creating DbContext..."
   - Solutions:
     a) Add a design-time factory class in src/Infrastructure implementing IDesignTimeDbContextFactory<ApplicationDbContext>.
     b) Or pass --context YourNamespace.ApplicationDbContext to the dotnet ef command.

   Example design-time factory (add to src/Infrastructure):

   ```csharp
   // example: src/Infrastructure/DesignTimeDbContextFactory.cs
   using Microsoft.EntityFrameworkCore;
   using Microsoft.EntityFrameworkCore.Design;
   // using YourNamespace.Infrastructure; // adjust namespace
   // using YourNamespace.Domain; // adjust as needed

   public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
   {
       public ApplicationDbContext CreateDbContext(string[] args)
       {
           var builder = new DbContextOptionsBuilder<ApplicationDbContext>();
           // Use the same connection string you use in Program.cs
           var conn = "Data Source=App_Data/local.db";
           builder.UseSqlite(conn, b => b.MigrationsAssembly("Infrastructure"));
           return new ApplicationDbContext(builder.Options);
       }
   }
   ```

6. Migrations assembly mismatch

   - If migrations are applied from a different assembly, ensure migrations assembly is set:
     options.UseSqlite(conn, b => b.MigrationsAssembly("Infrastructure"));

7. Alternative: run update using the Web project as startup and pointing to migrations assembly

   - dotnet ef database update --startup-project src/Web --project src/Infrastructure --context ApplicationDbContext --verbose

8. If you still see errors, copy the full --verbose output and inspect the first exception stack frame — common causes are missing config at design-time, missing packages, or build failures.

## Contributing

1. Create a feature branch: `feature/task-number-description`
2. Make changes following code style guidelines
3. Add tests for new functionality
4. Run `dotnet format` and `dotnet test`
5. Submit a pull request

See `CONTRIBUTING.md` for detailed guidelines.
