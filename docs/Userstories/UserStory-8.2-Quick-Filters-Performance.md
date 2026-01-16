# UserStory-8.2: Quick Filters & Performance Optimization

## Objective

Implement advanced filtering capabilities and performance optimizations for the transaction assignment workflow. Provide quick filter dropdown with multiple filter options, persist filter state across sessions, add recent/favorite category shortcuts to reduce assignment friction, and optimize transaction list performance for 1000+ transactions through pagination enhancements, debouncing, and loading skeletons.

## Requirements

- Add quick filter dropdown bar with 6+ filter options (assignment status, date range, category, amount, account)
- Persist filter state in browser localStorage (restore on page reload)
- Display recent category shortcuts (top 5 most used) with one-click assignment
- Add favorite categories feature (star icon, show at top of selector)
- Optimize transaction list performance for 1000+ transactions (pagination enhancements, debouncing)
- Add loading skeletons for improved perceived performance
- Measure performance baseline before optimizations (1000 transactions, <500ms target)

## Prerequisites (Dependencies)

This user story depends on **US-5 (Basic Assignment UI)** completion:

### Required Components from US-5:

- ✅ `CategorySelector.razor` exists and is functional (for favorite categories feature)
- ✅ `TransactionAssignModal.razor` exists (for recent categories integration)
- ✅ `Transactions.razor` has basic transaction list (for filter enhancements)
- ✅ All US-5 unit and integration tests passing

**Note:** Unlike US-8.1, this story does NOT require US-6 or US-7 completion. Quick filters and performance optimizations can be implemented as soon as US-5 delivers the basic assignment UI.

**Partial Implementation Possible:** Tasks 1-6 (QuickFilters, FilterStateService, pagination) can start immediately after US-5. Tasks 7-8 (recent/favorite categories) can proceed in parallel once TransactionAssignModal exists.

## Pattern Adherence

This story **enhances** existing components and adds new filtering infrastructure:

### Components Enhanced

- `Transactions.razor` from US-5 - Add quick filter bar, pagination controls, loading skeletons
- `CategorySelector.razor` from US-5 - Add favorites display at top of dropdown
- `TransactionAssignModal.razor` from US-5 - Add recent categories section

### New Components Created

- `QuickFilters.razor` - Advanced filter dropdown bar
- `FilterStateService.cs` - Persist filter state in localStorage via IJSRuntime
- `RecentCategoriesService.cs` - Track category usage and favorites in localStorage
- `TransactionListSkeleton.razor` - Loading skeleton placeholder

### New DTOs Created

- `FilterState.cs` - Data transfer object for filter state (assignment status, date range, amount range, etc.)
- `CategoryUsage.cs` - Track category usage count for recent categories feature

### UX Patterns

