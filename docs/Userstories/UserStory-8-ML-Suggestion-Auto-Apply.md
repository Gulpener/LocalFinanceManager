# UserStory-8: ML-Powered Suggestion & Auto-Apply

## Objective

Implement AI-powered transaction assignment automation: display ML-based category suggestions with confidence scores, enable one-click accept/reject, configure auto-apply settings with confidence thresholds, and provide monitoring dashboard for auto-apply performance tracking.

## Requirements

- Display ML category suggestions with confidence scores and feature explanations
- Implement one-click accept/reject buttons for suggestions
- Create auto-apply configuration page (enable/disable, confidence threshold, account selection)
- Build monitoring dashboard showing auto-apply statistics (accuracy, undo rate, alerts)
- Implement background job for auto-applying high-confidence suggestions
- Integrate with existing `SuggestionsController` and `AutomationController` endpoints
- Enforce UserStory-4 validation for auto-applied assignments
- Follow US-5 patterns: service interfaces, error handling, component structure
- Reuse `CategorySelector.razor` component from US-5 in settings pages
- Provide focused unit and integration tests for auto-apply logic and monitoring

> ⚠️ **UserStory-4 Validation:** `Category.BudgetPlanId` must match `Account.CurrentBudgetPlanId`. Return HTTP 400 with message: "Category '{CategoryName}' belongs to a budget plan for a different account."

## Pattern Adherence from US-5 & US-6

This story **MUST** follow patterns established in UserStory-5 and UserStory-6:

### Component Reuse (NO Modifications)

- **MUST reuse** `CategorySelector.razor` from US-5 without modification
  - Use in auto-apply settings for default category selection
  - Maintain AccountId filtering behavior

### Service Extension Pattern

- **MUST follow** service interface conventions from US-5
  - Create `IAutoApplyService` following same patterns as `ITransactionAssignmentService`
  - Return `Result<T>` or `OperationResult` for error handling
  - Use async/await with CancellationToken
  - Validate Category.BudgetPlanId matches Account.CurrentBudgetPlanId

### Badge Component Pattern

- **MUST follow** badge component pattern from US-5 warning badges
  - Consistent styling for suggestion badges
  - Same tooltip pattern for explanations
  - Reuse badge color scheme (warning/success/info)

### Error Handling (Same Format)

- **MUST use** RFC 7231 Problem Details format from US-5
- HTTP 400 for validation errors (budget plan mismatch)
- HTTP 409 for concurrency conflicts with reload + retry
- Property-level errors in `errors` dictionary

### Test Organization (Same Structure)

- Unit tests in `LocalFinanceManager.Tests/Services/`
- Integration tests in `LocalFinanceManager.Tests/Integration/`
- Use `TestDbContextFactory` for in-memory SQLite
- Follow AAA pattern

## Implementation Tasks

### 1. DTOs & Validation

- [ ] Create `MLSuggestionDTO` with `CategoryId`, `CategoryName`, `ConfidenceScore`, `FeatureImportance` (dictionary)
- [ ] Create `AcceptSuggestionRequest` DTO with `TransactionId`, `SuggestionId` (optional for tracking)
- [ ] Create `AutoApplySettingsDTO` with `Enabled`, `MinimumConfidence`, `AccountIds` (array), `ExcludedCategoryIds` (array)
- [ ] Create `AutoApplyStatsDTO` with `TotalAutoApplied`, `AcceptedCount`, `UndoCount`, `UndoRate`, `LastRunTimestamp`
- [ ] Create `AutoApplyAlertDTO` with `AlertType` (enum: UndoRateHigh, ModelStale), `Threshold`, `CurrentValue`, `Message`
- [ ] Add `AutoApplySettingsValidator` using FluentValidation
- [ ] Validate `MinimumConfidence` between 0.0 and 1.0 (60%-95% recommended)
- [ ] Validate `AccountIds` reference existing accounts owned by user
- [ ] Add unit tests for settings validation

### 2. Components - MLSuggestionBadge

- [ ] Create `MLSuggestionBadge.razor` component in `Components/Shared/`
- [ ] Add `@Parameter TransactionId` (Guid) to fetch suggestion
- [ ] Add `@Parameter OnSuggestionAccepted` (EventCallback) for parent notification
- [ ] Display suggestion badge following US-5 badge pattern:
  - Category name with confidence percentage (e.g., "Food 85% 🤖")
  - Badge color based on confidence: >80% green, 60-80% yellow, <60% gray
  - Robot emoji or AI icon indicator
