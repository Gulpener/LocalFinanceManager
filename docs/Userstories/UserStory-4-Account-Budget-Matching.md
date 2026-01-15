# Post-MVP-4: Enforce Account-Budget Plan Matching

## Objective

Enforce data integrity by validating that transaction splits reference categories from budget plans associated with the transaction's account. Allow uncategorized transactions with UI warnings.

## Requirements

- Add validation to `TransactionSplit` ensuring `Transaction.AccountId` matches `Category.BudgetPlan.AccountId`
- Update validators in `TransactionsController` to check account-plan consistency
- Allow uncategorized transactions with UI warnings
- Display warnings for transactions without category assignments

## Implementation Tasks

- [ ] Add validation method to `TransactionSplit.cs` or validator class
- [ ] Update `TransactionSplitValidator` to verify:
  ```
  Transaction.AccountId == Split.Category.BudgetPlan.AccountId
  ```
- [ ] Update `TransactionsController` to enforce validation on create/update
- [ ] Add `HasUnassignedSplits` computed property or method to `Transaction`
- [ ] Update Blazor UI to show warning icons for uncategorized transactions
- [ ] Add tooltip explaining why consistency is required
- [ ] Add unit tests for validation logic
- [ ] Add integration tests for cross-account assignment attempts

## Validation Rules

1. If a transaction split references a category, that category's budget plan MUST be associated with the transaction's account
2. Transactions may have zero category assignments (uncategorized)
3. Uncategorized transactions should display a warning in UI but are not blocked

## Error Handling

- Return HTTP 400 Bad Request with clear error message when validation fails
- Error message format: "Category '{CategoryName}' belongs to a budget plan for a different account"

## Success Criteria

- Cannot assign categories from wrong budget plan
- Validation enforced at API level
- Uncategorized transactions allowed
- UI clearly shows uncategorized transaction warnings
- All tests pass
