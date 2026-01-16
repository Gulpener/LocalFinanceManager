# UserStory-5: Basic Transaction Assignment UI

## Objective

Enable manual transaction-to-category assignment via Blazor UI, implementing foundational components and patterns for the transaction assignment feature series (US-5, US-5.1, US-6, US-7, US-8, US-9).

## Requirements

- Create reusable `CategorySelector.razor` component with account-based filtering
- Implement transaction assignment modal with category/budget line selection
- Display uncategorized transaction warnings in transaction list view
- Show assignment audit trail for individual transactions
- Enforce UserStory-4 validation (category budget plan must match account budget plan)
- Establish extensible service interfaces and error handling patterns
- Provide focused unit and integration tests for assignment and validation flows

> ⚠️ **UserStory-4 Validation:** `Category.BudgetPlanId` must match `Account.CurrentBudgetPlanId`. Return HTTP 400 with message: "Category '{CategoryName}' belongs to a budget plan for a different account."

## Patterns for Subsequent Stories

This story establishes foundational patterns that **MUST** be followed in US-5.1, US-6, US-7, US-8, and US-9:

### Component Patterns

```csharp
// CategorySelector.razor - Reusable category selection component
// Parameters:
//   - AccountId (Guid): Filter categories by account's current budget plan
//   - OnCategorySelected (EventCallback): Notify parent of selection
//   - AllowBudgetLineSelection (bool): Enable budget line dropdown
// Usage: Embed in modals/forms without modification
```

### Service Interface Pattern

```csharp
// ITransactionAssignmentService - Core assignment operations
// Methods follow async/await pattern with CancellationToken
// Return Result<T> or OperationResult for error handling
// Validate Category.BudgetPlanId matches Account.CurrentBudgetPlanId
```

### Error Handling Pattern

- Use RFC 7231 Problem Details format for validation errors
- Property-level errors with `errors` dictionary
- HTTP 400 for validation failures (account-budget mismatch)
- HTTP 409 for concurrency conflicts (RowVersion mismatch)
- Reload entity and retry on conflict (last-write-wins)

### Test Organization

- Unit tests: `LocalFinanceManager.Tests/Services/` (assignment logic, validation)
- Integration tests: `LocalFinanceManager.Tests/Integration/` (API endpoints, EF Core queries)
- Follow AAA pattern (Arrange, Act, Assert)
- Use `TestDbContextFactory` for in-memory SQLite

## Implementation Tasks

### 1. DTOs & Validation

- [ ] Create `AssignTransactionRequest` DTO with `TransactionId`, `CategoryId`, `BudgetLineId` (optional)
- [ ] Create `TransactionAssignmentResponse` DTO with `Success`, `CategoryName`, `AssignmentDate`
- [ ] Create `ValidationErrorResponse` DTO following RFC 7231 Problem Details format
- [ ] Add `AssignTransactionRequestValidator` using FluentValidation
- [ ] Validate `CategoryId` references existing category
- [ ] Add unit tests for DTO validation

### 2. Components - CategorySelector

- [ ] Create `CategorySelector.razor` component in `Components/Shared/`
- [ ] Add `@Parameter AccountId` (Guid) for filtering
- [ ] Add `@Parameter OnCategorySelected` (EventCallback<Guid>)
- [ ] Add `@Parameter AllowBudgetLineSelection` (bool, default false)
- [ ] Implement category dropdown filtered by `Account.CurrentBudgetPlanId`
- [ ] Add optional budget line dropdown (shown when category has budget lines)
- [ ] Display category type indicator (Income/Expense badge)
- [ ] Add loading spinner during category fetch
- [ ] Style with consistent badge/dropdown patterns

### 3. Components - TransactionAssignModal

- [ ] Create `TransactionAssignModal.razor` component in `Components/Shared/`
- [ ] Add `@Parameter Transaction` (TransactionDTO) to display details
- [ ] Embed `CategorySelector.razor` for category selection
- [ ] Display transaction info: Date, Description, Amount, Counterparty
- [ ] Show current assignment status (if already assigned)
- [ ] Add "Assign" button calling API endpoint
- [ ] Add "Cancel" button to close modal
- [ ] Display validation errors inline (Problem Details format)
- [ ] Handle HTTP 409 conflicts with reload + retry prompt
- [ ] Show success toast notification on assignment

### 4. Components - Transaction List Updates

- [ ] Update `Transactions.razor` to include assignment status column
- [ ] Add warning badge (⚠️ icon) for uncategorized transactions
- [ ] Add "Assign" action button per row opening `TransactionAssignModal`
- [ ] Display current category name badge for assigned transactions
- [ ] Add tooltip on warning badge: "Transaction not assigned to category"
- [ ] Add filter dropdown: All / Assigned / Uncategorized
- [ ] Implement pagination (page size 50 transactions)

