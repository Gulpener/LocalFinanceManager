# User Story Refinement Recommendations

**Date:** January 16, 2026  
**Purpose:** Identify which user stories need refinement before implementation

---

## Executive Summary

**Status Overview:**

- ✅ **5 stories ready** for immediate implementation (US-1, US-4, US-5, US-6, US-7)
- ✅ **4 stories refined and ready** (US-3.1, US-3.2, US-3.3, US-5.1/5.2/5.3) - Split January 16, 2026
- ⚠️ **0 stories need minor refinement**
- 🔴 **4 stories need major refinement** (split or expand) (US-9, US-11, US-12, US-13)

**Key Finding:** UserStory-5 (Basic Assignment UI) serves as the **gold standard template** for well-structured user stories. All other stories should follow its pattern.

---

## 🔴 Needs Major Refinement (Split or Expand)

### ✅ UserStory-8: UX Refinements & E2E Test Infrastructure (REFINED)

**Status:** ✅ **Refined January 16, 2026** - Split into UserStory-5.1, 5.2, 5.3

**File:** [docs/Userstories/Archive/UserStory-8-UX-Refinements-E2E-Tests.md](docs/Userstories/Archive/UserStory-8-UX-Refinements-E2E-Tests.md) (archived)

**Original Issues:**

1. **Too large:** 86 implementation tasks spanning E2E infrastructure, UX enhancements, and accessibility audit
2. E2E test infrastructure should be completed **before** US-5 finishes to enable parallel test development
3. UX enhancements are independent features that could be deferred
4. Accessibility audit is separate concern

**Resolution:** **Split into 3 sub-stories with US-5.x numbering (assignment feature family)**

#### UserStory-5.1: E2E Test Infrastructure Setup

**File:** [docs/Userstories/UserStory-5.1-E2E-Infrastructure.md](docs/Userstories/UserStory-5.1-E2E-Infrastructure.md)

**Scope:**

- Enhance existing `TestWebApplicationFactory` with dedicated SQLite test database
- Create reusable `SeedDataHelper.cs` with 5 seed methods (accounts, categories, transactions, ML data, auto-apply history)
- Build `PageObjectModel` base classes (TransactionsPageModel, AssignmentModalPageModel, SplitEditorPageModel, BulkAssignModalPageModel)
- Configure screenshot/video capture on failure
- Add `.runsettings` for parallel execution (max 4 workers)
- Refactor existing tests to use new seed helpers

**Estimated Effort:** 1-2 days

**Tasks:** ~25 implementation tasks

**Success Criteria:**

- E2E project runs successfully with Playwright browser automation
- Seed data helpers reusable across all tests (no inline creation)
- CI pipeline runs E2E tests in headless mode with screenshot capture

**Dependencies:** None (can start immediately, parallel with US-5)

---

#### UserStory-5.2: E2E Tests for Assignment Flows

**File:** [docs/Userstories/UserStory-5.2-E2E-Tests-Assignment-Flows.md](docs/Userstories/UserStory-5.2-E2E-Tests-Assignment-Flows.md)

**Scope:**

- E2E tests: Basic assignment (US-5) - 11 test cases
- E2E tests: Split assignment (US-6) - 9 test cases
- E2E tests: Bulk assignment (US-6) - 9 test cases
- E2E tests: ML suggestions (US-7) - 8 test cases
- E2E tests: Auto-apply configuration (US-7) - 8 test cases
- E2E tests: Monitoring dashboard (US-7) - 9 test cases
- E2E tests: Cross-feature workflows - 2 comprehensive tests
- E2E tests: Accessibility validation with axe-core - 4 test cases
- E2E tests: Performance validation - 3 test cases

**Estimated Effort:** 2-3 days

**Tasks:** ~42 implementation tasks

**Success Criteria:**

- 63+ E2E test cases covering all assignment features
- PageObjectModels from US-5.1 used consistently
- Accessibility audit passes WCAG 2.1 Level AA (zero critical violations)
- Performance tests validate <500ms load time for 1000+ transactions
- Coverage >90% for TransactionsController and CategoriesController

**Dependencies:** UserStory-5/6/7 REQUIRED (tests assignment UI components)

---

#### UserStory-5.3: Assignment UX Enhancements

**File:** [docs/Userstories/UserStory-5.3-UX-Enhancements.md](docs/Userstories/UserStory-5.3-UX-Enhancements.md)

**Scope:**

