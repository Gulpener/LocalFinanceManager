# User Story Refinement Recommendations

**Date:** April 10, 2026  
**Purpose:** Identify which user stories need refinement before implementation

---

## Status Overview

- ✅ **27 stories completed & archived** — see `docs/Userstories/Archive/`
- 🟡 **2 stories ready** for implementation (US-18, US-19)
- 🔴 **1 story needs refinement** (US-20)
- 🟡 **2 stories ready** for implementation (US-21)

**Key Finding:** UserStory-5 (Basic Assignment UI) serves as the **gold standard template** for well-structured user stories.

**Deferred E2E test (1):** `AutoApply_AuditTrail_ShowsAutoAppliedIndicator` — blocked on UserStory-18 (`/transactions/{id}/audit` page).

---

## Active Stories

### � UserStory-16: Design Overhaul

**File:** [docs/Userstories/UserStory-16-Design-Overhaul.md](docs/Userstories/UserStory-16-Design-Overhaul.md)

**Status:** New — full specification written. Estimated effort: 5-7 days.

**Scope:** Full visual redesign with a Finance Blue color palette, custom Bootstrap 5 theme, dark mode, mobile-first responsive navigation, dashboard KPI cards, and consistent component patterns.

---

### ✅ UserStory-17: Azure B1 Deployment — COMPLETED (April 10, 2026)

**File:** [UserStory-17-Azure-B1-Deployment.md](Userstories/Archive/UserStory-17-Azure-B1-Deployment.md)

**Status:** Implemented & archived.

---

### 🟡 UserStory-19: Unified Admin Panel

**File:** [docs/Userstories/UserStory-19-Admin-Panel-Unified.md](docs/Userstories/UserStory-19-Admin-Panel-Unified.md)

**Status:** Refined (April 10, 2026) — ready for implementation. Estimated effort: 3–4 days.

**Scope:** Reorganize all existing admin pages (Settings, Auto-Apply, ML, Monitoring) into one tabbed panel with a persistent tab bar. Add a new Users tab for user management (overview, viewing active shares, and revoking them). Introduces `IsAdmin` role, `AdminPolicy` authorization, and `IUserContext.IsAdminAsync()`.

**Refinements applied (April 10, 2026):**

- Added `IUserContext.IsAdminAsync()` spec (interface + implementation) — required by `AdminRouteGuard` and `NavMenu`
- Added unit tests for `UserContext.IsAdminAsync()` (admin/non-admin/unauthenticated)
- Confirmed `User` inherits `BaseEntity.IsArchived` — `.Where(u => !u.IsArchived)` filter is valid
- Added UI error handling criteria for failed toggle-admin and failed revoke (inline alert, auto-dismiss 5s)
- Clarified `AutoApplyRedirect.razor` implementation (dedicated page, `replace: true` redirect, no UI rendered)

---

### 🔴 UserStory-20: Small Improvements

**File:** [docs/Userstories/UserStory-20-Small-Improvements.md](docs/Userstories/UserStory-20-Small-Improvements.md)

**Status:** Needs refinement — too thin to implement safely.

**Scope:** Deploy script build number display name, "Home" → "Dashboard" rename with icon swap, CI path filters to skip doc-only builds.

**Missing before implementation:**

- No technical notes — which deploy script file? Which CI YAML file? Which nav icon (`bi-speedometer2`?)?
- No acceptance criteria for the icon swap (what icon replaces the home icon?)
- No tests specified
- Dashboard rename: is this the same as the `NavMenu.razor` item? Conditional on `@attribute [Authorize]`?

---

### 🟡 UserStory-21: User Profile Page

**File:** [docs/Userstories/UserStory-21-User-Profile-Page.md](docs/Userstories/UserStory-21-User-Profile-Page.md)

**Status:** Ready — full specification written. Estimated effort: 3–4 days.

**Scope:** Profile picture upload (Supabase Storage), first/last name in `UserPreferences`, circular avatar in nav bar with initials fallback, profile dropdown (theme toggle, logout), `/account` profile page.

---

**File:** [docs/Userstories/UserStory-18-Transaction-Audit-Trail-UI.md](docs/Userstories/UserStory-18-Transaction-Audit-Trail-UI.md)

**Status:** Ready — no refinement needed. Estimated effort: 2-3 days.

**Key Features:**

- Transaction audit trail page (`/transactions/{id}/audit`)
- Timeline layout showing change history
- Auto-applied badges with confidence scores
- Before/After state diff viewer
- Link from transaction list to audit page

---

### ✅ UserStory-16 (Design Overhaul) — COMPLETED

**File:** [docs/Userstories/Archive/UserStory-16-Design-Overhaul.md](docs/Userstories/Archive/UserStory-16-Design-Overhaul.md)

**Status:** Implemented & archived.

