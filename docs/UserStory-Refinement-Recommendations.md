# User Story Refinement Recommendations

**Date:** January 17, 2026  
**Purpose:** Identify which user stories need refinement before implementation

---

## Executive Summary

**Status Overview:**

- ✅ **8 stories completed** (US-1, US-2, US-3, US-3.1, US-3.2, US-3.3, US-8, US-8-refinements) - Archived
- ✅ **8 stories ready** for immediate implementation (US-4, US-5, US-5.1, US-5.3, US-6, US-7, US-8.1, US-8.2, US-9, US-10)
- 🔴 **5 stories need refinement** (US-11, US-12, US-13, US-14, US-15) - Post-MVP features

**Key Finding:** UserStory-5 (Basic Assignment UI) serves as the **gold standard template** for well-structured user stories. All other stories should follow its pattern.

**Completed & Archived Stories:**

- UserStory-1 (CI Pipeline) - ✅ **COMPLETED & ARCHIVED**
- UserStory-2 (Branching Strategy) - ✅ **COMPLETED & ARCHIVED** - GitHub Flow workflow, PR/issue templates, CODEOWNERS
- UserStory-3 (Category Ownership) - ✅ **COMPLETED & ARCHIVED**
- UserStory-3.1 (Category Budget Plan Scoping) - ✅ **COMPLETED & ARCHIVED** - Budget-plan-scoped categories with migration
- UserStory-3.2 (Category Template System) - ✅ **COMPLETED & ARCHIVED**
- UserStory-3.3 (Category UI Tests) - ✅ **COMPLETED & ARCHIVED**
- UserStory-8 (UX Enhancements) - ✅ **COMPLETED & ARCHIVED**
- UserStory-8 (UX Refinements E2E Tests) - ✅ **COMPLETED & ARCHIVED**

---

## Detailed User Story Status

### ✅ Ready for Implementation (No Refinement Needed)

#### UserStory-4: Account-Budget Matching

**File:** [docs/Userstories/UserStory-4-Account-Budget-Matching.md](docs/Userstories/UserStory-4-Account-Budget-Matching.md)

**Status:** ✅ **Ready** - Comprehensive data model changes, validation infrastructure, and implementation tasks defined.

**Key Features:**

- Enforce `BudgetLineId` as required on `TransactionSplit`
- Implement `IBudgetAccountLookupService` with caching
- Batch validation optimization
- Complete API changes and validation rules

**Estimated Effort:** 3-4 days

---

#### UserStory-5: Basic Assignment UI

**File:** [docs/Userstories/UserStory-5-Basic-Assignment-UI.md](docs/Userstories/UserStory-5-Basic-Assignment-UI.md)

**Status:** ✅ **Ready** - **Gold standard template** for all user stories. Comprehensive component patterns, service interfaces, and test scenarios.

**Key Features:**

- Reusable `CategorySelector.razor` component
- Transaction assignment modal
- Uncategorized transaction warnings
- Assignment audit trail

**Estimated Effort:** 2-3 days

---

#### UserStory-5.1: E2E Infrastructure

**File:** [docs/Userstories/UserStory-5.1-E2E-Infrastructure.md](docs/Userstories/UserStory-5.1-E2E-Infrastructure.md)

**Status:** ✅ **Ready** - E2E test infrastructure with NUnit + Playwright.

**Key Features:**

- `WebApplicationFactory` setup with test database
- Playwright page object pattern
- Authentication fixture
- Browser context management

**Estimated Effort:** 1-2 days

---

#### UserStory-5.3: UX Enhancements

**File:** [docs/Userstories/UserStory-5.3-UX-Enhancements.md](docs/Userstories/UserStory-5.3-UX-Enhancements.md)

**Status:** ✅ **Ready** - UX improvements for assignment workflow.

**Estimated Effort:** 2-3 days

---

#### UserStory-6: Split & Bulk Assignment

**File:** [docs/Userstories/UserStory-6-Split-Bulk-Assignment.md](docs/Userstories/UserStory-6-Split-Bulk-Assignment.md)

**Status:** ✅ **Ready** - Comprehensive split editor UI and bulk assignment features with validation.

**Key Features:**

- Dynamic split editor with add/remove rows
- Real-time sum validation (±0.01 tolerance)
- Bulk assignment with multi-select
- Progress bar and error handling

**Estimated Effort:** 3-4 days

---

#### UserStory-7: ML Suggestion & Auto-Apply

**File:** [docs/Userstories/UserStory-7-ML-Suggestion-Auto-Apply.md](docs/Userstories/UserStory-7-ML-Suggestion-Auto-Apply.md)

**Status:** ✅ **Ready** - AI-powered suggestions with auto-apply configuration and monitoring dashboard.

**Key Features:**

- ML category suggestions with confidence scores
- One-click accept/reject
- Auto-apply configuration (threshold, account selection)
- Monitoring dashboard with statistics

**Estimated Effort:** 4-5 days

---

#### UserStory-8.1: Keyboard Shortcuts

**File:** [docs/Userstories/UserStory-8.1-Keyboard-Shortcuts.md](docs/Userstories/UserStory-8.1-Keyboard-Shortcuts.md)

