# UserStory-3.3: Category UI Updates & Comprehensive Tests

## Objective

Implement Blazor UI updates for budget-plan-scoped category management with template selection, add comprehensive E2E test coverage for the complete category workflow, and verify full integration of UserStory-3.1 and UserStory-3.2.

## Requirements

- Add template dropdown to budget plan creation form
- Scope category dropdown in budget plan edit form to current budget plan
- Add budget plan selector to category management page
- Update category list to show budget plan context
- Add breadcrumb navigation showing budget plan hierarchy
- Add E2E tests for budget plan creation with template selection
- Add E2E tests for category management within budget plan context
- Add E2E tests for transaction assignment with scoped categories
- Verify performance: E2E flow completes in <5 seconds
- Ensure no cross-budget-plan data leakage in UI

## Dependencies

- **REQUIRED: UserStory-3.2** - Category Template System
  - Template definitions must exist
  - `CreateFromTemplateAsync()` method must be functional
  - All unit tests from US-3.2 must be passing

## Implementation Tasks

### 1. Blazor UI: Budget Plan Creation with Template Selection

- [ ] Update [Components/Pages/BudgetPlanCreate.razor](../../LocalFinanceManager/Components/Pages/BudgetPlanCreate.razor) to add template dropdown:

  ```razor
  <EditForm Model="@model" OnValidSubmit="@HandleSubmit">
      <div class="form-group">
          <label for="accountId">Account</label>
          <InputSelect id="accountId" class="form-control" @bind-Value="model.AccountId">
              @foreach (var account in accounts)
              {
                  <option value="@account.Id">@account.Name</option>
              }
          </InputSelect>
      </div>

      <div class="form-group">
          <label for="name">Budget Plan Name</label>
          <InputText id="name" class="form-control" @bind-Value="model.Name" />
      </div>

      <div class="form-group">
          <label for="year">Year</label>
          <InputNumber id="year" class="form-control" @bind-Value="model.Year" />
      </div>

      <!-- NEW: Template Selection -->
      <div class="form-group">
          <label for="template">Category Template</label>
          <InputSelect id="template" class="form-control" @bind-Value="model.TemplateName">
              <option value="Personal">Personal (Salary, Housing, Food, etc.)</option>
              <option value="Business">Business (Revenue, COGS, Marketing, etc.)</option>
              <option value="Household">Household (Rent, Utilities, Groceries, etc.)</option>
              <option value="Empty">Empty (No categories)</option>
          </InputSelect>
          <small class="form-text text-muted">
              Select a template to auto-create categories. You can edit or delete them later.
          </small>
      </div>

      <button type="submit" class="btn btn-primary">Create Budget Plan</button>
  </EditForm>
  ```

### 2. Blazor UI: Budget Plan Edit with Scoped Categories

- [ ] Update [Components/Pages/BudgetPlanEdit.razor](../../LocalFinanceManager/Components/Pages/BudgetPlanEdit.razor) to scope category dropdown:

  ```razor
  @code {
      [Parameter]
      public Guid Id { get; set; }

      private BudgetPlanDto budgetPlan;
      private List<CategoryDto> scopedCategories;

      protected override async Task OnInitializedAsync()
      {
          budgetPlan = await BudgetPlanService.GetByIdAsync(Id);

          // Load only categories for this budget plan
          scopedCategories = await CategoryService.GetByBudgetPlanAsync(budgetPlan.Id);
      }
  }
  ```

### 3. Blazor UI: Category Management with Budget Plan Context

