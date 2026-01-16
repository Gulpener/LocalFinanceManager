# UserStory-3.1: Category Budget Plan Scoping & Migration

## Objective

Restructure categories from global entities to budget-plan-scoped entities by adding `BudgetPlanId` foreign key, implementing data migration logic, and updating all repository/service layers to enforce budget plan isolation.

## Requirements

- Add `BudgetPlanId` foreign key to `Category` entity with cascade delete
- Update `AppDbContext` to configure navigation properties
- Create EF Core migration with SQL data migration logic
- Duplicate existing global categories into each budget plan
- Update repository queries to scope by budget plan
- Add TransactionSplit validation for budget-plan-scoped categories
- Update category name uniqueness validation to `(BudgetPlanId, Name)` scope
- Update all API controllers to require `budgetPlanId` parameter
- Ensure all tests pass with new scoped queries

## Dependencies

- **None** - This is the foundational story for category restructuring

## Execution Guidance

This story is organized into **3 phases** that must be completed sequentially. Each phase includes an explicit checkpoint to ensure stability before proceeding:

**Phase 1: Entity Model & Migration** (~9 tasks, Day 1)

- Complete entity changes and generate migration
- **CHECKPOINT:** Run `dotnet ef migrations add AddCategoryBudgetPlanScoping` and verify migration builds without errors. Inspect generated SQL to confirm `BudgetPlanId` column and data duplication logic.

**Phase 2: Repository & Service Updates** (~7 tasks, Day 2)

- Update repository interfaces and service methods
- **CHECKPOINT:** Run `dotnet test LocalFinanceManager.Tests --filter Category` and verify all repository unit tests pass with budget plan filtering.

**Phase 3: Controller & Integration Tests** (~7 tasks, Day 3)

- Update API controllers and add comprehensive tests
- **CHECKPOINT:** Run full test suite (`dotnet test`) and verify 100% pass rate before marking story complete. Manually test migration on sample database.

## Implementation Tasks

### Phase 1: Entity Model & Migration

**1. Entity Model Updates**

- [ ] Add `BudgetPlanId` property to [Models/Category.cs](../../LocalFinanceManager/Models/Category.cs):
  ```csharp
  public Guid BudgetPlanId { get; set; }
  ```
- [ ] Add `BudgetPlan` navigation property to [Models/Category.cs](../../LocalFinanceManager/Models/Category.cs):
  ```csharp
  public BudgetPlan BudgetPlan { get; set; }
  ```
- [ ] Add `ICollection<Category>` navigation property to [Models/BudgetPlan.cs](../../LocalFinanceManager/Models/BudgetPlan.cs):
  ```csharp
  public ICollection<Category> Categories { get; set; }
  ```

**2. AppDbContext Configuration**

- [ ] Update [Data/AppDbContext.cs](../../LocalFinanceManager/Data/AppDbContext.cs) to configure `Category → BudgetPlan` relationship:
  ```csharp
  entity.HasOne(c => c.BudgetPlan)
      .WithMany(bp => bp.Categories)
      .HasForeignKey(c => c.BudgetPlanId)
      .OnDelete(DeleteBehavior.Cascade);
  ```
- [ ] Change `BudgetLine → Category` relationship to cascade delete:
  ```csharp
  entity.HasOne(bl => bl.Category)
      .WithMany()
      .HasForeignKey(bl => bl.CategoryId)
      .OnDelete(DeleteBehavior.Cascade);  // CHANGED from Restrict
  ```
- [ ] Update Category index from `Name` to composite `(BudgetPlanId, Name)`:
  ```csharp
  entity.HasIndex(c => new { c.BudgetPlanId, c.Name });
  ```

**3. Database Migration with Data Migration**

- [ ] Generate migration: `dotnet ef migrations add AddCategoryBudgetPlanScoping`
- [ ] Implement SQL data migration in `Up()` method of generated migration file:

  ```csharp
  // 1. Add nullable BudgetPlanId column first
  migrationBuilder.AddColumn<Guid>(
      name: "BudgetPlanId",
      table: "Categories",
      nullable: true);

  // 2. Duplicate categories for each budget plan
  migrationBuilder.Sql(@"
      INSERT INTO Categories (Id, Name, Type, BudgetPlanId, IsArchived, CreatedAt, UpdatedAt, RowVersion)
      SELECT
          NEWID(),
          c.Name,
          c.Type,
          bp.Id AS BudgetPlanId,
          c.IsArchived,
          GETUTCDATE(),
          GETUTCDATE(),
          c.RowVersion
      FROM Categories c
      CROSS JOIN BudgetPlans bp
      WHERE c.BudgetPlanId IS NULL;

      -- Assign orphaned categories to newest budget plan per account
      UPDATE c
      SET c.BudgetPlanId = (
          SELECT TOP 1 bp.Id
          FROM BudgetPlans bp
          ORDER BY bp.CreatedAt DESC
      )
      FROM Categories c
      WHERE c.BudgetPlanId IS NULL;

      -- Delete original global categories
      DELETE FROM Categories WHERE BudgetPlanId IS NULL;
  ");

  // 3. Make BudgetPlanId non-nullable
  migrationBuilder.AlterColumn<Guid>(
      name: "BudgetPlanId",
      table: "Categories",
      nullable: false);
  ```

