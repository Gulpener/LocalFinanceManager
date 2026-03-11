# Local Finance Manager

![CI](https://github.com/Gulpener/LocalFinanceManager/actions/workflows/ci.yml/badge.svg)

A personal finance management application built with Blazor Server and .NET 10, featuring budget planning, account management, transaction import, and ML-powered categorization.

## Features

- **Account Management**: Create and manage multiple financial accounts with IBAN validation
- **Budget Planning**: Create yearly budget plans with category-based monthly allocations
- **Transaction Import**: Import transactions from CSV/OFX/MT940 formats
- **Auto-Categorization**: ML.NET-powered automatic transaction categorization
- **Budget Tracking**: Track spending against budget allocations with visual indicators
- **Soft-Delete**: Archive accounts, budget plans, and transactions instead of permanent deletion

## Keyboard Shortcuts

The transaction workflow supports keyboard-first navigation for accessibility and power users.

- `?` — Open keyboard shortcut help
- `/` — Focus filter input
- `Esc` — Close active modal
- `Tab` — Navigate through modal form controls
- `Enter` — Submit when save/assign button is focused
- `Space` — Toggle selected transaction checkbox
- `Ctrl+A` / `Cmd+A` — Select all visible transactions
- `Ctrl+D` / `Cmd+D` — Deselect all visible transactions
- `Arrow Up/Down` — Navigate focused transaction row
- `Home` / `End` — Jump to first/last transaction row

On touch devices, keyboard shortcuts are disabled and the help modal shows touch gestures instead.

## Technologies

- **.NET 10** (Blazor Server)
- **Entity Framework Core** with PostgreSQL (Supabase-compatible)
- **ML.NET** for machine learning
- **FluentValidation** for input validation
- **Playwright** for end-to-end testing
- **NUnit/xUnit** for unit and integration testing

## Security

- Use the secure coding checklist during development and review: [docs/Security-Checklist.md](docs/Security-Checklist.md)

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

#### Supabase & User Secrets Setup (Single Project)

Use one Supabase project for the application. E2E tests run with mocked auth by default, so a second Supabase test project is not required.

1. Create Supabase project

- Open https://app.supabase.com and create a project.
- Wait until provisioning completes.

2. Enable email authentication

- Go to **Authentication → Providers → Email**.
- Enable **Email** provider.
- Enable **Confirm email**.
- Save changes.

3. Verify email templates

- Go to **Authentication → Email Templates**.
- Confirm templates exist for **Confirm signup** and **Reset password**.

4. Copy API values

- Go to **Settings → API**.
- Copy:
  - Project URL
  - anon/public key
  - JWT secret

5. Configure local user secrets

```powershell
dotnet user-secrets init --project LocalFinanceManager
dotnet user-secrets set "Supabase:Url" "https://<project-ref>.supabase.co" --project LocalFinanceManager
dotnet user-secrets set "Supabase:Key" "<anon-key>" --project LocalFinanceManager
dotnet user-secrets set "Supabase:JwtSecret" "<jwt-secret>" --project LocalFinanceManager
dotnet user-secrets list --project LocalFinanceManager
```

6. Configure GitHub Actions secrets

- Repository → **Settings → Secrets and variables → Actions**.
- Add:
  - `SUPABASE_URL`
  - `SUPABASE_KEY`
  - `JWT_SECRET`

7. Validate local setup

```powershell
dotnet build LocalFinanceManager.sln
dotnet test tests/LocalFinanceManager.Tests/
```

If auth-related configuration fails, verify the secret names and values with:

```powershell
dotnet user-secrets list --project LocalFinanceManager
```

#### Database Configuration

The application targets PostgreSQL (Supabase-compatible) for runtime and production.

**Development:**

- Database: Local PostgreSQL instance (for example `localhost:5432`)
- Seed data: Automatically loaded on first run in Development

**Production:**

- Database: Supabase PostgreSQL
- Seed data: Not loaded (manual data entry or migration script)
- Schema updates: Forward-only EF Core migrations (no production reset/recreate)

**Admin Settings:**

- View current database configuration at `/admin/settings`
- Shows environment, database path, file size, migrations, and seed data status

**Connection String Configuration:**

Database configuration is stored in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "Default": "Host=localhost;Port=5432;Database=localfinancemanager;Username=postgres;Password=postgres"
  }
}
```

Supabase-compatible format:

```json
{
  "ConnectionStrings": {
    "Default": "Host=db.xxx.supabase.co;Database=postgres;Username=postgres;Password=xxx;SSL Mode=Require;Trust Server Certificate=true"
  }
}
```

**First-Time Supabase Database Provisioning (No Existing PostgreSQL Database):**

1. Create a new Supabase project and wait until the PostgreSQL instance is ready.
2. In Supabase Dashboard, copy database host, port, database name, username, and password.
3. Store runtime DB connection in user-secrets (never commit credentials):

```powershell
dotnet user-secrets init --project LocalFinanceManager
dotnet user-secrets set "ConnectionStrings:Default" "Host=db.xxx.supabase.co;Database=postgres;Username=postgres;Password=xxx;SSL Mode=Require;Trust Server Certificate=true" --project LocalFinanceManager
dotnet user-secrets list --project LocalFinanceManager
```

4. Start the app once so startup migrations run against the empty Supabase database.
5. Verify schema creation in Supabase and confirm health endpoint + login/account/transaction smoke flows.

**Environment Variable Override:**

You can override the connection string with environment variables:

```powershell
# PowerShell (Windows)
$env:ASPNETCORE_ConnectionStrings__Default = "Host=localhost;Port=5432;Database=localfinancemanager;Username=postgres;Password=postgres"
dotnet run