- [ ] Update [Components/Pages/Categories.razor](../../LocalFinanceManager/Components/Pages/Categories.razor) to add budget plan selector:

  ```razor
  <h3>Manage Categories</h3>

  <!-- Budget Plan Selector -->
  <div class="form-group">
      <label for="budgetPlanSelector">Budget Plan</label>
      <InputSelect id="budgetPlanSelector" class="form-control" @bind-Value="selectedBudgetPlanId" @bind-Value:after="LoadCategories">
          @foreach (var plan in budgetPlans)
          {
              <option value="@plan.Id">@plan.Name (@plan.Year)</option>
          }
      </InputSelect>
  </div>

  <!-- Category List -->
  <table class="table">
      <thead>
          <tr>
              <th>Name</th>
              <th>Type</th>
              <th>Budget Plan</th>
              <th>Actions</th>
          </tr>
      </thead>
      <tbody>
          @foreach (var category in categories)
          {
              <tr>
                  <td>@category.Name</td>
                  <td>@category.Type</td>
                  <td>@budgetPlans.FirstOrDefault(bp => bp.Id == category.BudgetPlanId)?.Name</td>
                  <td>
                      <button class="btn btn-sm btn-primary" @onclick="() => Edit(category.Id)">Edit</button>
                      <button class="btn btn-sm btn-danger" @onclick="() => Delete(category.Id)">Delete</button>
                  </td>
              </tr>
          }
      </tbody>
  </table>

  @code {
      private List<BudgetPlanDto> budgetPlans = new();
      private List<CategoryDto> categories = new();
      private Guid selectedBudgetPlanId;

      protected override async Task OnInitializedAsync()
      {
          budgetPlans = await BudgetPlanService.GetAllAsync();
          if (budgetPlans.Any())
          {
              selectedBudgetPlanId = budgetPlans.First().Id;
              await LoadCategories();
          }
      }

      private async Task LoadCategories()
      {
          categories = await CategoryService.GetByBudgetPlanAsync(selectedBudgetPlanId);
      }
  }
  ```

### 4. Blazor UI: Breadcrumb Navigation

- [ ] Create [Components/Shared/Breadcrumb.razor](../../LocalFinanceManager/Components/Shared/Breadcrumb.razor) component:

  ```razor
  <nav aria-label="breadcrumb">
      <ol class="breadcrumb">
          <li class="breadcrumb-item"><a href="/">Home</a></li>
          @foreach (var crumb in Crumbs)
          {
              @if (crumb == Crumbs.Last())
              {
                  <li class="breadcrumb-item active" aria-current="page">@crumb.Text</li>
              }
              else
              {
                  <li class="breadcrumb-item"><a href="@crumb.Url">@crumb.Text</a></li>
              }
          }
      </ol>
  </nav>

  @code {
      [Parameter]
      public List<BreadcrumbItem> Crumbs { get; set; } = new();

      public class BreadcrumbItem
      {
          public string Text { get; set; }
          public string Url { get; set; }
      }
  }
  ```

- [ ] Update category pages to include breadcrumbs showing budget plan context:

  ```razor
  <Breadcrumb Crumbs="@breadcrumbs" />

  @code {
      private List<Breadcrumb.BreadcrumbItem> breadcrumbs = new()
      {
          new() { Text = "Budget Plans", Url = "/budget-plans" },
          new() { Text = "Annual Budget 2026", Url = "/budget-plans/123" },
          new() { Text = "Categories", Url = "" }
      };
  }
  ```

### 5. E2E Tests: Budget Plan Creation with Template

- [ ] Add E2E test in [LocalFinanceManager.E2E/BudgetPlanTests.cs](../../tests/LocalFinanceManager.E2E/BudgetPlanTests.cs):
  ```csharp
  [Test]
  public async Task CreateBudgetPlanWithPersonalTemplate_AutoCreatesCategories()
  {
      await Page.GotoAsync($"{BaseUrl}/budget-plans/create");

      await Page.SelectOptionAsync("#accountId", testAccountId.ToString());
      await Page.FillAsync("#name", "Test Budget 2026");
      await Page.FillAsync("#year", "2026");
      await Page.SelectOptionAsync("#template", "Personal");

      await Page.ClickAsync("button[type='submit']");

      // Wait for redirect to budget plan details
      await Page.WaitForURLAsync($"{BaseUrl}/budget-plans/*");

      // Verify categories auto-created
      var categoryRows = await Page.Locator("table.categories tbody tr").CountAsync();
      Assert.That(categoryRows, Is.EqualTo(6), "Personal template should create 6 categories");

      // Verify category names
      await Expect(Page.Locator("text=Salary")).ToBeVisibleAsync();
      await Expect(Page.Locator("text=Housing")).ToBeVisibleAsync();
      await Expect(Page.Locator("text=Transportation")).ToBeVisibleAsync();
  }
  ```

### 6. E2E Tests: Category Management with Budget Plan Scoping