- [ ] Add tooltip showing feature importance (top 3 features):
  - "Based on: Description (40%), Amount (30%), Counterparty (20%)"
- [ ] Show "Accept" button (checkmark icon) calling `POST /api/suggestions/{transactionId}/accept`
- [ ] Show "Reject" button (X icon) recording feedback via `POST /api/suggestions/{transactionId}/feedback`
- [ ] Display loading spinner during API call
- [ ] Show success toast on accept
- [ ] Hide badge after acceptance (transaction now assigned)
- [ ] Show "Auto-applied" indicator if suggestion was auto-applied

### 3. Components - Transaction List Updates (ML Integration)

- [ ] Update `Transactions.razor` to fetch ML suggestions for unassigned transactions
- [ ] Display `MLSuggestionBadge.razor` next to unassigned transactions with confidence >60%
- [ ] Add filter option: "Has Suggestion" / "No Suggestion"
- [ ] Sort by suggestion confidence (highest first) when filter active
- [ ] Batch-fetch suggestions for visible page (avoid N+1 query problem)
- [ ] Cache suggestions in component state (refresh on assignment)

### 4. Components - AutoApplySettings

- [ ] Create `AutoApplySettings.razor` page in `Components/Pages/`
- [ ] Add navigation menu item: "Settings > Auto-Apply"
- [ ] Display toggle switch: "Enable Auto-Apply" (on/off)
- [ ] Add confidence threshold slider (60%-95%, default 80%)
- [ ] Show confidence explanation: "Higher = more accurate but fewer auto-assignments"
- [ ] Add account multi-select dropdown:
  - Default: All accounts
  - Allow selecting specific accounts for auto-apply
- [ ] Add excluded categories multi-select dropdown:
  - Allow excluding sensitive categories (e.g., "Taxes", "Investments")
- [ ] Embed `CategorySelector.razor` from US-5 for category exclusion (set `AllowBudgetLineSelection=false`)
- [ ] Show preview stats: "Based on last 100 transactions, {X} would auto-apply"
- [ ] Add "Save Settings" button calling `POST /api/automation/settings`
- [ ] Display current settings on page load via `GET /api/automation/settings`
- [ ] Show validation errors inline (Problem Details format from US-11.1)

### 5. Components - MonitoringDashboard

- [ ] Create `MonitoringDashboard.razor` page in `Components/Pages/`
- [ ] Add navigation menu item: "Settings > Auto-Apply Monitoring"
- [ ] Display key metrics cards:
  - Total auto-applied (last 30 days)
  - Acceptance rate (assignments not undone)
  - Undo rate (red if >10% per existing backend threshold)
  - Last auto-apply run timestamp
- [ ] Add chart/graph showing undo rate trend over time (last 30 days)
- [ ] Display active alerts from `GET /api/automation/alerts`:
  - "⚠️ Undo rate exceeds threshold (15% > 10%)"
  - "⚠️ ML model may be stale (last retrain 45 days ago)"
- [ ] Show auto-apply history table (last 50 auto-applied transactions):
  - Transaction description, category assigned, confidence score, status (accepted/undone)
- [ ] Add "Undo Auto-Apply" button per transaction row calling `POST /api/automation/undo/{id}`
- [ ] Add "Check if Can Undo" validation using `GET /api/automation/can-undo/{id}`
- [ ] Refresh metrics every 30 seconds (auto-refresh)

### 6. Services - IAutoApplyService

- [ ] Create `IAutoApplyService` interface in `Services/` following US-11.1 patterns
- [ ] Add `GetSuggestionAsync(Guid transactionId, CancellationToken ct)` method
- [ ] Add `AcceptSuggestionAsync(Guid transactionId, CancellationToken ct)` method
- [ ] Add `RecordFeedbackAsync(Guid transactionId, bool accepted, Guid? overrideCategoryId, CancellationToken ct)` method
- [ ] Add `GetSettingsAsync(CancellationToken ct)` method
- [ ] Add `UpdateSettingsAsync(AutoApplySettingsDTO settings, CancellationToken ct)` method
- [ ] Add `GetStatsAsync(CancellationToken ct)` method
- [ ] Implement service in `AutoApplyService.cs`
- [ ] Inject `IRepository<Transaction>`, `IRepository<Category>`, `IRepository<Account>`, `IMLService` via DI
- [ ] Implement UserStory-4 validation: Check `Category.BudgetPlanId == Account.CurrentBudgetPlanId` before auto-apply
- [ ] Store auto-apply settings in database (AutoApplySettings entity) OR appsettings.json (per-user config)
- [ ] Handle `DbUpdateConcurrencyException` with reload + retry (last-write-wins)