- Filter persistence improves UX (users don't lose filter state on page reload)
- Recent categories reduce friction for frequent assignments (80/20 rule: 80% of assignments use 20% of categories)
- Favorite categories provide even faster access for power users
- Loading skeletons improve perceived performance (users perceive faster load times)
- Debouncing prevents excessive API calls during typing (300ms delay standard)

## Implementation Tasks

### 0. Performance Baseline Measurement

> **CRITICAL:** This task must be completed FIRST to establish measurable performance targets.

- [ ] Create `PerformanceBaselineTests.cs` in `LocalFinanceManager.E2E/Tests/`
- [ ] Test: Measure transaction list load time with 1000 transactions (no filters)
  - Use `SeedDataHelper.SeedTransactionsAsync()` to create 1000 transactions
  - Clear browser cache
  - Start stopwatch before navigation
  - Navigate to `/transactions` page
  - Stop stopwatch when table rendered (`page.WaitForSelectorAsync(".transaction-row")`)
  - Log elapsed time: `Console.WriteLine($"Baseline load time: {elapsed.TotalMilliseconds}ms")`
  - Target: <500ms (document current performance for comparison)
- [ ] Test: Measure pagination navigation time (page 1 → page 2 with 50 per page)
  - Load 500 transactions (10 pages)
  - Start stopwatch
  - Click "Next" button
  - Stop stopwatch when page 2 rendered
  - Log elapsed time
  - Target: <200ms
- [ ] Test: Measure filter application time (apply "Uncategorized" filter to 1000 transactions)
  - Load 1000 transactions
  - Start stopwatch
  - Select "Uncategorized" filter
  - Stop stopwatch when filtered results rendered
  - Log elapsed time
  - Target: <300ms
- [ ] Document baseline results in `docs/PERFORMANCE_BASELINE.md`:
  - Record hardware specs (CPU, RAM)
  - Record browser version
  - Record all baseline times
  - Set measurable targets for post-optimization verification

### 1. Quick Filters Component Structure

- [ ] Create `QuickFilters.razor` component in `Components/Shared/`
- [ ] Add collapsible filter bar (expand/collapse button with chevron icon)
- [ ] Add filter bar header:
  - "Filters" label
  - Active filter count badge: "3 filters active"
  - "Clear All Filters" button (visible when filters applied)
  - Expand/collapse toggle button
- [ ] Add filter options container (hidden when collapsed)
- [ ] Add responsive layout:
  - Desktop: Horizontal filter row (3 columns)
  - Mobile: Vertical stack (1 column, full width)
- [ ] Add CSS for consistent styling (match existing UI patterns from US-5)

### 2. Quick Filters - Filter Options Implementation

- [ ] Implement **Assignment Status** filter:
  - Dropdown with options: All / Assigned / Unassigned / Split / Auto-Applied
  - Bind to `FilterState.AssignmentStatus` enum property
  - Default: "All"
- [ ] Implement **Suggestion Status** filter:
  - Dropdown with options: All / Has Suggestion / No Suggestion
  - Bind to `FilterState.SuggestionStatus` enum property
  - Default: "All"
  - Note: This filter depends on US-7 (ML suggestions) backend, but UI can be implemented now
- [ ] Implement **Date Range** filter:
  - Dropdown with options: All Time / Last 7 days / Last 30 days / Last 90 days / Custom range
  - When "Custom range" selected, show date picker inputs (start date, end date)
  - Bind to `FilterState.DateRangeType` and `FilterState.StartDate`, `FilterState.EndDate`
  - Default: "All Time"
- [ ] Implement **Category** multi-select filter:
  - Multi-select dropdown (checkboxes in dropdown)
  - Load categories from current account via `ICategoryRepository`
  - Bind to `FilterState.CategoryIds` (List<Guid>)
  - Show selected count: "3 categories selected"
  - Default: Empty (all categories)
- [ ] Implement **Amount Range** filter:
  - Two number inputs: Min Amount, Max Amount
  - Bind to `FilterState.MinAmount`, `FilterState.MaxAmount`
  - Validation: Min <= Max
  - Default: Empty (no range filter)
- [ ] Implement **Account** filter:
  - Dropdown with all user accounts
  - Only show if multiple accounts exist (hide if single account)
  - Bind to `FilterState.AccountId`
  - Default: Current account

### 3. Filter State DTO

- [ ] Create `FilterState.cs` in `DTOs/`
- [ ] Add properties:

  ```csharp
  public enum AssignmentStatusFilter { All, Assigned, Unassigned, Split, AutoApplied }
  public enum SuggestionStatusFilter { All, HasSuggestion, NoSuggestion }
  public enum DateRangeTypeFilter { AllTime, Last7Days, Last30Days, Last90Days, CustomRange }

  public class FilterState
  {
      public AssignmentStatusFilter AssignmentStatus { get; set; } = AssignmentStatusFilter.All;
      public SuggestionStatusFilter SuggestionStatus { get; set; } = SuggestionStatusFilter.All;
      public DateRangeTypeFilter DateRangeType { get; set; } = DateRangeTypeFilter.AllTime;
      public DateTime? StartDate { get; set; }
      public DateTime? EndDate { get; set; }
      public List<Guid> CategoryIds { get; set; } = new();
      public decimal? MinAmount { get; set; }
      public decimal? MaxAmount { get; set; }
      public Guid? AccountId { get; set; }
  }
  ```

- [ ] Add `IsDefaultState()` method (returns true if all filters at default values)
- [ ] Add `GetActiveFilterCount()` method (returns count of non-default filters)
- [ ] Add JSON serialization attributes for localStorage storage

### 4. Filter State Persistence Service

- [ ] Create `FilterStateService.cs` in `Services/`
- [ ] Inject `IJSRuntime` for localStorage access
- [ ] Implement `SaveFiltersAsync(FilterState filters)` method:
  ```csharp
  public async Task SaveFiltersAsync(FilterState filters)
  {
      var json = JsonSerializer.Serialize(filters);
      await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "transactionFilters", json);
  }
  ```
- [ ] Implement `LoadFiltersAsync()` method:
  ```csharp
  public async Task<FilterState?> LoadFiltersAsync()
  {
      var json = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", "transactionFilters");
      if (string.IsNullOrEmpty(json)) return null;
      return JsonSerializer.Deserialize<FilterState>(json);
  }
  ```
- [ ] Implement `ClearFiltersAsync()` method:
  ```csharp
  public async Task ClearFiltersAsync()
  {
      await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "transactionFilters");
  }
  ```
- [ ] Add error handling for localStorage quota exceeded (5-10MB limit)
- [ ] Register service as scoped in `Program.cs`:
  ```csharp
  builder.Services.AddScoped<IFilterStateService, FilterStateService>();
  ```

### 5. Transactions.razor Filter Integration

- [ ] Update `Transactions.razor` to add `<QuickFilters>` component above transaction table
- [ ] Add `FilterState` property to component
- [ ] Implement `OnInitializedAsync()` to load saved filters:
  ```csharp
  protected override async Task OnInitializedAsync()
  {
      _filterState = await _filterStateService.LoadFiltersAsync() ?? new FilterState();
      await LoadTransactionsAsync();
  }
  ```
- [ ] Implement `OnFilterChanged(FilterState newFilters)` event handler:
  - Update `_filterState` property
  - Call `await _filterStateService.SaveFiltersAsync(newFilters)`
  - Call `await LoadTransactionsAsync()` to refresh transaction list
  - Add debouncing (300ms delay) for text inputs (amount range)
- [ ] Update `LoadTransactionsAsync()` to apply filters:
  - Call `TransactionsController` with filter parameters
  - Pass `FilterState` as query parameters or request body
- [ ] Add "Clear All Filters" button handler:
  - Reset `_filterState` to default
  - Call `await _filterStateService.ClearFiltersAsync()`
  - Call `await LoadTransactionsAsync()`

### 6. Backend Filter Query Support

- [ ] Update `TransactionsController.GetTransactions()` to accept filter parameters:
  - Add optional query parameters for each filter (or accept FilterState DTO in request body)
  - Example: `?assignmentStatus=Unassigned&dateRange=Last30Days&minAmount=50`
- [ ] Update `ITransactionRepository.GetTransactionsAsync()` to build filtered query:

  ```csharp
  var query = _context.Transactions.Where(t => !t.IsArchived);

  if (filterState.AssignmentStatus == AssignmentStatusFilter.Unassigned)
      query = query.Where(t => t.CategoryId == null);

  if (filterState.DateRangeType == DateRangeTypeFilter.Last30Days)
      query = query.Where(t => t.Date >= DateTime.UtcNow.AddDays(-30));

  if (filterState.MinAmount.HasValue)
      query = query.Where(t => t.Amount >= filterState.MinAmount.Value);

  // ... other filters

  return await query.ToListAsync();
  ```

- [ ] Ensure database indexes exist on filtered columns:
  - Index on `Date` column (for date range filters)
  - Index on `Amount` column (for amount range filters)
  - Index on `CategoryId` column (for assignment status filters)
  - Add indexes via EF Core migration if missing

### 7. Recent Categories Service

- [ ] Create `RecentCategoriesService.cs` in `Services/`
- [ ] Inject `IJSRuntime` for localStorage access
- [ ] Implement `TrackCategoryUsageAsync(Guid categoryId)` method:
  ```csharp
  public async Task TrackCategoryUsageAsync(Guid categoryId)
  {
      var json = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", "categoryUsage");
      var usage = string.IsNullOrEmpty(json)
          ? new Dictionary<Guid, int>()
          : JsonSerializer.Deserialize<Dictionary<Guid, int>>(json);

      usage[categoryId] = usage.GetValueOrDefault(categoryId) + 1;

      // Trim to top 20 categories (prevent localStorage bloat)
      if (usage.Count > 20)
      {
          usage = usage.OrderByDescending(kvp => kvp.Value)
                       .Take(20)
                       .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
      }

      var updatedJson = JsonSerializer.Serialize(usage);
      await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "categoryUsage", updatedJson);
  }
  ```
- [ ] Implement `GetRecentCategoriesAsync(int count = 5)` method:
  ```csharp
  public async Task<List<CategoryUsage>> GetRecentCategoriesAsync(int count = 5)
  {
      var json = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", "categoryUsage");
      if (string.IsNullOrEmpty(json)) return new List<CategoryUsage>();

      var usage = JsonSerializer.Deserialize<Dictionary<Guid, int>>(json);
      return usage.OrderByDescending(kvp => kvp.Value)
                  .Take(count)
                  .Select(kvp => new CategoryUsage { CategoryId = kvp.Key, UsageCount = kvp.Value })
                  .ToList();
  }
  ```
- [ ] Implement `ToggleFavoriteAsync(Guid categoryId)` method:
  ```csharp
  public async Task ToggleFavoriteAsync(Guid categoryId)
  {
      var json = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", "favoriteCategories");
      var favorites = string.IsNullOrEmpty(json)
          ? new List<Guid>()
          : JsonSerializer.Deserialize<List<Guid>>(json);

      if (favorites.Contains(categoryId))
          favorites.Remove(categoryId);
      else
          favorites.Add(categoryId);

      var updatedJson = JsonSerializer.Serialize(favorites);
      await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "favoriteCategories", updatedJson);
  }
  ```
- [ ] Implement `GetFavoriteCategoriesAsync()` method:
  ```csharp
  public async Task<List<Guid>> GetFavoriteCategoriesAsync()
  {
      var json = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", "favoriteCategories");
      return string.IsNullOrEmpty(json) ? new List<Guid>() : JsonSerializer.Deserialize<List<Guid>>(json);
  }
  ```
- [ ] Create `CategoryUsage.cs` DTO in `DTOs/`:
  ```csharp
  public class CategoryUsage
  {
      public Guid CategoryId { get; set; }
      public int UsageCount { get; set; }
  }
  ```
- [ ] Register service as scoped in `Program.cs`

### 8. Recent Categories UI Integration

- [ ] Update `TransactionAssignModal.razor` to display recent categories:
  - Add "Recent Categories" section above category selector
  - Inject `IRecentCategoriesService`
  - Load recent categories on modal open: `await _recentCategoriesService.GetRecentCategoriesAsync(5)`
  - Fetch category details from `ICategoryRepository` for display (name, color)
  - Display recent categories as clickable pills/badges:
    - Show category name and usage count: "Food - Used 23 times"
    - Add click handler for one-click assignment (skip modal, assign immediately)
  - Call `RecentCategoriesService.TrackCategoryUsageAsync()` after successful assignment
- [ ] Add loading state while fetching recent categories (skeleton or spinner)
- [ ] Style with compact layout (horizontal pills or small cards)
- [ ] Add empty state message if no recent categories: "No recently used categories yet"

### 9. Favorite Categories UI Integration

- [ ] Update `CategorySelector.razor` to display favorites at top:
  - Inject `IRecentCategoriesService`
  - Load favorite categories on component init: `await _recentCategoriesService.GetFavoriteCategoriesAsync()`
  - Fetch category details from `ICategoryRepository`
  - Display favorites at top of dropdown with ⭐ icon
  - Add separator line between favorites and regular categories
- [ ] Add star icon button next to each category in dropdown:
  - Click star to toggle favorite status
  - Call `RecentCategoriesService.ToggleFavoriteAsync()`
  - Update UI immediately (add/remove from favorites section)
  - Filled star (⭐) for favorites, outline star (☆) for non-favorites
- [ ] Persist favorites across sessions (already handled by localStorage in service)
- [ ] Add tooltip on star icon: "Add to favorites" / "Remove from favorites"

### 10. Performance Optimization - Pagination Enhancements

- [ ] Update `Transactions.razor` pagination controls:
  - Add page size selector dropdown: 25 / 50 / 100 / 200 transactions per page
  - Store selected page size in localStorage via `FilterStateService` (add `PageSize` property to `FilterState`)
  - Add "Jump to page" number input (direct page navigation)
  - Add "First" and "Last" page buttons (in addition to Previous/Next)
  - Display pagination info: "Showing 1-50 of 1,234 transactions"
- [ ] Ensure server-side pagination (only fetch current page):
  - Update `TransactionsController` to accept `pageNumber` and `pageSize` query parameters
  - Return `PagedResult<TransactionDto>` with total count and current page data
  - Don't fetch all transactions and paginate client-side (inefficient for 1000+ rows)
- [ ] Add keyboard navigation for pagination:
  - Arrow Left: Previous page
  - Arrow Right: Next page
  - Home key: First page
  - End key: Last page

### 11. Performance Optimization - Loading Skeletons

- [ ] Create `TransactionListSkeleton.razor` component in `Components/Shared/`
- [ ] Add skeleton structure matching transaction table:
  - Display 10 placeholder rows (configurable via parameter)
  - Match table columns: Date, Description, Counterparty, Amount, Category, Actions
  - Add animated shimmer effect (CSS animation)
  - Use gray rectangles for text placeholders
- [ ] Update `Transactions.razor` to show skeleton during loading:
  - Add `_isLoading` boolean property
  - Show `<TransactionListSkeleton>` when `_isLoading` is true
  - Show actual transaction table when `_isLoading` is false
  - Set `_isLoading = true` before API call, `_isLoading = false` after
- [ ] Add skeleton for category selector dropdown (optional):
  - Display placeholder options while categories load in `CategorySelector.razor`
- [ ] Add CSS for shimmer animation:
  ```css
  @keyframes shimmer {
    0% {
      background-position: -1000px 0;
    }
    100% {
      background-position: 1000px 0;
    }
  }
  .skeleton {
    animation: shimmer 2s infinite linear;
    background: linear-gradient(to right, #f0f0f0 8%, #f8f8f8 18%, #f0f0f0 33%);
    background-size: 1000px 100%;
  }
  ```

### 12. Performance Optimization - Filter Debouncing

- [ ] Add debouncing to amount range inputs in `QuickFilters.razor`:
  - Implement 300ms delay before applying filter
  - Use `System.Threading.Timer` or JavaScript `debounce` function via JSInterop
  - Cancel previous timer if new input received within 300ms
  - Example implementation:
    ```csharp
    private Timer? _debounceTimer;
    private async Task OnAmountChanged()
    {
        _debounceTimer?.Dispose();
        _debounceTimer = new Timer(async _ =>
        {
            await InvokeAsync(async () => await ApplyFiltersAsync());
        }, null, 300, Timeout.Infinite);
    }
    ```
- [ ] Add loading indicator during filter application:
  - Show small spinner icon in filter bar header
  - Disable filter inputs during processing (prevent rapid changes)
  - Fade-in effect when filters applied
- [ ] Optimize filter queries on backend:
  - Ensure database indexes on `Date`, `Amount`, `CategoryId`, `AccountId` columns
  - Review SQL query plans for performance bottlenecks
  - Use `AsNoTracking()` for read-only queries (improves performance)

### 13. Mobile Responsiveness

- [ ] Update `QuickFilters.razor` for mobile:
  - Stack filters vertically on small screens (CSS media query `@media (max-width: 768px)`)
  - Make filter bar collapsed by default on mobile (expand on tap)
  - Use full-width inputs and dropdowns
  - Increase touch target size (min 44x44px per iOS guidelines)
- [ ] Update pagination controls for mobile:
  - Hide "Jump to page" input on mobile (replace with Previous/Next buttons only)
  - Stack pagination info vertically
- [ ] Test recent categories section on mobile:
  - Ensure pills/badges wrap properly
  - Ensure touch tap works (not just hover)

### 14. Testing - Unit Tests (FilterStateService)

- [ ] Create `FilterStateServiceTests.cs` in `LocalFinanceManager.Tests/Services/`
- [ ] Test: SaveFiltersAsync stores filters in localStorage
  - Mock IJSRuntime
  - Call SaveFiltersAsync with test FilterState
  - Verify `localStorage.setItem` called with correct JSON
- [ ] Test: LoadFiltersAsync retrieves filters from localStorage
  - Mock IJSRuntime to return test JSON
  - Call LoadFiltersAsync
  - Assert returned FilterState matches expected values
- [ ] Test: LoadFiltersAsync returns null when no filters saved
  - Mock IJSRuntime to return null
  - Call LoadFiltersAsync
  - Assert returns null
- [ ] Test: ClearFiltersAsync removes filters from localStorage
  - Mock IJSRuntime
  - Call ClearFiltersAsync
  - Verify `localStorage.removeItem` called

### 15. Testing - Unit Tests (RecentCategoriesService)

- [ ] Create `RecentCategoriesServiceTests.cs` in `LocalFinanceManager.Tests/Services/`
- [ ] Test: TrackCategoryUsageAsync increments usage count
  - Mock IJSRuntime with empty usage
  - Call TrackCategoryUsageAsync for category A twice
  - Verify usage count is 2
- [ ] Test: GetRecentCategoriesAsync returns top 5 most used
  - Mock IJSRuntime with 10 categories (varying usage counts)
  - Call GetRecentCategoriesAsync(5)
  - Assert returns 5 categories with highest usage counts
- [ ] Test: TrackCategoryUsageAsync trims to top 20 categories
  - Mock IJSRuntime with 21 categories
  - Call TrackCategoryUsageAsync
  - Verify localStorage contains only 20 categories (lowest usage removed)
- [ ] Test: ToggleFavoriteAsync adds favorite
  - Mock IJSRuntime with empty favorites
  - Call ToggleFavoriteAsync for category A
  - Verify category A in favorites list
- [ ] Test: ToggleFavoriteAsync removes favorite
  - Mock IJSRuntime with category A in favorites
  - Call ToggleFavoriteAsync for category A
  - Verify category A removed from favorites list

### 16. Testing - E2E Tests (Quick Filters)

> **Note:** Requires US-5.1 E2E infrastructure (PlaywrightFixture, SeedDataHelper).

- [ ] Create `QuickFiltersTests.cs` in `LocalFinanceManager.E2E/Tests/`
- [ ] Test: Apply "Unassigned" filter → Only unassigned transactions shown
  - Seed 10 transactions (5 assigned, 5 unassigned)
  - Navigate to transactions page
  - Select "Unassigned" in assignment status filter
  - Assert 5 transactions displayed
- [ ] Test: Apply date range filter (Last 30 days) → Transactions within range shown
  - Seed 20 transactions (10 recent, 10 older than 30 days)
  - Select "Last 30 days" in date range filter
  - Assert 10 transactions displayed
- [ ] Test: Apply amount range filter (min: 50, max: 200) → Transactions in range shown
  - Seed transactions with amounts: 10, 50, 100, 200, 300
  - Enter min: 50, max: 200
  - Assert 3 transactions displayed (50, 100, 200)
- [ ] Test: Apply multiple filters (unassigned + last 30 days) → Combined filter works
  - Seed 20 transactions (mix of assigned/unassigned, recent/old)
  - Apply both filters
  - Assert correct transactions displayed
- [ ] Test: Clear all filters → All transactions shown
  - Apply multiple filters
  - Click "Clear All Filters" button
  - Assert all transactions displayed
- [ ] Test: Filter state persists (apply filter, reload page, assert filter still active)
  - Apply "Unassigned" filter
  - Reload page
  - Assert "Unassigned" filter still selected
  - Assert filtered results still displayed

### 17. Testing - E2E Tests (Recent Categories)

- [ ] Create `RecentCategoriesTests.cs` in `LocalFinanceManager.E2E/Tests/`
- [ ] Test: Assign transaction to "Food" category → Next assignment shows "Food" in recent
  - Seed transaction, seed "Food" category
  - Open assignment modal, assign to "Food"
  - Open assignment modal for another transaction
  - Assert "Food" appears in recent categories section
- [ ] Test: Click recent category "Food" → Transaction assigned immediately
  - Seed transaction with recent category "Food"
  - Open assignment modal
  - Click "Food" in recent categories section
  - Assert transaction assigned without selecting from dropdown
- [ ] Test: Favorite "Food" category → Shows at top of category selector with ⭐ icon
  - Open category selector
  - Click star icon next to "Food" category
  - Assert "Food" moves to favorites section at top
  - Assert star icon filled (⭐)
- [ ] Test: Unfavorite category → Removed from favorites section
  - Favorite "Food" category
  - Click star icon again to unfavorite
  - Assert "Food" removed from favorites section

### 18. Testing - E2E Tests (Performance Validation)

- [ ] Extend `PerformanceBaselineTests.cs` with post-optimization tests:
- [ ] Test: Load transaction list with 1000 transactions → Load time <500ms (verify improvement)
  - Use same test as Task 0 baseline
  - Compare post-optimization time to baseline
  - Assert load time <= baseline or <500ms target
- [ ] Test: Scroll through pages → Each page loads <200ms
  - Seed 500 transactions (10 pages at 50 per page)
  - Measure time to navigate to page 2, 3, 5, 10
  - Assert each page load <200ms
- [ ] Test: Apply filter to 1000 transactions → Filter applies <300ms
  - Load 1000 transactions
  - Start stopwatch
  - Apply "Uncategorized" filter
  - Stop stopwatch when filtered results rendered
  - Assert elapsed time <300ms
- [ ] Test: Page size selector works (change from 50 to 100 per page)
  - Load 200 transactions (defaults to 50 per page = 4 pages)
  - Change page size to 100
  - Assert 100 transactions displayed on page 1
  - Assert pagination shows 2 pages total

### 19. Documentation

- [ ] Update `README.md` with quick filters section:
  - List all 6+ filter options with descriptions
  - Note filter persistence (localStorage)
  - Note recent/favorite categories feature
- [ ] Create `docs/FILTERING.md`:
  - Document all filter options and their behavior
  - Explain filter state persistence (localStorage)
  - Describe recent categories algorithm (top 5 by usage count)
  - Describe favorite categories feature
  - Include screenshots of filter bar
- [ ] Update `docs/PERFORMANCE_BASELINE.md` with post-optimization results:
  - Compare baseline vs optimized performance
  - Document optimization techniques (pagination, debouncing, loading skeletons)
  - Include performance testing methodology
- [ ] Add inline help tooltips in UI:
  - Tooltip on filter icon: "Filter transactions by status, date, amount, etc."
  - Tooltip on star icon in category selector: "Add to favorites"
  - Tooltip on recent category pill: "Click to assign immediately"

## Testing

### Quick Filters Test Scenarios

1. **Assignment Status Filter:**

   - Select "All" → All transactions shown
   - Select "Assigned" → Only assigned transactions shown
   - Select "Unassigned" → Only unassigned transactions shown
   - Select "Split" → Only split transactions shown
   - Select "Auto-Applied" → Only auto-applied transactions shown

2. **Date Range Filter:**

   - Select "All Time" → All transactions shown
   - Select "Last 7 days" → Transactions from last 7 days shown
   - Select "Last 30 days" → Transactions from last 30 days shown
   - Select "Last 90 days" → Transactions from last 90 days shown
   - Select "Custom range" → Date pickers appear, filtered by range

3. **Amount Range Filter:**

   - Enter min: 50 → Transactions >= 50 shown
   - Enter max: 200 → Transactions <= 200 shown
   - Enter min: 50, max: 200 → Transactions between 50-200 shown

4. **Category Multi-Select Filter:**

   - Select "Food" → Only "Food" transactions shown
   - Select "Food" + "Transport" → Food or Transport transactions shown
   - Deselect all → All transactions shown

5. **Filter Persistence:**

   - Apply multiple filters → Reload page → Filters still active
   - Clear all filters → Reload page → No filters active

6. **Filter Debouncing:**
   - Type in amount range → No filter applied for 300ms
   - Wait 300ms → Filter applied automatically

### Recent Categories Test Scenarios

1. **Usage Tracking:**

   - Assign transaction to "Food" → Usage count increments
   - Assign 5 transactions to "Food" → Usage count = 5
   - Assign to 10 different categories → Only top 20 tracked

2. **Recent Categories Display:**

   - Assign to "Food" 23 times → Shows "Food - Used 23 times"
   - Open modal → Top 5 most used categories displayed
   - No recent categories → Empty state message shown

3. **One-Click Assignment:**
   - Click recent category pill → Transaction assigned immediately
   - Modal closes without dropdown interaction

### Favorite Categories Test Scenarios

1. **Favorite Toggle:**

   - Click star icon → Category added to favorites
   - Click star icon again → Category removed from favorites
   - Favorites persist across sessions (localStorage)

2. **Favorites Display:**
   - Favorite 3 categories → All 3 shown at top of selector with ⭐ icon
   - Separator line between favorites and regular categories
   - Favorites sorted alphabetically

### Performance Test Scenarios

1. **Load Time:**

   - Load 1000 transactions → <500ms
   - Load 100 transactions → <100ms

2. **Pagination:**

   - Navigate between pages → <200ms per page
   - Change page size → Immediate update

3. **Filter Application:**

   - Apply filter to 1000 transactions → <300ms
   - Apply multiple filters → <500ms

4. **Loading Skeletons:**
   - During load → Skeleton visible
   - After load → Skeleton replaced with data

## Success Criteria

- ✅ Quick filter dropdown with 6+ filter options implemented (assignment status, suggestion status, date range, category, amount, account)
- ✅ Filter state persists in browser localStorage (restored on page reload)
- ✅ Recent category shortcuts display top 5 most used categories with usage count
- ✅ One-click assignment from recent categories works
- ✅ Favorite categories feature with star icon and top-of-list placement
- ✅ Favorites persist across sessions (localStorage)
- ✅ Transaction list pagination enhancements (page size selector: 25/50/100/200, jump to page, First/Last buttons)
- ✅ Loading skeletons implemented (improve perceived performance during data fetch)
- ✅ Filter debouncing (300ms) prevents excessive API calls
- ✅ Performance baseline measured (Task 0) and post-optimization improvements verified
- ✅ Transaction list with 1000+ transactions loads <500ms
- ✅ Pagination navigation <200ms per page
- ✅ Filter application <300ms
- ✅ Mobile responsiveness verified (filters stack vertically, touch interactions work)
- ✅ E2E tests pass (6 quick filter tests + 4 recent categories tests + 4 performance tests = 14 tests)
- ✅ Unit tests pass (4 FilterStateService tests + 5 RecentCategoriesService tests = 9 tests)
- ✅ Documentation complete (`FILTERING.md`, `PERFORMANCE_BASELINE.md`, README updates)

## Definition of Done

- **Task 0 (Performance Baseline)** completed FIRST with documented baseline times
- `QuickFilters.razor` component with 6+ filter options implemented
- `FilterStateService` persists filter state in localStorage via IJSRuntime
- `RecentCategoriesService` tracks category usage and favorites in localStorage (hardcoded top 5)
- Recent categories section in `TransactionAssignModal` (top 5 most used, one-click assignment)
- Favorite categories feature in `CategorySelector` (star icon, top-of-list display)
- Performance optimizations: page size selector (25/50/100/200), loading skeletons, filter debouncing (300ms)
- Backend filter query support in `TransactionsController` with database indexes
- E2E tests pass for quick filters (6 tests in `QuickFiltersTests.cs`)
- E2E tests pass for recent categories (4 tests in `RecentCategoriesTests.cs`)
- E2E tests pass for performance validation (4 tests in `PerformanceBaselineTests.cs`)
- Unit tests pass for `FilterStateService` (4 tests)
- Unit tests pass for `RecentCategoriesService` (5 tests)
- Documentation complete (`docs/FILTERING.md`, `docs/PERFORMANCE_BASELINE.md`, README updates)
- Code follows Implementation-Guidelines.md (.NET 10.0, async/await, localStorage via IJSRuntime)
- Mobile responsiveness verified (collapsible filters, touch interactions)
- localStorage usage monitored (max 5MB, error handling for quota exceeded)

## Dependencies

- **UserStory-5 (Basic Assignment UI):** REQUIRED - Enhances `CategorySelector`, `TransactionAssignModal`, `Transactions.razor` components.
- **UserStory-5.1 (E2E Infrastructure):** REQUIRED for E2E tests - Uses `PlaywrightFixture` and `SeedDataHelper`.
- **UserStory-7 (ML Suggestions):** OPTIONAL - "Suggestion Status" filter works better with US-7, but filter UI can be implemented without backend.

## Estimated Effort

**2-2.5 days** (~19 implementation tasks including Task 0 + 14 E2E tests + 9 unit tests = 42 total tasks)

## Roadmap Timing

**Phase 3** (Weeks 6-7, parallel with US-7, after US-5 complete)

## Notes

- **Task 0 critical:** Performance baseline measurement must happen FIRST to establish measurable targets and validate optimizations.
- **localStorage first approach:** Recent/favorite categories use localStorage for MVP simplicity. Database migration deferred to US-10 multi-user refactoring with `UserPreference` entity.
- **Recent categories hardcoded at top 5:** Non-configurable for MVP. Reduces complexity. Can add configuration later if user feedback requests it.
- **localStorage has ~5-10MB limit:** Sufficient for filter state and recent categories. Monitor usage, add error handling for quota exceeded.
- **Filter persistence improves UX:** Users don't lose filter state on page reload. Industry standard pattern (Gmail, GitHub, JIRA all persist filters).
- **Recent categories reduce friction:** 80/20 rule applies - 80% of assignments use 20% of categories. Tracking usage optimizes for common case.
- **Favorite categories for power users:** Even faster than recent categories for frequently used categories that might not be "recent" anymore.
- **Loading skeletons improve perceived performance:** Users perceive faster load times even if actual time unchanged. Standard UX pattern (Facebook, LinkedIn, Twitter).
- **Debouncing critical for performance:** Prevents excessive API calls during typing. 300ms standard delay (not too short, not too long).
- **Server-side pagination essential:** Client-side pagination of 1000+ transactions causes memory issues and slow rendering. Always paginate on backend.
- **Database indexes critical:** Filters won't perform well on large datasets without indexes on `Date`, `Amount`, `CategoryId`, `AccountId`.
- **Future enhancement:** Multi-device sync of recent/favorite categories requires database storage. Deferred to US-10 with `UserPreference` entity (Guid UserId, string Key, string Value JSON).
- **Browser compatibility:** localStorage supported in all modern browsers (IE 11+, Chrome, Firefox, Safari, Edge). No polyfill needed.
- **Mobile touch detection:** `navigator.maxTouchPoints > 0` reliably detects touch devices. Handles hybrid devices (touchscreen laptops) correctly.
