# Local Finance Manager

A personal finance management application built with Blazor Server and .NET 10, featuring budget planning, account management, transaction import, and ML-powered categorization.

## Features

- **Account Management**: Create and manage multiple financial accounts with IBAN validation
- **Budget Planning**: Create yearly budget plans with category-based monthly allocations
- **Transaction Import**: Import transactions from CSV/OFX/MT940 formats
- **Auto-Categorization**: ML.NET-powered automatic transaction categorization
- **Budget Tracking**: Track spending against budget allocations with visual indicators
- **Soft-Delete**: Archive accounts, budget plans, and transactions instead of permanent deletion

## Technologies

- **.NET 10** (Blazor Server)
- **Entity Framework Core** with SQLite
- **ML.NET** for machine learning
- **FluentValidation** for input validation
- **Playwright** for end-to-end testing
- **NUnit/xUnit** for unit and integration testing

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- A code editor (Visual Studio 2022, VS Code, or Rider)

## Getting Started

### Clone the Repository

```bash
git clone <repository-url>
cd LocalFinanceManager
```

### Restore Dependencies

```bash
dotnet restore
```

### Run the Application

```bash
cd LocalFinanceManager
dotnet run
```

The application will start and be available at:

- **http://localhost:5244**

The database will be automatically created and migrations applied on first run. Seed data will be loaded in Development environment.

### Configuration

Database configuration is stored in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "Default": "Data Source=localfinancemanager.db"
  }
}
```

You can override this with environment variables:

```bash
$env:ASPNETCORE_ConnectionStrings__Default = "Data Source=mydb.db"
```

### Recreating the Database

By default, the database is persisted between runs. To recreate the database from scratch (Development only), use one of these methods:

**Option 1: Environment variable**

```bash
cd LocalFinanceManager; $env:RecreateDatabase="true"; dotnet run
```

**Option 2: Add to appsettings.Development.json**

```json
{
  "RecreateDatabase": true
}
```

**Option 3: Command-line argument**

```bash
dotnet run --RecreateDatabase=true
```

This will delete the existing database, recreate the schema from migrations, and reseed with fresh development data.

## Running Tests

### Unit & Integration Tests

Unit and integration tests use in-memory SQLite database:

```bash
cd tests/LocalFinanceManager.Tests
dotnet test
```

Run with coverage:

```bash
dotnet test --collect:"XPlat Code Coverage"
```

### End-to-End Tests

E2E tests use NUnit + Playwright and test against a running instance:

```bash
# First time: Install Playwright browsers
pwsh tests/LocalFinanceManager.E2E/bin/Debug/net10.0/playwright.ps1 install

# Run E2E tests
cd tests/LocalFinanceManager.E2E
dotnet test
```

### ML Model Tests

ML tests validate model training and inference:

```bash
cd tests/LocalFinanceManager.ML.Tests
dotnet test
```

### Run All Tests

From the solution root:

```bash
dotnet test
```

## Project Structure

```
LocalFinanceManager/
├── LocalFinanceManager/          # Main Blazor Server application
│   ├── Components/               # Razor components and pages
│   ├── Controllers/              # API controllers
│   ├── Data/                     # DbContext and repositories
│   ├── DTOs/                     # Data transfer objects
│   ├── Models/                   # Domain entities
│   ├── Services/                 # Business logic services
│   └── Migrations/               # EF Core migrations
├── LocalFinanceManager.ML/       # ML.NET model training/inference
├── tests/
│   ├── LocalFinanceManager.Tests/        # Unit & integration tests
│   ├── LocalFinanceManager.E2E/          # Playwright E2E tests
│   └── LocalFinanceManager.ML.Tests/     # ML model tests
└── docs/                         # Documentation
    ├── MVP-*.md                  # MVP specifications
    ├── Implementation-Guidelines.md
    └── TODO.md
```

## Development Workflow

### Adding a New Migration

When you modify entity models, create a migration:

```bash
cd LocalFinanceManager
dotnet ef migrations add <MigrationName>
```

Migrations are automatically applied when the application starts.

### Creating a New Feature

1. Define the feature in a new MVP document in `docs/`
2. Implement domain models in `Models/`
3. Create DTOs and validators in `DTOs/`
4. Implement repository in `Data/Repositories/`
5. Create service in `Services/`
6. Add API controller in `Controllers/`
7. Build Blazor UI in `Components/Pages/`
8. Write unit/integration tests
9. Add E2E tests for user flows

## Key Design Patterns

### BaseEntity & Soft Delete

All entities inherit from `BaseEntity`:

```csharp
public abstract class BaseEntity
{
    public Guid Id { get; set; }
    public byte[] RowVersion { get; set; }  // Optimistic concurrency
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsArchived { get; set; }    // Soft delete
}
```

### Repository Pattern

Repositories encapsulate data access with automatic soft-delete filtering:

```csharp
public interface IRepository<T> where T : BaseEntity
{
    Task<T?> GetByIdAsync(Guid id);
    Task<List<T>> GetActiveAsync();  // Filters !IsArchived
    Task AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task ArchiveAsync(Guid id);      // Soft delete
}
```

### Optimistic Concurrency

Entities use `RowVersion` for concurrency control. Update conflicts return HTTP 409 with current state.

## API Endpoints

### Accounts

- `GET /api/accounts` - List all active accounts
- `GET /api/accounts/{id}` - Get account by ID
- `POST /api/accounts` - Create new account
- `PUT /api/accounts/{id}` - Update account
- `POST /api/accounts/{id}/archive` - Archive account

### Budget Plans

- `GET /api/budgetplans?accountId={id}` - List budget plans for account
- `GET /api/budgetplans/{id}` - Get budget plan with lines
- `POST /api/budgetplans` - Create new budget plan
- `PUT /api/budgetplans/{id}` - Update budget plan
- `POST /api/budgetplans/{id}/archive` - Archive budget plan
- `POST /api/budgetplans/{planId}/lines` - Add budget line
- `PUT /api/budgetplans/{planId}/lines/{lineId}` - Update budget line
- `POST /api/budgetplans/{planId}/lines/{lineId}/archive` - Archive line

### Categories

- `GET /api/categories` - List all categories
- `POST /api/categories` - Create category

## Troubleshooting

### Database Locked Errors

SQLite can have locking issues with concurrent access. Ensure only one instance is running.

### Port Already in Use

If port 5244 is in use, modify `Properties/launchSettings.json` or set:

```bash
$env:ASPNETCORE_URLS = "http://localhost:5000"
```

### Playwright Installation

If E2E tests fail, ensure browsers are installed:

```bash
pwsh tests/LocalFinanceManager.E2E/bin/Debug/net10.0/playwright.ps1 install
```

## Contributing

1. Create a feature branch from `main`
2. Follow the implementation guidelines in `docs/Implementation-Guidelines.md`
3. Write tests for all new features
4. Ensure all tests pass: `dotnet test`
5. Submit a pull request

## License

[Specify your license here]

## Support

For issues and questions, please open an issue on the repository.
