# UserStory-14: Backup and Restore

## Objective

Enable users to export their complete financial data as a JSON file and restore it later, with three conflict-resolution strategies (Merge, Overwrite, Skip) and dry-run validation before committing a restore.

## Scope

**Entities included in backup:** Accounts, BudgetPlans, Categories, BudgetLines, Transactions, TransactionSplits.

**Explicitly out of scope:** AccountShares, BudgetPlanShares, TransactionAuditLogs, AppSettings, MLModels, LabeledExamples. Archived entities (`IsArchived = true`) are **excluded** from both export and import.

**Deferred:** Optional backup encryption (AES-256) — separate future story.

---

## Architecture Decisions

| Decision | Choice | Rationale |
|---|---|---|
| Export HTTP method | `GET /api/backup/export` | Conventional file download |
| Conflict resolution UI | Global strategy selector | Simpler; no per-conflict modal |
| Overwrite delete mechanism | `ExecuteDeleteAsync` scoped to `UserId` | Efficient bulk delete without loading entities |
| App version in backup | Hardcoded constant `"1.0"` in `BackupService` | No version tag exists in csproj/appsettings |
| Dry-run endpoint | Yes — `POST /api/backup/validate` | Allows UI to show errors before committing |
| Max upload size | 10 MB | Sufficient for typical user data |

---

## DTOs (`LocalFinanceManager/DTOs/BackupDTOs.cs` — new file)

```csharp
public class BackupData
{
    public DateTime ExportedAt { get; set; }
    public string Version { get; set; } = "1.0";
    public List<BackupAccountDto> Accounts { get; set; } = [];
    public List<BackupBudgetPlanDto> BudgetPlans { get; set; } = [];
    public List<BackupCategoryDto> Categories { get; set; } = [];
    public List<BackupBudgetLineDto> BudgetLines { get; set; } = [];
    public List<BackupTransactionDto> Transactions { get; set; } = [];
    public List<BackupTransactionSplitDto> TransactionSplits { get; set; } = [];
}

public class BackupAccountDto
{
    public Guid Id { get; set; }
    public string Label { get; set; } = "";
    public string AccountType { get; set; } = "";
    public string Currency { get; set; } = "";
    public string? IBAN { get; set; }
    public decimal StartingBalance { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

// BackupBudgetPlanDto: Id, AccountId, Year, Name, CreatedAt, UpdatedAt
// BackupCategoryDto:   Id, Name, CategoryType, BudgetPlanId, CreatedAt, UpdatedAt
// BackupBudgetLineDto: Id, BudgetPlanId, CategoryId, MonthlyAmountsJson, Notes, CreatedAt, UpdatedAt
// BackupTransactionDto: Id, Amount, Date, Description, Counterparty, AccountId,
//                        ExternalId, ImportBatchId, SourceFileName, ImportedAt, CreatedAt, UpdatedAt
// BackupTransactionSplitDto: Id, TransactionId, BudgetLineId, Amount, Note, CreatedAt, UpdatedAt

public enum ConflictResolutionStrategy { Merge, Overwrite, Skip }

public class RestoreRequest
{
    public BackupData Backup { get; set; } = null!;
    public ConflictResolutionStrategy Strategy { get; set; } = ConflictResolutionStrategy.Merge;
}

public class BackupRestoreResultDto
{
    public bool Success { get; set; }
    public int AccountsImported { get; set; }
    public int AccountsUpdated { get; set; }
    public int AccountsSkipped { get; set; }
    public int BudgetPlansImported { get; set; }
    public int CategoriesImported { get; set; }
    public int BudgetLinesImported { get; set; }
    public int TransactionsImported { get; set; }
    public int TransactionSplitsImported { get; set; }
    public List<string> Errors { get; set; } = [];
}

public class BackupValidationResultDto
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = [];
}
```

---

## Service Interface (`LocalFinanceManager/Services/IBackupService.cs` — new)

```csharp
public interface IBackupService
{
    Task<BackupData> CreateBackupAsync(Guid userId);
    Task<BackupValidationResultDto> ValidateBackupAsync(BackupData backup, Guid userId);
    Task<BackupRestoreResultDto> RestoreBackupAsync(Guid userId, BackupData backup, ConflictResolutionStrategy strategy);
}
```

---

## Service Implementation (`LocalFinanceManager/Services/BackupService.cs` — new)

Constructor DI: `AppDbContext`, `IUserContext`, `ILogger<BackupService>`