- Keyboard shortcuts (Tab/Enter/Esc/Space/Ctrl+A/Ctrl+D/`/`/`?`) with `ShortcutHelp.razor` modal
- `QuickFilters.razor` component (assignment status, date range, category, amount, account filters)
- Filter state persistence in localStorage
- `RecentCategoriesService` tracking top 5 used categories with one-click assignment
- Favorite categories feature (star icon, top-of-list placement)
- Performance optimization (page size selector, loading skeletons, filter debouncing)

**Estimated Effort:** 2-3 days

**Tasks:** ~24 implementation tasks

**Success Criteria:**

- Keyboard shortcuts documented in help modal (accessible via `?` key)
- Recent categories reduce assignment friction (top 5 most used displayed)
- Quick filters with 6+ options functional
- Filter state persists across page reloads (localStorage)
- Favorites displayed at top of CategorySelector

**Dependencies:** UserStory-5/6/7 REQUIRED (enhances assignment UI components)

---

**Total Estimated Effort (US-5.1/5.2/5.3):** 5-8 days (vs 10-15 days as single story)

**Implementation Order:**

1. **US-5.1** (E2E Infrastructure) → Parallel with US-5 (complete before US-5 finishes)
2. **US-5.2** (E2E Tests) → Incremental after US-5/6/7 (test each feature as built)
3. **US-5.3** (UX Enhancements) → After US-7 (can be prioritized independently)

---
- E2E tests run in <5 minutes in CI pipeline

---

**Total Estimated Effort (US-8 Split):** 5-8 days (vs 10-15 days as single story)

**Implementation Order:**

1. **US-8.1** (E2E Infrastructure) → Start immediately, complete before US-5 finishes
2. **US-8.3** (E2E Tests) → Implement incrementally after US-5, US-6, US-7 complete
3. **US-8.2** (UX Enhancements) → Defer to post-MVP or implement in parallel

---

### UserStory-9: Multi-User Authentication with Supabase

**File:** [docs/Userstories/UserStory-9-Multi-User-Authentication.md](docs/Userstories/UserStory-9-Multi-User-Authentication.md)

**Issues:**

1. **Missing implementation tasks section** - Only high-level requirements listed
2. No concrete technical breakdown for Supabase integration
3. Missing JWT middleware configuration details
4. No migration strategy for adding `UserId` to existing entities
5. No testing scenarios defined
6. No Definition of Done checklist

**Required Refinements:**

Add the following sections:

#### Implementation Tasks

**1. Supabase Setup & Configuration**

- [ ] Create Supabase project (user manual via Supabase dashboard)
- [ ] Install `Supabase.Gotrue` NuGet package in `LocalFinanceManager`
- [ ] Add Supabase configuration to `appsettings.json`:
  ```json
  {
    "Supabase": {
      "Url": "https://<project-ref>.supabase.co",
      "Key": "<anon-public-key>",
      "JwtSecret": "<jwt-secret>"
    }
  }
  ```
- [ ] Create `SupabaseOptions` configuration class with `IOptions<T>` pattern

**2. JWT Authentication Middleware**

- [ ] Install `Microsoft.AspNetCore.Authentication.JwtBearer` package
- [ ] Configure JWT bearer authentication in `Program.cs`:
  ```csharp
  builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
      .AddJwtBearer(options => {
          options.TokenValidationParameters = new TokenValidationParameters {
              ValidateIssuer = true,
              ValidateAudience = true,
              ValidateLifetime = true,
              ValidIssuer = supabaseOptions.Url,
              ValidAudience = supabaseOptions.Url,
              IssuerSigningKey = new SymmetricSecurityKey(
                  Encoding.UTF8.GetBytes(supabaseOptions.JwtSecret))
          };
      });
  ```
- [ ] Add `app.UseAuthentication()` and `app.UseAuthorization()` middleware

**3. User Entity & Database Schema**

- [ ] Create `User` entity in `Models/`:
  ```csharp
  public class User : BaseEntity {
      public string SupabaseUserId { get; set; } // UUID from Supabase Auth
      public string Email { get; set; }
      public string DisplayName { get; set; }
      // Navigation properties
      public ICollection<Account> Accounts { get; set; }
      public ICollection<BudgetPlan> BudgetPlans { get; set; }
  }
  ```