### 7. Services - Background Job Orchestration

- [ ] Create `AutoApplyBackgroundJob` hosted service implementing `IHostedService`
- [ ] Register in `Program.cs` via `builder.Services.AddHostedService<AutoApplyBackgroundJob>()`
- [ ] Run job on timer (configurable interval, default every 15 minutes)
- [ ] Fetch unassigned transactions for enabled accounts
- [ ] Get ML suggestions via `IMLService.PredictCategoryAsync()`
- [ ] Filter suggestions by confidence threshold from settings
- [ ] Exclude transactions for excluded categories
- [ ] Auto-assign via `ITransactionAssignmentService.AssignAsync()` with `IsAutoApplied=true` flag
- [ ] Record audit trail with `IsAutoApplied=true` and `ConfidenceScore`
- [ ] Log auto-apply statistics (success count, failure count)
- [ ] Handle UserStory-4 validation errors (skip transactions with mismatched budget plans)
- [ ] Implement cancellation token support for graceful shutdown

### 8. API Integration (Use Existing Endpoints)

- [ ] Document existing endpoint: `GET /api/suggestions/{transactionId}` (get ML suggestion)
- [ ] Document existing endpoint: `POST /api/suggestions/{transactionId}/feedback` (record accept/reject)
- [ ] Document existing endpoint: `GET /api/suggestions/model-info` (get active model details)
- [ ] Document existing endpoint: `POST /api/automation/undo/{transactionId}` (undo auto-applied assignment)
- [ ] Document existing endpoint: `GET /api/automation/can-undo/{transactionId}` (check if undo allowed)
- [ ] Document existing endpoint: `GET /api/automation/stats` (get auto-apply statistics)
- [ ] Document existing endpoint: `GET /api/automation/undo-rate-exceeded` (check if undo rate >10%)
- [ ] Verify endpoints return RFC 7231 Problem Details on validation errors
- [ ] Verify HTTP 400 for budget plan mismatch during auto-apply
- [ ] Verify HTTP 409 for concurrency conflicts

### 9. Tests - Unit Tests (ML Suggestions)

- [ ] Add unit tests for `AutoApplyService.GetSuggestionAsync()` in `LocalFinanceManager.Tests/Services/`
- [ ] Test suggestion fetched successfully with confidence score
- [ ] Test no suggestion returned when model unavailable
- [ ] Test feature importance dictionary populated correctly
- [ ] Add unit tests for `AutoApplyService.AcceptSuggestionAsync()`
- [ ] Test suggestion accepted creates assignment
- [ ] Test UserStory-4 validation: Suggested category from different budget plan rejected
- [ ] Mock `IMLService` with NSubstitute or Moq

### 10. Tests - Unit Tests (Auto-Apply Job)

- [ ] Add unit tests for `AutoApplyBackgroundJob.ExecuteAsync()` in `LocalFinanceManager.Tests/Services/`
- [ ] Test job fetches unassigned transactions for enabled accounts only
- [ ] Test job applies suggestions meeting confidence threshold
- [ ] Test job skips suggestions below confidence threshold
- [ ] Test job excludes transactions for excluded categories
- [ ] Test job records audit trail with `IsAutoApplied=true`
- [ ] Test job handles validation errors gracefully (continues to next transaction)
- [ ] Test job respects cancellation token (graceful shutdown)

### 11. Tests - Unit Tests (Monitoring & Undo)

- [ ] Add unit tests for `AutoApplyService.GetStatsAsync()` in `LocalFinanceManager.Tests/Services/`
- [ ] Test stats calculated correctly (total auto-applied, undo count, undo rate)
- [ ] Test undo rate threshold detection (>10% triggers alert)
- [ ] Add unit tests for `AutoApplyService.UndoAutoApplyAsync()`
- [ ] Test undo succeeds for auto-applied assignment
- [ ] Test undo fails for manually assigned transaction (not auto-applied)
- [ ] Test undo records audit trail entry

### 12. Tests - Integration Tests (Suggestions)