**Status:** ✅ **Ready** - Keyboard navigation for power users.

**Estimated Effort:** 1-2 days

---

#### UserStory-8.2: Quick Filters & Performance

**File:** [docs/Userstories/UserStory-8.2-Quick-Filters-Performance.md](docs/Userstories/UserStory-8.2-Quick-Filters-Performance.md)

**Status:** ✅ **Ready** - Performance optimizations and quick filter UI.

**Estimated Effort:** 2-3 days

---

#### UserStory-9: Integration Workflow Tests

**File:** [docs/Userstories/UserStory-9-Integration-Workflow-Tests.md](docs/Userstories/UserStory-9-Integration-Workflow-Tests.md)

**Status:** ✅ **Ready** - End-to-end integration testing for complete workflows.

**Estimated Effort:** 2-3 days

---

#### UserStory-10: Multi-User Authentication

**File:** [docs/Userstories/UserStory-10-Multi-User-Authentication.md](docs/Userstories/UserStory-10-Multi-User-Authentication.md)

**Status:** ✅ **Ready** - Supabase authentication with JWT tokens and multi-tenant data isolation.

**Key Features:**

- Supabase authentication integration
- JWT token management
- User registration/login UI
- Tenant-isolated queries

**Estimated Effort:** 5-7 days

---

### 🔴 Needs Major Refinement

#### UserStory-11: Supabase PostgreSQL Migration

**File:** [docs/Userstories/UserStory-11-Supabase-PostgreSQL.md](docs/Userstories/UserStory-11-Supabase-PostgreSQL.md)

**Issues:**

1. **Missing test strategy** - Should tests remain SQLite in-memory or migrate to PostgreSQL?
2. **No rollback plan** - If migration fails, how to revert to SQLite?
3. **Data migration strategy undefined** - How to migrate existing SQLite data to PostgreSQL?
4. **Health check implementation incomplete** - Need detailed health check logic
5. **No performance benchmarks** - Should include before/after performance comparison

**Required Refinements:**

Add the following sections:

#### Test Strategy

**Decision Required:**

1. **Option A:** Keep unit/integration tests with in-memory SQLite, only E2E tests use PostgreSQL
   - **Pros:** Fast test execution, no external dependencies
   - **Cons:** Doesn't catch PostgreSQL-specific issues in unit tests

2. **Option B:** Migrate all tests to PostgreSQL test database
   - **Pros:** Tests match production environment
   - **Cons:** Slower execution, requires test database setup

**Recommendation:** Option A (SQLite for unit tests, PostgreSQL for E2E)

#### Rollback Plan

**Pre-Migration Backup:**

- [ ] Create SQLite backup file before migration
- [ ] Store backup in `backups/` directory with timestamp
- [ ] Document connection string for rollback

**Rollback Steps:**

1. Restore SQLite backup file
2. Revert `AppDbContext` to use SQLite provider
3. Update `Program.cs` to use SQLite connection
4. Run application with `--skip-migration` flag

**Rollback Trigger Criteria:**

- Migration fails with data integrity errors
- Performance degradation >50% in critical queries
- PostgreSQL connection issues persist >24 hours

#### Data Migration Strategy

**Approach: Export/Import with Validation**

1. **Export SQLite data:**
   - [ ] Create `MigrationService` with `ExportSqliteDataAsync()` method
   - [ ] Export all entities to JSON format (reuse backup format from US-13)
   - [ ] Validate data integrity (foreign keys, constraints)

2. **PostgreSQL schema creation:**
   - [ ] Apply migrations to empty PostgreSQL database
   - [ ] Verify schema matches SQLite structure

3. **Import data:**
   - [ ] Create `MigrationService.ImportPostgresDataAsync(jsonData)` method
   - [ ] Import in dependency order (Accounts → BudgetPlans → Categories → Transactions)
   - [ ] Validate relationships after import

4. **Verification:**
   - [ ] Compare row counts between SQLite and PostgreSQL
   - [ ] Run smoke tests on imported data

**Estimated Migration Time:** 2-3 hours for typical dataset (<10,000 transactions)

#### Health Check Implementation

**Detailed Health Check Logic:**

```csharp
[HttpGet("/health")]
public async Task<IActionResult> Health()
{
    var health = new HealthStatus
    {
        Status = "Healthy",
        Checks = new List<Check>()
    };

    // Database connectivity check
    try
    {
        await _context.Database.CanConnectAsync();
        health.Checks.Add(new Check { Name = "Database", Status = "Healthy" });
    }
    catch (Exception ex)
    {
        health.Checks.Add(new Check { Name = "Database", Status = "Unhealthy", Error = ex.Message });
        health.Status = "Unhealthy";
    }

    // Query performance check (should complete <1s)
    var stopwatch = Stopwatch.StartNew();
    try
    {
        await _context.Accounts.CountAsync();
        stopwatch.Stop();
        var queryTime = stopwatch.ElapsedMilliseconds;
        health.Checks.Add(new Check
        {
            Name = "Query Performance",
            Status = queryTime < 1000 ? "Healthy" : "Degraded",
            Metadata = new { QueryTimeMs = queryTime }
        });
    }
    catch (Exception ex)
    {
        health.Checks.Add(new Check { Name = "Query Performance", Status = "Unhealthy", Error = ex.Message });
        health.Status = "Unhealthy";
    }

    return health.Status == "Healthy" ? Ok(health) : StatusCode(503, health);
}
```