# Bash (Linux/macOS)
export ASPNETCORE_ConnectionStrings__Default="Host=localhost;Port=5432;Database=localfinancemanager;Username=postgres;Password=postgres"
dotnet run
```

**Switching Environments:**

```powershell
# Development (default)
dotnet run

# Production
$env:ASPNETCORE_ENVIRONMENT="Production"; dotnet run
```

#### Auto-Apply Runtime Settings

- `POST /api/automation/settings` persists auto-apply settings to the `AppSettings` table.
- The auto-apply background worker reads these persisted values at runtime (enabled flag, confidence threshold, interval, account filter, excluded categories).
- Settings are cached in memory using `Caching` options (`AbsoluteExpirationMinutes` / `SlidingExpirationMinutes`) and cache is invalidated when settings are saved.
- If no `AppSettings` record exists, the worker falls back to `AutomationOptions` defaults from configuration.

**⚠️ Important:** Never commit secrets (database passwords, Supabase credentials) to source control.

### Database Lifecycle

- Runtime schema changes are applied automatically at startup via EF Core migrations.
- Development seeding runs only in Development environment.
- Production resets/recreate workflows are out of bounds; use forward-only migrations.

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

E2E tests use NUnit + Playwright against a PostgreSQL-backed application host:

- For E2E database initialization and migration behavior, see [tests/LocalFinanceManager.E2E/E2E_INFRASTRUCTURE.md](tests/LocalFinanceManager.E2E/E2E_INFRASTRUCTURE.md#database-migration-strategy-e2e).
- For metrics/stat cards that update asynchronously after user actions, avoid immediate single-shot assertions. Prefer a short polling wait (e.g., up to 5s with 100ms interval) and assert once the expected value appears.

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

### Wrong Database Connection

If you're seeing unexpected data or an empty database:

1. Check which environment you're running:

   ```powershell
   echo $env:ASPNETCORE_ENVIRONMENT
   ```

2. Navigate to `/admin/settings` to verify:
   - Current environment (Development/Production)

- Active database provider/connection information
- Seed data status

3. Verify connection string target:

- Development: local PostgreSQL instance
- Production: Supabase PostgreSQL

4. Switch environment explicitly:
   ```powershell
   $env:ASPNETCORE_ENVIRONMENT="Development"; dotnet run
   ```

### Database Target Validation

Confirm the resolved `ASPNETCORE_ConnectionStrings__Default` value and verify the target host/database match your intended environment.

### Database Connectivity Errors

For local PostgreSQL failures, verify PostgreSQL is running and credentials in `ConnectionStrings:Default` are correct.

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

We welcome contributions! Please see our [Contributing Guide](CONTRIBUTING.md) for details on:

- Branching strategy and workflow
- Pull request process and requirements
- Code standards and testing requirements
- Development setup

For technical implementation details, refer to [`docs/Implementation-Guidelines.md`](docs/Implementation-Guidelines.md).

## License

[Specify your license here]

## Support

For issues and questions, please open an issue on the repository.
