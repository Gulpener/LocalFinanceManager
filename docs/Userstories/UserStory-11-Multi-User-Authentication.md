# UserStory-11: Multi-User Authentication with Email Verification

## Objective

Implement multi-user authentication with Supabase Auth, enforce email verification before login, and enforce user data isolation across the application.

This story is the identity and tenancy foundation for later stories and must be completed before UserStory-12 and UserStory-15 are finalized.

## Locked Decisions (March 2026)

- Auth model: **Hybrid cookie + JWT**
  - Cookie-based auth state for Blazor Server route/UI protection
  - JWT bearer validation for API endpoints
- Cross-user access behavior: **HTTP 403 Forbidden**
- Local user lifecycle: **Create/sync local `User` on first successful login**
- Ownership scope: `AppSettings` and `MLModel` are **per-user** entities
- Token storage: JWT remains in **sessionStorage only** (no localStorage, no refresh tokens, no remember-me)
- E2E strategy: **mocked authentication by default** (no second Supabase project)

## Requirements

- Supabase Auth integration with JWT token validation
- Email verification required before login, including "Resend verification email" support
- Local `User` entity with `SupabaseUserId`, `Email`, `DisplayName`, and `EmailConfirmed`
- Add `Guid UserId` ownership to entities inheriting from `BaseEntity`
- Full Blazor auth UX: Login, Register, Logout, Password Reset
- User-scoped repository queries filtered by `UserId`
- `[Authorize]` protection on API controllers
- Route/UI protection for unauthenticated users in Blazor
- E2E tests that validate auth-required flows with deterministic mocked identities

## Prerequisites (User Manual Setup Required)

- [ ] **(User Manual)** Create **one** Supabase project via https://app.supabase.com:
  - Authentication → Providers → Email
  - Enable Email provider
  - Enable **Confirm email**
  - Verify email templates (confirmation + password reset)
  - Copy URL, anon key, JWT secret
- [ ] **(User Manual)** Configure local User Secrets (single project credentials)
- [ ] **(User Manual)** Configure GitHub Secrets:
  - `SUPABASE_URL`
  - `SUPABASE_KEY`
  - `JWT_SECRET`

### Step-by-step setup guide (single Supabase project)

1. Create one Supabase project
  - Open https://app.supabase.com and create a new project.
  - Wait until project provisioning is complete.

2. Enable email authentication + verification
  - Go to **Authentication → Providers → Email**.
  - Enable **Email** provider.
  - Enable **Confirm email**.
  - Save changes.

3. Verify auth templates
  - Go to **Authentication → Email Templates**.
  - Confirm templates exist for:
    - Confirm signup
    - Reset password

4. Copy required API values
  - Go to **Settings → API**.
  - Copy:
    - Project URL
    - anon/public key
    - JWT secret

5. Configure local user-secrets for this repo
  - Run from repository root:
  ```powershell
  dotnet user-secrets init --project LocalFinanceManager
  dotnet user-secrets set "Supabase:Url" "https://<project-ref>.supabase.co" --project LocalFinanceManager
  dotnet user-secrets set "Supabase:Key" "<anon-key>" --project LocalFinanceManager
  dotnet user-secrets set "Supabase:JwtSecret" "<jwt-secret>" --project LocalFinanceManager
  dotnet user-secrets list --project LocalFinanceManager
  ```

6. Configure GitHub repository secrets
  - Open GitHub repository → **Settings → Secrets and variables → Actions**.
  - Create these secrets:
    - `SUPABASE_URL`
    - `SUPABASE_KEY`
    - `JWT_SECRET`

7. Validate configuration locally
  - Start app and verify config resolves without startup errors:
  ```powershell
  dotnet build LocalFinanceManager.sln
  dotnet test tests/LocalFinanceManager.Tests/
  ```
  - Note: E2E auth runs in mocked mode by default for this story.

## Dependencies

This story blocks:

- **UserStory-12: Supabase PostgreSQL Migration**
  - Depends on finalized identity mapping and entity ownership model
- **UserStory-13: Sharing System**
  - Depends on tenant-safe multi-user foundation
