# Contributing to Local Finance Manager

Thank you for your interest in contributing to Local Finance Manager! This document outlines the development workflow, branching strategy, and code standards for the project.

## Branching Strategy (GitHub Flow)

We use **GitHub Flow** for all development:

- `main` is the production branch (protected)
- All changes go through pull requests with required review
- Feature branches are short-lived and merged frequently

### Branch Naming Conventions

Use descriptive branch names with the following prefixes:

- `feature/` — New features or enhancements (e.g., `feature/transaction-export`)
- `bugfix/` — Bug fixes (e.g., `bugfix/iban-validation`)
- `hotfix/` — Urgent production fixes (e.g., `hotfix/security-patch`)
- `docs/` — Documentation-only changes (e.g., `docs/update-readme`)

**Examples:**
```bash
git checkout -b feature/budget-template-system
git checkout -b bugfix/category-dropdown-empty
git checkout -b docs/api-documentation
```

## Pull Request Process

### Before Creating a PR

1. **Create a feature branch** from `main`:
   ```bash
   git checkout main
   git pull origin main
   git checkout -b feature/your-feature-name
   ```

2. **Follow code standards** (see [Technical Standards](#technical-standards))

3. **Write tests** for all new features:
   - Unit tests for business logic
   - Integration tests for database operations
   - E2E tests for user workflows

4. **Run all tests locally**:
   ```bash
   dotnet test
   ```

5. **Ensure the build passes**:
   ```bash
   dotnet build --configuration Release
   ```

### Creating a PR

1. **Push your branch**:
   ```bash
   git push origin feature/your-feature-name
   ```

2. **Create a pull request** on GitHub with:
   - Clear title describing the change
   - Description linking to related issue(s)
   - Checklist of completed items (use the PR template)

3. **Address review feedback** by pushing additional commits

4. **Keep your branch up to date** with `main`:
   ```bash
   git fetch origin
   git rebase origin/main
   git push --force-with-lease
   ```

### Merge Requirements

All pull requests must meet these requirements before merging:

- ✅ **CI must pass** (all test suites: unit/integration, ML, E2E)
- ✅ **At least 1 approval** from code reviewer
- ✅ **Branch must be up to date** with `main`
- ✅ **All conversations resolved**
- ✅ **Linked to an issue** (if applicable)

**Note:** Branch protection rules enforce these requirements automatically on the `main` branch.

## Code Review Process

### As a Contributor

- Respond to feedback promptly and professionally
- Ask questions if feedback is unclear
- Make requested changes in separate commits for easier review
- Mark conversations as resolved after addressing them

### As a Reviewer

- Reviews are automatically requested via CODEOWNERS (@gjgie)
- Review within 48 hours when possible
- Focus on:
  - Code correctness and logic
  - Test coverage
  - Performance implications
  - Security concerns
  - Adherence to code standards
- Approve when satisfied, or request changes with clear reasoning

## Technical Standards

All code must follow the technical decisions specified in [`docs/Implementation-Guidelines.md`](docs/Implementation-Guidelines.md):

### Key Standards

- **.NET Version:** net10.0
- **Logging:** Built-in `Microsoft.Extensions.Logging` (ILogger)
- **Error Responses:** RFC 7231 Problem Details format
- **Configuration:** `appsettings.json` + environment-specific files with `IOptions<T>`
- **Async Patterns:** Async all the way (all I/O operations use async/await)
- **DI Conventions:** Scoped services with `I<Name>` interfaces
- **Database:** SQLite file (`localfinancemanager.db`)
- **Code Style:** Nullable reference types enabled, warnings not-as-errors

### Testing Requirements

- **Unit Tests:** Business logic, validation, edge cases
- **Integration Tests:** Database operations, repositories, migrations (in-memory SQLite)
- **E2E Tests:** User workflows with NUnit + Playwright
- **ML Tests:** Model training validation + fixture models

For detailed examples and patterns, see [`docs/Implementation-Guidelines.md`](docs/Implementation-Guidelines.md).

## Development Setup

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- A code editor (Visual Studio 2022, VS Code, or Rider)

### Initial Setup

1. **Clone the repository:**
   ```bash
   git clone https://github.com/Gulpener/LocalFinanceManager.git
   cd LocalFinanceManager
   ```

2. **Restore dependencies:**
   ```bash
   dotnet restore
   ```

3. **Run the application:**
   ```bash
   cd LocalFinanceManager
   dotnet run
   ```
   
   The app will start at http://localhost:5244 with automatic migrations and seed data.

4. **Install Playwright browsers** (for E2E tests):
   ```bash
   pwsh tests/LocalFinanceManager.E2E/bin/Debug/net10.0/playwright.ps1 install
   ```

### Running Tests

```bash
# All tests
dotnet test

# Specific test project
dotnet test tests/LocalFinanceManager.Tests/
dotnet test tests/LocalFinanceManager.ML.Tests/
dotnet test tests/LocalFinanceManager.E2E/

# With code coverage
dotnet test --collect:"XPlat Code Coverage"
```

### Database Configuration

- **Development:** Uses `localfinancemanager.dev.db` with automatic seed data
- **Production:** Uses `localfinancemanager.db` without seed data
- **Recreate database:** Set `RecreateDatabase=true` in `appsettings.Development.json`
- **Admin dashboard:** View database status at `/admin/settings`

See [README.md](README.md#configuration) for detailed configuration options.

## Coding Conventions

### C# Style

- Use nullable reference types (`<Nullable>enable</Nullable>`)
- Follow Microsoft C# coding conventions
- Use meaningful variable and method names
- Keep methods focused and small
- Add XML documentation comments for public APIs

### Entity Patterns

All entities inherit from `BaseEntity`:

```csharp
public abstract class BaseEntity
{
    public Guid Id { get; set; }
    public byte[] RowVersion { get; set; }      // Optimistic concurrency
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsArchived { get; set; }        // Soft delete
}
```

### Repository Pattern

Use `IRepository<T>` for data access with automatic soft-delete filtering:

```csharp
public interface IRepository<T> where T : BaseEntity
{
    Task<T?> GetByIdAsync(Guid id);
    Task<List<T>> GetActiveAsync();       // Filters !IsArchived
    Task AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task ArchiveAsync(Guid id);           // Soft delete
}
```

### Concurrency Control

- Use `RowVersion` for optimistic concurrency
- Handle `DbUpdateConcurrencyException` with last-write-wins reload
- Return HTTP 409 Conflict with current entity state

### Validation

- Use FluentValidation for input validation
- Return RFC 7231 Problem Details format for validation errors
- Validate at the API boundary (DTOs)

## Git Commit Messages

Write clear, concise commit messages:

```
Add budget template system

- Create BudgetTemplate entity with monthly allocations
- Add API endpoints for template CRUD operations
- Implement template application to budget plans
- Add unit and integration tests for template logic
```

### Format

- **First line:** Imperative mood summary (50 chars max)
- **Blank line**
- **Body:** Detailed explanation with bullet points (wrap at 72 chars)

### Examples

- ✅ "Add transaction import from CSV files"
- ✅ "Fix IBAN validation for international accounts"
- ✅ "Update README with contribution guidelines"
- ❌ "fixed bug"
- ❌ "WIP"

## Reporting Issues

When reporting a bug or requesting a feature, use the appropriate GitHub issue template:

- **Bug Report:** For reproducible bugs with steps to reproduce
- **Feature Request:** For new features or enhancements
- **Documentation:** For documentation improvements
- **Performance Issue:** For performance-related problems
- **Security Issue:** For security vulnerabilities (use responsibly)

Provide as much detail as possible, including:
- Steps to reproduce (for bugs)
- Expected vs. actual behavior
- Environment details (.NET version, OS, browser)
- Screenshots or error logs (if applicable)

## Code of Conduct

- Be respectful and professional in all interactions
- Focus on constructive feedback
- Welcome newcomers and help them get started
- Keep discussions focused and on-topic
- Report unacceptable behavior to project maintainers

## Questions?

- Check the [README.md](README.md) for general documentation
- Review [`docs/Implementation-Guidelines.md`](docs/Implementation-Guidelines.md) for technical details
- Open an issue with the "Documentation" template for clarifications
- Reach out to maintainers for guidance

Thank you for contributing to Local Finance Manager! 🚀
