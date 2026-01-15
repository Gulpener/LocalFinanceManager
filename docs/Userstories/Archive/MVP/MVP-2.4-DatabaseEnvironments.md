# MVP 2.4 — Database Environment Configuratie (Development vs Production)

Doel

- Gescheiden database configuratie voor Development en Production omgevingen zodat productie-data nooit wordt overschreven tijdens development.
- Database bestanden (.db) worden uitgesloten van versie controle om gevoelige data en merge-conflicten te voorkomen.
- Admin Settings page toont huidige database configuratie en environment informatie.

Acceptatiecriteria

- Development gebruikt `localfinancemanager.dev.db`, Production gebruikt `localfinancemanager.db`.
- `appsettings.Development.json` bevat Development-specifieke connection string.
- `.gitignore` bevat exclusies voor alle SQLite database bestanden (`*.db`, `*.db-shm`, `*.db-wal`).
- README.md documenteert database setup voor beide omgevingen.
- `RecreateDatabase` flag werkt alleen in Development (safety voor Production).
- **Admin Settings page (`/admin/settings`) toont read-only systeem informatie:**
  - Huidige environment (Development/Production)
  - Database bestandspad
  - Database bestandsgrootte
  - Laatst uitgevoerde migratie
  - Seed data status (geladen/niet geladen)

Data model

- **Geen nieuwe entities:** Bestaande entities blijven onveranderd.
- **Geen database wijzigingen:** Admin Settings page leest configuratie en file system informatie.

Business regels

- **Development omgeving:**
  - Database bestand: `localfinancemanager.dev.db` (in project root)
  - Seed data automatisch geladen bij eerste run
  - `RecreateDatabase=true` flag toegestaan voor fresh database
  - Database mag gedelete worden zonder impact op productie
- **Production omgeving:**
  - Database bestand: `localfinancemanager.db` (in project root of configureerbaar via environment variable)
  - Geen automatische seed data (handmatig via admin interface of script)
  - `RecreateDatabase=true` flag wordt genegeerd (safety measure)
  - Database wordt gemigreerd maar nooit opnieuw aangemaakt
- **Version control:**

  - Database bestanden (.db, .db-shm, .db-wal) nooit committen
  - Connection strings met gevoelige informatie (indien van toepassing) via environment variables
  - Migrations altijd committen (code-first approach)

- **Admin Settings toegang:**
  - Geen authenticatie vereist voor MVP 2.4 (toekomstige uitbreiding: admin role required)
  - Read-only informatie; geen edit functionaliteit
  - Link in NavMenu onder "Admin" dropdown (alleen Development) of altijd zichtbaar

Configuration details

**appsettings.json (Production default):**

```json
{
  "ConnectionStrings": {
    "Default": "Data Source=localfinancemanager.db"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

**appsettings.Development.json (Development override):**

```json
{
  "ConnectionStrings": {
    "Default": "Data Source=localfinancemanager.dev.db"
  },
  "RecreateDatabase": false,
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Information",
      "Microsoft.EntityFrameworkCore.Database.Command": "Information"
    }
  }
}
```

**Environment variable override (optioneel, voor beide omgevingen):**

```bash
# PowerShell
$env:ASPNETCORE_ConnectionStrings__Default = "Data Source=C:\Data\myapp.db"