#### Performance Benchmarks

**Benchmark Scenarios:**

1. **Account List Query (100 accounts):**
   - SQLite baseline: Target <50ms
   - PostgreSQL target: <30ms (20% improvement expected)

2. **Transaction Assignment (1000 transactions):**
   - SQLite baseline: Target <200ms
   - PostgreSQL target: <150ms (25% improvement expected)

3. **Budget Line Lookup (cached):**
   - SQLite baseline: Target <10ms
   - PostgreSQL target: <10ms (no change expected, cache dominant)

**Measurement Tool:** BenchmarkDotNet in `LocalFinanceManager.Tests/Benchmarks/`

**Estimated Refinement Time:** 2-3 hours

**Estimated Implementation Effort After Refinement:** 3-4 days

---

#### UserStory-12: Sharing System

**File:** [docs/Userstories/UserStory-12-Sharing-System.md](docs/Userstories/UserStory-12-Sharing-System.md)

**Issues:**

1. **Missing authorization middleware implementation** - How to integrate with existing controllers?
2. **UI mockups needed** - No visual guidance for sharing modal/page
3. **Permission inheritance unclear** - If Account is shared, are Transactions automatically accessible?
4. **Notification system undefined** - Should users be notified when something is shared with them?
5. **Share expiration mechanism missing** - No cron job or background service for expiration checks

**Required Refinements:**

Add the following sections:

#### Authorization Middleware Implementation

**Custom Authorization Policy:**

```csharp
// Startup/Program.cs
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AccountAccess", policy =>
        policy.Requirements.Add(new AccountAccessRequirement()));
});

builder.Services.AddScoped<IAuthorizationHandler, AccountAccessHandler>();
```

**Authorization Handler:**

```csharp
public class AccountAccessHandler : AuthorizationHandler<AccountAccessRequirement, Guid>
{
    private readonly IShareService _shareService;

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        AccountAccessRequirement requirement,
        Guid accountId)
    {
        var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var hasAccess = await _shareService.CanAccessAccountAsync(userId, accountId, requirement.MinimumPermission);

        if (hasAccess)
            context.Succeed(requirement);
    }
}
```

**Controller Usage:**

```csharp
[HttpGet("{id}")]
[Authorize(Policy = "AccountAccess")]
public async Task<IActionResult> GetAccount(Guid id)
{
    // User is already authorized by policy
    var account = await _accountService.GetByIdAsync(id);
    return Ok(account);
}
```

#### Permission Inheritance Rules

**Explicit Rules:**

1. **Account Shared → Transaction Access:**
   - If User B has "Viewer" access to Account A → User B can view all Transactions on Account A (read-only)
   - If User B has "Editor" access to Account A → User B can create/edit Transactions on Account A
   - If User B has "Owner" access to Account A → User B can delete Transactions on Account A

2. **BudgetPlan Shared → Category/BudgetLine Access:**
   - If User B has "Viewer" access to BudgetPlan X → User B can view all Categories and BudgetLines in BudgetPlan X
   - If User B has "Editor" access to BudgetPlan X → User B can create/edit Categories and BudgetLines in BudgetPlan X
   - If User B has "Owner" access to BudgetPlan X → User B can delete BudgetPlan X and all child entities

3. **No Cross-Entity Inheritance:**
   - Sharing an Account does NOT grant access to BudgetPlans
   - Sharing a BudgetPlan does NOT grant access to Accounts

#### Notification System

**Notification Strategy:**

- [ ] Create `Notification` entity:

  ```csharp
  public class Notification : BaseEntity
  {
      public string UserId { get; set; }
      public string Title { get; set; }
      public string Message { get; set; }
      public string Type { get; set; } // "Share", "Revoke", "System"
      public bool IsRead { get; set; }
      public Guid? RelatedEntityId { get; set; }
      public string RelatedEntityType { get; set; } // "Account", "BudgetPlan"
  }
  ```

- [ ] Trigger notifications on share/revoke:

  ```csharp
  // When User A shares Account X with User B
  await _notificationService.CreateAsync(new Notification
  {
      UserId = userBId,
      Title = "Account Shared With You",
      Message = $"{userA.Name} shared account '{account.Name}' with you (Viewer access)",
      Type = "Share",
      RelatedEntityId = accountId,
      RelatedEntityType = "Account"
  });
  ```

- [ ] Create `Notifications.razor` page showing unread notifications
- [ ] Add notification bell icon in header with unread count badge

#### Share Expiration Background Service

**Hosted Service Implementation:**

```csharp
public class ShareExpirationService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _serviceProvider.CreateScope();
            var shareService = scope.ServiceProvider.GetRequiredService<IShareService>();

            // Find expired shares
            var expiredShares = await shareService.GetExpiredSharesAsync();

            foreach (var share in expiredShares)
            {
                share.IsRevoked = true;
                await shareService.UpdateAsync(share);

                // Notify user
                await _notificationService.CreateAsync(new Notification
                {
                    UserId = share.SharedWithUserId,
                    Title = "Share Expired",
                    Message = $"Your access to {share.ResourceName} has expired",
                    Type = "System"
                });
            }

            // Check every hour
            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }
}
```

