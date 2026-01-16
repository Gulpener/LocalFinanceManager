# Post-MVP-8: Add Backup and Restore

## Objective

Enable users to export their complete financial data as JSON and restore it later, with conflict resolution for data integrity.

## Requirements

- Create `BackupController` with JSON export endpoint
- Implement `BackupService` to serialize tenant-isolated data
- Include all user's accounts, plans, transactions, categories with relationships
- Add restore endpoint with conflict resolution (merge vs overwrite)
- Add Blazor UI pages for download and upload

## Implementation Tasks

- [ ] Create `BackupService.cs` with methods:
  - `CreateBackupAsync(userId)` → returns `BackupData` object
  - `RestoreBackupAsync(userId, backupData, conflictResolution)`
- [ ] Define `BackupData` DTO structure:
  ```csharp
  public class BackupData
  {
      public DateTime BackupDate { get; set; }
      public string Version { get; set; }
      public List<AccountDto> Accounts { get; set; }
      public List<BudgetPlanDto> BudgetPlans { get; set; }
      public List<CategoryDto> Categories { get; set; }
      public List<BudgetLineDto> BudgetLines { get; set; }
      public List<TransactionDto> Transactions { get; set; }
      public List<TransactionSplitDto> TransactionSplits { get; set; }
  }
  ```
- [ ] Implement tenant-isolated backup:
  - Query all user's data using `UserId` filter
  - Serialize with relationships preserved
  - Include metadata (backup date, app version)
- [ ] Create `BackupController.cs` with endpoints:
  - `GET /api/backup/export` → returns JSON file download
  - `POST /api/backup/restore` → accepts JSON file upload
  - `POST /api/backup/validate` → validates backup file without restoring
- [ ] Implement conflict resolution strategies:
  - **Merge**: Keep existing data, add new items only
  - **Overwrite**: Replace all data (delete existing, import backup)
  - **Skip Existing**: Only import items with new IDs
- [ ] Add validation:
  - Check backup version compatibility
  - Validate data integrity before restore
  - Verify relationships are intact
- [ ] Create Blazor UI pages:
  - `Backup.razor` with download button
  - `Restore.razor` with file upload and conflict resolution options
  - Progress indicator during restore
  - Confirmation dialog for overwrite mode
- [ ] Add error handling:
  - Invalid backup format
  - Version mismatch
  - Relationship integrity errors

## Backup Format Example

```json
{
  "backupDate": "2026-01-16T10:30:00Z",
  "version": "1.0",
  "accounts": [
    {
      "id": "guid",
      "name": "Checking Account",
      "iban": "NL91ABNA0417164300",
      "currency": "EUR"
    }
  ],
  "budgetPlans": [...],
  "categories": [...],
  "budgetLines": [...],
  "transactions": [...],
  "transactionSplits": [...]
}
```

## Testing

- Unit tests for backup serialization
- Integration tests for restore with different conflict modes
- Verify tenant isolation (no cross-user data)
- Test error scenarios (invalid format, version mismatch)
- E2E test full backup/restore workflow

## Success Criteria

- User can download complete data backup as JSON
- Backup includes all related entities
- Restore successfully recreates data
- Conflict resolution options work correctly
- Validation prevents corrupt data restore
- UI provides clear feedback during process
- Tenant isolation is maintained