- [ ] Implement rollback logic in `Down()` method (convert scoped categories back to global)

**CHECKPOINT:** Run `dotnet ef migrations add AddCategoryBudgetPlanScoping` and verify migration builds without errors. Inspect generated SQL to confirm `BudgetPlanId` column and data duplication logic.

---

### Phase 2: Repository & Service Updates

**4. Repository Interface Updates**

- [ ] Update [Data/Repositories/ICategoryRepository.cs](../../LocalFinanceManager/Data/Repositories/ICategoryRepository.cs):
  - Change `GetActiveAsync()` to `GetByBudgetPlanAsync(Guid budgetPlanId)`
  - Update `GetByNameAsync(string name)` to `GetByNameAsync(Guid budgetPlanId, string name)`

**5. Repository Implementation Updates**

- [ ] Update [Data/Repositories/CategoryRepository.cs](../../LocalFinanceManager/Data/Repositories/CategoryRepository.cs):

  ```csharp
  public async Task<List<Category>> GetByBudgetPlanAsync(Guid budgetPlanId)
  {
      return await _context.Categories
          .Where(c => c.BudgetPlanId == budgetPlanId && !c.IsArchived)
          .ToListAsync();
  }

  public async Task<Category?> GetByNameAsync(Guid budgetPlanId, string name)
  {
      return await _context.Categories
          .Where(c => c.BudgetPlanId == budgetPlanId && c.Name == name && !c.IsArchived)
          .FirstOrDefaultAsync();
  }
  ```

**6. Service Layer Updates**

- [ ] Update `CategoryService` methods to require `budgetPlanId` parameter
- [ ] Update category validators to check `(BudgetPlanId, Name)` uniqueness
- [ ] Update [DTOs/CategoryDTOs.cs](../../LocalFinanceManager/DTOs/CategoryDTOs.cs) to include `BudgetPlanId` property:

  ```csharp
  public class CategoryDto
  {
      public Guid Id { get; set; }
      public string Name { get; set; }
      public CategoryType Type { get; set; }
      public Guid BudgetPlanId { get; set; }  // NEW
  }

  public class CreateCategoryDto
  {
      public string Name { get; set; }
      public CategoryType Type { get; set; }
      public Guid BudgetPlanId { get; set; }  // NEW
  }
  ```

**7. TransactionSplit Category Validation**

- [ ] Add validation rule in `TransactionService` or `TransactionSplitValidator`:
  ```csharp
  // Ensure CategoryId references category belonging to transaction's account budget plan
  RuleFor(ts => ts.CategoryId)
      .MustAsync(async (split, categoryId, context, cancellationToken) => {
          var transaction = await _transactionRepo.GetByIdAsync(split.TransactionId);
          var category = await _categoryRepo.GetByIdAsync(categoryId);
          return category?.BudgetPlan.AccountId == transaction?.AccountId;
      })
      .WithMessage("Category must belong to budget plan associated with transaction's account");
  ```
- [ ] Add unit tests for TransactionSplit validation in [LocalFinanceManager.Tests/Validators/](../../tests/LocalFinanceManager.Tests/Validators/)

**CHECKPOINT:** Run `dotnet test LocalFinanceManager.Tests --filter Category` and verify all repository unit tests pass with budget plan filtering.

---

### Phase 3: Controller & Integration Tests

**8. Controller Updates**

- [ ] Update [Controllers/CategoriesController.cs](../../LocalFinanceManager/Controllers/CategoriesController.cs) GET endpoint to require `budgetPlanId` query parameter:
  ```csharp
  [HttpGet]
  public async Task<ActionResult<List<CategoryDto>>> GetCategories([FromQuery] Guid budgetPlanId)
  {
      var categories = await _categoryService.GetByBudgetPlanAsync(budgetPlanId);
      return Ok(categories);
  }
  ```
- [ ] Validate budget plan ownership in all category operations (ensure user owns the account that owns the budget plan)

**9. Test Updates**

- [ ] Update existing category unit tests in [LocalFinanceManager.Tests/Services/](../../tests/LocalFinanceManager.Tests/Services/) to include budget plan context
- [ ] Add integration tests in [LocalFinanceManager.Tests/Integration/](../../tests/LocalFinanceManager.Tests/Integration/):
  - Test scoped queries return only categories for specified budget plan
  - Test cross-budget-plan isolation (Category A in BudgetPlan 1 not visible when querying BudgetPlan 2)
  - Test cascade delete: deleting budget plan removes its categories
  - Test cascade delete: deleting category removes its budget lines
