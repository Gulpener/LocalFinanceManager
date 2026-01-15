# Production-Ready Multi-Tenant LocalFinanceManager

Transform the single-user MVP into a production-ready multi-tenant application with PostgreSQL backend, authentication, sharing capabilities, automated Azure deployment, and backup/restore functionality. Categories will be scoped to budget plans with editable starter templates. Transactions can remain uncategorized with UI warnings.

## Implementation Steps

### 1. Add CI Pipeline

Create [.github/workflows/ci.yml](.github/workflows/ci.yml) to build solution with `dotnet build`, run unit/integration tests in [LocalFinanceManager.Tests/](../../tests/LocalFinanceManager.Tests/), run E2E tests with Playwright in [LocalFinanceManager.E2E/](../../tests/LocalFinanceManager.E2E/), run ML tests in [LocalFinanceManager.ML.Tests/](../../tests/LocalFinanceManager.ML.Tests/), enforce code quality checks, run on pull requests and main branch commits.

### 2. Implement Branching Strategy

Adopt GitHub Flow with `main` as production branch, create feature branches (`feature/*`), require pull requests for all changes to `main`, configure branch protection rules (require CI passing, require reviews), document workflow in [CONTRIBUTING.md](../../CONTRIBUTING.md).

### 3. Restructure Category Ownership

Add `BudgetPlanId` foreign key to [Category.cs](../../LocalFinanceManager/Models/Category.cs), update [AppDbContext.cs](../../LocalFinanceManager/Data/AppDbContext.cs) to configure `BudgetPlan.Categories` navigation property, create starter templates (Personal, Business, Household) with editable categories after creation, add `BudgetPlanService.CreateFromTemplate()` method.

### 4. Enforce Account-Budget Plan Matching

Add validation to [TransactionSplit.cs](../../LocalFinanceManager/Models/TransactionSplit.cs) ensuring `Transaction.AccountId` matches `Category.BudgetPlan.AccountId`, update [TransactionsController.cs](../../LocalFinanceManager/Controllers/TransactionsController.cs) validators to check account-plan consistency, allow uncategorized transactions with UI warnings.

### 5. Add Multi-User Authentication

Integrate ASP.NET Core Identity with Supabase Auth (JWT-based), add [User.cs](../../LocalFinanceManager/Models/User.cs) entity, update [BaseEntity.cs](../../LocalFinanceManager/Models/BaseEntity.cs) with `UserId` foreign key, implement authentication middleware in [Program.cs](../../LocalFinanceManager/Program.cs).

### 6. Migrate to Supabase PostgreSQL

Replace SQLite with Npgsql.EntityFrameworkCore.PostgreSQL, update [AppDbContext.cs](../../LocalFinanceManager/Data/AppDbContext.cs) with PostgreSQL-specific configurations and query filters for tenant isolation, update connection string management for Supabase, handle JSON column differences (SQLite vs PostgreSQL).

### 7. Implement Sharing System

Create `AccountShare`, `BudgetPlanShare` entities with permission levels (Owner/Editor/Viewer), add sharing controllers and UI, update repositories to respect shared access with cascade rules (shared BudgetPlan includes Categories/BudgetLines/Transactions), add authorization checks in all controllers.

### 8. Add Backup and Restore

Create `BackupController` with JSON export endpoint for user-scoped data, implement `BackupService` to serialize tenant-isolated backup including all user's accounts/plans/transactions/categories with relationships, add restore endpoint with conflict resolution (merge vs overwrite), add Blazor UI pages for download backup and upload restore.

### 9. Improve Application Flow

Add authentication guards, create user dashboard showing owned and shared accounts/plans with uncategorized transaction warnings, implement breadcrumb navigation, add onboarding wizard with template selection and category customization for first budget plan, refactor Blazor pages for multi-tenant UX.

### 10. Deploy to Azure App Service Free Tier

Create [.github/workflows/deploy.yml](.github/workflows/deploy.yml) for CD with `dotnet publish`, configure Azure App Service (Linux, .NET 10), add GitHub Secrets for Supabase connection string and Azure credentials, implement health checks endpoint, configure production logging, trigger deployment only after CI workflow passes.

## Technical Decisions

- **Categories:** Scoped to budget plans (not global), editable after template application
- **Uncategorized Transactions:** Allowed with UI warnings
- **Backup Format:** JSON (full data structure with relationships)
- **Backup Storage:** Local download only (no cloud storage)
- **Authentication:** Supabase Auth (JWT-based) for unified stack
- **Database:** Supabase PostgreSQL with tenant isolation via query filters
- **Branching:** GitHub Flow (main + feature branches)
- **CI/CD:** GitHub Actions (CI on PRs, CD to Azure after CI passes)
