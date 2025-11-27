# Contributing to LocalFinanceManager

Thank you for contributing to LocalFinanceManager! This guide will help you maintain code quality and consistency.

## Branch and PR Strategy

- Create feature branches from `main` using the format: `feature/<task-number>-<short-description>` (e.g., `feature/A.1-ef-core-setup`)
- Create bugfix branches using the format: `bugfix/<issue-number>-<short-description>`
- Keep commits atomic and focused on a single change
- Write clear commit messages following the format: `[Task X.Y] Brief description`
- Create a Pull Request when your feature is complete and tests pass
- Reference the task number in the PR title and description
- Mark tasks as completed in `docs/ProjectPlan.md` when merged

## Code Style

- Follow the rules defined in `.editorconfig`
- Run `dotnet format` before committing to ensure formatting consistency
- Use meaningful variable and method names
- Add XML documentation comments for public APIs
- Keep methods focused and concise (Single Responsibility Principle)

## Testing Requirements

- Write unit tests for all business logic (aim for ≥80% coverage for core services)
- Write integration tests for database operations and multi-layer flows
- All tests must pass before opening a PR
- Run `dotnet test` locally before pushing
- Use descriptive test method names: `MethodName_Scenario_ExpectedBehavior`

## Building and Running

### Prerequisites
- .NET 10 SDK
- SQLite (included with .NET)

### Build Commands
```bash
# Restore dependencies
dotnet restore

# Build the solution
dotnet build

# Run tests
dotnet test

# Run the web application
dotnet run --project src/Web

# Apply database migrations
dotnet ef database update --project src/Infrastructure --startup-project src/Web

# Create new migration
dotnet ef migrations add MigrationName --project src/Infrastructure --startup-project src/Web
```

## Project Structure

```
LocalFinanceManager/
├── src/
│   ├── Domain/          # Core entities and domain logic
│   ├── Application/     # Business services and interfaces
│   ├── Infrastructure/  # Data access, repositories, EF Core
│   └── Web/            # Blazor Server UI and endpoints
├── tests/
│   ├── Domain.Tests/
│   ├── Application.Tests/
│   ├── Infrastructure.Tests/
│   └── Integration.Tests/
└── docs/               # Documentation
```

## Dependency Flow

- `Domain` has no dependencies
- `Application` depends on `Domain`
- `Infrastructure` depends on `Domain` and `Application`
- `Web` depends on all projects and wires up DI

## Database Conventions

- Use SQLite for local development
- Store database in `App_Data/local.db`
- Always use EF Core migrations for schema changes
- Never modify the database schema directly
- Preserve `OriginalCsv` field for all imported transactions

## Feature Flags

- Use `appsettings.json` for feature toggles
- Document all configuration options in `appsettings.Development.json`
- Use `IConfiguration` for runtime access

## Code Review Checklist

Before requesting a review, ensure:
- [ ] Code follows `.editorconfig` rules
- [ ] `dotnet format` has been run
- [ ] All tests pass (`dotnet test`)
- [ ] Solution builds without warnings (`dotnet build`)
- [ ] New features have unit tests
- [ ] Public APIs have XML documentation
- [ ] Task checkbox updated in `docs/ProjectPlan.md`
- [ ] Migrations applied successfully

## Questions or Issues?

Open an issue in the repository with:
- Clear description of the problem
- Steps to reproduce (if applicable)
- Expected vs. actual behavior
- Environment details (.NET version, OS)