- [ ] Add `UserId` foreign key to `Account`, `BudgetPlan`, `Category`, `Transaction` entities
- [ ] Update `BaseEntity` to include `public Guid UserId { get; set; }`
- [ ] Create EF Core migration: `dotnet ef migrations add AddUserEntity`

**4. Data Migration Strategy**

- [ ] Create migration script to assign all existing entities to "System" user:

  ```csharp
  migrationBuilder.Sql(@"
      INSERT INTO Users (Id, SupabaseUserId, Email, DisplayName, CreatedAt, UpdatedAt)
      VALUES ('system-user-guid', 'system', 'system@local', 'System User', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);

      UPDATE Accounts SET UserId = 'system-user-guid' WHERE UserId IS NULL;
      UPDATE BudgetPlans SET UserId = 'system-user-guid' WHERE UserId IS NULL;
      -- Repeat for all entities
  ");
  ```

- [ ] Add non-nullable constraint after data migration

**5. Repository Query Filters**

- [ ] Update `IRepository<T>` interface to include `GetByUserIdAsync(Guid userId)` method
- [ ] Implement user-scoped queries in all repositories:
  ```csharp
  public async Task<List<Account>> GetByUserIdAsync(Guid userId) {
      return await _context.Accounts
          .Where(a => a.UserId == userId && !a.IsArchived)
          .ToListAsync();
  }
  ```
- [ ] Update all existing repository methods to filter by `UserId`

**6. Authorization Attributes**

- [ ] Add `[Authorize]` attribute to all API controllers
- [ ] Create `UserContext` service to extract current user from JWT claims:
  ```csharp
  public interface IUserContext {
      Guid GetCurrentUserId();
      string GetCurrentUserEmail();
  }
  ```
- [ ] Inject `IUserContext` into services and repositories

**7. Blazor Auth Components**

- [ ] Create `Login.razor` page with Supabase Auth UI
- [ ] Create `Register.razor` page
- [ ] Create `AuthStateProvider` inheriting `AuthenticationStateProvider`
- [ ] Add `<AuthorizeView>` wrapper in `App.razor`
- [ ] Store JWT token in local storage or session storage

**8. Tests**

- [ ] Add unit tests for `UserContext.GetCurrentUserId()` in `LocalFinanceManager.Tests/Services/`
- [ ] Add integration tests for user-scoped queries in `LocalFinanceManager.Tests/Integration/`
- [ ] Test scenario: User A cannot access User B's accounts (HTTP 403)
- [ ] Test scenario: Anonymous request returns HTTP 401
- [ ] Add E2E tests for login/logout flow in `LocalFinanceManager.E2E/`

#### Testing Scenarios

1. **Authentication:**

   - Valid JWT token grants access to API (HTTP 200)
   - Invalid/expired JWT token returns HTTP 401
   - Missing JWT token returns HTTP 401

2. **Authorization:**

   - User can only access their own entities (accounts, budget plans, etc.)
   - Attempt to access another user's entity returns HTTP 403
   - System user can access legacy data migrated from pre-auth system

3. **User Registration:**

   - New user registration creates `User` entity in database
   - Email verification required before login (configurable)

4. **Data Isolation:**
   - User A creates account → User B cannot see it in GET /api/accounts
   - Seed data creates separate entities for each test user

#### Definition of Done

- [ ] Supabase Auth integration complete with JWT middleware
- [ ] All entities have `UserId` foreign key and user-scoped queries
- [ ] Data migration assigns existing entities to "System" user
- [ ] `[Authorize]` attribute applied to all API controllers
- [ ] Blazor login/register pages functional with AuthStateProvider
- [ ] Unit tests cover user context extraction from JWT claims
- [ ] Integration tests verify data isolation between users
- [ ] E2E tests cover login → create account → logout → verify data persists
- [ ] No manual migrations required (automatic via `Database.MigrateAsync()`)

**Estimated Refinement Time:** 3-4 hours

**Estimated Implementation Effort After Refinement:** 5-7 days

---

### UserStory-11: Sharing System (Multi-Tenant Permissions)

**File:** [docs/Userstories/UserStory-11-Sharing-System.md](docs/Userstories/UserStory-11-Sharing-System.md)

**Issues:**

1. **Missing implementation tasks section** - Only requirements listed
2. No schema design for `AccountShare` and `BudgetPlanShare` entities
3. Permission cascade rules unclear (e.g., shared BudgetPlan → automatic Category access?)
4. Authorization filter implementation details missing
5. No test scenarios defined

**Required Refinements:**

Add the following sections:

