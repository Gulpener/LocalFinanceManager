# Post-MVP-10: Improve Application Flow

## Objective

Enhance user experience with authentication guards, dashboard improvements, breadcrumb navigation, and onboarding wizard for new users.

## Requirements

- Add authentication guards to protected pages
- Create user dashboard showing owned and shared resources
- Display uncategorized transaction warnings
- Implement breadcrumb navigation
- Add onboarding wizard with template selection
- Refactor Blazor pages for multi-tenant UX

## Implementation Tasks

### Authentication Guards

- [ ] Create `AuthGuard` component or service
- [ ] Add authentication state check to all protected pages
- [ ] Redirect unauthenticated users to login page
- [ ] Store and restore intended destination after login

### User Dashboard

- [ ] Create `Dashboard.razor` as home page:
  - Section: My Accounts (owned)
  - Section: Shared with Me (accounts and budget plans)
  - Section: Recent Transactions
  - Section: Uncategorized Transactions (with warning count)
- [ ] Add quick action buttons:
  - Create New Account
  - Create New Budget Plan
  - Import Transactions
  - View All Transactions
- [ ] Display summary statistics:
  - Total accounts
  - Active budget plans
  - Uncategorized transaction count (with warning icon)
  - Recent activity timeline

### Breadcrumb Navigation

- [ ] Create `Breadcrumb.razor` component
- [ ] Add to layout header
- [ ] Auto-generate breadcrumbs based on route hierarchy:
  - Home → Accounts → Account Details
  - Home → Budget Plans → Budget Plan → Categories
- [ ] Add navigation helpers for complex paths

### Onboarding Wizard

- [ ] Create `Onboarding.razor` wizard page
- [ ] Show wizard on first login (check user's account count)
- [ ] Step 1: Welcome message and app overview
- [ ] Step 2: Create first account (IBAN, currency, name)
- [ ] Step 3: Create first budget plan with template selection:
  - Personal
  - Business
  - Household
- [ ] Step 4: Review and customize categories from template
- [ ] Step 5: Complete setup and redirect to dashboard
- [ ] Add "Skip" option to complete later

### Multi-Tenant UX Refactoring

- [ ] Update all list pages to show owner info for shared items
- [ ] Add permission badges (Owner, Editor, Viewer) to shared resources
- [ ] Add filter dropdown: All / Owned by Me / Shared with Me
- [ ] Update edit/delete buttons visibility based on permissions
- [ ] Show "Shared by [username]" label on shared resource cards

### Uncategorized Transaction Warnings

- [ ] Add warning icon to uncategorized transactions in list view
- [ ] Add tooltip: "This transaction is not assigned to any category"
- [ ] Add dashboard widget: "You have X uncategorized transactions"
- [ ] Add quick action: "Categorize Now" button linking to transaction assignment page

## UI Components to Create

- `Dashboard.razor`
- `Onboarding.razor`
- `Breadcrumb.razor`
- `AuthGuard.razor` or `AuthGuardService.cs`
- `ResourceCard.razor` (reusable card component with permission badges)
- `QuickActions.razor` (dashboard quick action menu)

## Testing

- E2E tests for complete onboarding flow
- E2E tests for dashboard interactions
- Verify authentication guards redirect correctly
- Verify breadcrumbs update correctly on navigation
- Verify shared resource labels and badges display correctly

## Success Criteria

- Unauthenticated users cannot access protected pages
- Dashboard provides clear overview of user's data
- Uncategorized transaction warnings are visible
- Onboarding wizard guides new users through setup
- Breadcrumbs help users navigate complex hierarchies
- Multi-tenant UI clearly shows ownership and permissions
- User experience is intuitive and polished