- [ ] Add integration tests for `GET /api/suggestions/{transactionId}` in `LocalFinanceManager.Tests/Integration/`
- [ ] Use `TestDbContextFactory` with in-memory SQLite (`:memory:`)
- [ ] Seed test data: Account, BudgetPlan, Categories, Transaction, LabeledExamples
- [ ] Test suggestion returned with confidence score >60% → HTTP 200
- [ ] Test no suggestion returned when model confidence <60% → HTTP 200 (empty response)
- [ ] Add integration tests for `POST /api/suggestions/{transactionId}/feedback`
- [ ] Test feedback recorded as LabeledExample for retraining

### 13. Tests - Integration Tests (Auto-Apply)

- [ ] Add integration tests for auto-apply background job in `LocalFinanceManager.Tests/Integration/`
- [ ] Seed test data: 10 unassigned transactions with labeled examples for training
- [ ] Enable auto-apply with 80% confidence threshold
- [ ] Run job manually (trigger `ExecuteAsync()`)
- [ ] Verify transactions auto-assigned when confidence ≥80%
- [ ] Verify audit trail has `IsAutoApplied=true` and `ConfidenceScore`
- [ ] Verify transactions skipped when confidence <80%

### 14. Tests - Integration Tests (Monitoring & Undo)

- [ ] Add integration tests for `GET /api/automation/stats` in `LocalFinanceManager.Tests/Integration/`
- [ ] Seed test data: 100 auto-applied transactions, 8 undone (undo rate 8%)
- [ ] Verify stats returned correctly: `TotalAutoApplied=100, UndoCount=8, UndoRate=8%`
- [ ] Add integration tests for `POST /api/automation/undo/{transactionId}`
- [ ] Seed auto-applied transaction
- [ ] Undo transaction → HTTP 200, audit trail updated
- [ ] Verify `GET /api/automation/can-undo/{transactionId}` returns `true` before undo, `false` after

### 15. Tests - E2E Tests (ML Suggestions)

> **Note:** Write E2E tests **immediately after** implementing corresponding UI components for faster feedback. Uses PageObjectModels and SeedDataHelper from UserStory-5.1.

- [ ] Create `MLSuggestionTests.cs` test class in `LocalFinanceManager.E2E/Tests/`
- [ ] Use `SeedDataHelper.SeedMLDataAsync()` to create 100+ LabeledExamples (simulate trained model)
- [ ] Test: Navigate to Transactions page → Unassigned transactions with suggestions show `MLSuggestionBadge`
  - Seed transactions with patterns matching labeled examples
  - Assert suggestion badges visible on unassigned transactions
- [ ] Test: Hover over suggestion badge → Tooltip shows feature importance (top 3 features)
  - Hover over badge
  - Assert tooltip displays feature weights
- [ ] Test: Click "Accept" button on suggestion → Transaction assigned to suggested category → Badge disappears
  - Click accept button on suggestion badge
  - Assert transaction assigned to suggested category
  - Assert suggestion badge no longer visible
- [ ] Test: Click "Reject" button on suggestion → Feedback recorded → Badge remains (transaction still unassigned)
  - Click reject button
  - Assert feedback recorded (check via API or audit trail)
  - Assert transaction still unassigned
- [ ] Test: Filter transactions by "Has Suggestion" → Only transactions with ML suggestions shown
  - Use `TransactionsPageModel.SelectFilterAsync("Has Suggestion")`
  - Assert only transactions with suggestion badges visible
- [ ] Test: Sort transactions by suggestion confidence (highest first) → Order correct
  - Sort by confidence
  - Assert transactions ordered by confidence score descending
- [ ] Test: Verify suggestion badge color coding (>80% green, 60-80% yellow, <60% gray)
  - Seed transactions with varying confidence scores
  - Assert badge colors match confidence thresholds
- [ ] Test: Navigate to ML model info page → Shows active model details (version, accuracy, last trained)
  - Navigate to ML settings/info page
  - Assert model metadata displayed
- [ ] Add screenshots for suggestion badge states (high confidence, medium confidence, tooltip)

### 16. Tests - E2E Tests (Auto-Apply Configuration)

- [ ] Create `AutoApplyTests.cs` test class in `LocalFinanceManager.E2E/Tests/`
- [ ] Test: Navigate to "Settings > Auto-Apply" page → Settings page loads
  - Navigate to auto-apply settings
  - Assert settings page renders
- [ ] Test: Toggle "Enable Auto-Apply" switch → Setting saved
  - Toggle switch to enabled
  - Reload page, assert switch remains enabled
- [ ] Test: Adjust confidence threshold slider (60% → 85%) → Preview stats update: "Based on last 100 transactions, X would auto-apply"
  - Drag slider to 85%
  - Assert preview statistics update
