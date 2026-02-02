# UserStory-11: Multi-User Authentication with Email Verification

## Objective

Integrate Supabase Auth with JWT-based authentication, enforce email verification before login, and implement multi-user data isolation across all entities.

## Requirements

- Supabase Auth integration with JWT token validation
- Email verification required before login with "Resend verification email" functionality
- User entity with `SupabaseUserId`, `Email`, `DisplayName`, and `EmailConfirmed` properties
- Add `Guid UserId` property to all entities inheriting from `BaseEntity`
- Full Blazor authentication UI: Login, Register, Logout, and Password Reset pages
- JWT tokens stored in sessionStorage (session-only, no refresh tokens or "Remember Me")
- User-scoped repository queries filtering by `UserId`
- Authorization attributes on all API controllers

## Prerequisites (User Manual Setup Required)

- [ ] **(User Manual)** Create **production** Supabase project via [Supabase Dashboard](https://app.supabase.com):
  - Navigate to Authentication → Providers → Email
  - Enable "Email" provider
  - **Enable "Confirm email" setting** to require email verification
  - Verify default email templates are configured (confirmation email, password reset email)
  - Copy project URL, anon key, and JWT secret from Settings → API
- [ ] **(User Manual)** Create **test** Supabase project for E2E tests:
  - Create separate project for testing (e.g., `localfinancemanager-test`)
  - Enable "Email" provider
  - **Disable "Confirm email"** setting (allows E2E tests to login without email verification)
  - Copy test project URL, anon key, and JWT secret
- [ ] **(User Manual)** Configure User Secrets for local development (production credentials):
  ```powershell
  dotnet user-secrets init --project LocalFinanceManager
  dotnet user-secrets set "Supabase:Url" "https://<prod-project-ref>.supabase.co" --project LocalFinanceManager
  dotnet user-secrets set "Supabase:Key" "<prod-anon-key>" --project LocalFinanceManager
  dotnet user-secrets set "Supabase:JwtSecret" "<prod-jwt-secret>" --project LocalFinanceManager
  ```
- [ ] **(User Manual)** Configure GitHub Secrets for CI/CD:
  - Production secrets: `SUPABASE_URL`, `SUPABASE_KEY`, `JWT_SECRET`
  - Test environment secrets (for E2E tests): `SUPABASE_TEST_URL`, `SUPABASE_TEST_KEY`, `SUPABASE_TEST_JWT_SECRET`
  - Navigate to GitHub repository → Settings → Secrets and variables → Actions → New repository secret

## Dependencies

This user story **blocks** the following stories:
- **UserStory-12: Supabase PostgreSQL Migration** (requires `UserId` on entities)
- **UserStory-13: Sharing System** (requires multi-user foundation)

## ⚠️ Breaking Change Warning

**This is a BREAKING CHANGE that requires database recreation:**
- Existing `localfinancemanager.db` will be incompatible after adding `UserId` to `BaseEntity`
- **Action Required:** Delete existing database file and run fresh migrations
- **Data Loss:** All existing accounts, transactions, budgets, and categories will be lost (acceptable for pre-production development phase)
- **Migration Commands:**
  ```powershell
  dotnet ef database drop --force --project LocalFinanceManager
  dotnet ef database update --project LocalFinanceManager
  ```

## Technical Decisions

This user story follows technical patterns documented in `docs/Implementation-Guidelines.md`:
- **.NET Version:** `net10.0` target framework
- **Configuration:** `IOptions<T>` pattern for `SupabaseOptions` configuration class
- **Async Patterns:** All I/O operations use `async/await`
- **DI Conventions:** Scoped services with `I<Name>` interfaces (`IUserContext`, `IAuthService`)
- **Database:** SQLite with EF Core automatic migrations applied at startup
- **Logging:** Built-in `Microsoft.Extensions.Logging` (`ILogger<T>`)
- **Error Responses:** RFC 7231 Problem Details format for API errors
- **Validation:** FluentValidation for DTOs, DataAnnotations for Blazor forms

## Implementation Tasks

### 1. Supabase Setup (User Manual)

- [ ] **(User Manual)** Create production Supabase project with email verification enabled (see Prerequisites section)
- [ ] **(User Manual)** Create test Supabase project with email verification disabled (see Prerequisites section)
- [ ] **(User Manual)** Configure User Secrets locally with production credentials
- [ ] **(User Manual)** Configure GitHub Secrets for both production and test environments

### 2. Configuration & NuGet Packages (Agent)

- [ ] **(Agent)** Add `<UserSecretsId>` to `LocalFinanceManager/LocalFinanceManager.csproj`:
  ```xml
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <UserSecretsId>localfinancemanager-a1b2c3d4-e5f6-7890-abcd-ef1234567890</UserSecretsId>
    <!-- ...existing properties... -->
  </PropertyGroup>
  ```
- [ ] **(Agent)** Install required NuGet packages:
  ```powershell
  dotnet add LocalFinanceManager package Supabase.Gotrue
  dotnet add LocalFinanceManager package Microsoft.AspNetCore.Authentication.JwtBearer
  ```
- [ ] **(Agent)** Create `LocalFinanceManager/Configuration/SupabaseOptions.cs`:
  ```csharp
  public class SupabaseOptions
  {
      public string Url { get; set; } = string.Empty;
      public string Key { get; set; } = string.Empty;
      public string JwtSecret { get; set; } = string.Empty;
  }
  ```
- [ ] **(Agent)** Add placeholder configuration to `appsettings.json` (actual values in User Secrets):
  ```json
  {
    "Supabase": {
      "Url": "https://placeholder.supabase.co",
      "Key": "placeholder-anon-key",
      "JwtSecret": "placeholder-jwt-secret"
    }
  }
  ```

### 3. JWT Authentication Middleware (Agent)

- [ ] **(Agent)** Configure JWT bearer authentication in `LocalFinanceManager/Program.cs`:
  ```csharp
  using Microsoft.AspNetCore.Authentication.JwtBearer;
  using Microsoft.IdentityModel.Tokens;
  using System.Text;
  
  // Add before builder.Build()
  builder.Services.Configure<SupabaseOptions>(builder.Configuration.GetSection("Supabase"));
  
  var supabaseOptions = builder.Configuration.GetSection("Supabase").Get<SupabaseOptions>();
  
  builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
      .AddJwtBearer(options =>
      {
          options.TokenValidationParameters = new TokenValidationParameters
          {
              ValidateIssuer = true,
              ValidateAudience = true,
              ValidateLifetime = true,
              ValidateIssuerSigningKey = true,
              ValidIssuer = supabaseOptions.Url,
              ValidAudience = supabaseOptions.Url,
              IssuerSigningKey = new SymmetricSecurityKey(
                  Encoding.UTF8.GetBytes(supabaseOptions.JwtSecret))
          };
      });
  
  builder.Services.AddAuthorization();
  
  // Add after app.UseRouting() but before app.MapControllers()
  app.UseAuthentication();
  app.UseAuthorization();
  ```

### 4. User Entity & Database Schema (Agent)

- [ ] **(Agent)** Create `LocalFinanceManager/Models/User.cs` entity:
  ```csharp
  public class User : BaseEntity
  {
      public string SupabaseUserId { get; set; } = string.Empty; // UUID from Supabase Auth
      public string Email { get; set; } = string.Empty;
      public string DisplayName { get; set; } = string.Empty;
      public bool EmailConfirmed { get; set; } = false;
      
      // Navigation properties
      public ICollection<Account> Accounts { get; set; } = new List<Account>();
      public ICollection<BudgetPlan> BudgetPlans { get; set; } = new List<BudgetPlan>();
      public ICollection<Category> Categories { get; set; } = new List<Category>();
      public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
  }
  ```
- [ ] **(Agent)** Configure `User` entity in `LocalFinanceManager/Data/AppDbContext.cs`:
  ```csharp
  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
      base.OnModelCreating(modelBuilder);
      
      modelBuilder.Entity<User>(entity =>
      {
          entity.HasIndex(e => e.SupabaseUserId).IsUnique();
          entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
          entity.Property(e => e.DisplayName).IsRequired().HasMaxLength(100);
          entity.Property(e => e.SupabaseUserId).IsRequired().HasMaxLength(36);
      });
      
      // ...existing entity configurations...
  }
  ```

### 5. BaseEntity Migration (Agent, BREAKING CHANGE)

- [ ] **(Agent)** Update `LocalFinanceManager/Models/BaseEntity.cs` to add `UserId`:
  ```csharp
  public abstract class BaseEntity
  {
      public Guid Id { get; set; }
      public byte[]? RowVersion { get; set; }
      public DateTime CreatedAt { get; set; }
      public DateTime UpdatedAt { get; set; }
      public bool IsArchived { get; set; }
      
      // NEW: User ownership for multi-user support
      public Guid UserId { get; set; }
      public User User { get; set; } = null!;
  }
  ```
- [ ] **(Agent)** Create EF Core migration:
  ```powershell
  dotnet ef migrations add AddMultiUserSupport --project LocalFinanceManager
  ```
- [ ] **(Agent)** **Delete existing database** and apply fresh migration:
  ```powershell
  dotnet ef database drop --force --project LocalFinanceManager
  dotnet ef database update --project LocalFinanceManager
  ```

### 6. User Context Service (Agent)

- [ ] **(Agent)** Create `LocalFinanceManager/Services/IUserContext.cs` interface:
  ```csharp
  public interface IUserContext
  {
      Guid GetCurrentUserId();
      string GetCurrentUserEmail();
      bool IsAuthenticated();
  }
  ```
- [ ] **(Agent)** Implement `LocalFinanceManager/Services/UserContext.cs`:
  ```csharp
  public class UserContext : IUserContext
  {
      private readonly IHttpContextAccessor _httpContextAccessor;
      
      public UserContext(IHttpContextAccessor httpContextAccessor)
      {
          _httpContextAccessor = httpContextAccessor;
      }
      
      public Guid GetCurrentUserId()
      {
          var user = _httpContextAccessor.HttpContext?.User;
          if (user == null || !user.Identity?.IsAuthenticated == true)
          {
              throw new UnauthorizedAccessException("User is not authenticated");
          }
          
          var subClaim = user.FindFirst("sub")?.Value;
          if (string.IsNullOrEmpty(subClaim) || !Guid.TryParse(subClaim, out var userId))
          {
              throw new UnauthorizedAccessException("Invalid user ID in token");
          }
          
          return userId;
      }
      
      public string GetCurrentUserEmail()
      {
          var user = _httpContextAccessor.HttpContext?.User;
          return user?.FindFirst("email")?.Value ?? string.Empty;
      }
      
      public bool IsAuthenticated()
      {
          return _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;
      }
  }
  ```
- [ ] **(Agent)** Register services in `Program.cs`:
  ```csharp
  builder.Services.AddHttpContextAccessor();
  builder.Services.AddScoped<IUserContext, UserContext>();
  ```

### 7. Repository User Filtering (Agent)

- [ ] **(Agent)** Update all repositories in `LocalFinanceManager/Data/Repositories/` to inject `IUserContext` and filter queries by `UserId`:
  ```csharp
  // Example: AccountRepository.cs
  public class AccountRepository : IAccountRepository
  {
      private readonly AppDbContext _context;
      private readonly IUserContext _userContext;
      
      public AccountRepository(AppDbContext context, IUserContext userContext)
      {
          _context = context;
          _userContext = userContext;
      }
      
      public async Task<List<Account>> GetActiveAsync()
      {
          var currentUserId = _userContext.GetCurrentUserId();
          return await _context.Accounts
              .Where(a => a.UserId == currentUserId && !a.IsArchived)
              .ToListAsync();
      }
      
      public async Task<Account?> GetByIdAsync(Guid id)
      {
          var currentUserId = _userContext.GetCurrentUserId();
          return await _context.Accounts
              .FirstOrDefaultAsync(a => a.Id == id && a.UserId == currentUserId && !a.IsArchived);
      }
      
      // Apply same pattern to all repository methods
  }
  ```
- [ ] **(Agent)** Update repositories: `AccountRepository`, `BudgetPlanRepository`, `CategoryRepository`, `TransactionRepository`, `BudgetLineRepository`

### 8. Blazor Authentication UI (Agent)

- [ ] **(Agent)** Create `LocalFinanceManager/Services/IAuthService.cs` interface:
  ```csharp
  public interface IAuthService
  {
      Task<AuthResponse> SignUpAsync(string email, string password);
      Task<AuthResponse> SignInAsync(string email, string password);
      Task SignOutAsync();
      Task SendPasswordResetEmailAsync(string email);
      Task ResendVerificationEmailAsync(string email);
  }
  
  public class AuthResponse
  {
      public bool Success { get; set; }
      public string? AccessToken { get; set; }
      public string? ErrorMessage { get; set; }
      public bool EmailVerified { get; set; }
  }
  ```
- [ ] **(Agent)** Implement `LocalFinanceManager/Services/AuthService.cs` using Supabase.Gotrue client
- [ ] **(Agent)** Create custom `AuthenticationStateProvider` in `LocalFinanceManager/Services/CustomAuthenticationStateProvider.cs`:
  - Store JWT in **sessionStorage** (not localStorage)
  - Parse JWT claims to build `ClaimsPrincipal`
  - Implement `GetAuthenticationStateAsync()` and `NotifyAuthenticationStateChanged()`
- [ ] **(Agent)** Create `LocalFinanceManager/Components/Pages/Login.razor` following `EditForm` pattern:
  - Email and password input fields
  - Submit button with loading state (`isSubmitting`)
  - **Email verification check**: Display "Email not verified. [Resend verification email]" button when Supabase returns unverified status
  - Resend button calls `AuthService.ResendVerificationEmailAsync()`
  - Success: Store JWT in sessionStorage and navigate to `/`
  - Error handling with `errorMessage` display
- [ ] **(Agent)** Create `LocalFinanceManager/Components/Pages/Register.razor`:
  - Email, password, and confirm password fields
  - Submit calls `AuthService.SignUpAsync()`
  - Display "Registration successful! Check your email to verify your account." message
  - Link to login page
- [ ] **(Agent)** Create `LocalFinanceManager/Components/Pages/Logout.razor`:
  - POST confirmation form with "Are you sure you want to logout?" message
  - Submit button calls `AuthService.SignOutAsync()`, clears sessionStorage, and redirects to `/login`
- [ ] **(Agent)** Create `LocalFinanceManager/Components/Pages/PasswordReset.razor`:
  - Email input field
  - Submit calls `AuthService.SendPasswordResetEmailAsync()`
  - Display "Password reset email sent. Check your inbox." message
- [ ] **(Agent)** Update `LocalFinanceManager/Components/Layout/MainLayout.razor` top-row:
  ```razor
  <div class="top-row px-4">
      <AuthorizeView>
          <Authorized>
              <span>Hello, @context.User.Identity?.Name</span>
              <a href="/logout">Logout</a>
          </Authorized>
          <NotAuthorized>
              <a href="/login">Login</a>
              <a href="/register">Register</a>
          </NotAuthorized>
      </AuthorizeView>
  </div>
  ```
- [ ] **(Agent)** Update `LocalFinanceManager/Components/App.razor` to wrap routes:
  ```razor
  <CascadingAuthenticationState>
      <Routes />
  </CascadingAuthenticationState>
  ```
- [ ] **(Agent)** Configure authentication state provider in `Program.cs`:
  ```csharp
  builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthenticationStateProvider>();
  builder.Services.AddScoped<IAuthService, AuthService>();
  builder.Services.AddCascadingAuthenticationState();
  ```

### 9. Authorization & CI/CD (Agent)

- [ ] **(Agent)** Add `[Authorize]` attribute to all API controllers:
  - `AccountsController`
  - `BudgetPlansController`
  - `CategoriesController`
  - `TransactionsController`
  - `SuggestionsController`
  - `MLController`
  - `AutomationController`
- [ ] **(Agent)** Update `.github/workflows/ci.yml` to inject **test environment** secrets for E2E tests:
  ```yaml
  - name: Run E2E Tests
    run: dotnet test tests/LocalFinanceManager.E2E/ --configuration Release --logger "console;verbosity=normal" --collect:"XPlat Code Coverage"
    env:
      SUPABASE_URL: ${{ secrets.SUPABASE_TEST_URL }}
      SUPABASE_KEY: ${{ secrets.SUPABASE_TEST_KEY }}
      JWT_SECRET: ${{ secrets.SUPABASE_TEST_JWT_SECRET }}
  ```

## Testing Scenarios

### Authentication Scenarios

- [ ] **Registration Flow:**
  - User registers with valid email/password → Returns HTTP 201
  - Verification email sent to user inbox (production environment only)
  - User entity created in database with `EmailConfirmed = false`
- [ ] **Email Verification Enforcement:**
  - Unverified user attempts login → Returns HTTP 401 with error message "Email not verified"
  - Login page displays **"Email not verified. [Resend verification email]" button**
  - User clicks "Resend verification email" → New verification email sent
  - User clicks verification link in email → `User.EmailConfirmed` updates to `true` in database
  - User logs in after verification → Returns JWT token stored in sessionStorage
- [ ] **Login Flow:**
  - Valid credentials with verified email → Returns JWT token in sessionStorage
  - Invalid credentials → Returns HTTP 401
  - Expired token used for API call → Returns HTTP 401
- [ ] **Session Management:**
  - JWT stored in **sessionStorage only** (not localStorage)
  - Browser tab closes → sessionStorage cleared (user must login again)
  - No refresh token mechanism (user re-authenticates when token expires)

### Email Verification (E2E Environment)

- [ ] **Test Environment Configuration:**
  - E2E tests use separate Supabase project with **email verification disabled**
  - E2E tests can login immediately without verification step
  - Production environment enforces email verification
- [ ] **Production Verification Workflow (Manual Testing):**
  - Registration sends email with verification link
  - Clicking verification link confirms email in Supabase
  - `User.EmailConfirmed` property updates correctly
  - Login succeeds only after email confirmation

### Authorization Scenarios

- [ ] **Protected Endpoints:**
  - Request to `[Authorize]` endpoint without JWT → Returns HTTP 401
  - Request with valid JWT → Grants access to protected resources
- [ ] **Blazor Page Protection:**
  - Unauthenticated user navigates to protected page → Redirects to `/login`
  - Authenticated user accesses protected pages successfully

### Data Isolation Scenarios

- [ ] **User-Scoped Queries:**
  - User A GET `/api/accounts` → Returns only User A's accounts (filtered by `UserId`)
  - User A GET `/api/accounts/{userBAccountId}` → Returns HTTP 403 Forbidden
  - User A PUT `/api/accounts/{userBAccountId}` → Returns HTTP 403 Forbidden
  - User A DELETE `/api/accounts/{userBAccountId}` → Returns HTTP 403 Forbidden
- [ ] **Repository Filtering:**
  - All repository methods automatically filter by `UserId` via `IUserContext`
  - Cross-user access prevented at data layer (not just controller layer)

### E2E Workflows

- [ ] **Complete Registration → Login → Use Application → Logout Flow (Manual Testing):**
  - Register new user → Verify email received → Click verification link → Login → Create account → Logout → Login again
- [ ] **Password Reset Flow:**
  - User requests password reset → Email received → Click reset link → Enter new password → Login with new password succeeds
- [ ] **Resend Verification Email Flow:**
  - Register user → Attempt login before verification → Click "Resend verification email" → New email received → Verify email → Login succeeds

## Definition of Done

- [ ] Production Supabase project created with "Confirm email" setting enabled
- [ ] Test Supabase project created with email verification disabled (for E2E tests)
- [ ] Default Supabase email templates configured (confirmation email, password reset email)
- [ ] User Secrets initialized locally with production Supabase credentials (`dotnet user-secrets list` shows values)
- [ ] GitHub Secrets configured for CI/CD:
  - Production: `SUPABASE_URL`, `SUPABASE_KEY`, `JWT_SECRET`
  - Test: `SUPABASE_TEST_URL`, `SUPABASE_TEST_KEY`, `SUPABASE_TEST_JWT_SECRET`
- [ ] JWT authentication middleware validates tokens correctly in `Program.cs`
- [ ] `User` entity created with `SupabaseUserId`, `Email`, `DisplayName`, `EmailConfirmed` properties
- [ ] `UserId` property (`Guid` type) added to all entities inheriting from `BaseEntity`
- [ ] Fresh database created successfully via `dotnet ef database drop --force && dotnet ef database update` (existing data discarded)
- [ ] All repositories inject `IUserContext` and filter queries by current user's `UserId`
- [ ] Unauthorized API calls return HTTP 401
- [ ] Cross-user access attempts return HTTP 403
- [ ] Unverified email blocks login with clear error message and "Resend verification email" button displayed
- [ ] Login, Register, Logout, and PasswordReset pages functional following `EditForm` validation pattern
- [ ] JWT stored in **sessionStorage only** (no localStorage, no refresh tokens, no "Remember Me")
- [ ] E2E tests pass using test Supabase environment (verification disabled)
- [ ] GitHub Actions workflow passes with test environment secrets injected
- [ ] User story immediately archived to `docs/Userstories/Archive/UserStory-11-Multi-User-Authentication.md`

## CLI Commands Reference

```powershell
# Install NuGet packages
dotnet add LocalFinanceManager package Supabase.Gotrue
dotnet add LocalFinanceManager package Microsoft.AspNetCore.Authentication.JwtBearer

# Configure User Secrets (local development - production credentials)
dotnet user-secrets init --project LocalFinanceManager
dotnet user-secrets set "Supabase:Url" "https://<prod-project-ref>.supabase.co" --project LocalFinanceManager
dotnet user-secrets set "Supabase:Key" "<prod-anon-key>" --project LocalFinanceManager
dotnet user-secrets set "Supabase:JwtSecret" "<prod-jwt-secret>" --project LocalFinanceManager

# Verify User Secrets
dotnet user-secrets list --project LocalFinanceManager

# Create EF Core migration
dotnet ef migrations add AddMultiUserSupport --project LocalFinanceManager

# Delete existing database and apply fresh migration (BREAKING CHANGE)
dotnet ef database drop --force --project LocalFinanceManager
dotnet ef database update --project LocalFinanceManager

# Run tests
dotnet test tests/LocalFinanceManager.Tests/
dotnet test tests/LocalFinanceManager.E2E/
```

## Success Criteria

- Multi-user authentication functional with Supabase Auth integration
- Email verification enforced before login with user-friendly "Resend verification email" functionality
- JWT token validation middleware working correctly
- Data isolation enforced: users can only access their own resources
- Unverified users cannot login (production environment)
- E2E tests use separate test environment with verification disabled
- sessionStorage manages JWT tokens (session-only, no persistent login)
- Password reset flow operational with default Supabase email templates
- All API endpoints protected with `[Authorize]` attribute
- Cross-user access attempts return HTTP 403 Forbidden
- Fresh database migration applied successfully (existing data discarded)

## Manual Testing Checklist

- [ ] **(User Manual)** Register new user and verify confirmation email received (production environment)
- [ ] **(User Manual)** Attempt login before email verification → Verify "Email not verified" error displayed with "Resend" button
- [ ] **(User Manual)** Click "Resend verification email" button → Verify new email received
- [ ] **(User Manual)** Click verification link in email → Verify redirect to success page
- [ ] **(User Manual)** Login with verified email → Verify JWT stored in sessionStorage (check browser DevTools → Application → Session Storage)
- [ ] **(User Manual)** Create account while logged in → Verify account saved successfully
- [ ] **(User Manual)** Close browser tab and reopen → Verify sessionStorage cleared and user must login again
- [ ] **(User Manual)** Test password reset flow: Request reset → Check email → Click link → Enter new password → Login with new password
- [ ] **(User Manual)** Create second user account → Verify User A cannot access User B's accounts (HTTP 403 in Network tab)
- [ ] **(User Manual)** Verify E2E tests pass in GitHub Actions with test environment secrets

---

**Upon completion:** Immediately archive this user story to `docs/Userstories/Archive/UserStory-11-Multi-User-Authentication.md` after successful implementation, verification, and all DoD items checked.