**Register in Program.cs:**

```csharp
builder.Services.AddHostedService<ShareExpirationService>();
```

#### UI Wireframe Requirements

**ShareAccountModal.razor:**

- Search input: "Find user by email"
- Dropdown: Permission level (Viewer/Editor/Owner)
- Date picker: Expiration date (optional)
- Checkbox: "Send notification to user"
- Buttons: "Share" (primary), "Cancel" (secondary)

**SharedWithMe.razor Page:**

- Table columns: Resource Type, Name, Shared By, Permission, Shared Date, Expiration
- Filter: Show only Accounts / Show only BudgetPlans
- Sort: By shared date (newest first)
- Action buttons: "Open", "Request Full Access" (if Viewer)

**Estimated Refinement Time:** 2-3 hours

**Estimated Implementation Effort After Refinement:** 5-7 days

---

#### UserStory-13: Backup & Restore

**File:** [docs/Userstories/UserStory-13-Backup-Restore.md](docs/Userstories/UserStory-13-Backup-Restore.md)

**Issues:**

1. Conflict resolution strategy needs concrete examples
2. Missing implementation tasks for Blazor UI
3. No integration test scenarios defined
4. Backup format example provided but not validated against current schema
5. **Missing security considerations** - Should backups be encrypted?
6. **No versioning strategy** - How to handle schema changes between backup and restore?

**Required Refinements:**

Add the following sections:

#### Conflict Resolution Strategy

**Scenario 1: Duplicate Account ID**

- **Detection:** Backup contains `AccountId=X` that exists locally
- **Resolution Options:**
  1. **Skip:** Keep local account, ignore backup account
  2. **Overwrite:** Replace local account with backup account (loses local changes)
  3. **Merge:** Compare `UpdatedAt` timestamps, keep newer version
  4. **Rename:** Import backup account with new GUID, append " (Imported)" to name

**Default Strategy:** **Merge by timestamp** (configurable in `appsettings.json`)

**Scenario 2: IBAN Conflict**

- **Detection:** Backup contains Account with IBAN that exists on different account locally
- **Resolution:** ❌ **Reject import** → IBAN must be unique (database constraint)
- **User Action Required:** Manually resolve by editing backup JSON or deleting local account

**Scenario 3: Category Name Conflict**

- **Detection:** Backup contains Category "Groceries" in BudgetPlan A, but local has Category "Groceries" in BudgetPlan B
- **Resolution:** ✅ **Allow both** → Category names are unique per budget plan, not globally

**Scenario 4: Transaction Duplicate Detection**

- **Detection:** Transaction with same `AccountId`, `Date`, `Amount`, `Counterparty` exists
- **Resolution:** **Skip duplicate** → Assume it's the same transaction
- **Threshold:** Match if all 4 fields identical (configurable fuzzy matching disabled by default)

#### Implementation Tasks

**1. Backup Export:**

- [ ] Add `POST /api/backup/export` endpoint returning JSON file
- [ ] Export format:
  ```json
  {
    "exportedAt": "2026-01-16T12:00:00Z",
    "version": "1.0",
    "accounts": [...],
    "budgetPlans": [...],
    "categories": [...],
    "transactions": [...],
    "budgetLines": [...]
  }
  ```
- [ ] Include all user-owned entities (filter by `UserId`)
- [ ] Exclude soft-deleted entities (`IsArchived = true`)

**2. Backup Import:**

- [ ] Add `POST /api/backup/import` endpoint accepting JSON file
- [ ] Validate backup format version compatibility
- [ ] Implement conflict detection for each entity type
- [ ] Apply conflict resolution strategy (merge by timestamp)
- [ ] Return `ImportResult` DTO with counts (imported, skipped, errors)

**3. Blazor UI:**

- [ ] Create `Backup.razor` page in `Components/Pages/`
- [ ] Add "Export Backup" button → Downloads JSON file
- [ ] Add "Import Backup" file upload component
- [ ] Display import preview modal:
  - Show entities to be imported
  - Highlight conflicts with resolution action
  - Allow user to select resolution per conflict
- [ ] Show import progress bar during processing
- [ ] Display import result summary (X accounts imported, Y skipped, Z errors)

**4. Tests:**

- [ ] Unit tests for conflict detection logic
- [ ] Integration tests for export endpoint (verify JSON structure)
- [ ] Integration tests for import endpoint:
  - Import into empty database → All entities imported
  - Import with duplicate IDs → Merge by timestamp
  - Import with IBAN conflict → Reject with error message
- [ ] E2E test: Export → Clear database → Import → Verify data restored

#### Testing Scenarios

1. **Full Backup & Restore:**
   - User has 5 accounts, 2 budget plans, 100 transactions
   - Export backup → JSON file size ~50KB
   - Clear local database
   - Import backup → 100% data restored

2. **Partial Conflict:**
   - Local has Account A (UpdatedAt: Jan 15)
   - Backup has Account A (UpdatedAt: Jan 16) with different name
   - Import with merge strategy → Local Account A updated to Jan 16 version

