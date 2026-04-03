# Post-MVP-8: Implement Sharing System

## Objective

Enable users to share accounts and budget plans with other users via an invite-and-accept flow, with configurable permission levels and cascade rules for related entities.

## Design Decisions

- `SharedWithUserId` is `Guid` (local DB user ID, not Supabase UUID string)
- Shares are created with `Status = Pending`; access is only granted once the recipient **accepts**
- Share by exact email only — no user search/listing endpoint (prevents user enumeration)
- Shared resources appear in a dedicated **"Shared with me"** section, not mixed into the main dashboard
- `Declined` shares never grant access
- Only the owner can revoke a share (at any status)
- Repository filters only apply to `Status == Accepted` shares
- Cascade: an accepted shared `BudgetPlan` grants access to its Categories, BudgetLines, and Transactions

## Phase 1 — Domain Layer

- [ ] Create `Models/PermissionLevel.cs`:
  ```csharp
  public enum PermissionLevel { Owner, Editor, Viewer }
  ```
- [ ] Create `Models/ShareStatus.cs`:
  ```csharp
  public enum ShareStatus { Pending, Accepted, Declined }
  ```
- [ ] Create `Models/AccountShare.cs`:
  ```csharp
  public class AccountShare : BaseEntity
  {
      public Guid AccountId { get; set; }
      public Account Account { get; set; } = null!;
      public Guid SharedWithUserId { get; set; }
      public User SharedWithUser { get; set; } = null!;
      public PermissionLevel Permission { get; set; }
      public ShareStatus Status { get; set; } = ShareStatus.Pending;
  }
  ```
- [ ] Create `Models/BudgetPlanShare.cs` with same structure (`BudgetPlanId` instead of `AccountId`)

## Phase 2 — Data Layer

- [ ] Add inverse navigations:
  - `Account.Shares` → `ICollection<AccountShare>`
  - `BudgetPlan.Shares` → `ICollection<BudgetPlanShare>`
  - `User.AccountShares`, `User.BudgetPlanShares`
- [ ] Update `AppDbContext` — add `DbSet<AccountShare>`, `DbSet<BudgetPlanShare>`, configure FK relationships and `xmin` concurrency
- [ ] Generate EF Core migration for share tables (auto-applied at startup via `MigrateAsync`)

## Phase 3 — Service Layer

- [ ] Create `ISharingService` and `SharingService`:
  - `ShareAccountAsync(accountId, targetEmail, permission)` — exact `Users.Email` match; caller must be owner; blocks duplicate Pending/Accepted shares; returns 404 if email not found
  - `ShareBudgetPlanAsync(planId, targetEmail, permission)` — same guards
  - `AcceptShareAsync(shareId)` — recipient only; sets `Status = Accepted`
  - `DeclineShareAsync(shareId)` — recipient only; sets `Status = Declined`
  - `RevokeAccountShareAsync(shareId)` — owner only; works on any status
  - `RevokeBudgetPlanShareAsync(shareId)` — owner only
  - `GetPendingSharesForUserAsync(userId)` — all incoming Pending invitations for current user
  - `GetAccountSharesAsync(accountId)` — owner sees all shares with statuses
  - `GetBudgetPlanSharesAsync(planId)` — owner sees all shares with statuses
  - `GetUserAccountAccessLevelAsync(accountId, userId)` → `PermissionLevel?` (Accepted only)
  - `CanView(resourceId, userId)`, `CanEdit(resourceId, userId)`, `IsOwner(resourceId, userId)`

## Phase 4 — Repository Updates

- [ ] Update `AccountRepository` query filter to OR:
  ```csharp
  .Where(e => e.UserId == currentUserId
      || e.Shares.Any(s => s.SharedWithUserId == currentUserId && s.Status == ShareStatus.Accepted))
  ```
- [ ] Update `BudgetPlanRepository` — same filter pattern
- [ ] Update `TransactionRepository` and `CategoryRepository` for cascade: accepted shared BudgetPlan grants access to its linked Categories, BudgetLines, and Transactions

## Phase 5 — API Layer

- [ ] Create `DTOs/SharingDTOs.cs` — request/response DTOs for all share operations
- [ ] Create `Controllers/SharingController.cs` with endpoints:
  - `POST /api/accounts/{id}/share` — body: `{ email, permission }`; creates Pending share
  - `POST /api/budgetplans/{id}/share` — body: `{ email, permission }`
  - `POST /api/shares/accounts/{id}/accept` — recipient accepts
  - `POST /api/shares/accounts/{id}/decline` — recipient declines
  - `POST /api/shares/budgetplans/{id}/accept`
  - `POST /api/shares/budgetplans/{id}/decline`
  - `DELETE /api/shares/accounts/{id}` — owner revokes
  - `DELETE /api/shares/budgetplans/{id}` — owner revokes
  - `GET /api/accounts/{id}/shares` — owner sees all shares with statuses
  - `GET /api/budgetplans/{id}/shares`
  - `GET /api/shares/pending` — current user's incoming pending invitations
