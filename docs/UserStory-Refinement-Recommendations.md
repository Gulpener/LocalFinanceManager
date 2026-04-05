# User Story Refinement Recommendations

**Date:** April 5, 2026  
**Purpose:** Identify which user stories need refinement before implementation

---

## Status Overview

- ✅ **24 stories completed & archived** — see `docs/Userstories/Archive/`
- 🟡 **4 stories ready** for implementation (US-15, US-16, US-18, US-19)
- 🔴 **0 stories need refinement**

**Key Finding:** UserStory-5 (Basic Assignment UI) serves as the **gold standard template** for well-structured user stories.

**Deferred E2E test (1):** `AutoApply_AuditTrail_ShowsAutoAppliedIndicator` — blocked on UserStory-18 (`/transactions/{id}/audit` page).

---

## Active Stories

### � UserStory-16: Design Overhaul

**File:** [docs/Userstories/UserStory-16-Design-Overhaul.md](docs/Userstories/UserStory-16-Design-Overhaul.md)

**Status:** New — full specification written. Estimated effort: 5-7 days.

**Scope:** Full visual redesign with a Finance Blue color palette, custom Bootstrap 5 theme, dark mode, mobile-first responsive navigation, dashboard KPI cards, and consistent component patterns.

---

### 🟡 UserStory-19: Unified Admin Panel

**File:** [docs/Userstories/UserStory-19-Admin-Panel-Unified.md](docs/Userstories/UserStory-19-Admin-Panel-Unified.md)

**Status:** New — full specification written. Estimated effort: 3-4 days.

**Scope:** Reorganize all existing admin pages (Settings, Auto-Apply, ML, Monitoring) into one tabbed panel with a persistent tab bar. Add a new Users tab for user management (overview, viewing active shares, and revoking them).

---

### 🟡 UserStory-18: Transaction Audit Trail UI

**File:** [docs/Userstories/UserStory-18-Transaction-Audit-Trail-UI.md](docs/Userstories/UserStory-18-Transaction-Audit-Trail-UI.md)

**Status:** Ready — no refinement needed. Estimated effort: 2-3 days.

**Key Features:**

- Transaction audit trail page (`/transactions/{id}/audit`)
- Timeline layout showing change history
- Auto-applied badges with confidence scores
- Before/After state diff viewer
- Link from transaction list to audit page

---

### ✅ UserStory-14: Backup & Restore — COMPLETED (April 5, 2026)

**File:** [docs/Userstories/Archive/UserStory-14-Backup-Restore.md](docs/Userstories/Archive/UserStory-14-Backup-Restore.md)

**Status:** Implemented & archived.

---

### 🟡 UserStory-15: Application Flow & Onboarding

**File:** [docs/Userstories/UserStory-15-Application-Flow.md](docs/Userstories/UserStory-15-Application-Flow.md)

**Status:** Refined — ready to implement. Estimated effort: 4–5 days.

**Scope:** Five implementation phases:

1. **Dashboard** — replace `Home.razor` at `/` with a 5-widget responsive grid (AccountSummary, BudgetStatus, RecentTransactions, UncategorizedAlert, MLSuggestion) + quick-actions bar; unauthenticated visitors see a hero with Login/Register CTAs.
2. **Auth Guards** — add `[Authorize]` to 14 currently unprotected pages; relies on existing `RedirectToLogin.razor` pattern.
3. **Auto-Generated Breadcrumbs** — new `IBreadcrumbService` (scoped) parses the URL into friendly labels; `MainLayout.razor` subscribes and renders `<Breadcrumb>` automatically; pages call `SetSectionTitle(id, name)` for entity names; migrate 3 pages that currently use manual breadcrumbs.
4. **Onboarding Wizard** — 6-step wizard at `/onboarding`; triggered when a new user has 0 accounts after login (no DB schema change); covers account creation, budget plan, category templates, and optional CSV import.
5. **Multi-Tenant UX** — permission badges (Owner/Editor/Viewer), "Shared by [email]" labels, and filter dropdowns on `Accounts.razor`, `BudgetPlans.razor`, and `SharedWithMe.razor`.

---

## Implementation Roadmap (Current)

### ✅ All Phases 1–5: COMPLETED & ARCHIVED

See `docs/Userstories/Archive/` for all 23 completed stories (US-1 through US-14).

### Active / Next Up

1. 🟡 **UserStory-18** (Transaction Audit Trail UI) — Ready, implement now (2–3 days)
2. 🟡 **UserStory-19** (Unified Admin Panel) — Ready, implement now (3–4 days)
3. 🟡 **UserStory-16** (Design Overhaul) — Ready to implement (5–7 days)
4. 🟡 **UserStory-15** (Application Flow & Onboarding) — Refined, ready to implement (4–5 days)

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

1. **Immediate:** Implement UserStory-18 (Transaction Audit Trail UI) — fully ready, no refinement needed (2-3 days)
2. **Immediate:** Implement UserStory-19 (Unified Admin Panel) — fully ready, no refinement needed (3-4 days)
3. **Next:** Implement UserStory-16 (Design Overhaul) — ready to plan and implement (5-7 days)
4. **Next:** Refine and implement UserStory-15 (Application Flow & Onboarding) — dashboard widgets, onboarding wizard (refinement ~2-3h; impl 4-5 days)

**Total Refinement Effort Remaining:** ~2-3 hours (US-15 only)
