# UserStory-5.3: Assignment UX Enhancements

## Objective

Implement user experience enhancements for transaction assignment workflows: keyboard shortcuts for power users, quick filters for efficient transaction discovery, and recent/favorite category shortcuts to reduce assignment friction. Improve accessibility, perceived performance, and overall usability of assignment features from US-5, US-6, and US-7.

## Requirements

- Implement keyboard navigation shortcuts (Tab, Enter, Esc, Space, Ctrl+A, /) for assignment workflows
- Add quick filter dropdown bar with 6+ filter options (assignment status, date range, category, amount, account)
- Display recent category shortcuts (top 5 most used) with one-click assignment
- Add favorite categories feature (star icon, show at top of selector)
- Persist filter state in browser localStorage (restore on page reload)
- Optimize transaction list performance for 1000+ transactions (pagination enhancements)
- Provide keyboard shortcut documentation (help modal triggered by `?` key)

## Pattern Adherence from US-5, US-6, US-7

This story **enhances** all assignment artifacts from previous stories:

### Components Enhanced

- `CategorySelector.razor` from US-5 - Add favorites display, recent categories
- `TransactionAssignModal.razor` from US-5 - Add keyboard shortcuts, recent categories section
- `Transactions.razor` from US-5/6/7 - Add quick filters, keyboard selection, performance optimization

### New Components Created

- `ShortcutHelp.razor` - Keyboard shortcut documentation modal
- `QuickFilters.razor` - Advanced filter dropdown bar
- `RecentCategoriesService.cs` - Track category usage and favorites

### UX Patterns