**CreateBackupAsync:**
- Query all non-archived entities filtered by `UserId`
- Map to `Backup*Dto` (do not reuse existing API DTOs)
- Set `Version = "1.0"`, `ExportedAt = DateTime.UtcNow`

**ValidateBackupAsync:**
1. Reject if `backup.Version != "1.0"`
2. Check internal referential integrity:
   - Every `BudgetLine.BudgetPlanId` exists in `backup.BudgetPlans`
   - Every `BudgetLine.CategoryId` exists in `backup.Categories`
   - Every `TransactionSplit.TransactionId` exists in `backup.Transactions`
   - Every `TransactionSplit.BudgetLineId` exists in `backup.BudgetLines`
   - Every `Transaction.AccountId` exists in `backup.Accounts`
   - Every `BudgetPlan.AccountId` exists in `backup.Accounts`
3. IBAN conflict check: for each backup Account whose `Id` does not match an existing local account, verify the IBAN is not already used by another local account → add error if conflict found
4. Return `BackupValidationResultDto { IsValid, Errors }`

**RestoreBackupAsync:**
- Always validate first; return early with errors if invalid
- Wrap everything in `await using var tx = await _db.Database.BeginTransactionAsync()`
- **Overwrite:** hard-delete all user's entities in reverse FK order using `ExecuteDeleteAsync` scoped to `UserId`:
  `TransactionSplits → Transactions → BudgetLines → Categories → BudgetPlans → Accounts`
  Then insert all backup entities in FK order:
  `Accounts → BudgetPlans → Categories → BudgetLines → Transactions → TransactionSplits`
- **Merge:** For each entity (in FK order), match on `Id`:
  - Not found → insert (increment Imported count)
  - Found + backup `UpdatedAt` newer → update (increment Updated count)
  - Found + local same/newer → skip (increment Skipped count)
  - IBAN conflict on Account (different `Id`, same IBAN as existing) → add error, rollback, return failure
- **Skip:** For each entity, if `Id` already exists → skip; else insert
- Commit; return `BackupRestoreResultDto` with counts per entity type

---

## Controller (`LocalFinanceManager/Controllers/BackupController.cs` — new)

```csharp
[Authorize]
[ApiController]
[Route("api/backup")]
public class BackupController : ControllerBase
{
    // GET /api/backup/export
    // Returns FileContentResult("application/json") with filename backup-yyyyMMdd.json
    // userId resolved inside service via IUserContext

    // POST /api/backup/validate
    // [RequestSizeLimit(10_485_760)]
    // [FromBody] BackupData — returns BackupValidationResultDto

    // POST /api/backup/restore
    // [RequestSizeLimit(10_485_760)]
    // [FromBody] RestoreRequest — returns BackupRestoreResultDto or 400 ProblemDetails if invalid
}
```

---

## Blazor UI (`LocalFinanceManager/Components/Pages/Backup.razor` — new)

Route: `/backup`

**Export section:**
- "Export Backup" button → JS interop file download (`window.open` or fetch blob pattern)
- Loading spinner while request is in flight

**Import section:**
- `InputFile` component (accept=".json", max 10 MB hint label)
- `<select>` for `ConflictResolutionStrategy`: Merge (default), Overwrite, Skip
- "Validate" button → reads file content as string → `POST /api/backup/validate` → show green "Backup is valid" or red error list
- "Restore" button (disabled until validation returns `IsValid = true`)
  - If strategy is Overwrite: show confirmation modal ("This will delete all your existing data")
  - On confirm: `POST /api/backup/restore` with selected strategy
  - Show loading spinner during restore
  - Display `BackupRestoreResultDto` summary: imported/updated/skipped counts per entity type + any errors

**Nav link:** Add entry to `Components/Layout/NavMenu.razor`

---

## Conflict Resolution Reference

| Scenario | Detection | Resolution |
|---|---|---|
| Duplicate entity ID (Merge) | `Id` already exists locally | Compare `UpdatedAt`; update if backup is newer, skip if local is same/newer |
| Duplicate entity ID (Skip) | `Id` already exists locally | Skip — never overwrite |
| IBAN conflict | Different `Id`, same IBAN on another local account | Reject import — return error message identifying the conflicting account |
| Category name in same plan | Not possible — Categories unique per `BudgetPlanId` | N/A — import normally |
| Transaction duplicate | Same `AccountId + Date + Amount + Counterparty` | Not auto-detected; use Merge/Skip by ID as primary key — fuzzy matching deferred |