- [ ] Add E2E test for category scoping:
  ```csharp
  [Test]
  public async Task CategoryList_FiltersBySelectedBudgetPlan()
  {
      // Setup: Create two budget plans with different categories
      var budgetPlan1 = await CreateBudgetPlanWithTemplate("Budget A", "Personal");
      var budgetPlan2 = await CreateBudgetPlanWithTemplate("Budget B", "Business");

      await Page.GotoAsync($"{BaseUrl}/categories");

      // Select Budget A
      await Page.SelectOptionAsync("#budgetPlanSelector", budgetPlan1.Id.ToString());
      await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

      // Verify only Personal categories visible
      await Expect(Page.Locator("text=Salary")).ToBeVisibleAsync();
      await Expect(Page.Locator("text=Revenue")).Not.ToBeVisibleAsync();

      // Select Budget B
      await Page.SelectOptionAsync("#budgetPlanSelector", budgetPlan2.Id.ToString());
      await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

      // Verify only Business categories visible
      await Expect(Page.Locator("text=Revenue")).ToBeVisibleAsync();
      await Expect(Page.Locator("text=Salary")).Not.ToBeVisibleAsync();
  }
  ```

### 7. E2E Tests: Transaction Assignment with Scoped Categories

- [ ] Add E2E test for transaction-category scoping:
  ```csharp
  [Test]
  public async Task TransactionAssignment_OnlyShowsCategoriesFromAccountBudgetPlan()
  {
      // Setup: Create Account A with BudgetPlan X (Personal template)
      var accountA = await CreateAccount("Account A");
      var budgetPlanX = await CreateBudgetPlanWithTemplate(accountA.Id, "Budget X", "Personal");
      await SetCurrentBudgetPlan(accountA.Id, budgetPlanX.Id);

      // Setup: Create Account B with BudgetPlan Y (Business template)
      var accountB = await CreateAccount("Account B");
      var budgetPlanY = await CreateBudgetPlanWithTemplate(accountB.Id, "Budget Y", "Business");

      // Create transaction on Account A
      var transaction = await CreateTransaction(accountA.Id, 100m, "Test Transaction");

      await Page.GotoAsync($"{BaseUrl}/transactions/{transaction.Id}/assign");

      // Open category dropdown
      await Page.ClickAsync("#categoryDropdown");

      // Verify only Personal categories visible (from BudgetPlan X)
      await Expect(Page.Locator("option:has-text('Salary')")).ToBeVisibleAsync();
      await Expect(Page.Locator("option:has-text('Housing')")).ToBeVisibleAsync();

      // Verify Business categories NOT visible (from BudgetPlan Y)
      await Expect(Page.Locator("option:has-text('Revenue')")).Not.ToBeVisibleAsync();
      await Expect(Page.Locator("option:has-text('COGS')")).Not.ToBeVisibleAsync();
  }
  ```

### 8. E2E Tests: Category Editing After Template Application

- [ ] Add E2E test for category editability:
  ```csharp
  [Test]
  public async Task TemplateCategory_CanBeEdited()
  {
      var budgetPlan = await CreateBudgetPlanWithTemplate("Test Budget", "Personal");

      await Page.GotoAsync($"{BaseUrl}/categories?budgetPlanId={budgetPlan.Id}");

      // Find "Salary" category and click Edit
      await Page.ClickAsync("tr:has-text('Salary') button:has-text('Edit')");

      // Change name to "Income"
      await Page.FillAsync("#name", "Income");
      await Page.ClickAsync("button[type='submit']");

      // Verify change persisted
      await Expect(Page.Locator("text=Income")).ToBeVisibleAsync();
      await Expect(Page.Locator("text=Salary")).Not.ToBeVisibleAsync();
  }
  ```

### 9. E2E Performance Test

- [ ] Add performance test for full category workflow:
  ```csharp
  [Test]
  public async Task FullCategoryWorkflow_CompletesUnder5Seconds()
  {
      var stopwatch = System.Diagnostics.Stopwatch.StartNew();

      // Create budget plan with template
      await Page.GotoAsync($"{BaseUrl}/budget-plans/create");
      await Page.SelectOptionAsync("#accountId", testAccountId.ToString());
      await Page.FillAsync("#name", "Performance Test Budget");
      await Page.FillAsync("#year", "2026");
      await Page.SelectOptionAsync("#template", "Personal");
      await Page.ClickAsync("button[type='submit']");
      await Page.WaitForURLAsync($"{BaseUrl}/budget-plans/*");

      // Navigate to categories
      await Page.ClickAsync("a:has-text('Manage Categories')");

      // Create transaction and assign category
      var budgetPlanId = await GetCurrentBudgetPlanId();
      var transaction = await CreateTransaction(testAccountId, 50m, "Test Transaction");
      await Page.GotoAsync($"{BaseUrl}/transactions/{transaction.Id}/assign");
      await Page.SelectOptionAsync("#categoryDropdown", "Salary");
      await Page.ClickAsync("button:has-text('Assign')");

      stopwatch.Stop();

      Assert.That(stopwatch.ElapsedMilliseconds, Is.LessThan(5000),
          $"E2E flow took {stopwatch.ElapsedMilliseconds}ms (expected <5000ms)");
  }
  ```

