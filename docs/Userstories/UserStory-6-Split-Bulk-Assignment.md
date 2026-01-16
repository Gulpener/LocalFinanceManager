# UserStory-6: Transaction Split & Bulk Assignment UI

## Objective

Enable advanced transaction assignment features: split single transactions across multiple categories with real-time validation, and bulk-assign multiple transactions to a single category with progress tracking.

## Requirements

- Create dynamic split editor UI with add/remove split rows
- Implement real-time sum validation (splits must equal transaction amount ±0.01 tolerance)
- Display per-split category selection using `CategorySelector.razor` from US-5
- Implement bulk assignment UI with multi-select checkboxes
- Show progress bar and error handling for bulk operations
- Enforce UserStory-4 validation for both split and bulk assignments
- Follow US-5 patterns: service interfaces, error handling, component structure
- Provide focused unit and integration tests for split validation and bulk operations

> ⚠️ **UserStory-4 Validation:** `Category.BudgetPlanId` must match `Account.CurrentBudgetPlanId`. Return HTTP 400 with message: "Category '{CategoryName}' belongs to a budget plan for a different account."

## Pattern Adherence from US-5

This story **MUST** follow patterns established in UserStory-5:

### Component Reuse (NO Modifications)

- **MUST reuse** `CategorySelector.razor` from US-5 without modification
  - Use `AllowBudgetLineSelection=true` parameter for split rows
  - Maintain AccountId filtering behavior

### Service Extension Pattern

- **MUST extend** `ITransactionAssignmentService` following interface patterns from US-5
  - Add `SplitTransactionAsync()` method
  - Add `BulkAssignAsync()` method
  - Return `Result<T>` for consistent error handling
  - Use same CancellationToken patterns

### Error Handling (Same Format)

- **MUST use** RFC 7231 Problem Details format from US-5
- HTTP 400 for validation errors (sum mismatch, budget plan mismatch)
- HTTP 409 for concurrency conflicts with reload + retry
- Property-level errors in `errors` dictionary

### Test Organization (Same Structure)

- Unit tests in `LocalFinanceManager.Tests/Services/`
- Integration tests in `LocalFinanceManager.Tests/Integration/`
- Use `TestDbContextFactory` for in-memory SQLite
- Follow AAA pattern

## Implementation Tasks

### 1. DTOs & Validation

- [ ] Create `SplitTransactionRequest` DTO with `TransactionId`, `Splits` (array of `SplitItemDto`)
- [ ] Create `SplitItemDto` with `CategoryId`, `BudgetLineId` (optional), `Amount`
- [ ] Create `BulkAssignRequest` DTO with `TransactionIds` (array), `CategoryId`, `BudgetLineId` (optional)
- [ ] Create `BulkAssignResponse` DTO with `SuccessCount`, `FailureCount`, `Errors` (array of transaction-specific errors)
- [ ] Add `SplitTransactionRequestValidator` using FluentValidation
- [ ] Validate sum of split amounts equals transaction amount (±0.01 tolerance)
- [ ] Validate each split has valid `CategoryId`
- [ ] Validate at least 2 splits required (minimum for split transaction)
- [ ] Add unit tests for split sum validation edge cases (exact match, +0.01, -0.01, >0.01)

### 2. Components - SplitEditor

- [ ] Create `SplitEditor.razor` component in `Components/Shared/`
- [ ] Add `@Parameter Transaction` (TransactionDTO) to display total amount
- [ ] Add `@Parameter OnSplitSaved` (EventCallback) for parent notification
- [ ] Display transaction summary: Description, Total Amount, Account
- [ ] Create dynamic split row list (initially 2 rows)
- [ ] Each row contains:
  - Embed `CategorySelector.razor` from US-5 (with `AllowBudgetLineSelection=true`)
  - Amount input field (decimal, 2 decimal places)
  - "Remove" button (disable if only 2 rows remain)
- [ ] Add "Add Split" button to insert new row
- [ ] Display real-time sum calculation: `Σ Splits` vs `Transaction Amount`
- [ ] Show visual indicator: Green checkmark (sum matches ±0.01) or Red warning (sum mismatch)
- [ ] Display difference: `Remaining: {TransactionAmount - Σ Splits}`
- [ ] Disable "Save" button if sum validation fails
- [ ] Call `POST /api/transactions/{id}/split` on save
- [ ] Display validation errors inline (Problem Details format from US-11.1)
- [ ] Show success toast notification on save

### 3. Components - Transaction List Updates (Split Support)

- [ ] Update `Transactions.razor` to add "Split" action button per row
- [ ] Open `SplitEditor.razor` modal when "Split" button clicked
- [ ] Display split indicator badge for transactions with multiple splits
- [ ] Show category breakdown tooltip on hover (e.g., "3 splits: Food €20, Transport €15, Entertainment €10")
- [ ] Update assignment status column to show "Split" badge instead of single category

### 4. Components - BulkAssignModal

