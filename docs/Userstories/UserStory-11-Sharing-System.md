# Post-MVP-7: Implement Sharing System

## Objective

Enable users to share accounts and budget plans with other users, with configurable permission levels and cascade rules for related entities.

## Requirements

- Create `AccountShare` and `BudgetPlanShare` entities
- Support permission levels: Owner, Editor, Viewer
- Add sharing controllers and UI
- Update repositories to respect shared access
- Implement cascade rules (shared BudgetPlan includes Categories/BudgetLines/Transactions)
- Add authorization checks in all controllers

## Implementation Tasks

- [ ] Create `AccountShare.cs` entity:
  ```csharp
  public class AccountShare : BaseEntity
  {
      public Guid AccountId { get; set; }
      public string SharedWithUserId { get; set; }
      public PermissionLevel Permission { get; set; }  // Owner/Editor/Viewer
  }
  ```
- [ ] Create `BudgetPlanShare.cs` entity with same structure
- [ ] Create `PermissionLevel` enum:
  ```csharp
  public enum PermissionLevel { Owner, Editor, Viewer }
  ```
- [ ] Update `AppDbContext` with share entities and relationships
- [ ] Create migration for share tables
- [ ] Implement `SharingService` with methods:
  - `ShareAccountAsync(accountId, userId, permission)`
  - `ShareBudgetPlanAsync(planId, userId, permission)`
  - `RevokeAccessAsync(shareId)`
  - `GetUserAccessLevelAsync(resourceId, userId)`
- [ ] Update repository query filters to include shared resources:
  ```csharp
  .Where(e => e.UserId == currentUserId || e.Shares.Any(s => s.SharedWithUserId == currentUserId))
  ```
- [ ] Add authorization helper methods:
  - `CanView(resourceId, userId)`
  - `CanEdit(resourceId, userId)`
  - `IsOwner(resourceId, userId)`
- [ ] Update all controllers to check permissions before operations
- [ ] Create `SharingController` with endpoints:
  - `POST /api/accounts/{id}/share`
  - `POST /api/budgetplans/{id}/share`
  - `DELETE /api/shares/{id}`
  - `GET /api/accounts/{id}/shares`
  - `GET /api/budgetplans/{id}/shares`
- [ ] Add Blazor UI pages:
  - Share modal with user search
  - Shared with me dashboard section
  - Permission management UI
- [ ] Implement cascade rules: shared BudgetPlan grants access to Categories, BudgetLines, and Transactions

## Cascade Rules

- Shared Account → User can view/edit account details (based on permission)
- Shared BudgetPlan → User can view/edit budget plan, categories, budget lines, and transactions (based on permission)
- Viewer permission → Read-only access
- Editor permission → Read and write access (except delete)
- Owner permission → Full access (owner cannot be revoked)

## Testing

- Unit tests for permission checks
- Integration tests for shared resource access
- E2E tests for sharing workflow
- Verify viewers cannot edit
- Verify cascade rules work correctly
- Verify share revocation removes access

## Success Criteria

- Users can share accounts and budget plans
- Permission levels are enforced correctly
- Shared resources appear in user's dashboard
- Cascade rules grant appropriate access
- Revocation immediately removes access
- All authorization checks pass
