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

#### Database Configuration

The application uses SQLite with separate databases for Development and Production environments.

**Development:**

- Database: `localfinancemanager.dev.db` (project root)
- Seed data: Automatically loaded on first run
- Recreate database: Set `RecreateDatabase=true` in `appsettings.Development.json` or environment variable

**Production:**

- Database: `localfinancemanager.db` (project root or custom path via environment variable)
- Seed data: Not loaded (manual data entry or migration script)
- Recreate database: Not allowed (safety measure)

**Admin Settings:**

- View current database configuration at `/admin/settings`
- Shows environment, database path, file size, migrations, and seed data status

**Connection String Configuration:**

Database configuration is stored in `appsettings.json` (Production default):

```json
{
  "ConnectionStrings": {
    "Default": "Data Source=localfinancemanager.db"
  }
}
```

Development overrides in `appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "Default": "Data Source=localfinancemanager.dev.db"
  }
}
```

**Environment Variable Override:**

You can override the connection string with environment variables:

```powershell
# PowerShell (Windows)
$env:ASPNETCORE_ConnectionStrings__Default = "Data Source=C:\Data\myapp.db"
dotnet run

# Bash (Linux/macOS)
export ASPNETCORE_ConnectionStrings__Default="Data Source=/var/data/myapp.db"
dotnet run
```

**Switching Environments:**

```powershell
# Development (default)
dotnet run

# Production
$env:ASPNETCORE_ENVIRONMENT="Production"; dotnet run
```

**⚠️ Important:** Database files (`.db`, `.db-shm`, `.db-wal`) are excluded from version control. Never commit database files containing real data.

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
    ├── Implementation-Guidelines.md
    ├── MVP/                      # MVP specifications (completed)
    │   ├── MVP-*.md
    │   └── TODO.md
    └── PrdReady/                 # Post-MVP features (documented)
        └── Post-MVP-Notes.md
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

1. Define the feature in a new document in `docs/`
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

### Wrong Database File

If you're seeing unexpected data or an empty database:

1. Check which environment you're running:

   ```powershell
   echo $env:ASPNETCORE_ENVIRONMENT
   ```

2. Navigate to `/admin/settings` to verify:

   - Current environment (Development/Production)
   - Database file path in use
   - Database file existence and size
   - Seed data status

3. Verify database file in use:

   - Development: `localfinancemanager.dev.db`
   - Production: `localfinancemanager.db`

4. Switch environment explicitly:
   ```powershell
   $env:ASPNETCORE_ENVIRONMENT="Development"; dotnet run
   ```

### Database File Location

Database files are stored in the project root by default:

- `LocalFinanceManager/localfinancemanager.dev.db` (Development)
- `LocalFinanceManager/localfinancemanager.db` (Production)

To verify the exact path, visit `/admin/settings` or set a custom location via environment variable (see Configuration section).

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