- [ ] Create `BulkAssignModal.razor` component in `Components/Shared/`
- [ ] Add `@Parameter TransactionIds` (List<Guid>) for selected transactions
- [ ] Add `@Parameter OnBulkAssigned` (EventCallback) for parent notification
- [ ] Display count of selected transactions: "Assign {Count} transactions"
- [ ] Embed `CategorySelector.razor` from US-5 for single category selection
- [ ] Add confirmation message: "All selected transactions will be assigned to {CategoryName}"
- [ ] Show "Assign All" button calling `POST /api/transactions/bulk-assign`
- [ ] Display progress bar during bulk operation (percentage based on `SuccessCount` / `Total`)
- [ ] Show partial success summary: "{SuccessCount} assigned, {FailureCount} failed"
- [ ] Display per-transaction errors in expandable accordion (if any failures)
- [ ] Add "Close" button after operation completes

### 5. Components - Transaction List Updates (Bulk Support)

- [ ] Update `Transactions.razor` to add checkbox column (first column)
- [ ] Add "Select All" checkbox in table header
- [ ] Show bulk action toolbar when 1+ transactions selected (fixed bottom bar)
- [ ] Bulk toolbar contains:
  - Selected count: "{Count} transactions selected"
  - "Bulk Assign" button opening `BulkAssignModal`
  - "Deselect All" button
- [ ] Hide checkbox column when no transactions loaded
- [ ] Persist selections across pagination (store selected IDs in component state)

### 6. Services - ITransactionAssignmentService Extension

- [ ] Extend `ITransactionAssignmentService` interface from US-11.1
- [ ] Add `SplitTransactionAsync(Guid transactionId, List<SplitItemDto> splits, CancellationToken ct)` method
- [ ] Add `BulkAssignAsync(List<Guid> transactionIds, Guid categoryId, Guid? budgetLineId, CancellationToken ct)` method
- [ ] Implement methods in `TransactionAssignmentService.cs`
- [ ] Inject `IRepository<TransactionSplit>` via DI
- [ ] **Split validation:**
  - Verify sum of split amounts equals transaction amount (±0.01 tolerance)
  - Verify each split's `Category.BudgetPlanId == Account.CurrentBudgetPlanId` (UserStory-4)
  - Delete existing splits and create new splits atomically
- [ ] **Bulk validation:**
  - Validate all transactions belong to same account (or skip validation and handle per-transaction errors)
  - Verify `Category.BudgetPlanId` matches each transaction's account budget plan
  - Use transaction scope for atomicity (all or nothing) OR partial success with error collection
- [ ] Handle `DbUpdateConcurrencyException` with reload + retry (last-write-wins)
- [ ] Return `Result<BulkAssignResponse>` with success/failure details

### 7. API Integration (Use Existing Endpoints)

- [ ] Document existing endpoint: `POST /api/transactions/{id}/split`
- [ ] Document existing endpoint: `POST /api/transactions/bulk-assign`
- [ ] Verify endpoints return RFC 7231 Problem Details on validation errors
- [ ] Verify HTTP 400 for sum mismatch and account-budget mismatch errors
- [ ] Verify HTTP 409 for concurrency conflicts
- [ ] Verify bulk endpoint returns partial success details (success count + error list)

### 8. Tests - Unit Tests (Split)

- [ ] Add unit tests for `SplitTransactionRequestValidator` in `LocalFinanceManager.Tests/Validators/`
- [ ] Test sum validation matrix:
  - Exact match (€100.00 transaction, splits sum €100.00) → Pass
  - Within tolerance (€100.00 transaction, splits sum €100.01) → Pass
  - Within tolerance (€100.00 transaction, splits sum €99.99) → Pass
  - Out of tolerance (€100.00 transaction, splits sum €100.02) → Fail
  - Out of tolerance (€100.00 transaction, splits sum €99.98) → Fail
- [ ] Test minimum split count validation (require ≥2 splits)
- [ ] Test UserStory-4 validation: Split with category from different budget plan rejected
- [ ] Mock repositories with NSubstitute or Moq

### 9. Tests - Unit Tests (Bulk)

- [ ] Add unit tests for `TransactionAssignmentService.BulkAssignAsync()` in `LocalFinanceManager.Tests/Services/`
- [ ] Test successful bulk assignment (all transactions same account, valid category)
- [ ] Test partial failure (some transactions fail validation, others succeed)
- [ ] Test validation failure: Category from different budget plan rejected for all transactions
- [ ] Test concurrency conflict during bulk operation (retry behavior)

### 10. Tests - Integration Tests (Split)

- [ ] Add integration tests for `POST /api/transactions/{id}/split` in `LocalFinanceManager.Tests/Integration/`
- [ ] Use `TestDbContextFactory` with in-memory SQLite (`:memory:`)
- [ ] Seed test data: Account, BudgetPlan, Categories (Food, Transport), Transaction (€100)
- [ ] Test successful split (€60 Food + €40 Transport = €100) → HTTP 200
- [ ] Test sum mismatch (€60 Food + €30 Transport = €90 ≠ €100) → HTTP 400
- [ ] Test budget plan mismatch (split with category from different budget plan) → HTTP 400
- [ ] Verify existing splits deleted and replaced with new splits
- [ ] Verify audit trail records split operation

### 11. Tests - Integration Tests (Bulk)

