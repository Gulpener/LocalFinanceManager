# SUPERSEDED: Split into UserStory-3.1, 3.2, 3.3 (January 16, 2026)

**This user story has been split into three focused sub-stories for better implementation tracking and reduced risk:**

- **[UserStory-3.1: Category Budget Plan Scoping & Migration](UserStory-3.1-Category-Budget-Scoping.md)** - Database schema restructuring, data migration, repository/service updates (~2-3 days)
- **[UserStory-3.2: Category Template System](UserStory-3.2-Category-Template-System.md)** - Template definitions and auto-creation logic (~1-2 days)
- **[UserStory-3.3: Category UI Updates & Comprehensive Tests](UserStory-3.3-Category-UI-Tests.md)** - Blazor UI and E2E test coverage (~1-2 days)

**Total Effort:** 4-7 days across three sequential stories (vs 7-10 days as single story)

**Implementation Order:** US-3.1 (REQUIRED) → US-3.2 (REQUIRED) → US-3.3 (final integration gate)

---

# Original User Story (Archived)

## Objective

Restructure categories from global entities to budget-plan-scoped entities with editable starter templates, enabling proper multi-budget-plan isolation while maintaining category flexibility.

## Requirements

- Add `BudgetPlanId` foreign key to `Category` entity
- Update `AppDbContext` to configure navigation properties with cascade delete
- Create starter templates (Personal, Business, Household, Empty) as hard-coded definitions
- Make categories editable after template application
- Add `BudgetPlanService.CreateFromTemplateAsync()` method
- Update repository queries to scope by budget plan
- Add TransactionSplit validation for budget-plan-scoped categories
- Implement data migration strategy for existing global categories
- Update category name uniqueness validation to `(BudgetPlanId, Name)` scope

## Implementation Tasks

### 1. Entity Model & Relationship Updates

- [ ] Add `BudgetPlanId` property to `Category.cs`
- [ ] Add `BudgetPlan` navigation property to `Category.cs`
- [ ] Add `ICollection<Category>` navigation property to `BudgetPlan.cs`
- [ ] Configure `Category → BudgetPlan` relationship in `AppDbContext.cs` with `DeleteBehavior.Cascade`
- [ ] Change `BudgetLine → Category` relationship to `DeleteBehavior.Cascade`
- [ ] Update Category index from `Name` to composite `(BudgetPlanId, Name)`

### 2. Database Migration & Data Migration

- [ ] Generate migration adding `BudgetPlanId` column to Categories table
- [ ] Implement SQL data migration in `Up()` method:
  - Duplicate existing global categories into each budget plan
  - Assign orphaned categories (not referenced by BudgetLines) to newest budget plan per account
  - Update BudgetLine FK references to new category IDs
- [ ] Test migration rollback with `Down()` method

### 3. TransactionSplit Category Validation

- [ ] Add validation rule in `TransactionService` or validator
- [ ] Ensure TransactionSplits with direct `CategoryId` reference categories belonging to budget plans owned by transaction's account
- [ ] Validate via `Category.BudgetPlan.AccountId == Transaction.AccountId`
- [ ] Add unit tests for validation

### 4. Template System Implementation

- [ ] Create hard-coded template definitions in `BudgetPlanService`:
  - **Personal**: Salary, Housing, Transportation, Food, Entertainment, Savings
  - **Business**: Revenue, COGS, Operating Expenses, Marketing, Payroll
  - **Household**: Income, Rent/Mortgage, Utilities, Groceries, Childcare, Healthcare
  - **Empty**: No categories pre-created
- [ ] Add `CreateFromTemplateAsync(CreateBudgetPlanDto dto, string templateName)` method
- [ ] Auto-create categories when budget plan created with template
- [ ] Ensure categories are fully editable post-creation

### 5. Repository & Service Updates

- [ ] Update `ICategoryRepository`:
  - Change `GetActiveAsync()` to `GetByBudgetPlanAsync(Guid budgetPlanId)`
  - Update `GetByNameAsync(string name)` to `GetByNameAsync(Guid budgetPlanId, string name)`
- [ ] Update `CategoryRepository` implementations with budget plan filtering
- [ ] Update `CategoryService` methods to require `budgetPlanId` parameter
- [ ] Update category validators to check `(BudgetPlanId, Name)` uniqueness
- [ ] Update `CategoryDto` to include `BudgetPlanId` property

### 6. Controller Updates

- [ ] Update `CategoriesController` GET endpoint to require `budgetPlanId` query parameter
- [ ] Validate budget plan ownership in all category operations
- [ ] Modify `BudgetPlansController.CreateAsync()` to accept `TemplateName` in DTO
- [ ] Add validation for template names

### 7. Blazor UI Updates

- [ ] Add template dropdown to `BudgetPlanCreate.razor`
- [ ] Scope category dropdown in `BudgetPlanEdit.razor` to current budget plan
- [ ] Add budget plan selector to `Categories.razor`
- [ ] Update category management UI for budget-plan-scoped CRUD