- **UserStory-15: Application Flow**
  - Depends on auth guards, login redirects, and user context

## Migration & Data Note

This is a schema-affecting change and may require data migration/backfill.

- Primary flow follows project guidelines: **EF Core migrations are applied automatically at app startup** (`Database.MigrateAsync()` in `Program.cs`).
- A destructive database reset is **optional development fallback only**, not the default path.
- If reset is used in development, treat as data-loss operation.

## Technical Decisions

This story follows `docs/Implementation-Guidelines.md`:

- .NET `net10.0`
- `IOptions<T>` configuration pattern
- Async all the way (`async/await`)
- Scoped DI services (`IUserContext`, `IAuthService`)
- SQLite (current story scope), startup automatic migrations
- Built-in `ILogger<T>` logging
- RFC 7231 Problem Details error responses

## Implementation Tasks

### 1) Supabase Setup (User Manual)

- [ ] Create one Supabase project per prerequisites
- [ ] Configure local User Secrets
- [ ] Configure GitHub Actions secrets

### 2) Configuration & Packages (Agent)

- [ ] Add `<UserSecretsId>` in `LocalFinanceManager.csproj`
- [ ] Install packages:
  - `Supabase.Gotrue`
  - `Microsoft.AspNetCore.Authentication.JwtBearer`
- [ ] Add `SupabaseOptions` configuration class
- [ ] Add placeholder `Supabase` section in `appsettings.json`

### 3) Startup Auth Wiring (Agent)

- [ ] Configure JWT bearer validation in `Program.cs`
- [ ] Configure cookie authentication for Blazor auth state
- [ ] Register and enable authentication/authorization middleware in correct order
- [ ] Register `IHttpContextAccessor`

### 4) User & Ownership Schema (Agent)

- [ ] Add `User` entity with required fields (`SupabaseUserId`, `Email`, `DisplayName`, `EmailConfirmed`)
- [ ] Add `Guid UserId` (and navigation where appropriate) to `BaseEntity`
- [ ] Update entities and model configuration for ownership
- [ ] Ensure `AppSettings` and `MLModel` are user-owned
- [ ] Add/update migration for multi-user support

### 5) User Context & Provisioning (Agent)

- [ ] Implement `IUserContext` and `UserContext`
- [ ] Implement local user provisioning/sync on first successful login
- [ ] Map Supabase identity (`sub`) to local user ownership ID

### 6) Repository Tenant Enforcement (Agent)

- [ ] Update repositories to scope all active queries by current `UserId`
- [ ] Ensure archive/get/update paths preserve user scoping
- [ ] Keep soft-delete filtering (`!IsArchived`) in combination with ownership filtering

### 7) API Authorization (Agent)

- [ ] Add `[Authorize]` to API controllers
- [ ] Ensure unauthenticated requests return `401`
- [ ] Ensure cross-user access attempts return `403` with Problem Details response shape

### 8) Blazor Authentication UI (Agent)

- [ ] Add auth service abstraction (`IAuthService`) and implementation
- [ ] Add custom authentication state provider
- [ ] Add pages:
  - `Login.razor`
  - `Register.razor`
  - `Logout.razor`
  - `PasswordReset.razor`
- [ ] Implement resend verification action in login flow
- [ ] Wrap app routes with `CascadingAuthenticationState`
- [ ] Add authenticated/unauthenticated actions in layout/nav

### 9) E2E Auth Mocking + Tests (Agent)

- [ ] Add E2E test authentication handler (mock scheme) in `tests/LocalFinanceManager.E2E`
- [ ] Wire mock auth in `TestWebApplicationFactory` (enabled by default for E2E)
- [ ] Add helper to switch test identities (User A / User B) for isolation scenarios
- [ ] Add/update E2E tests for:
  - Protected routes and `401`
  - Cross-user access and `403`
  - Session behavior in UI
- [ ] Keep one **manual/optional** real Supabase verification scenario (not required per PR)

### 10) CI/CD (Agent)

- [ ] Update `.github/workflows/ci.yml` so E2E runs with mocked auth by default
- [ ] Keep only single-project Supabase secrets (`SUPABASE_URL`, `SUPABASE_KEY`, `JWT_SECRET`)