- [ ] Update `AccountsController` and `BudgetPlansController` — enforce `CanEdit`/`IsOwner` checks before mutating operations

## Phase 6 — Blazor UI

- [ ] Create `Components/Pages/Sharing/ShareModal.razor`:
  - Email input + permission dropdown
  - Error message if user not found or share already exists
- [ ] Create `Components/Pages/Sharing/SharedWithMe.razor`:
  - **Pending invitations** sub-section — Accept / Decline buttons per invite
  - **Accepted shares** sub-section — list of shared accounts and budget plans with permission label
- [ ] Owner permission management view (per resource): current shares list with status + Revoke button
- [ ] Pending invitations badge in nav layout — shows count of incoming Pending shares
- [ ] Wire share button into Account and BudgetPlan detail pages (owner-only, opens ShareModal)

## Cascade Rules

- Shared Account (Accepted) → access to account details based on permission level
- Shared BudgetPlan (Accepted) → access to budget plan, categories, budget lines, and transactions based on permission level
- Pending or Declined share → no access granted
- Viewer → read-only; Editor → read and write (no delete); Owner → full access; Owner share cannot be revoked

## Testing

- [ ] Unit tests: all permission levels, owner guards, duplicate share prevention, accept/decline transitions
- [ ] Integration tests:
  - Pending share does NOT grant repository access
  - Accepted share grants access
  - Viewer cannot mutate (403)
  - Cascade: accepted shared BudgetPlan grants access to its Transactions
  - Revocation immediately removes access
  - Declined share never grants access
- [ ] E2E: share → recipient sees pending invite → accepts → resource visible in "Shared with me" section

## Files to Create

- `LocalFinanceManager/Models/PermissionLevel.cs`
- `LocalFinanceManager/Models/ShareStatus.cs`
- `LocalFinanceManager/Models/AccountShare.cs`
- `LocalFinanceManager/Models/BudgetPlanShare.cs`
- `LocalFinanceManager/Services/ISharingService.cs`
- `LocalFinanceManager/Services/SharingService.cs`
- `LocalFinanceManager/Controllers/SharingController.cs`
- `LocalFinanceManager/DTOs/SharingDTOs.cs`
- `LocalFinanceManager/Components/Pages/Sharing/ShareModal.razor`
- `LocalFinanceManager/Components/Pages/Sharing/SharedWithMe.razor`
- `tests/LocalFinanceManager.Tests/Unit/SharingServiceTests.cs`
- `tests/LocalFinanceManager.Tests/Integration/SharingIntegrationTests.cs`
- `tests/LocalFinanceManager.E2E/SharingTests.cs`

## Files to Modify

- `LocalFinanceManager/Models/Account.cs` — add `Shares` navigation
- `LocalFinanceManager/Models/BudgetPlan.cs` — add `Shares` navigation
- `LocalFinanceManager/Models/User.cs` — add inverse share navigations
- `LocalFinanceManager/Data/AppDbContext.cs` — add DbSets and entity config
- `LocalFinanceManager/Data/Repositories/AccountRepository.cs` — extend filters (Accepted only)
- `LocalFinanceManager/Data/Repositories/BudgetPlanRepository.cs` — extend filters
- `LocalFinanceManager/Data/Repositories/TransactionRepository.cs` — cascade sharing
- `LocalFinanceManager/Controllers/AccountsController.cs` — permission checks
- `LocalFinanceManager/Controllers/BudgetPlansController.cs` — permission checks
- `LocalFinanceManager/Extensions/ServiceCollectionExtensions.cs` — register `ISharingService`
- `LocalFinanceManager/Components/Pages/Accounts/*` — share button (owner only)
- `LocalFinanceManager/Components/Pages/BudgetPlans/*` — share button (owner only)
- `LocalFinanceManager/Components/Layout/NavMenu.razor` — pending invitations badge

## Success Criteria

- Users can share accounts and budget plans by entering the recipient's exact email
- Recipient receives a pending invitation and must accept before gaining access
- Permission levels are enforced: Viewer read-only, Editor no delete, Owner full access
- Pending and Declined shares grant no repository access
- Shared resources appear only in the "Shared with me" section (not the main dashboard)
- Cascade rules grant appropriate access to linked entities
- Revocation immediately removes access at any share status
- All authorization checks pass