# Linux/macOS
export ASPNETCORE_ConnectionStrings__Default="Data Source=/var/data/myapp.db"
```

**appsettings.Production.json (optioneel, voor expliciete production settings):**

```json
{
  "ConnectionStrings": {
    "Default": "Data Source=/var/lib/localfinancemanager/localfinancemanager.db"
  },
  "RecreateDatabase": false,
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

Program.cs updates

**Safety check voor RecreateDatabase in Production:**

```csharp
// Apply migrations and seed data
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    // Recreate database if environment variable is set (Development only)
    var recreateDb = app.Configuration.GetValue<bool>("RecreateDatabase");
    if (recreateDb)
    {
        if (app.Environment.IsDevelopment())
        {
            await context.Database.EnsureDeletedAsync();
            app.Logger.LogInformation("Database deleted due to RecreateDatabase flag");
        }
        else
        {
            app.Logger.LogWarning("RecreateDatabase flag ignored in non-Development environment for safety");
        }
    }

    await context.Database.MigrateAsync();

    // Seed only in Development
    if (app.Environment.IsDevelopment())
    {
        await context.SeedAsync();
    }
}
```

.gitignore updates

**Toe te voegen aan .gitignore (na SQL Server files sectie):**

```gitignore
# SQLite database files (local development & production)
*.db
*.db-shm
*.db-wal
*.sqlite
*.sqlite3

# Exclude all database files in project root
localfinancemanager.db
localfinancemanager.dev.db

# Allow test database fixtures (pre-seeded for E2E tests)
!tests/**/fixtures/**/*.db
!tests/**/fixtures/**/*.sqlite
```

**Rationale:**

- `*.db` — SQLite database bestand
- `*.db-shm` — SQLite shared memory bestand (temporary)
- `*.db-wal` — SQLite write-ahead log bestand (temporary)
- Fixture databases in test projects mogen wel gecommit worden (kleine, pre-seeded data voor reproduceerbare tests)

UI aanwijzingen (Blazor)

**Nieuwe page: Components/Pages/Admin/Settings.razor**

**URL:** `/admin/settings`

**Layout:**

```razor
@page "/admin/settings"
@inject IConfiguration Configuration
@inject IWebHostEnvironment Environment
@inject AppDbContext DbContext

<PageTitle>Admin Settings</PageTitle>

<h3>Systeem Instellingen</h3>

<div class="card">
    <div class="card-header">
        <h5>Environment Informatie</h5>
    </div>
    <div class="card-body">
        <table class="table table-borderless">
            <tbody>
                <tr>
                    <th scope="row">Environment:</th>
                    <td>
                        <span class="badge @(_environmentBadgeClass)">@Environment.EnvironmentName</span>
                    </td>
                </tr>
                <tr>
                    <th scope="row">Application Name:</th>
                    <td>@Environment.ApplicationName</td>
                </tr>
                <tr>
                    <th scope="row">Content Root Path:</th>
                    <td><code>@Environment.ContentRootPath</code></td>
                </tr>
            </tbody>
        </table>
    </div>
</div>

<div class="card mt-3">
    <div class="card-header">
        <h5>Database Configuratie</h5>
    </div>
    <div class="card-body">
        <table class="table table-borderless">
            <tbody>
                <tr>
                    <th scope="row">Connection String:</th>
                    <td><code>@_connectionString</code></td>
                </tr>
                <tr>
                    <th scope="row">Database Pad:</th>
                    <td><code>@_databasePath</code></td>
                </tr>
                <tr>
                    <th scope="row">Database Bestaat:</th>
                    <td>
                        @if (_databaseExists)
                        {
                            <span class="badge bg-success">Ja</span>
                        }
                        else
                        {
                            <span class="badge bg-warning">Nee</span>
                        }
                    </td>
                </tr>
                @if (_databaseExists)
                {
                    <tr>
                        <th scope="row">Bestandsgrootte:</th>
                        <td>@_databaseSize</td>
                    </tr>
                    <tr>
                        <th scope="row">Laatst Gewijzigd:</th>
                        <td>@_lastModified?.ToString("yyyy-MM-dd HH:mm:ss")</td>
                    </tr>
                }
                <tr>
                    <th scope="row">Laatst Uitgevoerde Migratie:</th>
                    <td><code>@_lastMigration</code></td>
                </tr>
                <tr>
                    <th scope="row">Pending Migrations:</th>
                    <td>
                        @if (_pendingMigrations?.Any() == true)
                        {
                            <span class="badge bg-warning">@_pendingMigrations.Count() pending</span>
                            <ul class="mt-2">
                                @foreach (var migration in _pendingMigrations)
                                {
                                    <li><code>@migration</code></li>
                                }
                            </ul>
                        }
                        else
                        {
                            <span class="badge bg-success">Geen</span>
                        }
                    </td>
                </tr>
            </tbody>
        </table>
    </div>
</div>

<div class="card mt-3">
    <div class="card-header">
        <h5>Seed Data Status</h5>
    </div>
    <div class="card-body">
        <table class="table table-borderless">
            <tbody>
                <tr>
                    <th scope="row">Accounts:</th>
                    <td>@_accountCount records</td>
                </tr>
                <tr>
                    <th scope="row">Categories:</th>
                    <td>@_categoryCount records</td>
                </tr>
                <tr>
                    <th scope="row">Budget Plans:</th>
                    <td>@_budgetPlanCount records</td>
                </tr>
                <tr>
                    <th scope="row">Seed Data Geladen:</th>
                    <td>
                        @if (_seedDataLoaded)
                        {
                            <span class="badge bg-success">Ja</span>
                        }
                        else
                        {
                            <span class="badge bg-secondary">Nee (Production of handmatig geleegd)</span>
                        }
                    </td>
                </tr>
            </tbody>
        </table>
    </div>
</div>

@code {
    private string? _connectionString;
    private string? _databasePath;
    private bool _databaseExists;
    private string? _databaseSize;
    private DateTime? _lastModified;
    private string? _lastMigration;
    private IEnumerable<string>? _pendingMigrations;
    private int _accountCount;
    private int _categoryCount;
    private int _budgetPlanCount;
    private bool _seedDataLoaded;
    private string _environmentBadgeClass = "bg-secondary";

    protected override async Task OnInitializedAsync()
    {
        // Environment badge color
        _environmentBadgeClass = Environment.IsDevelopment() ? "bg-primary" : "bg-danger";

        // Connection string
        _connectionString = Configuration.GetConnectionString("Default");

        // Parse database path from connection string
        if (!string.IsNullOrEmpty(_connectionString))
        {
            var match = System.Text.RegularExpressions.Regex.Match(
                _connectionString,
                @"Data Source=([^;]+)",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            if (match.Success)
            {
                var relativePath = match.Groups[1].Value;
                _databasePath = Path.IsPathRooted(relativePath)
                    ? relativePath
                    : Path.Combine(Environment.ContentRootPath, relativePath);

                // Check if database file exists
                if (File.Exists(_databasePath))
                {
                    _databaseExists = true;
                    var fileInfo = new FileInfo(_databasePath);
                    _databaseSize = FormatBytes(fileInfo.Length);
                    _lastModified = fileInfo.LastWriteTime;
                }
            }
        }

        // Database migrations info
        var appliedMigrations = await DbContext.Database.GetAppliedMigrationsAsync();
        _lastMigration = appliedMigrations.LastOrDefault() ?? "Geen";
        _pendingMigrations = await DbContext.Database.GetPendingMigrationsAsync();

        // Seed data status
        _accountCount = await DbContext.Accounts.CountAsync();
        _categoryCount = await DbContext.Categories.CountAsync();
        _budgetPlanCount = await DbContext.BudgetPlans.CountAsync();
        _seedDataLoaded = _accountCount > 0 && _categoryCount > 0;
    }

    private string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}
```

**NavMenu update (Components/Layout/NavMenu.razor):**

```razor
<!-- Voeg toe na bestaande menu items -->
<div class="nav-item px-3">
    <NavLink class="nav-link" href="admin/settings">
        <span class="bi bi-gear-fill" aria-hidden="true"></span> Admin Settings
    </NavLink>
</div>
```

**Security note:** Voor MVP 2.4 is geen authenticatie vereist. Toekomstige uitbreiding: `@attribute [Authorize(Roles = "Admin")]` toevoegen.

Deployment/env

**Development workflow:**

```powershell
# First run: creates localfinancemanager.dev.db + seed data
cd LocalFinanceManager
dotnet run

# Navigate to http://localhost:5244/admin/settings
# Verify: Environment = Development, Database path = localfinancemanager.dev.db

# Recreate database (fresh start)
$env:RecreateDatabase="true"; dotnet run
# or edit appsettings.Development.json: "RecreateDatabase": true
```

**Production deployment:**

```bash
# Set environment to Production
export ASPNETCORE_ENVIRONMENT=Production

# Optional: override database path
export ASPNETCORE_ConnectionStrings__Default="Data Source=/var/lib/myapp/localfinancemanager.db"

# Run application (uses localfinancemanager.db, no seed data)
dotnet LocalFinanceManager.dll

# Navigate to http://yourapp/admin/settings
# Verify: Environment = Production, Database path = localfinancemanager.db
```

**Docker deployment (optioneel):**

```dockerfile
# Example: mount database volume for persistence
docker run -d \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e ASPNETCORE_ConnectionStrings__Default="Data Source=/app/data/localfinancemanager.db" \
  -v /host/data:/app/data \
  localfinancemanager:latest
```

Edgecases

- **Switching environments lokaal:** Bij wisselen van `ASPNETCORE_ENVIRONMENT` tussen Development/Production, wisselt automatisch de database. Data in de andere database blijft intact. Admin Settings page toont huidige environment en database pad.
- **Accidental RecreateDatabase in Production:** Flag wordt genegeerd; log warning maar delete geen data.
- **Missing database file:** EF Core migrations creëren automatisch nieuwe database bij startup (geen handmatige stappen nodig). Admin Settings page toont "Database Bestaat: Nee" voor refresh.
- **Database lock (SQLite):** Slechts één process tegelijk kan schrijven; bij concurrency errors gebruik connection string parameter: `Data Source=mydb.db;Mode=ReadWriteCreate;Cache=Shared`.
- **Backup strategy:** Production database moet gebackupped worden buiten git (scheduled backup script of database snapshot mechanisme).
- **Pending migrations op Admin Settings page:** Indien pending migrations zichtbaar, herstart applicatie om migrations toe te passen (automatic migrations bij startup).

Tests

**Unit tests** (`LocalFinanceManager.Tests`):

- Configuration test: verify Development environment loads `.dev.db` connection string
- Configuration test: verify Production environment loads `.db` connection string
- Safety test: `RecreateDatabase=true` in Production → database niet gedelete
- Database path parsing test: extract correct path from connection string

**Integration tests** (`LocalFinanceManager.Tests` met in-memory SQLite):

- Geen wijzigingen nodig; tests gebruiken reeds in-memory database (`:memory:` connection string)

**E2E tests** (`LocalFinanceManager.E2E` met Playwright):

- E2E tests gebruiken dedicated test database: `localfinancemanager.test.db` (via `WebApplicationFactory` configuratie override)
- Test database wordt opnieuw aangemaakt voor elke test run (clean slate)
- **Update E2E test setup:**
  ```csharp
  // In PlaywrightFixture.cs or WebApplicationFactory setup
  protected override void ConfigureWebHost(IWebHostBuilder builder)
  {
      builder.ConfigureAppConfiguration((context, config) =>
      {
          config.AddInMemoryCollection(new Dictionary<string, string>
          {
              ["ConnectionStrings:Default"] = "Data Source=localfinancemanager.test.db",
              ["RecreateDatabase"] = "true" // Always fresh database for E2E tests
          });
      });
  }
  ```

**E2E test voor Admin Settings page:**

- **Test:** View Admin Settings page
  - Navigate `/admin/settings` → Verify page loads → Verify Environment badge visible → Verify Database path displayed → Verify Seed data counts shown
- **Test:** Database info accuracy
  - Navigate `/admin/settings` → Verify database file path matches test configuration → Verify "Database Bestaat: Ja" badge shown → Verify last migration name displayed

Documentation updates

**README.md toevoegingen (Configuration sectie):**

````markdown
### Database Configuration

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

**Environment Variable Override:**

```powershell
# PowerShell (Windows)
$env:ASPNETCORE_ConnectionStrings__Default = "Data Source=C:\Data\myapp.db"
dotnet run

# Bash (Linux/macOS)
export ASPNETCORE_ConnectionStrings__Default="Data Source=/var/data/myapp.db"
dotnet run
```
````

**Switching Environments:**

```powershell
# Development (default)
dotnet run

# Production
$env:ASPNETCORE_ENVIRONMENT="Production"; dotnet run
```

**⚠️ Important:** Database files (`.db`) are excluded from version control. Never commit database files containing real data.

````

**README.md toevoegingen (Troubleshooting sectie):**

```markdown
### Wrong Database File

If you're seeing unexpected data or an empty database:

1. Check which environment you're running:
   ```powershell
   echo $env:ASPNETCORE_ENVIRONMENT
````

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

- `C:\Users\<user>\source\repos\LocalFinanceManager\LocalFinanceManager\localfinancemanager.dev.db` (Development)
- `C:\Users\<user>\source\repos\LocalFinanceManager\LocalFinanceManager\localfinancemanager.db` (Production)

To verify the exact path, visit `/admin/settings` or set a custom location via environment variable (see Configuration section).

```

Definition of Done

- `appsettings.Development.json` bevat `Data Source=localfinancemanager.dev.db` connection string.
- `appsettings.json` (Production default) bevat `Data Source=localfinancemanager.db`.
- Optioneel: `appsettings.Production.json` aangemaakt voor expliciete production settings.
- `.gitignore` bevat exclusies: `*.db`, `*.db-shm`, `*.db-wal`, met allow voor `tests/**/fixtures/**/*.db`.
- `Program.cs` safety check: `RecreateDatabase` flag genegeerd in non-Development omgevingen (met log warning).
- **Admin Settings page (`Components/Pages/Admin/Settings.razor`) volledig geïmplementeerd:**
  - Toont Environment naam met badge (Development = blue, Production = red)
  - Toont database connection string en parsed pad
  - Toont database bestandsgrootte en laatste wijzigingsdatum
  - Toont laatst uitgevoerde migratie + pending migrations (indien van toepassing)
  - Toont seed data counts (Accounts, Categories, BudgetPlans)
  - NavMenu bevat "Admin Settings" link
- README.md gedocumenteerd: Database Configuration sectie met Development/Production instructies, Admin Settings page usage, environment switching, troubleshooting.
- Unit tests: verify environment-specific connection strings geladen, database path parsing.
- E2E tests: gebruiken `localfinancemanager.test.db` met `RecreateDatabase=true` voor clean slate.
- E2E test: Admin Settings page loads en toont correcte informatie.
- Verificatie: Run app in Development → `localfinancemanager.dev.db` aangemaakt → Navigate `/admin/settings` → verify Environment = Development, database path correct.
- Verificatie: Switch naar Production → `localfinancemanager.db` aangemaakt → Navigate `/admin/settings` → verify Environment = Production.
- Verificatie: Commit `.gitignore` wijzigingen; verify `git status` toont geen `.db` bestanden.
- Geen bestaande database bestanden gecommit in repository (cleanup indien nodig: `git rm --cached *.db`).
```