---

## Implementation Tasks

### Phase 1 — DTOs
- [ ] Create `LocalFinanceManager/DTOs/BackupDTOs.cs` with all types listed above

### Phase 2 — BackupService
- [ ] Create `LocalFinanceManager/Services/IBackupService.cs`
- [ ] Create `LocalFinanceManager/Services/BackupService.cs` implementing all three strategies
- [ ] Register `IBackupService` / `BackupService` as scoped in `ServiceCollectionExtensions.cs`

### Phase 3 — BackupController
- [ ] Create `LocalFinanceManager/Controllers/BackupController.cs` with export, validate, restore endpoints

### Phase 4 — Blazor UI
- [ ] Create `LocalFinanceManager/Components/Pages/Backup.razor`
- [ ] Add nav link in `Components/Layout/NavMenu.razor`

### Phase 5 — Tests
- [ ] Unit tests `tests/LocalFinanceManager.Tests/BackupServiceTests.cs`:
  - `CreateBackupAsync_ExcludesArchivedEntities`
  - `CreateBackupAsync_OnlyReturnsTenantData`
  - `ValidateBackupAsync_RejectsIncompatibleVersion`
  - `ValidateBackupAsync_DetectsIbanConflict`
  - `ValidateBackupAsync_DetectsBrokenInternalReference`
  - `RestoreBackupAsync_Merge_InsertsNewEntities`
  - `RestoreBackupAsync_Merge_UpdatesWhenBackupNewer`
  - `RestoreBackupAsync_Merge_SkipsWhenLocalNewer`
  - `RestoreBackupAsync_Merge_AbortsOnIbanConflict`
  - `RestoreBackupAsync_Skip_DoesNotOverwriteExisting`
  - `RestoreBackupAsync_Overwrite_ReplacesAllData`
- [ ] Integration tests `tests/LocalFinanceManager.Tests/BackupControllerIntegrationTests.cs`:
  - `Export_ReturnsValidJsonWithAllEntityTypes`
  - `Validate_ReturnsIsValid_ForConsistentBackup`
  - `Validate_ReturnsErrors_ForIbanConflict`
  - `Restore_Overwrite_ReplacesAllExistingData`
  - `Restore_IntoEmptyDatabase_ImportsAllEntities`
  - `Restore_Merge_ReturnsCorrectCounts`
- [ ] E2E test `tests/LocalFinanceManager.E2E/BackupRestoreE2ETest.cs`:
  - Navigate to `/backup`
  - Export backup → verify file download triggered
  - Upload backup → validate → restore with Merge → verify success summary

---

## Backup Format Example

```json
{
  "exportedAt": "2026-04-05T10:30:00Z",
  "version": "1.0",
  "accounts": [
    {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "label": "Checking Account",
      "accountType": "Checking",
      "currency": "EUR",
      "iban": "NL91ABNA0417164300",
      "startingBalance": 0.00,
      "createdAt": "2025-01-01T00:00:00Z",
      "updatedAt": "2025-06-01T12:00:00Z"
    }
  ],
  "budgetPlans": [{ "id": "...", "accountId": "...", "year": 2026, "name": "2026 Budget", "createdAt": "...", "updatedAt": "..." }],
  "categories": [{ "id": "...", "name": "Groceries", "categoryType": "Expense", "budgetPlanId": "...", "createdAt": "...", "updatedAt": "..." }],
  "budgetLines": [{ "id": "...", "budgetPlanId": "...", "categoryId": "...", "monthlyAmountsJson": "[500,500,500,500,500,500,500,500,500,500,500,500]", "notes": null, "createdAt": "...", "updatedAt": "..." }],
  "transactions": [{ "id": "...", "amount": -45.50, "date": "2026-03-15", "description": "Albert Heijn", "counterparty": "AH", "accountId": "...", "createdAt": "...", "updatedAt": "..." }],
  "transactionSplits": []
}
```

---

## Success Criteria

- User can download complete data backup as JSON (6 entity arrays)
- Backup excludes archived data and other users' data
- `POST /api/backup/validate` returns errors without modifying any data
- Restore with Merge: newer-wins on conflict, IBAN conflicts rejected
- Restore with Overwrite: confirmation dialog shown; all data replaced atomically
- Restore with Skip: existing records never modified
- UI shows per-entity-type counts after restore
- All unit, integration, and E2E tests pass