- Keyboard shortcuts follow industry standards (Tab navigation, Enter submit, Esc cancel)
- Filter persistence improves UX (users don't lose filter state on page reload)
- Recent categories reduce friction for frequent assignments
- Loading skeletons improve perceived performance

## Implementation Tasks

### 1. Keyboard Shortcut Infrastructure

- [ ] Create `ShortcutHelp.razor` component in `Components/Shared/`
- [ ] Add modal with keyboard shortcut documentation table:
  - **Tab:** Navigate between form fields in modals
  - **Enter:** Submit assignment/split/bulk modal (when save button focused)
  - **Esc:** Close modal without saving
  - **Space:** Toggle transaction checkbox selection
  - **Ctrl+A / Cmd+A:** Select all visible transactions
  - **Ctrl+D / Cmd+D:** Deselect all transactions
  - **/:** Focus search/filter input
  - **?:** Show this help modal
- [ ] Add global keyboard event listener in `App.razor` or layout
- [ ] Implement `?` key handler to show help modal
- [ ] Style help modal with responsive design (mobile-friendly)

### 2. Modal Keyboard Navigation

- [ ] Update `TransactionAssignModal.razor` to handle keyboard events:
  - Add `@onkeydown` event handler
  - Implement Tab navigation (cycle through category → budget line → note → save → cancel)
  - Implement Enter to submit when save button focused
  - Implement Esc to close modal without saving
- [ ] Update `SplitEditor.razor` to handle keyboard events:
  - Tab navigation through split rows
  - Enter to submit when save button focused
  - Esc to close editor
- [ ] Update `BulkAssignModal.razor` to handle keyboard events:
  - Tab navigation through category selector → assign button
  - Enter to start bulk assignment
  - Esc to close modal
- [ ] Ensure keyboard shortcuts don't conflict with browser defaults
- [ ] Add visual focus indicators (CSS outline on focused elements)

### 3. Transaction List Keyboard Shortcuts

- [ ] Update `Transactions.razor` to handle keyboard events:
  - Implement Space key to toggle transaction checkbox when row focused
  - Implement Ctrl+A to select all visible transactions
  - Implement Ctrl+D to deselect all transactions
  - Implement `/` key to focus search/filter input
- [ ] Add keyboard focus management:
  - Arrow keys to navigate between transaction rows
  - Enter key to open assignment modal for focused row
- [ ] Add visual focus indicator for focused transaction row
- [ ] Ensure keyboard navigation works with pagination (focus resets to first row on page change)

### 4. Quick Filters Component

- [ ] Create `QuickFilters.razor` component in `Components/Shared/`
- [ ] Add collapsible filter bar (expand/collapse button)
- [ ] Implement filter options:
  - **Assignment Status:** Dropdown (All / Assigned / Unassigned / Split / Auto-Applied)
  - **Suggestion Status:** Dropdown (All / Has Suggestion / No Suggestion)
  - **Date Range:** Dropdown (Last 7 days / Last 30 days / Last 90 days / Custom range)
  - **Custom Date Range:** Date picker inputs (start date, end date)
  - **Category:** Multi-select dropdown (show only assigned categories from current account)
  - **Amount Range:** Number inputs (min amount, max amount)
  - **Account:** Dropdown (if multiple accounts exist)
- [ ] Add "Clear All Filters" button
- [ ] Add active filter count badge: "3 filters active"
- [ ] Style with consistent design (match existing UI patterns)

### 5. Filter State Persistence

- [ ] Create `FilterStateService.cs` in `Services/`
- [ ] Implement `SaveFiltersAsync(FilterState filters)` method:
  - Serialize filter state to JSON
  - Store in browser localStorage (`localStorage.setItem('transactionFilters', json)`)
- [ ] Implement `LoadFiltersAsync()` method:
  - Retrieve from localStorage
  - Deserialize JSON to `FilterState` object
  - Return null if not found
- [ ] Update `Transactions.razor` to load filters on page init:
  - Call `LoadFiltersAsync()` in `OnInitializedAsync()`
  - Apply loaded filters to transaction query
- [ ] Update `QuickFilters.razor` to save filters on change:
  - Call `SaveFiltersAsync()` when any filter changes
- [ ] Add "Reset Filters" option to clear saved state

### 6. Recent Categories Service

- [ ] Create `RecentCategoriesService.cs` in `Services/`
- [ ] Add `TrackCategoryUsageAsync(Guid categoryId)` method:
  - Increment usage count in localStorage
  - Store as JSON object: `{ categoryId: usageCount }`
  - Limit to top 20 categories (trim older entries)
- [ ] Add `GetRecentCategoriesAsync(int count = 5)` method:
  - Retrieve from localStorage
  - Sort by usage count descending
  - Return top N categories
- [ ] Add `ToggleFavoriteAsync(Guid categoryId)` method:
  - Store favorites in separate localStorage key: `favoriteCategories`
  - Toggle favorite status (add/remove from list)
- [ ] Add `GetFavoriteCategoriesAsync()` method:
  - Retrieve favorite category IDs from localStorage
- [ ] Register service as scoped in `Program.cs`

### 7. Recent Categories Pills UI

- [ ] Add "Recent Categories" section to top of `TransactionAssignModal.razor`:
  - Display horizontal pill buttons showing top 5 most recently used categories
  - Each pill shows category name with click handler to quickly select that budget line
  - Visual feedback: active pill gets accent background, hover state shows pointer cursor
  - Pills auto-update after successful category assignment
- [ ] Call `RecentCategoriesService.TrackCategoryUsageAsync()` after successful assignment
- [ ] Implement keyboard navigation: Tab moves between pills, Enter/Space selects pill, maintain ARIA labels

### 8. Budget Line Card Grid

- [ ] Replace dropdown budget line selector with clickable card grid in `TransactionAssignModal.razor`:
  - Display grid of budget line cards below Recent Categories section
  - Each card shows: category name (bold), remaining balance (e.g., "€450 remaining"), optional progress bar
  - Responsive grid layout: 4 columns (>1200px), 3 columns (768-1200px), 2 columns (576-768px), 1 column (<576px)
  - Visual feedback: selected card gets accent border/background, hover state shows pointer cursor
  - Click handler binds to `selectedBudgetLineId`
- [ ] Extend `BudgetLineDto` with `RemainingBalance` and `SpentAmount` calculated properties:
  - `SpentAmount` = sum of all assigned transaction amounts for this budget line in the current budget period/year
  - `RemainingBalance` = `YearTotal - SpentAmount`
- [ ] Ensure a dedicated implementation ticket is created and linked for the Budget Line Card Grid UX (responsive grid, visual feedback, keyboard navigation, loading skeletons) when this refinement is not implemented in the current PR.
- [ ] Add loading skeleton for budget line cards
- [ ] Implement keyboard navigation: Tab moves between cards, Enter/Space selects card, maintain ARIA labels

### 9. Favorite Categories Feature

- [ ] Update `CategorySelector.razor` to display favorites:
  - Fetch favorite categories from `RecentCategoriesService.GetFavoriteCategoriesAsync()`
  - Display favorites at top of dropdown with ⭐ icon
  - Add separator line between favorites and regular categories
- [ ] Add star icon button next to each category in dropdown:
  - Click star to toggle favorite status
  - Call `RecentCategoriesService.ToggleFavoriteAsync()`
  - Update UI immediately (add/remove from favorites section)
- [ ] Add "Manage Favorites" link in category selector:
  - Opens modal with favorite categories list
  - Allow reordering or removing favorites
- [ ] Persist favorites across sessions (localStorage)

### 10. Performance Optimization - Pagination Enhancements

- [ ] Update `Transactions.razor` pagination controls:
  - Add page size selector: 25 / 50 / 100 / 200 transactions per page
  - Store selected page size in localStorage
  - Add "Jump to page" input (direct page navigation)
- [ ] Implement virtual scrolling OR optimize pagination:
  - If virtual scrolling: Use `Virtualize` component for large lists
  - If pagination: Ensure server-side pagination (only fetch current page)
- [ ] Add loading skeletons for transaction list:
  - Display placeholder rows while loading
  - Improve perceived performance
- [ ] Optimize category selector loading:
  - Cache categories per account (avoid re-fetching on every modal open)
  - Store in component state with expiration (5 minutes)

### 10. Performance Optimization - Filter Debouncing

- [ ] Add debouncing to search/filter inputs in `QuickFilters.razor`:
  - Implement 300ms delay before applying filters
  - Cancel previous filter request if new input received
  - Use `System.Threading.Timer` or JavaScript `debounce` function
- [ ] Add loading indicator during filter application:
  - Show spinner in filter bar
  - Disable filter inputs during processing
- [ ] Optimize filter queries on backend:
  - Ensure database indexes on filtered columns (Date, Amount, CategoryId, AccountId)
  - Review query plans for performance bottlenecks

### 11. Loading States and Skeletons

- [ ] Create `TransactionListSkeleton.razor` component:
  - Display 10 placeholder rows with animated shimmer effect
  - Match transaction list table structure
- [ ] Update `Transactions.razor` to show skeleton during loading:
  - Replace "Loading..." text with skeleton component
  - Show skeleton during initial load and pagination
- [ ] Add skeleton for category selector dropdown:
  - Display placeholder options while categories load
- [ ] Add subtle loading indicator for filter application:
  - Small spinner icon in filter bar
  - Fade-in effect when filters applied

### 12. Mobile Responsiveness

- [ ] Update `QuickFilters.razor` for mobile:
  - Stack filters vertically on small screens
  - Make filter bar collapsible by default on mobile
  - Use full-width inputs and dropdowns
- [ ] Update keyboard shortcuts for touch devices:
  - Disable keyboard shortcuts on mobile (no physical keyboard)
  - Ensure touch interactions work (tap to select, swipe to dismiss modals)
- [ ] Test keyboard shortcut help modal on mobile:
  - Hide keyboard-specific shortcuts
  - Show touch gestures instead

### 13. Testing - Keyboard Shortcuts

- [ ] Add E2E tests in `LocalFinanceManager.E2E/Tests/` (extend US-8 tests):
  - Test: Press `Tab` in assignment modal → Focus moves through fields (category → budget line → save)
  - Test: Press `Enter` when save button focused → Modal submits
  - Test: Press `Esc` in modal → Modal closes without saving
  - Test: Press `Space` on transaction row → Checkbox toggles
  - Test: Press `Ctrl+A` → All visible transactions selected
  - Test: Press `/` → Search input focused
  - Test: Press `?` → Help modal opens
- [ ] Add unit tests for `ShortcutHelp.razor`:
  - Test: Help modal displays all keyboard shortcuts
  - Test: Modal closes when Esc pressed

### 14. Testing - Quick Filters

- [ ] Add E2E tests for filters (extend US-8):
  - Test: Apply "Unassigned" filter → Only unassigned transactions shown
  - Test: Apply date range filter (Last 30 days) → Transactions within range shown
  - Test: Apply amount range filter (min: 50, max: 200) → Transactions in range shown
  - Test: Apply multiple filters (unassigned + last 30 days) → Combined filter works
  - Test: Clear all filters → All transactions shown
  - Test: Filter state persists (apply filter, reload page, assert filter still active)
- [ ] Add unit tests for `FilterStateService`:
  - Test: SaveFiltersAsync stores filters in localStorage
  - Test: LoadFiltersAsync retrieves filters from localStorage

### 15. Testing - Recent Categories

- [ ] Add E2E tests for recent categories (extend US-8):
  - Test: Assign transaction to "Food" category → Next assignment shows "Food" in recent categories
  - Test: Click recent category "Food" → Transaction assigned immediately (no modal)
  - Test: Favorite "Food" category → Shows at top of category selector with ⭐ icon
  - Test: Unfavorite category → Removed from favorites section
- [ ] Add unit tests for `RecentCategoriesService`:
  - Test: TrackCategoryUsageAsync increments usage count
  - Test: GetRecentCategoriesAsync returns top 5 most used
  - Test: ToggleFavoriteAsync adds/removes favorite

### 16. Documentation

- [ ] Update `E2E_TEST_GUIDE.md` from US-8 with UX enhancement testing notes
- [ ] Create `UX_ENHANCEMENTS.md` in `docs/` with:
  - Keyboard shortcuts reference table
  - Quick filters usage guide
  - Recent categories feature description
  - Performance optimization techniques
  - Browser compatibility notes (localStorage support)
- [ ] Add inline help text in UI:
  - Tooltip on "?" icon: "Press ? to view keyboard shortcuts"
  - Tooltip on filter icon: "Filter transactions by status, date, amount, etc."
  - Tooltip on star icon in category selector: "Add to favorites"

### 17. Tests - E2E Tests (Accessibility Validation)

> **Note:** Write E2E tests **immediately after** implementing corresponding UI components for faster feedback. Uses PageObjectModels from UserStory-5.1.

- [ ] Install `Deque.AxeCore.Playwright` NuGet package in `LocalFinanceManager.E2E`
- [ ] Create `AccessibilityTests.cs` test class in `LocalFinanceManager.E2E/Tests/`
- [ ] Test: Run axe-core on Transactions page → Zero critical violations
  - Navigate to transactions page
  - Run axe-core scan
  - Assert no critical accessibility violations
- [ ] Test: Run axe-core on assignment modal → Zero critical violations
  - Open assignment modal
  - Run axe-core scan
  - Assert no critical violations
- [ ] Test: Tab through assignment modal → Focus order logical
  - Open assignment modal
  - Press Tab repeatedly
  - Assert focus moves in logical order (category → budget line → note → save → cancel)
- [ ] Test: Keyboard navigation works (Enter submits, Esc closes)
  - Open modal, press Enter (assert submits if valid)
  - Open modal, press Esc (assert closes without saving)
- [ ] Document accessibility features in README (keyboard shortcuts, screen reader support)

### 18. Tests - E2E Tests (Performance Validation)

- [ ] Create `PerformanceTests.cs` test class in `LocalFinanceManager.E2E/Tests/`
- [ ] Test: Load transaction list with 1000 transactions → Measures load time <500ms
  - Use `SeedDataHelper.SeedTransactionsAsync()` to create 1000 transactions
  - Start stopwatch
  - Navigate to transactions page
  - Stop stopwatch
  - Assert elapsed time <500ms
- [ ] Test: Scroll through pages → Each page loads <200ms
  - Seed 500 transactions (10 pages)
  - Measure time to navigate to page 2
  - Assert page load time <200ms
- [ ] Test: Apply filter to 1000 transactions → Filter applies <300ms
  - Load 1000 transactions
  - Start stopwatch
  - Apply "Uncategorized" filter
  - Stop stopwatch
  - Assert elapsed time <300ms

## Testing

### UX Enhancement Test Scenarios

1. **Keyboard Shortcuts:**
   - Tab navigation works in all modals (assignment, split, bulk)
   - Enter submits forms when save button focused
   - Esc closes modals without saving
   - Space toggles transaction checkbox
   - Ctrl+A selects all visible transactions
   - `/` focuses search input
   - `?` shows help modal

2. **Quick Filters:**
   - Assignment status filter (All/Assigned/Unassigned/Split/Auto-Applied) works
   - Date range filter (Last 7/30/90 days, Custom) works
   - Amount range filter (min/max) works
   - Category multi-select filter works
   - Account filter works (when multiple accounts exist)
   - Clear all filters button resets all filters
   - Filter state persists across page reloads (localStorage)

3. **Recent Categories:**
   - Category usage tracked after assignment
   - Top 5 recent categories displayed in assignment modal
   - One-click assignment from recent categories works
   - Usage count displayed correctly ("Food - Used 23 times")

4. **Favorite Categories:**
   - Star icon toggles favorite status
   - Favorites displayed at top of category selector
   - Favorites persist across sessions (localStorage)

5. **Performance:**
   - Transaction list with 1000+ transactions loads <500ms (with pagination)
   - Page size selector (25/50/100/200) works
   - Filter debouncing (300ms delay) works
   - Loading skeletons displayed during data fetch

## Success Criteria

- ✅ Keyboard shortcuts implemented for 8+ actions (Tab, Enter, Esc, Space, Ctrl+A, Ctrl+D, /, ?)
- ✅ Quick filter dropdown with 6+ filter options (assignment status, date, amount, category, account)
- ✅ Recent category shortcuts display top 5 most used categories with one-click assignment
- ✅ Favorite categories feature with star icon and top-of-list placement
- ✅ Filter state persists in browser localStorage (restored on page reload)
- ✅ Transaction list pagination enhancements (page size selector, jump to page)
- ✅ Loading skeletons improve perceived performance during data fetch
- ✅ Filter debouncing (300ms) prevents excessive API calls
- ✅ Keyboard shortcut help modal accessible via `?` key
- ✅ All features tested with E2E tests (keyboard navigation, filters, recent categories)
- ✅ Mobile responsiveness verified (filters stack vertically, touch interactions work)
- ✅ Documentation comprehensive (`UX_ENHANCEMENTS.md`, inline help tooltips)

## Definition of Done

- Keyboard shortcuts implemented in all assignment modals and transaction list (8+ shortcuts)
- `ShortcutHelp.razor` component displays keyboard shortcut documentation
- `QuickFilters.razor` component with 6+ filter options implemented
- `FilterStateService` persists filter state in localStorage
- `RecentCategoriesService` tracks category usage and favorites in localStorage
- Recent categories section in assignment modal (top 5 most used)
- Favorite categories feature in category selector (star icon, top-of-list)
- Performance optimizations: page size selector, loading skeletons, filter debouncing
- E2E tests cover keyboard shortcuts, filters, recent categories (extend US-8 test suite)
- Unit tests cover `FilterStateService` and `RecentCategoriesService`
- `UX_ENHANCEMENTS.md` documentation complete
- Code follows Implementation-Guidelines.md (.NET 10.0, async/await, localStorage via JSInterop)
- Mobile responsiveness verified (collapsible filters, touch interactions)

## Dependencies

- **UserStory-5 (Basic Assignment UI):** REQUIRED - Enhances CategorySelector, TransactionAssignModal, Transactions.razor components.
- **UserStory-6 (Split/Bulk Assignment):** REQUIRED - Adds keyboard shortcuts to SplitEditor, BulkAssignModal components.
- **UserStory-7 (ML Suggestions):** REQUIRED - Quick filters include ML suggestion status filter.
- **UserStory-5.1 (E2E Infrastructure):** REQUIRED for E2E tests - Must complete US-5.1 before running E2E tests in this story. Uses PageObjectModels and SeedDataHelper.
- **UserStory-8 (Integration Workflow Tests):** OPTIONAL - Can extend E2E test suite with additional keyboard/filter workflow tests.

## Estimated Effort

**2.5-3.5 days** (~31 implementation tasks: 24 implementation + 7 E2E tests)

## Notes

- Keyboard shortcuts follow industry standards: Tab (navigate), Enter (submit), Esc (cancel) are universal.
- Filter persistence (localStorage) improves UX: Users don't lose filter state on page reload.
- Recent categories reduce friction for frequent assignments: 80% of assignments use 20% of categories (Pareto principle).
- Favorite categories provide even faster access for power users.
- Loading skeletons improve perceived performance: Users perceive faster load times even if actual time unchanged.
- Filter debouncing critical for performance: Prevents excessive API calls during typing.
- Mobile responsiveness important: Many users manage finances on mobile devices.
- Accessibility maintained: Keyboard shortcuts enhance accessibility for keyboard-only users.
- localStorage has ~5-10MB limit: Sufficient for filter state and recent categories, but monitor usage.
- Consider future enhancement: Sync recent/favorite categories to server for multi-device consistency.