3. **IBAN Conflict (Reject):**
   - Local has Account A with IBAN "NL01ABCD0123456789"
   - Backup has Account B with same IBAN
   - Import fails with error: "IBAN NL01ABCD0123456789 already exists on account 'Account A'"

#### Security Considerations

**Encryption Strategy:**

- [ ] Add optional encryption for backup files using AES-256
- [ ] Generate encryption key from user password (PBKDF2, 10,000 iterations)
- [ ] Store IV (Initialization Vector) in backup file header
- [ ] UI: Checkbox "Encrypt backup" with password input field

**Implementation:**

```csharp
public async Task<byte[]> CreateEncryptedBackupAsync(string userId, string password)
{
    var backupData = await CreateBackupAsync(userId);
    var json = JsonSerializer.Serialize(backupData);

    using var aes = Aes.Create();
    aes.KeySize = 256;

    var key = DeriveKeyFromPassword(password, out var salt);
    aes.Key = key;
    aes.GenerateIV();

    using var encryptor = aes.CreateEncryptor();
    var encryptedData = encryptor.TransformFinalBlock(Encoding.UTF8.GetBytes(json), 0, json.Length);

    // Return: [salt][iv][encryptedData]
    return salt.Concat(aes.IV).Concat(encryptedData).ToArray();
}
```

#### Schema Versioning Strategy

**Backward Compatibility:**

- [ ] Add `schemaVersion` field to backup format (current: "1.0")
- [ ] Implement version-specific import handlers:
  ```csharp
  public async Task<ImportResult> RestoreBackupAsync(BackupData backup)
  {
      return backup.SchemaVersion switch
      {
          "1.0" => await RestoreV1Async(backup),
          "1.1" => await RestoreV1_1Async(backup),
          _ => throw new NotSupportedException($"Schema version {backup.SchemaVersion} not supported")
      };
  }
  ```

**Migration Logic:**

- If backup schema < current schema → Apply transformations before import
- Example: Schema 1.0 → 1.1 adds `CategoryType` field → Set default `CategoryType = Expense` for old backups

**Forward Compatibility Warning:**

- If backup schema > current schema → Display error: "Backup created with newer version. Please update the application."

**Estimated Refinement Time:** 2-3 hours

**Estimated Implementation Effort After Refinement:** 3-4 days

---

#### UserStory-14: Application Flow & Onboarding

**File:** [docs/Userstories/UserStory-14-Application-Flow.md](docs/Userstories/UserStory-14-Application-Flow.md)

**Issues:**

1. Tasks exist but lack detail (e.g., dashboard widget specifications unclear)
2. Onboarding wizard step-by-step flow needs mockups/wireframes
3. Breadcrumb generation logic unclear (static routes vs dynamic entity paths?)
4. Success criteria too vague ("user experience is intuitive")
5. **Missing user onboarding completion tracking** - How to prevent showing wizard on every login?

**Required Refinements:**

Add the following sections:

#### Dashboard Widget Specifications

**Widget 1: Account Summary**

- Display total balance across all accounts
- Show balance change from previous month (+5.3% ↑)
- List top 3 accounts by balance

**Widget 2: Budget Status**

- Show current month budget utilization (e.g., "65% of budget used")
- Display progress bar for each category (top 5 by spend)
- Highlight over-budget categories in red

**Widget 3: Recent Transactions**

- Show last 10 transactions across all accounts
- Group by date (Today, Yesterday, This Week)
- Add "Assign" quick action button for uncategorized transactions

**Widget 4: Uncategorized Transaction Alert**

- Display count of uncategorized transactions (e.g., "⚠️ 12 transactions need assignment")
- Add "Assign Now" button navigating to transaction list with filter

**Widget 5: ML Suggestion Summary**

- Show count of pending ML suggestions (e.g., "🤖 5 suggestions available")
- Add "Review Suggestions" button navigating to suggestions page

#### Onboarding Wizard Flow

**Step 1: Welcome Screen**

- Headline: "Welcome to Local Finance Manager!"
- Subtext: "Let's set up your first account and budget plan."
- Button: "Get Started" → Step 2

**Step 2: Create First Account**

- Form fields: Account Name, IBAN, Currency, Initial Balance
- Validation: IBAN format check, currency ISO-4217
- Button: "Next" → Step 3
- Skip option: "I'll do this later" → Dashboard

**Step 3: Create First Budget Plan**

- Form fields: Budget Plan Name, Start Date, End Date
- Pre-fill: Name = "My First Budget", Start = today, End = 1 year from today
- Button: "Next" → Step 4

**Step 4: Add Categories from Templates**

- Display category templates grouped by type (Income/Expense)
- Multi-select checkboxes for templates (default: select all)
- Button: "Create Categories" → Step 5

**Step 5: Import Transactions (Optional)**

- Upload CSV file (standard format: Date, Amount, Description, Counterparty)
- Preview first 5 rows in table
- Button: "Import" → Dashboard
- Skip option: "I'll add transactions manually" → Dashboard

**Step 6: Completion**

- Success message: "You're all set! 🎉"
- Summary: "1 account, 1 budget plan, 15 categories created"
- Button: "Go to Dashboard" → Dashboard

