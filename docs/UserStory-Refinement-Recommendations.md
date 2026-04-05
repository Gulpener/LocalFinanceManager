# User Story Refinement Recommendations

**Date:** April 4, 2026  
**Purpose:** Identify which user stories need refinement before implementation

---

## Status Overview

- ✅ **23 stories completed & archived** — see `docs/Userstories/Archive/`
- � **1 story ready** for immediate implementation (US-18)
- 🟡 **1 story new** and awaiting planning (US-16)
- 🔴 **2 stories need refinement** (US-14, US-15)

**Key Finding:** UserStory-5 (Basic Assignment UI) serves as the **gold standard template** for well-structured user stories.

**Deferred E2E test (1):** `AutoApply_AuditTrail_ShowsAutoAppliedIndicator` — blocked on UserStory-18 (`/transactions/{id}/audit` page).

---

## Active Stories

### � UserStory-16: Design Overhaul

**File:** [docs/Userstories/UserStory-16-Design-Overhaul.md](docs/Userstories/UserStory-16-Design-Overhaul.md)

**Status:** New — full specification written. Estimated effort: 5-7 days.

**Scope:** Volledige visuele redesign met Finance Blue kleurenpallet, custom Bootstrap 5 thema, dark mode, mobile-first responsive navigatie, dashboard KPI-kaarten en consistente componentpatronen.

---

### 🟡 UserStory-18: Transaction Audit Trail UI

**File:** [docs/Userstories/UserStory-18-Transaction-Audit-Trail-UI.md](docs/Userstories/UserStory-18-Transaction-Audit-Trail-UI.md)

**Status:** Ready — no refinement needed. Estimated effort: 2-3 days.

**Key Features:**

- Transaction audit trail page (`/transactions/{id}/audit`)
- Timeline layout showing change history
- Auto-applied badges with confidence scores
- Before/After state diff viewer
- Link from transaction list to audit page

---

### 🔴 UserStory-14: Backup & Restore

**File:** [docs/Userstories/UserStory-14-Backup-Restore.md](docs/Userstories/UserStory-14-Backup-Restore.md)

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

#### UserStory-15: Application Flow & Onboarding

**File:** [docs/Userstories/UserStory-15-Application-Flow.md](docs/Userstories/UserStory-15-Application-Flow.md)

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

## Implementation Roadmap (Current)

### ✅ All Phases 1–5: COMPLETED & ARCHIVED

See `docs/Userstories/Archive/` for all 23 completed stories (US-1 through US-15).

### Active / Next Up

1. 🟡 **UserStory-16** (Design Overhaul) — New, ready to plan and implement (5-7 days)
2. 🟡 **UserStory-18** (Transaction Audit Trail UI) — Ready, implement now (2-3 days)
3. 🔴 **UserStory-14** (Backup & Restore) — Needs refinement (3-4 days after refinement)
4. 🔴 **UserStory-15** (Application Flow & Onboarding) — Needs refinement (4-5 days after refinement)

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
2. **Missing Auth Details:** Avoid vague security descriptions; specify token type, claims, and JWT config explicitly
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

1. **Immediate:** Implement UserStory-17 (Transaction Audit Trail UI) — fully ready, no refinement needed (2-3 days)
2. **Next:** Refine and implement UserStory-14 (Backup & Restore) — conflict resolution, encryption, versioning (refinement ~2-3h; impl 3-4 days)
3. **Next:** Refine and implement UserStory-15 (Application Flow & Onboarding) — dashboard widgets, onboarding wizard (refinement ~2-3h; impl 4-5 days)

**Total Refinement Effort Remaining:** ~4-6 hours across 2 remaining unrefined stories (US-14, US-15)