#### Database Schema Design

**1. AccountShare Entity:**

```csharp
public class AccountShare : BaseEntity {
    public Guid AccountId { get; set; }
    public Account Account { get; set; }

    public Guid SharedWithUserId { get; set; }
    public User SharedWithUser { get; set; }

    public Guid SharedByUserId { get; set; }
    public User SharedByUser { get; set; }

    public SharePermission Permission { get; set; } // Read, Write, Admin
    public DateTime? ExpiresAt { get; set; }
    public bool IsRevoked { get; set; }
}

public enum SharePermission { Read, Write, Admin }
```

**2. BudgetPlanShare Entity:**

```csharp
public class BudgetPlanShare : BaseEntity {
    public Guid BudgetPlanId { get; set; }
    public BudgetPlan BudgetPlan { get; set; }

    public Guid SharedWithUserId { get; set; }
    public User SharedWithUser { get; set; }

    public Guid SharedByUserId { get; set; }
    public User SharedByUser { get; set; }

    public SharePermission Permission { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool IsRevoked { get; set; }
}
```

#### Permission Cascade Rules

**Sharing an Account:**

- **Read:** User can view account details, transactions (read-only)
- **Write:** User can create/edit transactions, assign categories
- **Admin:** User can share account with others, delete account

**Sharing a BudgetPlan:**

- **Automatic Access:** Shared budget plan grants access to all categories within that plan
- **Read:** User can view budget lines, categories (read-only)
- **Write:** User can edit budget lines, create categories
- **Admin:** User can share budget plan, delete plan

**Inheritance:**

- If Account A is shared with Write permission → User can view/edit all transactions on Account A
- If BudgetPlan B is shared with Read permission → User can view all categories in BudgetPlan B (but not edit)

#### Implementation Tasks

**1. Entity & Migration:**

- [ ] Create `AccountShare` entity
- [ ] Create `BudgetPlanShare` entity
- [ ] Add `SharePermission` enum
- [ ] Create EF Core migration: `dotnet ef migrations add AddSharingEntities`

**2. Repository & Service Layer:**

- [ ] Create `IShareService` interface with methods:
  - `ShareAccountAsync(Guid accountId, Guid userId, SharePermission permission)`
  - `RevokeAccountShareAsync(Guid shareId)`
  - `GetSharedAccountsAsync(Guid userId)` → Returns accounts shared with user
- [ ] Create `IAuthorizationService` interface with methods:
  - `CanAccessAccountAsync(Guid userId, Guid accountId, SharePermission required)`
  - `CanAccessBudgetPlanAsync(Guid userId, Guid budgetPlanId, SharePermission required)`

**3. Authorization Filters:**

- [ ] Create custom authorization attribute: `[RequireAccountAccess(Permission.Write)]`
- [ ] Implement authorization handler checking both ownership and shares
- [ ] Apply attribute to all account-related API endpoints

**4. Blazor UI:**

- [ ] Create `ShareAccountModal.razor` component:
  - Search users by email
  - Select permission level (Read/Write/Admin)
  - Set optional expiration date
- [ ] Add "Share" button to account details page
- [ ] Create `SharedWithMe.razor` page showing all shared accounts/budget plans
- [ ] Add "Revoke" button for shared entities

**5. Tests:**

- [ ] Unit tests for permission cascade logic
- [ ] Integration tests: User B can access shared account from User A
- [ ] Integration tests: User B cannot access revoked share (HTTP 403)
- [ ] E2E tests: Share account → Login as User B → View transactions → Logout

#### Testing Scenarios

1. **Sharing Flow:**

   - User A shares Account X with User B (Write permission)
   - User B sees Account X in "Shared With Me" page
   - User B can create transactions on Account X
   - User B cannot delete Account X (requires Admin permission)

2. **Revocation:**

   - User A revokes share → User B loses access immediately
   - User B receives HTTP 403 on subsequent API calls

3. **Expiration:**
   - Share expires at `ExpiresAt` timestamp → User B loses access
   - Cron job checks expired shares daily and marks as revoked

**Estimated Refinement Time:** 3-4 hours

**Estimated Implementation Effort After Refinement:** 5-7 days

---

### UserStory-12: Backup & Restore System

**File:** [docs/Userstories/UserStory-12-Backup-Restore.md](docs/Userstories/UserStory-12-Backup-Restore.md)

**Issues:**