#### Breadcrumb Generation Logic

**Static Routes:**

- Home → `/` (no breadcrumb)
- Accounts → `/accounts` → Breadcrumb: `Home / Accounts`
- Transactions → `/transactions` → Breadcrumb: `Home / Transactions`

**Dynamic Entity Paths:**

- Account Details → `/accounts/{id}` → Breadcrumb: `Home / Accounts / {AccountName}`
- Transaction Edit → `/transactions/{id}/edit` → Breadcrumb: `Home / Transactions / {TransactionId} / Edit`

**Implementation:**

- Store breadcrumb trail in `NavigationManager` state
- Update breadcrumb component on navigation events
- Fetch entity names asynchronously for dynamic breadcrumbs (cache for 5 minutes)

#### Implementation Tasks

**1. Dashboard:**

- [ ] Create `Dashboard.razor` page in `Components/Pages/`
- [ ] Create widget components:
  - `AccountSummaryWidget.razor`
  - `BudgetStatusWidget.razor`
  - `RecentTransactionsWidget.razor`
  - `UncategorizedAlertWidget.razor`
  - `MLSuggestionWidget.razor`
- [ ] Add responsive grid layout (CSS Grid, 2 columns on desktop, 1 on mobile)

**2. Onboarding Wizard:**

- [ ] Create `Onboarding.razor` page with multi-step form
- [ ] Implement wizard steps 1-6 with form validation
- [ ] Add progress indicator (e.g., "Step 2 of 6")
- [ ] Store wizard state in browser session storage
- [ ] Redirect to dashboard on completion
- [ ] Set `User.HasCompletedOnboarding = true` flag

**3. Breadcrumbs:**

- [ ] Create `Breadcrumb.razor` component in `Components/Shared/`
- [ ] Implement breadcrumb trail generation based on current route
- [ ] Add entity name fetching for dynamic breadcrumbs
- [ ] Style with chevron separator (›) and hover effects

**4. Tests:**

- [ ] Unit tests for breadcrumb generation logic
- [ ] E2E tests for onboarding wizard:
  - Complete all steps → Dashboard shows created entities
  - Skip steps → Dashboard shows empty state
- [ ] E2E tests for dashboard widgets:
  - Verify widget data accuracy (e.g., balance calculations)

#### Success Criteria (Measurable)

- ✅ 100% of new users complete onboarding wizard (tracked via `User.HasCompletedOnboarding`)
- ✅ Dashboard loads in <2 seconds with 5 widgets (measured via browser performance API)
- ✅ Breadcrumb navigation reduces "back button" usage by 30% (tracked via analytics)
- ✅ Average time to first transaction assignment: <5 minutes after onboarding

#### User Onboarding Completion Tracking

**Database Schema:**

```csharp
public class User
{
    public string Id { get; set; }
    public string Email { get; set; }
    public bool HasCompletedOnboarding { get; set; }
    public DateTime? OnboardingCompletedAt { get; set; }
}
```

**Workflow:**

1. User registers → `HasCompletedOnboarding = false`
2. User logs in → Check `HasCompletedOnboarding`:
   - If `false` → Redirect to `/onboarding`
   - If `true` → Redirect to `/dashboard`
3. User completes onboarding wizard → Set `HasCompletedOnboarding = true`, `OnboardingCompletedAt = DateTime.UtcNow`

**Skip Option:**

- Add "Skip Onboarding" button on wizard pages → Set `HasCompletedOnboarding = true` (tracks completion but not full participation)
- Add "Show me around" link on dashboard for users who skipped → Reopens onboarding wizard

**Estimated Refinement Time:** 2-3 hours

**Estimated Implementation Effort After Refinement:** 4-5 days

---

#### UserStory-15: Azure Deployment

**File:** [docs/Userstories/UserStory-15-Azure-Deployment.md](docs/Userstories/UserStory-15-Azure-Deployment.md)

**Issues:**

1. **Missing cost estimation** - Free tier has limits, what happens when exceeded?
2. **No monitoring/alerting strategy** - How to detect production issues?
3. **Database backup strategy undefined** - How to backup PostgreSQL in production?
4. **Environment variable management unclear** - How to manage secrets across environments?
5. **Rollback strategy missing** - How to revert to previous deployment if bugs occur?

**Required Refinements:**

Add the following sections:

#### Azure Free Tier Limits & Cost Estimation

**Free Tier (F1) Limits:**

- 60 CPU minutes/day
- 1 GB RAM
- 1 GB storage
- 10 GB bandwidth/month

**Expected Usage:**

- ~10 users × 30 requests/day = 300 requests/day
- Average response time: 200ms
- Daily CPU usage: ~60 seconds (well within limit)

**Cost Escalation Plan:**

- If CPU minutes exceeded → Upgrade to Basic (B1) tier ($13/month)
- Monitor daily usage via Azure Portal metrics

#### Monitoring & Alerting Strategy

**Azure Application Insights:**

- [ ] Install NuGet package: `Microsoft.ApplicationInsights.AspNetCore`
- [ ] Add to `Program.cs`:
  ```csharp
  builder.Services.AddApplicationInsightsTelemetry(options =>
  {
      options.ConnectionString = builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"];
  });
  ```

