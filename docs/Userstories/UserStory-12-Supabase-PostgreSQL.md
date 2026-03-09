# Post-MVP-7: Migrate to Supabase PostgreSQL

## Objective

Migrate runtime and production persistence from SQLite to PostgreSQL (Supabase-compatible) while keeping unit tests fast and reliable.

This story prioritizes production parity in integration and E2E tests, without introducing high-risk architectural changes to tenant isolation in the same step.

Production rollout is **greenfield one-time**: create a new PostgreSQL production database for initial go-live. No legacy SQLite data migration is required in this story.

After go-live, production database recreation/drop is out of bounds; all further schema evolution must be done via forward-only EF migrations.

## Scope Decisions (Locked)

- **Provider matrix**
  - Unit tests: in-memory SQLite (unchanged for speed)
  - Integration tests: PostgreSQL
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
  - Use **ephemeral PostgreSQL service container** in CI as authoritative test database

## Requirements

- Replace runtime EF provider with `Npgsql.EntityFrameworkCore.PostgreSQL`
- Keep automatic migrations at app startup via `Database.MigrateAsync()`
- Do not implement production data import/migration from existing SQLite files
- Do not rely on production database recreation after initial rollout
- Update EF model configuration for PostgreSQL compatibility (`timestamp`, `jsonb` where applicable)
- Preserve repository-level soft-delete and tenant filtering behavior
- Update connection string configuration for Supabase and local PostgreSQL
- Add database health check endpoint for PostgreSQL connectivity
- Update docs and CI to reflect provider matrix

## Implementation Tasks

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
- [ ] Update integration test infrastructure to run on PostgreSQL
- [ ] Update E2E `TestWebApplicationFactory` and reset strategy to be PostgreSQL-compatible and deterministic
- [ ] Update CI workflow to provision PostgreSQL service container and run integration + E2E against it
- [ ] Update developer documentation (`README.md`, `CONTRIBUTING.md`) with local PostgreSQL setup and env vars

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

## CI and E2E Requirements

- CI provisions PostgreSQL service container per run
- CI applies migrations before integration/E2E execution
- Integration tests run against PostgreSQL test database
- E2E tests run against PostgreSQL-backed application host
- Test data reset between E2E fixtures is deterministic and provider-safe (no SQLite-only PRAGMA assumptions)

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

# Run tests by layer
dotnet test tests/LocalFinanceManager.Tests/LocalFinanceManager.Tests.csproj
dotnet test tests/LocalFinanceManager.E2E/LocalFinanceManager.E2E.csproj
```

## Testing

- Unit tests remain on in-memory SQLite for speed
- Integration tests verify:
  - PostgreSQL schema creation/migrations
  - tenant isolation across at least two users
  - JSON round-trip behavior for mapped fields
- E2E tests verify:
  - key application flows on PostgreSQL backend
  - deterministic DB reset behavior
  - no provider-specific SQL assumptions leak into test setup

## Success Criteria

- Application starts and runs on PostgreSQL without runtime provider errors
- New empty PostgreSQL database can be provisioned and initialized fully from migrations
- Automatic migrations apply successfully at startup
- Post-go-live releases can update schema without recreating production database
- Repository-level tenant isolation prevents cross-user data visibility
- JSON fields persist and load correctly under chosen mappings
- Integration and E2E tests pass against PostgreSQL in CI
- Local developer setup is documented and reproducible
