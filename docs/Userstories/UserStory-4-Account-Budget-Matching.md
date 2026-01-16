# Post-MVP-4: Enforce Account-Budget Plan Matching

## Objective

Enforce data integrity by validating that transaction splits reference budget lines from budget plans associated with the transaction's account. Uses cached lookup service for performance optimization.

## Requirements

### Data Model Changes

- Enforce `BudgetLineId` as required on `TransactionSplit` (remove nullable)
- Remove `CategoryId` property from `TransactionSplit` (category accessed via `BudgetLine.Category`)
- Database reset allowed (no migration of existing data)

### Validation Infrastructure

- Implement `IBudgetAccountLookupService` with `IMemoryCache` (60-minute TTL, expiration-only invalidation)
- Batch validation optimization for multiple splits using `GetAccountIdsForBudgetLinesAsync(budgetLineIds[])`
- Validate `Transaction.AccountId == BudgetLine.BudgetPlan.AccountId` for all splits

### API Changes

- Update DTOs to replace `CategoryId` with required `BudgetLineId` in assignment requests
- Update FluentValidation validators to perform async account-budget plan consistency checks
- Include account labels in error messages for clarity

### Scope Exclusions

- UI workflow for assignment (separate user story)
- UI warnings for uncategorized transactions (separate user story)

## Implementation Tasks

### 1. Cache Infrastructure

- [ ] Register `IMemoryCache` in `Program.cs`
- [ ] Create `Configuration/CacheOptions.cs` with `BudgetMappingCacheDurationMinutes` property (default 60)
- [ ] Add `CacheOptions` configuration section to `appsettings.json`
- [ ] Register `IBudgetAccountLookupService` as scoped service

### 2. Data Model Updates

- [ ] Update `Models/TransactionSplit.cs`: Make `BudgetLineId` required, remove `CategoryId` property
- [ ] Update `Data/AppDbContext.cs`: Configure `BudgetLineId` as required FK, remove `CategoryId` relationship
- [ ] Update `DTOs/TransactionDTOs.cs`: Replace `CategoryId` with required `BudgetLineId` in `AssignTransactionRequest` and `SplitAllocationDto`
- [ ] Generate new EF Core migration for schema changes

### 3. Lookup Service Implementation

- [ ] Create `Services/BudgetAccountLookupService.cs` implementing `IBudgetAccountLookupService`
- [ ] Implement `GetAccountIdForBudgetLineAsync(Guid budgetLineId)` with caching (key: `"budget-account:{budgetLineId}"`)
- [ ] Implement `GetAccountIdsForBudgetLinesAsync(IEnumerable<Guid> budgetLineIds)` for batch queries
- [ ] Batch method queries uncached IDs in single database call and populates cache

### 4. Repository Extensions

- [ ] Add `GetByIdWithAccountAsync()` to `Data/Repositories/TransactionRepository.cs` (eager-load Account)
- [ ] Add `GetAccountMappingsAsync(IEnumerable<Guid> budgetLineIds)` to `Data/Repositories/BudgetLineRepository.cs`
- [ ] Returns `Dictionary<Guid, Guid>` mapping BudgetLineId → AccountId using single query

### 5. Validation Updates

- [ ] Update `DTOs/Validators/TransactionValidators.cs`: Inject `IBudgetAccountLookupService`, `ITransactionRepository`, `IAccountRepository`
- [ ] Add async validation rule in `AssignTransactionRequestValidator` checking account-budget plan consistency
- [ ] Add async validation rule in `SplitAllocationDtoValidator` using batch lookup for split collections
- [ ] Include account labels in error messages: "Category '{CategoryName}' belongs to budget plan for account '{AccountLabel}', but transaction is for account '{TransactionAccountLabel}'"
- [ ] Update `Services/TransactionAssignmentService.cs`: Add validation in `AssignToSingleAsync()` and `SplitAsync()` methods
- [ ] Throw `InvalidOperationException` with descriptive messages on validation failure

### 6. Query Updates

- [ ] Update all `TransactionSplit` queries to eager-load `BudgetLine.Category` instead of separate `Category` navigation
- [ ] Update `TransactionSplitRepository` queries to use new relationship path

### 7. Testing

- [ ] Create `BudgetAccountLookupServiceTests` for cache hit/miss scenarios
- [ ] Add tests for batch query optimization
- [ ] Create `CrossAccountValidationTests` integration tests verifying rejection at validator level
- [ ] Add integration tests verifying rejection at service level with proper error messages
- [ ] Test bulk split validation performance (100+ splits)
- [ ] Verify `BudgetLineId` required constraint enforcement

## Validation Rules

1. `TransactionSplit.BudgetLineId` is **required** (cannot be null)
2. `BudgetLine.BudgetPlan.AccountId` MUST equal `Transaction.AccountId` for all splits
3. Validation performed at both DTO validator level (FluentValidation) and service level
4. Batch validation used for multiple splits to optimize performance

## Technical Specifications

### Cache Configuration

```json
{
  "CacheOptions": {
    "BudgetMappingCacheDurationMinutes": 60
  }
}
```

### Cache Key Pattern

- Single lookup: `"budget-account:{budgetLineId}"`
- Absolute expiration: 60 minutes (configurable)
- Thread-safe via `IMemoryCache`

### Error Response Format (RFC 7231)

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Validation error",
  "status": 400,
  "detail": "Category assignment validation failed",
  "errors": {
    "BudgetLineId": [
      "Category 'Groceries' belongs to budget plan '2026 Household Budget' for account 'Savings Account', but transaction is associated with 'Checking Account'"
    ]
  }
}
```

### Performance Targets

- Cache hit: <1ms per validation
- Batch validation (100 splits): <100ms total
- Database query reduction: ~98% with 90% cache hit rate

## Success Criteria

- `TransactionSplit.BudgetLineId` is required (database constraint enforced)
- Cannot assign budget lines from wrong account's budget plan
- Validation enforced at both validator and service layers
- Error messages include account and budget plan labels for clarity
- Cache reduces database queries by ~98%
- Batch validation performs efficiently for bulk operations
- All unit and integration tests pass