### 5. Components - Assignment Audit Trail

- [ ] Create `AssignmentAuditTrail.razor` component in `Components/Shared/`
- [ ] Add `@Parameter TransactionId` (Guid) to load audit history
- [ ] Fetch audit records from `GET /api/transactions/{id}/audit` endpoint
- [ ] Display audit timeline: Timestamp, Category, User (if applicable), Auto-applied flag
- [ ] Show "Undo" indicator for undone assignments
- [ ] Format as vertical timeline with icons
- [ ] Include empty state message: "No assignment history"

### 6. Services - ITransactionAssignmentService

- [ ] Create `ITransactionAssignmentService` interface in `Services/`
- [ ] Add `AssignAsync(Guid transactionId, Guid categoryId, Guid? budgetLineId, CancellationToken ct)` method
- [ ] Add `ValidateAssignmentAsync(Guid transactionId, Guid categoryId, CancellationToken ct)` method
- [ ] Add `GetAssignmentHistoryAsync(Guid transactionId, CancellationToken ct)` method
- [ ] Return `Result<TransactionAssignmentResponse>` for error handling
- [ ] Implement service in `TransactionAssignmentService.cs`
- [ ] Inject `IRepository<Transaction>`, `IRepository<Category>`, `IRepository<Account>` via DI
- [ ] Implement UserStory-4 validation: Check `Category.BudgetPlanId == Account.CurrentBudgetPlanId`
- [ ] Throw `ValidationException` with clear message on mismatch
- [ ] Handle `DbUpdateConcurrencyException` with reload + retry (last-write-wins)

### 7. API Integration (Use Existing Endpoints)

- [ ] Document existing endpoint: `POST /api/transactions/{id}/assign`
- [ ] Document existing endpoint: `GET /api/transactions/{id}/audit`
- [ ] Ensure endpoints return Problem Details format on validation errors
- [ ] Verify HTTP 400 for account-budget mismatch errors
- [ ] Verify HTTP 409 for concurrency conflicts

### 8. Tests - Unit Tests

- [ ] Add unit tests for `AssignTransactionRequestValidator` in `LocalFinanceManager.Tests/Validators/`
- [ ] Add unit tests for `TransactionAssignmentService.AssignAsync()` in `LocalFinanceManager.Tests/Services/`
- [ ] Test happy path: Valid category assignment succeeds
- [ ] Test validation failure: Category from different budget plan rejected (HTTP 400)
- [ ] Test validation failure: Nonexistent category rejected
- [ ] Test concurrency conflict: RowVersion mismatch triggers retry
- [ ] Mock repositories with NSubstitute or Moq

### 9. Tests - Integration Tests

- [ ] Add integration tests for `POST /api/transactions/{id}/assign` in `LocalFinanceManager.Tests/Integration/`
- [ ] Use `TestDbContextFactory` with in-memory SQLite (`:memory:`)
- [ ] Seed test data: Account, BudgetPlan, Categories, Transaction
- [ ] Test successful assignment with matching budget plan
- [ ] Test validation error with mismatched budget plan (HTTP 400)
- [ ] Test audit trail creation after assignment
- [ ] Test filtering transactions by assignment status (assigned/uncategorized)
- [ ] Verify response format follows RFC 7231 Problem Details

### 10. Tests - E2E Tests (Basic Assignment)

> **Note:** Write E2E tests **immediately after** implementing corresponding UI components for faster feedback. Uses PageObjectModels and SeedDataHelper from UserStory-5.1.

- [ ] Create `BasicAssignmentTests.cs` test class in `LocalFinanceManager.E2E/Tests/`
- [ ] Use `SeedDataHelper` to create account, categories, and transactions in test setup
- [ ] Test: Navigate to Transactions page → Verify unassigned transactions show warning badges
  - Use `TransactionsPageModel.NavigateToAsync()`
  - Assert warning badge visible for unassigned transactions
- [ ] Test: Click "Assign" button on unassigned transaction → Modal opens with transaction details
  - Use `TransactionsPageModel.ClickAssignButtonAsync(transactionId)`
  - Assert `AssignmentModalPageModel.IsVisibleAsync()` returns true
- [ ] Test: Select category from `CategorySelector` dropdown → Click "Assign" → Transaction assigned successfully
  - Use `AssignmentModalPageModel.SelectCategoryAsync(categoryId)`
  - Use `AssignmentModalPageModel.ClickAssignAsync()`
  - Assert transaction shows category badge (no warning)