**Key Metrics to Monitor:**

1. **Request rate:** Requests per minute (alert if >100/min sustained)
2. **Response time:** Average response time (alert if >2s)
3. **Error rate:** HTTP 500 errors (alert if >5% of requests)
4. **Database connection failures:** Failed connection attempts (alert on any failure)

**Alert Configuration:**

- Email alerts to developer email on critical failures
- SMS alerts (optional) for production downtime

#### Database Backup Strategy

**Supabase Automatic Backups:**

- Supabase provides daily automated backups (retained for 7 days on free tier)
- No additional configuration required

**Manual Backup Schedule:**

- Weekly manual backup via `BackupController` export endpoint
- Store backup JSON files in Azure Blob Storage (cold tier, $0.002/GB/month)
- Retention: 4 weekly backups, 12 monthly backups

**Backup Automation:**

```csharp
// Azure Function (Timer Trigger: 0 0 * * 0 = every Sunday)
public async Task RunWeeklyBackup(TimerInfo timer)
{
    var backupData = await _backupService.CreateBackupAsync(adminUserId);
    var json = JsonSerializer.Serialize(backupData);

    await _blobService.UploadAsync(
        containerName: "backups",
        blobName: $"backup_{DateTime.UtcNow:yyyyMMdd}.json",
        content: json
    );
}
```

#### Environment Variable Management

**GitHub Secrets (Production):**

- `AZURE_WEBAPP_PUBLISH_PROFILE` - Deployment credentials
- `SUPABASE_CONNECTION_STRING` - PostgreSQL connection
- `JWT_SECRET` - Token signing key (auto-generated 256-bit key)
- `APPLICATIONINSIGHTS_CONNECTION_STRING` - Monitoring

**Azure App Service Configuration:**

- Use App Service → Configuration → Application Settings for runtime secrets
- Enable "Deployment slot settings" to prevent production secrets in staging

**Local Development:**

- Use `appsettings.Development.json` (never commit)
- Example secrets:
  ```json
  {
    "ConnectionStrings": {
      "Default": "Host=localhost;Database=lfm_dev;..."
    },
    "Jwt": {
      "Secret": "dev-secret-key-not-for-production"
    }
  }
  ```

#### Rollback Strategy

**Deployment Slots (Not Available on Free Tier):**

- Free tier doesn't support deployment slots → Manual rollback required

**Manual Rollback Steps:**

1. Identify last known good commit: `git log --oneline`
2. Create rollback branch: `git checkout -b rollback/{commit-hash}`
3. Trigger deployment: `git push origin rollback/{commit-hash}`
4. GitHub Actions redeploys previous version

**Automated Rollback Trigger:**

- If health check fails for >5 minutes after deployment → Trigger rollback workflow
- GitHub Actions workflow monitors `/health` endpoint post-deployment

**Rollback Time Target:** <10 minutes from detection to restored service

**Estimated Refinement Time:** 2-3 hours

**Estimated Implementation Effort After Refinement:** 3-4 days

---

## Implementation Roadmap

### Phase 1: Foundation ✅ **COMPLETED**

1. ✅ **UserStory-1** (CI Pipeline) - **COMPLETED & ARCHIVED**
2. ✅ **UserStory-2** (Branching Strategy) - **COMPLETED & ARCHIVED**
3. ✅ **UserStory-3** (Category Ownership) - **COMPLETED & ARCHIVED**
4. ✅ **UserStory-3.1** (Category Budget Plan Scoping) - **COMPLETED & ARCHIVED**
5. ✅ **UserStory-3.2** (Category Template System) - **COMPLETED & ARCHIVED**
6. ✅ **UserStory-3.3** (Category UI Tests) - **COMPLETED & ARCHIVED**
7. ✅ **UserStory-8** (UX Enhancements) - **COMPLETED & ARCHIVED**

### Phase 2: Core Features (Weeks 3-5)

4. ✅ **UserStory-4** (Account-Budget Matching) - Ready (3-4 days)
5. ✅ **UserStory-5** (Basic Assignment UI) - Ready (2-3 days)
   - **Parallel:** ✅ **UserStory-5.1** (E2E Infrastructure) - Ready (1-2 days)
6. ✅ **UserStory-6** (Split & Bulk Assignment) - Ready after US-5 (3-4 days)

### Phase 3: Advanced Features (Weeks 6-8)

7. ✅ **UserStory-7** (ML Suggestion & Auto-Apply) - Ready after US-6 (4-5 days)
8. ✅ **UserStory-9** (Integration Workflow Tests) - Ready, incremental with US-7 (2-3 days)

### Phase 4: UX Improvements (Weeks 9-10)

9. ✅ **UserStory-5.3** (UX Enhancements) - Ready after US-7 (2-3 days)
10. ✅ **UserStory-8.1** (Keyboard Shortcuts) - Ready (1-2 days)
11. ✅ **UserStory-8.2** (Quick Filters & Performance) - Ready (2-3 days)

### Phase 5: Multi-User & Post-MVP (Weeks 11+)

