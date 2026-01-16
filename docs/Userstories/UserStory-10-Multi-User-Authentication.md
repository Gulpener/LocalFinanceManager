# Post-MVP-5: Add Multi-User Authentication

## Objective

Integrate ASP.NET Core Identity with Supabase Auth to enable multi-user support with JWT-based authentication.

## Requirements

- Integrate ASP.NET Core Identity with Supabase Auth (JWT-based)
- Add `User` entity
- Update `BaseEntity` with `UserId` foreign key
- Implement authentication middleware in `Program.cs`
- Add user registration and login endpoints

## Implementation Tasks

- [ ] Install NuGet packages:
  - `Microsoft.AspNetCore.Identity.EntityFrameworkCore`
  - `Microsoft.AspNetCore.Authentication.JwtBearer`
  - `Supabase.Gotrue` (or Supabase client library)
- [ ] Create `User.cs` entity (or use `IdentityUser`)
- [ ] Update `BaseEntity.cs` to include `UserId` property:
  ```csharp
  public string UserId { get; set; }
  public User User { get; set; }
  ```
- [ ] Update `AppDbContext` to inherit from `IdentityDbContext<User>`
- [ ] Configure JWT authentication in `Program.cs`:
  - Add JWT validation with Supabase public key
  - Configure authentication middleware
  - Add authorization policies
- [ ] Update all repositories to filter by `UserId`
- [ ] Add authentication controllers:
  - `POST /api/auth/register`
  - `POST /api/auth/login`
  - `POST /api/auth/logout`
  - `GET /api/auth/me`
- [ ] Create migration for schema changes
- [ ] Add `[Authorize]` attributes to all controllers
- [ ] Update Blazor UI with login/logout components
- [ ] Add authentication state provider

## Database Schema Changes

All entities inheriting from `BaseEntity` will automatically include:
```csharp
public string UserId { get; set; }
public User User { get; set; }
```

## Testing

- Unit tests for authentication logic
- Integration tests for user isolation
- E2E tests for login/logout flow
- Verify cross-user access is prevented

## Success Criteria

- Users can register and login via Supabase Auth
- JWT tokens are validated correctly
- All entities are scoped to authenticated user
- Unauthorized access returns 401
- Cross-user data access is prevented
- Migration applies successfully