### 8. Seed Data Updates

- [ ] Modify `AppDbContext.SeedAsync()` to apply Personal template for sample budget plan
- [ ] Remove hardcoded global category seed data
- [ ] Ensure seed data uses template system

### 9. Test Updates

- [ ] Update existing category unit tests to include budget plan context
- [ ] Add template application tests
- [ ] Add TransactionSplit category validation tests
- [ ] Update integration tests for scoped queries
- [ ] Add cascade delete behavior tests
- [ ] Test data migration logic

## Database Schema Changes

```csharp
// Category.cs
public class Category : BaseEntity
{
    public string Name { get; set; }
    public CategoryType Type { get; set; }
    public Guid BudgetPlanId { get; set; }  // NEW
    public BudgetPlan BudgetPlan { get; set; }  // NEW
}

// BudgetPlan.cs
public class BudgetPlan : BaseEntity
{
    public Guid AccountId { get; set; }
    public Account Account { get; set; }
    public int Year { get; set; }
    public string Name { get; set; }
    public ICollection<BudgetLine> BudgetLines { get; set; }
    public ICollection<Category> Categories { get; set; }  // NEW
}

// AppDbContext.cs - EF Core Configuration
modelBuilder.Entity<Category>(entity =>
{
    // ... existing config ...

    entity.HasOne(c => c.BudgetPlan)
        .WithMany(bp => bp.Categories)
        .HasForeignKey(c => c.BudgetPlanId)
        .OnDelete(DeleteBehavior.Cascade);  // NEW

    entity.HasIndex(c => new { c.BudgetPlanId, c.Name });  // CHANGED from c.Name
});

modelBuilder.Entity<BudgetLine>(entity =>
{
    entity.HasOne(bl => bl.Category)
        .WithMany()
        .HasForeignKey(bl => bl.CategoryId)
        .OnDelete(DeleteBehavior.Cascade);  // CHANGED from Restrict
});
```

## Template Definitions

```csharp
public static class CategoryTemplates
{
    public static readonly Dictionary<string, List<(string Name, CategoryType Type)>> Templates = new()
    {
        ["Personal"] = new()
        {
            ("Salary", CategoryType.Income),
            ("Housing", CategoryType.Expense),
            ("Transportation", CategoryType.Expense),
            ("Food", CategoryType.Expense),
            ("Entertainment", CategoryType.Expense),
            ("Savings", CategoryType.Expense)
        },
        ["Business"] = new()
        {
            ("Revenue", CategoryType.Income),
            ("COGS", CategoryType.Expense),
            ("Operating Expenses", CategoryType.Expense),
            ("Marketing", CategoryType.Expense),
            ("Payroll", CategoryType.Expense)
        },
        ["Household"] = new()
        {
            ("Income", CategoryType.Income),
            ("Rent/Mortgage", CategoryType.Expense),
            ("Utilities", CategoryType.Expense),
            ("Groceries", CategoryType.Expense),
            ("Childcare", CategoryType.Expense),
            ("Healthcare", CategoryType.Expense)
        },
        ["Empty"] = new()
    };
}
```

## Data Migration Strategy

**Existing Global Categories → Budget-Plan-Scoped:**

1. For each existing budget plan with budget lines:

   - Duplicate all categories referenced by its budget lines
   - Assign duplicated categories to that budget plan
   - Update budget line FK references to new category IDs

2. For orphaned categories (not referenced by any budget line):

   - Assign to the newest budget plan for each account
   - If no budget plans exist for an account, categories are dropped

3. No rollback/retention strategy needed (per UserStory-6: database migration)

## Testing

### Unit Tests

- Template application logic (all four templates)
- Category name uniqueness within budget plan scope
- TransactionSplit category validation
- Cascade delete behavior

### Integration Tests

- Category CRUD operations scoped to budget plan
- BudgetPlan creation with template selection
- Data migration (global → scoped categories)
- Budget line category references after migration
- Cross-budget-plan isolation (verify no leakage)

### E2E Tests

- Budget plan creation with template selection UI
- Category management within budget plan context
- Budget line creation with scoped category dropdown
- Category editing after template application

## Success Criteria

- ✅ Categories are scoped to budget plans (foreign key enforced)
- ✅ Users can select from four predefined templates (Personal, Business, Household, Empty)
- ✅ Categories are fully editable after creation
- ✅ No cross-budget-plan category access (enforced by queries and validation)
- ✅ Migration applies successfully with data duplication
- ✅ Cascade delete: deleting budget plan removes its categories
- ✅ TransactionSplit validation ensures category belongs to correct budget plan
- ✅ Category name uniqueness scoped to `(BudgetPlanId, Name)`
- ✅ Seed data uses Personal template
- ✅ All tests pass