- [ ] Add integration tests for `POST /api/transactions/bulk-assign` in `LocalFinanceManager.Tests/Integration/`
- [ ] Seed test data: Account, BudgetPlan, Categories, 10 Transactions
- [ ] Test successful bulk assignment (all 10 transactions assigned) → HTTP 200, `SuccessCount=10, FailureCount=0`
- [ ] Test partial failure (5 valid, 5 with mismatched budget plan) → HTTP 200 or 207, `SuccessCount=5, FailureCount=5`
- [ ] Verify audit trail records bulk assignments
- [ ] Verify transaction list filters show updated assignment status

## Testing

### Unit Test Scenarios

1. **Split Validation:**

   - Sum exactly matches transaction amount (€100.00 = €100.00)
   - Sum within tolerance +0.01 (€100.00 vs €100.01)
   - Sum within tolerance -0.01 (€100.00 vs €99.99)
   - Sum exceeds tolerance +0.02 (€100.00 vs €100.02) → Fail
   - Sum below tolerance -0.02 (€100.00 vs €99.98) → Fail
   - Minimum 2 splits required
   - Each split has valid CategoryId

2. **Bulk Assignment:**

   - All transactions assigned successfully (same account, valid category)
   - Partial success: Some transactions fail validation
   - UserStory-4 validation rejects mismatched budget plans

3. **Component Logic:**
   - SplitEditor shows green checkmark when sum matches ±0.01
   - SplitEditor disables "Save" button when sum mismatch >0.01
   - BulkAssignModal displays progress bar (0-100%)
   - BulkAssignModal shows per-transaction error details

### Integration Test Scenarios

1. **Split Endpoint:**

   - POST /api/transactions/{id}/split with valid splits succeeds (HTTP 200)
   - POST /api/transactions/{id}/split with sum mismatch fails (HTTP 400)
   - POST /api/transactions/{id}/split with mismatched budget plan fails (HTTP 400)

2. **Bulk Endpoint:**

   - POST /api/transactions/bulk-assign with valid data assigns all (HTTP 200)
   - POST /api/transactions/bulk-assign with mixed validity returns partial success (HTTP 200 or 207)

3. **End-to-End Flow:**
   - User selects transaction → Opens split editor → Adds 3 splits → Sum validates → Saves successfully
   - User selects 10 transactions → Opens bulk modal → Selects category → Bulk assigns with progress bar
   - Warning badges disappear after bulk assignment

## Success Criteria

- ✅ Split editor validates sum in real-time with ±0.01 tolerance
- ✅ Split editor shows visual feedback (green checkmark / red warning) for sum status
- ✅ Bulk assignment handles 100+ transactions with progress bar
- ✅ Bulk assignment displays partial success summary (success count + failure details)
- ✅ UserStory-4 validation enforced for both split and bulk operations (HTTP 400)
- ✅ Unit tests cover split sum validation matrix (5 edge cases)
- ✅ Integration tests cover split and bulk API endpoints with seed data
- ✅ `CategorySelector.razor` from US-5 reused without modification
- ✅ Error messages follow RFC 7231 Problem Details format from US-5

## Definition of Done

- Blazor UI components (`SplitEditor.razor`, `BulkAssignModal.razor`, updated `Transactions.razor`) implemented and functional
- `ITransactionAssignmentService` extended with `SplitTransactionAsync()` and `BulkAssignAsync()` methods
- Real-time sum validation in split editor with ±0.01 tolerance
- Bulk assignment progress tracking with partial success handling
- Unit tests for split validation matrix and bulk operations (AAA pattern, in-memory repositories)
- Integration tests for split and bulk API endpoints (in-memory SQLite, seed data)
- UserStory-4 validation enforced for split and bulk assignments
- No manual migrations required (automatic via `Database.MigrateAsync()` in `Program.cs`)
- Code follows Implementation-Guidelines.md and US-5 patterns
- `CategorySelector.razor` component reused from US-5 without modification

## Dependencies

- **UserStory-5 (Basic Assignment UI):** ⚠️ **MUST complete US-5 before starting US-6.** Review US-5 patterns section before implementation.
  - Reuses `CategorySelector.razor` component
  - Extends `ITransactionAssignmentService` interface
  - Follows error handling patterns (Problem Details format)
  - Follows test organization structure
- **UserStory-3 (Category Ownership):** REQUIRED - Categories must be budget-plan-scoped
- **UserStory-4 (Account-Budget Matching):** REQUIRED - Validation rules enforced for splits and bulk
- **Existing Backend Services:** `TransactionsController.SplitAsync()` and `TransactionsController.BulkAssignAsync()` already implemented

## Estimated Effort

**3-4 days** (~30-35 implementation tasks)

## Notes

- Split editor designed for flexibility: Support 2-10 splits per transaction (UI limit to prevent performance issues).
- Bulk assignment uses partial success pattern: Some transactions can succeed while others fail (return success count + error list).
- Progress bar critical for UX: Bulk operations on 100+ transactions may take 5-10 seconds.
- Sum validation tolerance (±0.01) accounts for floating-point precision issues and rounding errors.