## Testing Scenarios

### Authentication

- [ ] Register with valid email/password
- [ ] Unverified login blocked with clear error + resend verification option
- [ ] Verified login succeeds
- [ ] Invalid credentials return `401`
- [ ] Expired/invalid bearer token returns `401`

### Session Management

- [ ] JWT is stored only in sessionStorage
- [ ] Closing tab ends session (sessionStorage cleared)
- [ ] No refresh token behavior exists

### Authorization & Isolation

- [ ] `[Authorize]` endpoints reject anonymous requests with `401`
- [ ] User A cannot access User B resources (`403` on valid foreign IDs)
- [ ] Repository layer enforces `UserId` filtering consistently

### Blazor Route Guarding

- [ ] Unauthenticated users on protected pages are redirected to `/login`
- [ ] Authenticated users access protected pages successfully

### E2E Environment

- [ ] E2E tests run with mocked identities by default
- [ ] E2E can switch identity to validate user isolation deterministically
- [ ] Optional manual smoke test against real Supabase remains available

## Definition of Done

- [ ] Single Supabase environment configured and validated
- [ ] Local and CI secrets configured (`SUPABASE_URL`, `SUPABASE_KEY`, `JWT_SECRET`)
- [ ] Hybrid auth (cookie + JWT) wired in startup
- [ ] `User` entity and `UserId` ownership model implemented
- [ ] `AppSettings` and `MLModel` are per-user
- [ ] Repositories enforce tenant filtering by current user
- [ ] API endpoints protected and return expected `401`/`403`
- [ ] Login/Register/Logout/PasswordReset pages functional
- [ ] Email verification + resend flow works end-to-end
- [ ] JWT stored in sessionStorage only
- [ ] Unit/integration/E2E tests pass
- [ ] E2E suite uses mocked auth by default in CI
- [ ] Story archived after completion to `docs/Userstories/Archive/UserStory-11-Multi-User-Authentication.md`

## CLI Commands Reference

```powershell
# Install required packages
dotnet add LocalFinanceManager package Supabase.Gotrue
dotnet add LocalFinanceManager package Microsoft.AspNetCore.Authentication.JwtBearer

# Configure local user secrets (single Supabase project)
dotnet user-secrets init --project LocalFinanceManager
dotnet user-secrets set "Supabase:Url" "https://<project-ref>.supabase.co" --project LocalFinanceManager
dotnet user-secrets set "Supabase:Key" "<anon-key>" --project LocalFinanceManager
dotnet user-secrets set "Supabase:JwtSecret" "<jwt-secret>" --project LocalFinanceManager
dotnet user-secrets list --project LocalFinanceManager

# Create migration for multi-user support
dotnet ef migrations add AddMultiUserSupport --project LocalFinanceManager

# Optional dev-only reset fallback (destructive)
dotnet ef database drop --force --project LocalFinanceManager

# Build & test
dotnet build LocalFinanceManager.sln
dotnet test tests/LocalFinanceManager.Tests/
dotnet test tests/LocalFinanceManager.E2E/
```

## Success Criteria

- Multi-user authentication works with Supabase
- Email verification is enforced in production flow
- Data isolation is enforced by design and by tests
- Cross-user access attempts fail with `403`
- Blazor protected routes behave correctly for auth state
- CI validates auth-required behavior with mocked E2E identities

## Manual Testing Checklist

- [ ] Register user and verify confirmation email is received
- [ ] Attempt login before verification; confirm blocked state + resend option
- [ ] Verify email and login successfully
- [ ] Confirm JWT is in sessionStorage
- [ ] Close tab, reopen app, confirm re-login is required
- [ ] Execute password reset flow end-to-end
- [ ] Create second user and verify user isolation (`403` cross-user access)
- [ ] Verify E2E auth tests pass in CI using mock auth mode

---

**Upon completion:** archive this story to `docs/Userstories/Archive/UserStory-11-Multi-User-Authentication.md` immediately after implementation, tests, and DoD validation are complete.
