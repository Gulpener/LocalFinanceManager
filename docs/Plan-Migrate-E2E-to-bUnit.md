# Plan: Migrate E2E Tests from Playwright to bUnit

**Date:** April 8, 2026  
**Status:** Proposal — not yet approved  
**Driver:** Playwright+Blazor Server flakiness caused by SignalR re-render races  
**Goal:** Near-zero-flakiness test suite, faster feedback loop, same or better coverage

---

## Why Playwright keeps being painful with Blazor Server

Blazor Server delivers DOM updates over a WebSocket (SignalR). The browser's JS
execution is not synchronized with the .NET render cycle. This gap is structural —
no browser automation tool (Playwright, Selenium, Cypress) avoids it. Workarounds
like `WaitForFunctionAsync`, `data-loaded-*` attributes, and extended timeouts are
patches, not a fix.

Root causes that keep recurring:

- A Blazor re-render triggered by `StateHasChanged` arrives milliseconds after
  Playwright's selector resolves but before the DOM reflects the new state
- `NetworkIdle` has no knowledge of SignalR; it fires on HTTP quiet, not on Blazor quiet
- Each workaround adds test-specific markup or timing constants to production code

---

## Proposed Architecture

```
LocalFinanceManager.Tests/          (already exists)
├── Unit/                           existing unit tests
├── Integration/                    existing integration tests (in-memory SQLite)
└── Components/                     NEW — bUnit component tests

LocalFinanceManager.E2E/            (keep, but shrink)
└── Tests/                          only 3-5 true smoke tests remain
```

### Layer 1 — bUnit component tests (replaces ~90% of E2E)

bUnit renders Blazor components **in-process**, in a synthetic DOM, with no browser
and no SignalR. Clicks, form fills, and assertions happen synchronously against the
rendered component tree. This eliminates the entire race condition class.

- Runs in milliseconds per test (<5s for the full suite)
- Plugs into NUnit — same test runner already in use
- Mocks `IRepository<T>` interfaces already in place
- No real DB, no Kestrel, no browser process
- Built-in mock for `IJSRuntime` handles `localStorage`, file download triggers, etc.

### Layer 2 — Integration tests (already exists, minimal gap to fill)

Persistence logic ("was this actually saved correctly?") belongs here, not in the
browser layer. Tests run against in-memory SQLite.

### Layer 3 — Playwright smoke tests (shrink from ~112 to 3-5)

Only flows that genuinely require a real browser stay here:

- Full backup/restore cycle (file picker, file download)
- Dark mode persists across navigation
- One happy-path login-to-assign-to-budget flow as a sanity check

---

## Not Doing This Immediately — Risks and Unknowns

Before committing to this migration, the following need investigation:

1. **Service injection vs constructor coupling**  
   If any Blazor pages use `@inject` against concrete types or call `new SomeService()`
   internally, bUnit injection won't work without a refactor. Need to audit all pages.

2. **JS interop coverage**  
   Pages that rely heavily on JS interop for rendering (charts, rich text, file I/O)
   are harder to cover with bUnit even with mock `IJSRuntime`. Some of these may
   still need Playwright.

3. **AuthenticationState**  
   Current E2E setup bypasses auth with a cookie. bUnit needs a fake
   `AuthenticationStateProvider` injected per test. Adds boilerplate.

4. **No equivalent for Playwright's visual inspection**  
   Accessibility tests (`<main>` landmark, color-contrast via axe-core) and keyboard
   navigation tests must remain in Playwright — bUnit has no accessibility audit API.

5. **Migration effort underestimate risk**  
   The current 9 E2E fixture files cover ~112 test cases. Migrating them to bUnit is
   not a mechanical translation — bUnit tests look fundamentally different. Budget
   uncertain.

---

## Migration Sequence (if approved)

### Phase 0 — Audit (1–2 days, before any code changes)

- [ ] Scan all `Components/Pages/` for concrete `@inject` or `new`-constructed services
- [ ] Identify all pages with heavy JS interop
- [ ] Count tests per fixture that can move to bUnit vs must stay in Playwright
- [ ] Confirm `AuthenticationState` wiring approach

### Phase 1 — Set up bUnit in existing test project (half day)

```powershell
dotnet add tests/LocalFinanceManager.Tests package bunit
```

- Create `tests/LocalFinanceManager.Tests/Components/` directory
- Add one sample component test to validate the setup

### Phase 2 — Migrate fixtures to bUnit, one at a time

Priority order (easiest first, validates approach before harder ones):

1. `BudgetPlanE2ETests` — pure CRUD, no file I/O, low JS interop
2. `AccountsE2ETests` — same characteristics
3. `BasicAssignmentTests` — modal interaction, some JS interop via mocks
4. `BulkAssignTests` — complex but high-flakiness payoff
5. `MLPredictionE2ETests` — needs fake ML service; skip if too complex
6. `AccessibilityTests` — **stays in Playwright** (axe-core)
7. `KeyboardNavigationE2ETests` — **stays in Playwright** (browser focus model)
8. `BackupRestoreE2ETest` — **stays in Playwright** (file picker/download)

### Phase 3 — Trim Playwright to smoke tests

- Remove all migrated test fixtures from `LocalFinanceManager.E2E/`
- Keep 3–5 smoke tests in a new `Tests/SmokeTests.cs` file
- Remove all Page Object Models that are no longer needed

### Phase 4 — Clean up production code

- Remove `data-loaded-*` attributes added purely for Playwright polling
- Remove `e2e-auth-token` cookie logic if no longer needed by the slim smoke suite

---

## Decision Criteria

This migration is worth doing if:

- Phase 0 audit shows <20% of pages need significant refactor for bUnit compatibility
- At least 70% of current E2E test cases can migrate cleanly
- The team is willing to learn bUnit's API (reasonable learning curve, ~1 day)

It is **not** worth doing if:

- Most pages directly instantiate services (would require widespread refactor first)
- The 3–5 remaining Playwright smoke tests are still flaky (problem not solved)
- Timeline pressure makes a week+ migration unacceptable right now

---

## Comparison

| Aspect                     | Current (Playwright)         | Proposed (bUnit + minimal Playwright)   |
| -------------------------- | ---------------------------- | --------------------------------------- |
| Flakiness                  | Structural — SignalR races   | Near zero for bUnit layer               |
| Test run time              | ~5–10 minutes                | <30s for bUnit; 2–3 min for smokes      |
| Debugging                  | Screenshot + trace files     | In-process stack trace, immediate       |
| Coverage of business logic | High (full stack)            | High (mocked services, component logic) |
| Real DB / network tested   | Yes (PostgreSQL per fixture) | No (integration tests cover this)       |
| Browser / CSS / JS tested  | Yes                          | Only for 3–5 smoke tests                |
| Setup complexity           | High (Kestrel + Postgres)    | Low for bUnit; same for smokes          |
| Learning curve             | Already learned              | ~1 day for bUnit                        |