12. ✅ **UserStory-10** (Multi-User Auth) - Ready (5-7 days)
13. 🔴 **UserStory-11** (Supabase PostgreSQL) - After US-10 + refinement (3-4 days)
14. 🔴 **UserStory-12** (Sharing System) - After US-11 + refinement (5-7 days)
15. 🔴 **UserStory-13** (Backup & Restore) - After refinement (3-4 days)
16. 🔴 **UserStory-14** (Application Flow) - After refinement (4-5 days)
17. 🔴 **UserStory-15** (Azure Deployment) - After refinement (3-4 days)

---

## Key Takeaways

### ✅ What Works Well (UserStory-5 Pattern)

1. **Clear Component Patterns:** Code examples with parameter documentation
2. **Service Interface Design:** Method signatures with return types specified
3. **Error Handling Standards:** RFC 7231 Problem Details format documented
4. **Test Organization:** Unit/integration/e2e separation with test scenarios
5. **Appropriate Task Size:** 15-35 tasks = 2-4 day sprint (optimal)
6. **Dependencies Listed:** Blocking relationships explicitly called out

### 🔴 Common Anti-Patterns to Avoid

1. **Too Many Tasks:** >50 tasks = story too large, should be split
2. ✅ **UserStory-1** (CI Pipeline) - \*\*Ready for implementation specifying JWT config
3. **Vague Success Criteria:** "UX is good" instead of measurable metrics
4. **No Testing Scenarios:** Missing concrete test case examples
5. **No DoD Checklist:** Success criteria exist but no explicit completion checkbox list

### 📋 Template for Future User Stories

Use this structure for all new stories:

```markdown
# UserStory-X: [Title]

## Objective

[1-2 sentence summary]

## Requirements

[Bullet list of functional requirements]

## Patterns for Subsequent Stories

[If foundational - document reusable patterns]

## Implementation Tasks

### 1. [Component/Feature Area]

- [ ] Task 1 with clear acceptance criteria
- [ ] Task 2 with code example if applicable

[15-35 total tasks recommended]

## Testing

### Unit Test Scenarios

1. **[Test Category]:** Description
2. **[Test Category]:** Description

### Integration Test Scenarios

1. **[API/Flow]:** Description

## Success Criteria

- ✅ Measurable criterion 1 (e.g., "100% of X display Y")
- ✅ Measurable criterion 2 (e.g., "Performance <2s")

## Definition of Done

- [ ] Checkbox for each deliverable
- [ ] Tests implemented and passing
- [ ] Code follows Implementation-Guidelines.md
- [ ] No manual migrations required

## Dependencies

- **UserStory-X:** REQUIRED - Reason
- **UserStory-Y:** OPTIONAL - Reason

## Estimated Effort

**X-Y days** (~Z implementation tasks)

## Notes

[Any additional context or warnings]
```

---

## Next Actions

1. **Immediate:** Start implementing UserStory-4 (Account-Budget Matching), UserStory-5 (Basic Assignment UI), and UserStory-5.1 (E2E Infrastructure in parallel) - all production-ready
2. **This Week:** Expand UserStory-11, 12, 13, 14, 15 with implementation tasks (10-12 hours total)

**Total Refinement Effort Remaining:** ~10-12 hours across 5 remaining stories

**Priority Order:**

1. ✅ ~~UserStory-1 (CI Pipeline)~~ - **COMPLETED & ARCHIVED**
2. ✅ ~~UserStory-2 (Branching Strategy)~~ - **COMPLETED & ARCHIVED**
3. ✅ ~~UserStory-3 (Category Ownership)~~ - **COMPLETED & ARCHIVED**
4. ✅ ~~UserStory-3.1 (Category Budget Plan Scoping)~~ - **COMPLETED & ARCHIVED**
5. ✅ ~~UserStory-3.2 (Category Template System)~~ - **COMPLETED & ARCHIVED**
6. ✅ ~~UserStory-3.3 (Category UI Tests)~~ - **COMPLETED & ARCHIVED**
7. ✅ ~~UserStory-8 (UX Enhancements & Refinements)~~ - **COMPLETED & ARCHIVED**
8. **UserStory-4** (Account-Budget Matching) - **NEXT: Start implementation**
9. **UserStory-5** (Basic Assignment UI) - **NEXT: Start implementation** (parallel with US-4)
10. **UserStory-5.1** (E2E Infrastructure) - Start implementation (parallel with US-5)
11. **UserStory-6** (Split & Bulk Assignment) - After US-5
12. **UserStory-7** (ML Suggestion & Auto-Apply) - After US-6
13. **UserStory-8.1** (Keyboard Shortcuts) - After US-7
14. **UserStory-8.2** (Quick Filters & Performance) - After US-8.1
15. **UserStory-9** (Integration Workflow Tests) - Incremental with US-7
16. **UserStory-10** (Multi-User Auth) - Ready for implementation
17. UserStory-11 (Supabase PostgreSQL) - Needs refinement (test strategy, rollback plan)
18. UserStory-12 (Sharing System) - Needs refinement (authorization middleware, permissions)
19. UserStory-13 (Backup & Restore) - Needs refinement (security/encryption, versioning)
20. UserStory-14 (Application Flow) - Needs refinement (onboarding tracking)
21. UserStory-15 (Azure Deployment) - Needs refinement (cost, monitoring, rollback)