1. Conflict resolution strategy needs concrete examples
2. Missing implementation tasks for Blazor UI
3. No integration test scenarios defined
4. Backup format example provided but not validated against current schema

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

**Estimated Refinement Time:** 2-3 hours

**Estimated Implementation Effort After Refinement:** 3-4 days

---

### UserStory-13: Application Flow & Onboarding

**File:** [docs/Userstories/UserStory-13-Application-Flow.md](docs/Userstories/UserStory-13-Application-Flow.md)

**Issues:**

1. Tasks exist but lack detail (e.g., dashboard widget specifications unclear)
2. Onboarding wizard step-by-step flow needs mockups/wireframes
3. Breadcrumb generation logic unclear (static routes vs dynamic entity paths?)
4. Success criteria too vague ("user experience is intuitive")

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

**Estimated Refinement Time:** 2-3 hours

**Estimated Implementation Effort After Refinement:** 4-5 days

---

## Implementation Roadmap

### Phase 1: Foundation (Immediate - Weeks 1-2)

1. ✅ **UserStory-5** (Basic Assignment UI) - **Start now** (2-3 days)
2. ⚠️ **UserStory-8.1** (E2E Test Infrastructure) - **Parallel with US-5** (1-2 days)
3. ✅ **UserStory-1** (CI Pipeline) - **Refined and ready** (1 day)

### Phase 2: Core Features (Weeks 3-5)

4. ✅ **UserStory-5** (Basic Assignment UI) - Start now (2-3 days)
   - **Parallel:** ✅ **UserStory-5.1** (E2E Infrastructure) - 1-2 days - **REFINED**
5. ✅ **UserStory-6** (Split & Bulk Assignment) - After US-5 (3-4 days)
6. ✅ **UserStory-3.1** (Category Budget Plan Scoping) - After US-6 (2-3 days) - **REFINED**
7. ✅ **UserStory-4** (Account-Budget Matching) - After US-3.1 (3-4 days)

### Phase 3: Advanced Features (Weeks 6-8)

8. ✅ **UserStory-3.2** (Category Template System) - After US-4 (1-2 days) - **REFINED**
9. ✅ **UserStory-7** (ML Suggestion & Auto-Apply) - After US-6 (4-5 days)
10. ✅ **UserStory-5.2** (E2E Tests Assignment Flows) - Incremental with US-7 (2-3 days) - **REFINED**

### Phase 4: UX Improvements (Weeks 9-10)

11. ✅ **UserStory-3.3** (Category UI Updates & E2E Tests) - After US-3.2 (1-2 days) - **REFINED**
12. ✅ **UserStory-5.3** (UX Enhancements) - After US-7 (2-3 days) - **REFINED**
13. 🔴 **UserStory-13** (Application Flow) - After refinement (4-5 days)

### Phase 5: Post-MVP (Weeks 11+)

14. 🔴 **UserStory-9** (Multi-User Auth) - After refinement (5-7 days)
15. 🔴 **UserStory-11** (Sharing System) - After US-9 + refinement (5-7 days)
16. 🔴 **UserStory-12** (Backup & Restore) - After refinement (3-4 days)

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

1. **Immediate:** Start implementing UserStory-1 (CI Pipeline) and UserStory-5 (Basic Assignment UI) + UserStory-5.1 (E2E Infrastructure parallel) - all production-ready
2. **This Week:** Expand UserStory-9, 11, 12, 13 with implementation tasks (6-8 hours total)

**Total Refinement Effort Remaining:** ~6-8 hours across remaining stories

**Priority Order:**

1. ✅ ~~UserStory-1 (CI Pipeline)~~ - **COMPLETED** (Refined January 16, 2026)
2. ✅ ~~UserStory-4 (Account-Budget Matching)~~ - **COMPLETED** (Refined January 16, 2026)
3. ✅ ~~UserStory-3 (Split into 3.1, 3.2, 3.3)~~ - **COMPLETED** (Split January 16, 2026)
4. ✅ ~~UserStory-8 (Split into 5.1, 5.2, 5.3)~~ - **COMPLETED** (Split January 16, 2026)
5. UserStory-9 (Multi-User Auth) - Expand implementation tasks
6. UserStory-11 (Sharing System) - Expand implementation tasks
7. UserStory-12 (Backup & Restore) - Add conflict resolution details
8. UserStory-13 (Application Flow) - Add widget specifications
5. UserStory-9 (Expand) - Post-MVP but critical for multi-user
