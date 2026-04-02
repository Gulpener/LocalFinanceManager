# Post-MVP-7: Migrate to Supabase PostgreSQL

## Objective

Migrate runtime and production persistence from SQLite to PostgreSQL (Supabase-compatible) while keeping unit tests fast and reliable.

This story prioritizes production parity in runtime and E2E tests, while keeping integration tests fast and deterministic on SQLite, without introducing high-risk architectural changes to tenant isolation in the same step.

Production rollout is **greenfield one-time**: create a new PostgreSQL production database for initial go-live. No legacy SQLite data migration is required in this story.

After go-live, production database recreation/drop is out of bounds; all further schema evolution must be done via forward-only EF migrations.

## Scope Decisions (Locked)

- **Provider matrix**
  - Unit tests: in-memory SQLite (unchanged for speed)
  - Integration tests: in-memory SQLite
  - E2E tests: PostgreSQL-backed test host
  - Runtime/Production: PostgreSQL (Supabase connection compatible)
- **Tenant isolation strategy**
  - Keep **repository-level tenant filtering** using `IUserContext` in this story
  - Do **not** introduce global EF `HasQueryFilter` in this story
- **Migration strategy**
  - Create a **PostgreSQL baseline migration** for a new database
  - No SQLite-to-PostgreSQL data transfer is in scope
  - After initial deployment, use non-destructive forward migrations only (no recreate/reset of production DB)
- **CI database strategy**
  - Use **ephemeral PostgreSQL service container** in CI for E2E PostgreSQL-backed host validation

## Requirements

- Replace runtime EF provider with `Npgsql.EntityFrameworkCore.PostgreSQL`
- Keep automatic migrations at app startup via `Database.MigrateAsync()`
- Integration/E2E tests must never run against production Supabase database
- Do not implement production data import/migration from existing SQLite files
- Do not rely on production database recreation after initial rollout
- Update EF model configuration for PostgreSQL compatibility (`timestamp`, `jsonb` where applicable)
- Preserve repository-level soft-delete and tenant filtering behavior
- Update connection string configuration for Supabase and local PostgreSQL
- Add database health check endpoint for PostgreSQL connectivity
- Update docs and CI to reflect provider matrix

## Implementation Tasks

### Copilot Tasks (Implementation)

- [ ] Update NuGet packages (runtime project):
  - Remove `Microsoft.EntityFrameworkCore.Sqlite`
  - Add `Npgsql.EntityFrameworkCore.PostgreSQL`
- [ ] Update DB provider wiring in `Program.cs` to use `UseNpgsql(connectionString)`
- [ ] Update `AppDbContext` provider-specific configuration:
  - Replace SQLite-specific defaults/functions (`datetime('now')` etc.)
  - Ensure decimal/precision mappings remain correct
  - Configure JSON properties to use PostgreSQL-compatible mapping (`jsonb` where intended)
- [ ] Keep and validate repository tenant isolation:
  - Ensure all repository queries remain scoped by `IUserContext` user id
  - Ensure `.Where(x => !x.IsArchived)` remains consistently applied through repository methods
- [ ] Create PostgreSQL baseline migration for current model
- [ ] Ensure initialization path assumes empty PostgreSQL database (fresh schema create via migrations)
- [ ] Document post-go-live migration policy: forward-only migrations, no production reset/drop workflow
- [ ] Validate startup migration flow and development seed behavior against PostgreSQL
- [ ] Add/enable health check endpoint for DB connectivity
- [ ] Keep integration test infrastructure on in-memory SQLite for speed and determinism
- [ ] Update E2E `TestWebApplicationFactory` and reset strategy to be PostgreSQL-compatible and deterministic
- [ ] Update CI workflow to provision PostgreSQL service container for E2E PostgreSQL-backed host execution
- [ ] Add explicit guard in CI/test configuration to prevent tests from targeting production Supabase connection strings
- [ ] Update developer documentation (`README.md`, `CONTRIBUTING.md`) with local PostgreSQL setup and env vars

### User Tasks (Operations & Configuration)

- [ ] Create and configure the single Supabase production project and confirm PostgreSQL readiness
- [ ] Retrieve authoritative connection details from Supabase Dashboard/Project Settings
- [ ] Configure local secrets (`ConnectionStrings:Default`) via user-secrets or environment variables
- [ ] Configure CI/local test execution to use non-Supabase PostgreSQL targets (service container/local instance), never production Supabase credentials
- [ ] Validate first-time greenfield provisioning runbook on an empty Supabase PostgreSQL database
- [ ] Execute rollout go/no-go approvals and pre-deploy backup/snapshot verification

## JSON Persistence Strategy

Review each JSON-like property and explicitly choose storage mapping:

- `BudgetLine.MonthlyAmountsJson` / `MonthlyAmounts`
- `AppSettings.AccountIdsJson` / `ExcludedCategoryIdsJson`
- `Transaction.OriginalImport`
- `TransactionAudit.BeforeState` / `AfterState`
- `MLModel.Metrics`

For each property, define whether it remains serialized string or is mapped to PostgreSQL `jsonb`, and add round-trip tests accordingly.

## Local Development

- Local development must support PostgreSQL connection through:
  - `ConnectionStrings:Default` in local config
  - override via environment variable `ASPNETCORE_ConnectionStrings__Default`
- App startup must:
  - apply migrations automatically
  - seed only in Development environment
  - be idempotent across restarts

### Example Connection String

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

## Supabase Provisioning (No Existing PostgreSQL Database)