- [ ] Test data migration logic:
  - Seed database with global categories
  - Run migration
  - Verify categories duplicated per budget plan
  - Verify orphaned categories assigned to newest budget plan

**CHECKPOINT:** Run full test suite (`dotnet test`) and verify 100% pass rate before marking story complete. Manually test migration on sample database.

---

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

## Data Migration Strategy

**Existing Global Categories → Budget-Plan-Scoped:**

1. **For each existing budget plan with budget lines:**

   - Duplicate all categories referenced by its budget lines
   - Assign duplicated categories to that budget plan
   - Update budget line FK references to new category IDs

2. **For orphaned categories (not referenced by any budget line):**

   - Assign to the newest budget plan for each account
   - If no budget plans exist for an account, categories are dropped

3. **No rollback/retention strategy needed** (per copilot instructions: database migration is one-way for UserStory-6 context)

## Testing

### Unit Test Scenarios

1. **Category Name Uniqueness:** Category "Groceries" can exist in multiple budget plans (unique per `BudgetPlanId`)
2. **Repository Filtering:** `GetByBudgetPlanAsync(budgetPlan1Id)` returns only categories for `budgetPlan1`, not other plans
3. **TransactionSplit Validation:** Creating TransactionSplit with `CategoryId` from wrong budget plan fails validation
4. **Cascade Delete:** Deleting budget plan removes all associated categories

### Integration Test Scenarios

1. **Cross-Budget-Plan Isolation:**

   - Create Category "Housing" in BudgetPlan A
   - Create Category "Housing" in BudgetPlan B
   - Query BudgetPlan A categories → Only returns BudgetPlan A's "Housing"

2. **Data Migration:**

   - Seed database with 5 global categories
   - Create 2 budget plans
   - Run migration
   - Verify 10 total categories exist (5 duplicated into each plan)

3. **TransactionSplit Validation:**
   - Create Transaction on Account A (linked to BudgetPlan X)
   - Create TransactionSplit with Category from BudgetPlan Y
   - Validation fails with "Category must belong to budget plan associated with transaction's account"

## Success Criteria

- ✅ Categories are scoped to budget plans (foreign key enforced at database level)
- ✅ No cross-budget-plan category access (enforced by repository queries and controller validation)
- ✅ Migration applies successfully with data duplication (existing categories duplicated per budget plan)
- ✅ Cascade delete: deleting budget plan removes its categories (EF Core configuration enforced)
- ✅ Cascade delete: deleting category removes its budget lines (EF Core configuration enforced)
- ✅ TransactionSplit validation ensures category belongs to correct budget plan (FluentValidation rule)
- ✅ Category name uniqueness scoped to `(BudgetPlanId, Name)` (database index enforced)
- ✅ All existing tests updated and passing with budget plan context
- ✅ Integration tests verify cross-budget-plan isolation

## Definition of Done

- [ ] `Category` entity has `BudgetPlanId` foreign key and `BudgetPlan` navigation property
- [ ] `BudgetPlan` entity has `ICollection<Category>` navigation property
- [ ] EF Core relationships configured with cascade delete for Category → BudgetPlan
- [ ] EF Core migration generated with data duplication SQL in `Up()` method
- [ ] Migration tested with rollback (`Down()` method works)
- [ ] `ICategoryRepository` interface updated with `GetByBudgetPlanAsync()` method
- [ ] `CategoryRepository` implementation filters all queries by `BudgetPlanId`
- [ ] `CategoryService` methods require `budgetPlanId` parameter
- [ ] `CategoryDto` includes `BudgetPlanId` property
- [ ] Category validators check `(BudgetPlanId, Name)` uniqueness
- [ ] TransactionSplit validation ensures category belongs to transaction's account budget plan
- [ ] `CategoriesController` GET endpoint requires `budgetPlanId` query parameter
- [ ] Unit tests pass with budget plan context (100% coverage for new validation rules)
- [ ] Integration tests verify cross-budget-plan isolation (no data leakage)
- [ ] Integration tests verify cascade delete behavior (budget plan deletion removes categories)
- [ ] Data migration tested on sample database with global categories
- [ ] No manual migrations required (automatic via `Database.MigrateAsync()`)
- [ ] Code follows [Implementation-Guidelines.md](../Implementation-Guidelines.md)
- [ ] All three phase checkpoints passed with green test suites

## Estimated Effort

**2-3 days** (~23 implementation tasks organized into 3 sequential phases)

## Notes

- **Breaking Change:** This is a foundational schema change that ripples through all category-related queries. All three phases must be completed together—partial implementation will break existing functionality.
- **Migration Complexity:** Data migration duplicates categories for each budget plan. Test thoroughly on realistic seed data before deploying.
- **Cascade Delete Risk:** Deleting a budget plan will remove all its categories and their associated budget lines. Ensure UI warns users before deletion.
- **Next Steps:** After this story completes, UserStory-3.2 will add template system to auto-create categories for new budget plans.