---

### ✅ UserStory-14: Backup & Restore — COMPLETED (April 5, 2026)

**File:** [docs/Userstories/Archive/UserStory-14-Backup-Restore.md](docs/Userstories/Archive/UserStory-14-Backup-Restore.md)

**Status:** Implemented & archived.

---

### ✅ UserStory-15: Application Flow & Onboarding — COMPLETED (April 6, 2026)

**File:** [docs/Userstories/Archive/UserStory-15-Application-Flow.md](docs/Userstories/Archive/UserStory-15-Application-Flow.md)

**Status:** Implemented & archived.

**Delivered:**

1. **Dashboard** — `Home.razor` replaced with 5-widget responsive grid + quick-actions bar; unauthenticated visitors see hero with Login/Register CTAs.
2. **Auth Guards** — `[Authorize]` added to 14 pages.
3. **Auto-Generated Breadcrumbs** — `IBreadcrumbService` + `BreadcrumbService` implemented; `MainLayout.razor` renders breadcrumbs automatically; manual breadcrumbs removed from 3 pages.
4. **Onboarding Wizard** — 6-step wizard at `/onboarding`; auto-redirects new users with 0 accounts.
5. **Multi-Tenant UX** — Permission badges, "Shared by" labels, and owner/shared filter dropdowns on `Accounts.razor`, `BudgetPlans.razor`, and `SharedWithMe.razor`.

---

## Implementation Roadmap (Current)

### ✅ All Phases 1–5: COMPLETED & ARCHIVED

See `docs/Userstories/Archive/` for all 27 completed stories (US-1 through US-17).

### Active / Next Up

1. 🟡 **UserStory-18** (Transaction Audit Trail UI) — Ready, implement now (2–3 days)
2. 🟡 **UserStory-19** (Unified Admin Panel) — Ready, implement now (3–4 days)

---

## Key Takeaways

### ✅ What Works Well (UserStory-5 Pattern)

1. **Clear Component Patterns:** Code examples with parameter documentation
2. **Service Interface Design:** Method signatures with return types specified
3. **Error Handling Standards:** RFC 7231 Problem Details format documented
4. **Test Organization:** Unit/integration/e2e separation with test scenarios
5. **Appropriate Task Size:** 15-35 tasks = 2-4 day sprint (optimal)
6. **Dependencies Listed:** Blocking relationships explicitly called out

### 🔴 Common Anti-Patterns to Avoid

1. **Too Many Tasks:** >50 tasks = story too large, should be split
2. **Missing Auth Details:** Avoid vague security descriptions; specify token type, claims, and JWT config explicitly
3. **Vague Success Criteria:** "UX is good" instead of measurable metrics
4. **No Testing Scenarios:** Missing concrete test case examples
5. **No DoD Checklist:** Success criteria exist but no explicit completion checkbox list

### 📋 Template for Future User Stories

Use this structure for all new stories:

```markdown
# UserStory-X: [Title]

## Objective

[1-2 sentence summary]

## Requirements

[Bullet list of functional requirements]

## Patterns for Subsequent Stories

[If foundational - document reusable patterns]

## Implementation Tasks

### 1. [Component/Feature Area]

- [ ] Task 1 with clear acceptance criteria
- [ ] Task 2 with code example if applicable

[15-35 total tasks recommended]

## Testing

### Unit Test Scenarios

1. **[Test Category]:** Description
2. **[Test Category]:** Description

### Integration Test Scenarios

1. **[API/Flow]:** Description

## Success Criteria

- ✅ Measurable criterion 1 (e.g., "100% of X display Y")
- ✅ Measurable criterion 2 (e.g., "Performance <2s")

## Definition of Done

- [ ] Checkbox for each deliverable
- [ ] Tests implemented and passing
- [ ] Code follows Implementation-Guidelines.md
- [ ] No manual migrations required

## Dependencies

- **UserStory-X:** REQUIRED - Reason
- **UserStory-Y:** OPTIONAL - Reason

## Estimated Effort

**X-Y days** (~Z implementation tasks)

## Notes

[Any additional context or warnings]
```

---

## Next Actions

1. 🟡 **UserStory-18** (Transaction Audit Trail UI) — Ready, implement now (2–3 days)
2. 🟡 **UserStory-19** (Unified Admin Panel) — Refined April 10, implement now (3–4 days)
3. 🟡 **UserStory-21** (User Profile Page) — Ready, implement after US-19 (3–4 days)
4. 🔴 **UserStory-20** (Small Improvements) — Refine first (1 day to specify, 1–2 days to implement)
5. **Next:** Implement UserStory-16 (Design Overhaul) — ready to plan and implement (5-7 days)

**Total Remaining Effort:** ~10–14 days across 3 active stories — all fully specified, no refinement needed.