1. Create the Supabase production project and wait until the PostgreSQL instance reports ready.
2. In Supabase Dashboard, open project database connection details and capture host, port, database, username, and password.
3. Compose the runtime connection string using Supabase-compatible settings (`SSL Mode=Require;Trust Server Certificate=true`) and confirm it targets the correct project.
4. Store the connection string in local developer secrets and in CI/host secret stores; do not place credentials in `appsettings*.json` committed to source control.
5. Start the application with the configured secret so startup bootstrap runs `Database.MigrateAsync()` against the empty Supabase PostgreSQL database.
6. Verify migration bootstrap completed by checking application logs and confirming expected schema objects/tables exist in Supabase.
7. Run health endpoint and smoke checks (login + account/transaction flows) to validate connectivity and baseline runtime behavior.

Note: Integration and E2E tests must run against non-production databases (in-memory SQLite for integration, PostgreSQL container/local instance for E2E), not against the Supabase production database.

## CI and E2E Requirements

- CI provisions PostgreSQL service container per run
- CI applies migrations before E2E execution against PostgreSQL-backed host
- Integration tests run against in-memory SQLite test database
- E2E tests run against PostgreSQL-backed application host
- Test environment must use dedicated non-production database/credentials only
- Test data reset between E2E fixtures is deterministic and provider-safe (no SQLite-only PRAGMA assumptions)

## Rollout Plan (Production)

Use this sequence for first PostgreSQL go-live and every subsequent release:

1. Validate migration on staging with production-like dataset
2. Verify pre-deploy backup/snapshot is available and restorable
3. Deploy application and apply forward-only migration in production
4. Run post-deploy smoke checks (health endpoint, login, account/transaction flows)
5. Monitor logs/metrics for migration and connectivity errors during stabilization window

## Rollout Gates (Go/No-Go)

- **Go** only when staging migration succeeds, health checks are green, and critical flows pass
- **No-Go** if migration has destructive/reset behavior, health check is degraded, or tenant isolation checks fail
- **Rollback strategy:** roll-forward remediation only (hotfix migration/app patch); no production DB recreate/drop

## Operational Readiness

- Store Supabase credentials in environment/secret store only (never in source control)
- Enforce TLS/SSL for production connections (`SSL Mode=Require`)
- Use least-privilege database credentials for runtime app access
- Define credential rotation procedure and ownership before go-live

## Release Guardrails (Post Go-Live)

Use this checklist for every production release after initial PostgreSQL go-live:

- [ ] Migration is forward-only and non-destructive for production data
- [ ] Migration script does not include production reset/drop/recreate operations
- [ ] Pre-deploy database backup/snapshot has been verified
- [ ] Migration tested on staging with production-like data volume
- [ ] Roll-forward remediation plan is documented (no rollback via DB recreation)
- [ ] Health check endpoint returns healthy after migration in staging

## Required CLI Commands

```powershell
# Runtime provider switch
dotnet remove LocalFinanceManager/LocalFinanceManager.csproj package Microsoft.EntityFrameworkCore.Sqlite
dotnet add LocalFinanceManager/LocalFinanceManager.csproj package Npgsql.EntityFrameworkCore.PostgreSQL

# Create PostgreSQL baseline migration
dotnet ef migrations add PostgreSqlBaseline --project LocalFinanceManager/LocalFinanceManager.csproj

# Apply migration locally
dotnet ef database update --project LocalFinanceManager/LocalFinanceManager.csproj

# Initialize and manage local development secrets
dotnet user-secrets init --project LocalFinanceManager/LocalFinanceManager.csproj
dotnet user-secrets set "ConnectionStrings:Default" "Host=db.xxx.supabase.co;Database=postgres;Username=postgres;Password=xxx;SSL Mode=Require;Trust Server Certificate=true" --project LocalFinanceManager/LocalFinanceManager.csproj
dotnet user-secrets list --project LocalFinanceManager/LocalFinanceManager.csproj

# Optional PowerShell env var override (session-scoped)
$env:ASPNETCORE_ConnectionStrings__Default="Host=localhost;Port=5432;Database=localfinancemanager;Username=postgres;Password=postgres"

# Run tests by layer
dotnet test tests/LocalFinanceManager.Tests/LocalFinanceManager.Tests.csproj
dotnet test tests/LocalFinanceManager.E2E/LocalFinanceManager.E2E.csproj
```

## Testing

- Unit tests remain on in-memory SQLite for speed
- Integration tests verify:
  - repository behavior against in-memory SQLite
  - tenant isolation across at least two users
  - JSON round-trip behavior for mapped fields
- E2E tests verify:
  - key application flows on PostgreSQL backend
  - PostgreSQL schema creation/migrations via startup migration flow
  - deterministic DB reset behavior
  - no provider-specific SQL assumptions leak into test setup

## Success Criteria

- Application starts and runs on PostgreSQL without runtime provider errors
- New empty PostgreSQL database can be provisioned and initialized fully from migrations
- Automatic migrations apply successfully at startup
- Post-go-live releases can update schema without recreating production database
- Repository-level tenant isolation prevents cross-user data visibility
- JSON fields persist and load correctly under chosen mappings
- Integration tests pass on in-memory SQLite and E2E tests pass on PostgreSQL-backed host in CI
- CI/test configuration prevents accidental test execution against production Supabase database
- Production rollout checklist and go/no-go gates are documented and reproducible
- Provisioning of a brand-new Supabase project is documented and reproducible via runbook plus secrets setup workflow
- Local developer setup is documented and reproducible
