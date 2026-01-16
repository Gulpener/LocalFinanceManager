# Post-MVP-6: Migrate to Supabase PostgreSQL

## Objective

Migrate from SQLite to Supabase PostgreSQL for production-ready multi-tenant support with better performance and scalability.

## Requirements

- Replace SQLite with `Npgsql.EntityFrameworkCore.PostgreSQL`
- Update `AppDbContext` with PostgreSQL-specific configurations
- Add query filters for tenant isolation
- Update connection string management for Supabase
- Handle JSON column differences between SQLite and PostgreSQL

## Implementation Tasks

- [ ] Install NuGet packages:
  - Remove `Microsoft.EntityFrameworkCore.Sqlite`
  - Add `Npgsql.EntityFrameworkCore.PostgreSQL`
- [ ] Update `Program.cs` to use PostgreSQL:
  ```csharp
  builder.Services.AddDbContext<AppDbContext>(options =>
      options.UseNpgsql(connectionString));
  ```
- [ ] Update `AppDbContext.cs`:
  - Configure PostgreSQL-specific types (e.g., JSON columns)
  - Add global query filters for `UserId` tenant isolation:
    ```csharp
    modelBuilder.Entity<Account>().HasQueryFilter(e => e.UserId == currentUserId);
    ```
  - Update value converters for PostgreSQL compatibility
- [ ] Update connection string configuration:
  - Add Supabase connection string to `appsettings.json`
  - Use environment variable for production: `ASPNETCORE_ConnectionStrings__Default`
- [ ] Handle JSON column differences:
  - PostgreSQL uses native `jsonb` type
  - Update JSON property configurations
- [ ] Create fresh migration for PostgreSQL schema
- [ ] Update seed data method to check PostgreSQL-specific constraints
- [ ] Update tests to use PostgreSQL (or keep in-memory SQLite for unit tests)
- [ ] Add database health check endpoint

## Connection String Format

```json
{
  "ConnectionStrings": {
    "Default": "Host=db.xxx.supabase.co;Database=postgres;Username=postgres;Password=xxx;SSL Mode=Require"
  }
}
```

## Testing

- Integration tests with PostgreSQL test database
- Verify tenant isolation with multiple users
- Performance testing for large datasets
- Verify all migrations apply successfully

## Success Criteria

- Application runs on PostgreSQL without errors
- Query filters enforce tenant isolation
- All tests pass with PostgreSQL
- Connection to Supabase works
- JSON columns work correctly
- Performance is acceptable