- [ ] Test: Verify assigned transaction shows category badge (no warning)
  - Assert badge text matches category name
- [ ] Test: Open assignment modal for assigned transaction → Shows current category → Re-assign to different category
  - Verify current category pre-selected in dropdown
  - Select new category and save
  - Assert category badge updated
- [ ] Test: Attempt to assign transaction with category from different budget plan → Validation error shown (HTTP 400)
  - Seed second account with different budget plan
  - Attempt mismatched assignment
  - Assert error message displayed in modal
- [ ] Test: Click "Audit Trail" link → Audit modal opens showing assignment history
  - Use `TransactionsPageModel.ClickAuditTrailAsync(transactionId)`
  - Assert audit entries displayed with timestamps
- [ ] Test: Filter transactions by "Uncategorized" → Only unassigned transactions shown
  - Use `TransactionsPageModel.SelectFilterAsync("Uncategorized")`
  - Assert only unassigned transactions visible
- [ ] Test: Filter transactions by "Assigned" → Only assigned transactions shown
  - Use `TransactionsPageModel.SelectFilterAsync("Assigned")`
  - Assert only assigned transactions visible
- [ ] Test: Pagination works correctly (50 transactions per page)
  - Seed 150 transactions
  - Assert page 1 shows 50 transactions
  - Navigate to page 2, assert next 50 transactions visible
- [ ] Add screenshots for key UI states (modal open, validation error, success toast)

## Testing

### Unit Test Scenarios

1. **Validation Logic:**

   - Valid assignment request passes validation
   - Missing CategoryId fails validation
   - Invalid TransactionId fails validation

2. **Service Logic:**

   - Assignment creates TransactionSplit with correct CategoryId/BudgetLineId
   - UserStory-4 validation rejects category from different budget plan
   - Concurrent updates handled with reload + retry

3. **Component Logic:**
   - CategorySelector filters categories by account's budget plan
   - TransactionAssignModal displays validation errors correctly
   - Warning badge shown for uncategorized transactions

### Integration Test Scenarios

1. **API Endpoint:**

   - POST /api/transactions/{id}/assign with valid data succeeds (HTTP 200)
   - POST /api/transactions/{id}/assign with mismatched budget plan fails (HTTP 400)
   - GET /api/transactions/{id}/audit returns audit trail

2. **End-to-End Flow:**
   - User selects transaction → Opens modal → Selects category → Assigns successfully
   - Warning badge disappears after assignment
   - Audit trail shows assignment record

## Success Criteria

- ✅ 100% of unassigned transactions display warning badge in list view
- ✅ Assignment success rate >95% for valid assignments (no errors)
- ✅ UserStory-4 validation enforced: Mismatched budget plans return HTTP 400
- ✅ Unit tests cover validation matrix (valid/invalid category, concurrency conflicts)
- ✅ Integration tests cover assignment API endpoint with seed data
- ✅ CategorySelector component reusable with clear parameter interface
- ✅ Error messages user-friendly and actionable (Problem Details format)

## Definition of Done

- Blazor UI components (`CategorySelector.razor`, `TransactionAssignModal.razor`, updated `Transactions.razor`) implemented and functional
- `ITransactionAssignmentService` interface and implementation with UserStory-4 validation
- Unit tests for validators and service logic (AAA pattern, in-memory repositories)
- Integration tests for assignment API endpoint (in-memory SQLite, seed data)
- No manual migrations required (automatic via `Database.MigrateAsync()` in `Program.cs`)
- Code follows Implementation-Guidelines.md (async/await, IOptions, nullable reference types)
- Pattern documentation clear for subsequent stories (US-5.1, US-6, US-7, US-8, US-9)

## Dependencies

- **UserStory-3 (Category Ownership):** REQUIRED - Categories must be budget-plan-scoped for filtering
- **UserStory-4 (Account-Budget Matching):** REQUIRED - Validation rules defined
- **UserStory-5.1 (E2E Infrastructure):** REQUIRED for E2E tests - Must complete US-5.1 before running E2E tests in this story. Can be developed in parallel with US-5 implementation tasks.
- **Existing Backend Services:** `TransactionsController.AssignAsync()` and audit endpoints already implemented

## Estimated Effort

**3-4 days** (~31 implementation tasks: 20 implementation + 11 E2E tests)

## Notes

- This story establishes foundational patterns. **Do NOT refactor** component signatures or service interfaces in US-6/7 without updating this story first.
- CategorySelector.razor designed for extensibility: Optional budget line selection added in US-6.
- Error handling pattern (Problem Details) ensures consistent UX across all assignment features.
- Pagination in transaction list prepares for performance optimization in US-8.