- [ ] Test: Select specific accounts for auto-apply → Only selected accounts processed
  - Seed multiple accounts
  - Select subset for auto-apply
  - Assert settings saved correctly
- [ ] Test: Add excluded categories → Transactions for those categories skipped
  - Add category to exclusion list
  - Assert setting saved
- [ ] Test: Save settings with invalid confidence (e.g., 110%) → Validation error shown
  - Attempt to set confidence >100%
  - Assert validation error displayed
- [ ] Test: Trigger auto-apply job manually (if API endpoint available) → Transactions auto-assigned
  - Trigger job via UI button or API
  - Wait for job completion
  - Assert transactions auto-assigned
- [ ] Test: Verify auto-applied transactions show "Auto-applied" indicator in audit trail
  - Open audit trail for auto-applied transaction
  - Assert "Auto-applied" source displayed
- [ ] Add screenshots for settings page (toggle on, slider adjusted, validation error)

### 17. Tests - E2E Tests (Monitoring Dashboard)

- [ ] Create `MonitoringDashboardTests.cs` test class in `LocalFinanceManager.E2E/Tests/`
- [ ] Use `SeedDataHelper.SeedAutoApplyHistoryAsync()` to create 100 auto-applied transactions (8 undone → 8% undo rate)
- [ ] Test: Navigate to "Settings > Auto-Apply Monitoring" page → Dashboard loads with stats
  - Navigate to monitoring dashboard
  - Assert page renders with metrics
- [ ] Test: Verify metrics cards show correct values (Total: 100, Undo Rate: 8%)
  - Assert total count card shows 100
  - Assert undo rate card shows 8%
- [ ] Test: Undo rate <10% → No alert shown → Green status indicator
  - Assert no alert banner visible
  - Assert status indicator green
- [ ] Seed additional undo (12 undone → 12% undo rate > 10% threshold)
  - Use seed helper to add more undone transactions
- [ ] Test: Undo rate >10% → Alert banner shown: "⚠️ Undo rate exceeds threshold (12% > 10%)"
  - Reload dashboard
  - Assert alert banner visible with warning message
- [ ] Test: Click "Undo Auto-Apply" button on transaction row → Confirmation dialog → Undo successful
  - Click undo button on transaction
  - Confirm in dialog
  - Assert transaction reverted to unassigned
- [ ] Test: Verify "Check if Can Undo" validation (button disabled for manually assigned transactions)
  - Seed manually assigned transaction
  - Assert undo button disabled or not visible
- [ ] Test: Auto-refresh works (metrics update every 30 seconds without page reload)
  - Seed new auto-applied transaction
  - Wait 30 seconds
  - Assert metrics updated without page reload
- [ ] Test: Auto-apply history table shows last 50 transactions with status (accepted/undone)
  - Assert history table displays last 50 entries
  - Assert status column shows "Accepted" or "Undone"
- [ ] Add screenshots for dashboard (normal stats, alert shown, undo confirmation)

## Testing

### Unit Test Scenarios

1. **ML Suggestions:**
   - Suggestion fetched with confidence score and feature importance
   - Suggestion acceptance creates assignment with correct category
   - UserStory-4 validation rejects category from different budget plan
   - Feedback recorded as LabeledExample

2. **Auto-Apply Job:**
   - Job processes only unassigned transactions for enabled accounts
   - Job applies suggestions meeting confidence threshold
   - Job skips suggestions below threshold
   - Job excludes transactions for excluded categories
   - Job records audit trail with `IsAutoApplied=true` and `ConfidenceScore`
   - Job handles validation errors gracefully

3. **Monitoring & Undo:**
   - Stats calculated correctly (undo rate = undo count / total auto-applied)
   - Undo succeeds for auto-applied assignments
   - Undo fails for manually assigned transactions
   - Undo rate alert triggered when >10%

4. **Component Logic:**
   - MLSuggestionBadge displays confidence with color coding
   - AutoApplySettings validates confidence threshold (60%-95%)
   - MonitoringDashboard shows real-time stats

### Integration Test Scenarios

1. **Suggestions API:**
   - GET /api/suggestions/{transactionId} returns suggestion with confidence >60% (HTTP 200)
   - POST /api/suggestions/{transactionId}/feedback records feedback

2. **Auto-Apply Job:**
   - Job auto-assigns transactions meeting confidence threshold
   - Job records audit trail with `IsAutoApplied=true`
   - Job skips transactions below threshold