## Testing

### E2E Test Scenarios

1. **Budget Plan Creation:**

   - Select Personal template → 6 categories auto-created
   - Select Empty template → 0 categories created
   - Template dropdown shows descriptions for each template

2. **Category Scoping:**

   - Budget plan selector shows all user's budget plans
   - Selecting budget plan filters category list
   - No categories from other budget plans visible

3. **Transaction Assignment:**

   - Transaction on Account A only shows categories from Account A's current budget plan
   - Dropdown does not include categories from other accounts or budget plans

4. **Cross-Budget-Plan Isolation:**

   - User creates Category "Housing" in BudgetPlan A
   - User creates Category "Housing" in BudgetPlan B
   - Category lists correctly show two separate "Housing" categories
   - Editing one does not affect the other

5. **Performance:**
   - Full workflow (create budget plan → view categories → assign to transaction) completes in <5s

## Success Criteria

- ✅ Budget plan creation form includes template dropdown with four options
- ✅ Template selection auto-creates categories (verified via E2E test)
- ✅ Category management page scopes list by selected budget plan
- ✅ Transaction assignment dropdown only shows categories from transaction's account budget plan
- ✅ Breadcrumb navigation shows budget plan hierarchy
- ✅ No cross-budget-plan data leakage in UI dropdowns
- ✅ E2E flow completes in <5 seconds (performance test passes)
- ✅ Categories editable after template application (E2E test verifies)
- ✅ All E2E tests pass (100% coverage of critical user journeys)

## Integration DoD

This story verifies the full UserStory-3.1 → 3.2 → 3.3 integration chain:

- ✅ Create budget plan with template → Categories auto-created (US-3.2 functionality)
- ✅ Categories scoped to budget plan (US-3.1 schema enforcement)
- ✅ Assign category to transaction → Validation ensures correct budget plan (US-3.1 validation)
- ✅ Verify scoping works end-to-end (no data leakage across budget plans)
- ✅ E2E flow completes in <5s (performance requirement)
- ✅ All US-3.1 and US-3.2 DoD criteria remain valid (regression test)

## Definition of Done

- [ ] `BudgetPlanCreate.razor` includes template dropdown with four options and descriptions
- [ ] `BudgetPlanEdit.razor` scopes category dropdown to current budget plan
- [ ] `Categories.razor` includes budget plan selector dropdown
- [ ] `Categories.razor` filters category list by selected budget plan
- [ ] `Breadcrumb.razor` component created and integrated into category pages
- [ ] Breadcrumbs show budget plan context (e.g., "Home / Budget Plans / Annual Budget / Categories")
- [ ] E2E test: Budget plan creation with Personal template auto-creates 6 categories
- [ ] E2E test: Category list filters by selected budget plan (no cross-plan leakage)
- [ ] E2E test: Transaction assignment dropdown only shows categories from account's budget plan
- [ ] E2E test: Template categories are editable (name change persists)
- [ ] E2E performance test: Full workflow completes in <5s
- [ ] Integration DoD: All US-3.1 and US-3.2 success criteria remain valid
- [ ] Integration DoD: E2E flow (create plan → auto-create categories → assign to transaction) passes
- [ ] No cross-budget-plan data leakage verified via E2E tests
- [ ] All E2E tests run successfully in CI pipeline
- [ ] Code follows [Implementation-Guidelines.md](../Implementation-Guidelines.md)
- [ ] Blazor UI follows accessibility best practices (WCAG 2.1 Level AA)

## Estimated Effort

**1-2 days** (~9 implementation tasks + comprehensive E2E test coverage)

## Notes

- **E2E Test Infrastructure Dependency:** This story assumes UserStory-5.1 (E2E Test Infrastructure Setup) is complete. If not, add 1 day to set up Playwright + `WebApplicationFactory` + PageObjectModel base classes.
- **Performance Baseline:** 5-second threshold based on typical Blazor Server round-trip time (~200ms per interaction × 5 pages × 2 DB queries = ~2s worst case + 3s buffer).
- **Accessibility:** Ensure all dropdowns have proper `<label>` associations and ARIA attributes for screen readers.
- **Final Integration Gate:** This story's DoD serves as the completion gate for the entire UserStory-3 split (3.1 → 3.2 → 3.3). All three stories must pass their individual DoD criteria AND this integration DoD.