3. **Monitoring API:**
   - GET /api/automation/stats returns correct statistics
   - POST /api/automation/undo/{transactionId} undoes auto-applied assignment
   - GET /api/automation/can-undo/{transactionId} validates undo eligibility

4. **End-to-End Flow:**
   - User enables auto-apply with 80% threshold → Job runs → Transactions auto-assigned
   - User views monitoring dashboard → Stats show auto-apply activity
   - User undoes auto-applied transaction → Audit trail updated
   - User accepts suggestion manually → Transaction assigned, badge disappears

## Success Criteria

- ✅ ML suggestions display with confidence scores >60% in transaction list
- ✅ Suggestion badge shows feature importance tooltip (top 3 features)
- ✅ One-click accept/reject buttons functional with API integration
- ✅ Auto-apply processes 100+ transactions with <5% undo rate (when configured properly)
- ✅ Auto-apply respects confidence threshold (configurable 60%-95%)
- ✅ Monitoring dashboard shows real-time stats (total, undo rate, alerts)
- ✅ Undo rate alert triggers when >10% (per existing backend threshold)
- ✅ UserStory-4 validation enforced for auto-applied assignments (HTTP 400)
- ✅ Unit tests cover auto-apply job edge cases (threshold filtering, exclusions, validation errors)
- ✅ Integration tests cover suggestions API and auto-apply job execution
- ✅ `CategorySelector.razor` from US-5 reused in settings page
- ✅ Badge pattern from US-5 followed for suggestion badges

## Definition of Done

- Blazor UI components (`MLSuggestionBadge.razor`, `AutoApplySettings.razor`, `MonitoringDashboard.razor`, updated `Transactions.razor`) implemented and functional
- `IAutoApplyService` interface and implementation with UserStory-4 validation
- `AutoApplyBackgroundJob` hosted service registered in `Program.cs`
- Background job runs on timer (15-minute interval) and auto-applies high-confidence suggestions
- Unit tests for ML service integration, auto-apply job, monitoring, and undo (AAA pattern, mocked dependencies)
- Integration tests for suggestions API and auto-apply job execution (in-memory SQLite, seed data)
- UserStory-4 validation enforced for auto-applied assignments
- No manual migrations required (automatic via `Database.MigrateAsync()` in `Program.cs`)
- Code follows Implementation-Guidelines.md and US-5 patterns
- `CategorySelector.razor` component reused from US-5 in settings page

## Dependencies

- **UserStory-5 (Basic Assignment UI):** ⚠️ **MUST complete US-5 before starting US-7.** Review US-5 patterns section before implementation.
  - Reuses `CategorySelector.razor` component in settings page
  - Extends service interface patterns (`IAutoApplyService` follows `ITransactionAssignmentService`)
  - Follows badge component pattern for suggestion badges
  - Follows error handling patterns (Problem Details format)
  - Follows test organization structure
- **UserStory-6 (Split/Bulk Assignment):** ⚠️ **MUST complete US-6 before starting US-7.** Review established patterns from both US-5 and US-6.
  - Auto-apply can leverage bulk assignment patterns for batch processing
  - Monitoring dashboard can track split transaction auto-assignments
- **UserStory-5.1 (E2E Infrastructure):** REQUIRED for E2E tests - Must complete US-5.1 before running E2E tests in this story.
- **UserStory-3 (Category Ownership):** REQUIRED - Categories must be budget-plan-scoped for filtering
- **UserStory-4 (Account-Budget Matching):** REQUIRED - Validation rules enforced for auto-applied assignments
- **Existing Backend Services:** `SuggestionsController`, `AutomationController`, `MLController`, `IMLService` already implemented

## Estimated Effort

**6-7 days** (~60-65 implementation tasks: 35-40 implementation + 25 E2E tests)

## Notes

- Auto-apply confidence threshold default (80%) balances accuracy vs coverage. Lower threshold = more auto-assignments but higher undo rate.
- Undo rate monitoring critical for model quality feedback loop. High undo rate (>10%) indicates model retraining needed.
- Background job interval (15 minutes) prevents excessive database load while maintaining responsiveness.
- Feature importance tooltips help users understand ML decisions and build trust in auto-apply.
- Auto-apply settings stored per-user (if multi-user) or globally (single-user MVP). For multi-user, add `UserId` FK to `AutoApplySettings` entity.
- Monitoring dashboard designed for ML model maintainers/power users to track auto-apply quality over time.
