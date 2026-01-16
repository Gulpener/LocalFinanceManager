# UserStory-3.2: Category Template System

## Objective

Implement hard-coded category template system (Personal, Business, Household, Empty) with `BudgetPlanService.CreateFromTemplateAsync()` method to auto-create categories when budget plans are created, while keeping categories fully editable post-creation.

## Requirements

- Create hard-coded template definitions for four templates (Personal, Business, Household, Empty)
- Add `CreateFromTemplateAsync()` method to `BudgetPlanService`
- Update `CreateBudgetPlanDto` to accept `TemplateName` parameter
- Auto-create categories when budget plan created with template
- Ensure categories are fully editable after template application
- Update `BudgetPlansController` to handle template selection
- Add validation for template names
- Update seed data to use Personal template
- Add unit tests for template application logic

## Dependencies

- **REQUIRED: UserStory-3.1** - Category Budget Plan Scoping & Migration
  - `Category.BudgetPlanId` foreign key must exist
  - All Phase 3 tests from US-3.1 must be passing
  - Repository methods `GetByBudgetPlanAsync()` must be functional

## Implementation Tasks

### 1. Template Definitions

- [ ] Create [Services/CategoryTemplates.cs](../../LocalFinanceManager/Services/CategoryTemplates.cs) with hard-coded template definitions:

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

      public static bool IsValidTemplate(string templateName)
      {
          return Templates.ContainsKey(templateName);
      }
  }
  ```

### 2. DTO Updates

- [ ] Update [DTOs/BudgetDTOs.cs](../../LocalFinanceManager/DTOs/BudgetDTOs.cs) to add `TemplateName` property:
  ```csharp
  public class CreateBudgetPlanDto
  {
      public Guid AccountId { get; set; }
      public int Year { get; set; }
      public string Name { get; set; }
      public string? TemplateName { get; set; }  // NEW - Optional, defaults to "Empty"
  }
  ```

### 3. BudgetPlanService Updates

- [ ] Add `CreateFromTemplateAsync()` method to `IBudgetPlanService`:

  ```csharp
  Task<BudgetPlanDto> CreateFromTemplateAsync(CreateBudgetPlanDto dto, string templateName);
  ```

- [ ] Implement `CreateFromTemplateAsync()` in `BudgetPlanService`:

  ```csharp
  public async Task<BudgetPlanDto> CreateFromTemplateAsync(CreateBudgetPlanDto dto, string templateName)
  {
      // 1. Validate template name
      if (!CategoryTemplates.IsValidTemplate(templateName))
      {
          throw new ValidationException($"Invalid template name: {templateName}");
      }

      // 2. Create budget plan
      var budgetPlan = await CreateAsync(dto);

      // 3. Auto-create categories from template
      if (templateName != "Empty")
      {
          var categoryDefinitions = CategoryTemplates.Templates[templateName];
          foreach (var (name, type) in categoryDefinitions)
          {
              var categoryDto = new CreateCategoryDto
              {
                  Name = name,
                  Type = type,
                  BudgetPlanId = budgetPlan.Id
              };
              await _categoryService.CreateAsync(categoryDto);
          }
      }

      return budgetPlan;
  }
  ```

- [ ] Update existing `CreateAsync()` method to use `CreateFromTemplateAsync()` internally if `TemplateName` is provided:
  ```csharp
  public async Task<BudgetPlanDto> CreateAsync(CreateBudgetPlanDto dto)
  {
      var templateName = dto.TemplateName ?? "Empty";
      return await CreateFromTemplateAsync(dto, templateName);
  }
  ```

### 4. Validator Updates

- [ ] Add template name validation to [DTOs/Validators/CreateBudgetPlanDtoValidator.cs](../../LocalFinanceManager/DTOs/Validators/CreateBudgetPlanDtoValidator.cs):
  ```csharp
  RuleFor(dto => dto.TemplateName)
      .Must(name => name == null || CategoryTemplates.IsValidTemplate(name))
      .WithMessage("Template name must be one of: Personal, Business, Household, Empty");
  ```

### 5. Controller Updates

- [ ] Update [Controllers/BudgetPlansController.cs](../../LocalFinanceManager/Controllers/BudgetPlansController.cs) `CreateAsync()` endpoint to handle template selection:
  ```csharp
  [HttpPost]
  public async Task<ActionResult<BudgetPlanDto>> Create([FromBody] CreateBudgetPlanDto dto)
  {
      var templateName = dto.TemplateName ?? "Empty";
      var result = await _budgetPlanService.CreateFromTemplateAsync(dto, templateName);
      return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
  }
  ```

### 6. Seed Data Updates

- [ ] Update [Data/AppDbContext.cs](../../LocalFinanceManager/Data/AppDbContext.cs) `SeedAsync()` method to use Personal template:

  ```csharp
  public static async Task SeedAsync(AppDbContext context)
  {
      if (!context.BudgetPlans.Any())
      {
          var account = context.Accounts.First();
          var budgetPlan = new BudgetPlan
          {
              Id = Guid.NewGuid(),
              AccountId = account.Id,
              Year = DateTime.UtcNow.Year,
              Name = "Annual Budget",
              CreatedAt = DateTime.UtcNow,
              UpdatedAt = DateTime.UtcNow
          };
          context.BudgetPlans.Add(budgetPlan);
          await context.SaveChangesAsync();

          // Apply Personal template
          var personalCategories = CategoryTemplates.Templates["Personal"];
          foreach (var (name, type) in personalCategories)
          {
              context.Categories.Add(new Category
              {
                  Id = Guid.NewGuid(),
                  Name = name,
                  Type = type,
                  BudgetPlanId = budgetPlan.Id,
                  CreatedAt = DateTime.UtcNow,
                  UpdatedAt = DateTime.UtcNow
              });
          }
          await context.SaveChangesAsync();
      }
  }
  ```

- [ ] Remove hardcoded global category seed data (all categories now scoped to budget plans)

### 7. Unit Tests

- [ ] Add template application tests in [LocalFinanceManager.Tests/Services/BudgetPlanServiceTests.cs](../../tests/LocalFinanceManager.Tests/Services/BudgetPlanServiceTests.cs):

  - Test creating budget plan with "Personal" template → 6 categories auto-created
  - Test creating budget plan with "Business" template → 5 categories auto-created
  - Test creating budget plan with "Household" template → 6 categories auto-created
  - Test creating budget plan with "Empty" template → 0 categories created
  - Test invalid template name throws `ValidationException`

- [ ] Add category editability tests:
  - Create budget plan with Personal template
  - Update category name from "Salary" to "Income"
  - Verify update succeeds (categories are fully editable post-creation)

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

## Testing

### Unit Test Scenarios

1. **Template Application Logic:**

   - Create budget plan with "Personal" template → Verify 6 categories auto-created with correct names and types
   - Create budget plan with "Empty" template → Verify 0 categories created

2. **Template Validation:**

   - Create budget plan with invalid template name "InvalidTemplate" → Throws `ValidationException`

3. **Category Editability:**

   - Create budget plan with template
   - Update category name and type
   - Verify updates persist (categories are not read-only)

4. **Seed Data:**
   - Run `SeedAsync()` on empty database
   - Verify budget plan created with Personal template applied (6 categories exist)

## Success Criteria

- ✅ Users can select from four predefined templates (Personal, Business, Household, Empty)
- ✅ Categories are auto-created when budget plan created with non-Empty template
- ✅ Categories are fully editable after creation (not locked to template definitions)
- ✅ Seed data uses Personal template (no hardcoded global categories)
- ✅ Invalid template names rejected with validation error
- ✅ Empty template creates budget plan with no categories
- ✅ All unit tests pass (template application logic covered)

## Definition of Done

- [ ] `CategoryTemplates` class created with four template definitions (Personal, Business, Household, Empty)
- [ ] `CreateBudgetPlanDto` includes optional `TemplateName` property
- [ ] `IBudgetPlanService` has `CreateFromTemplateAsync()` method signature
- [ ] `BudgetPlanService.CreateFromTemplateAsync()` implemented with template logic
- [ ] `BudgetPlanService.CreateAsync()` delegates to `CreateFromTemplateAsync()` when template specified
- [ ] `CreateBudgetPlanDtoValidator` validates template names
- [ ] `BudgetPlansController.Create()` handles template selection
- [ ] `AppDbContext.SeedAsync()` uses Personal template for sample budget plan
- [ ] Hardcoded global category seed data removed
- [ ] Unit tests verify template application for all four templates
- [ ] Unit tests verify categories are editable post-creation
- [ ] Unit tests verify invalid template names throw `ValidationException`
- [ ] Seed data test verifies Personal template applied on first run
- [ ] Code follows [Implementation-Guidelines.md](../Implementation-Guidelines.md)
- [ ] No manual migrations required (categories auto-created via service layer)

## Estimated Effort

**1-2 days** (~12 implementation tasks)

## Notes

- **Future Extensibility:** This implementation uses hard-coded templates for MVP simplicity. Post-MVP enhancement could add database-backed custom user templates (new `CategoryTemplate` entity) allowing users to save/share their own template definitions.
- **Template Immutability:** Templates are read-only definitions. Once applied, categories become independent entities that can be edited/deleted without affecting the template definition.
- **Empty Template Use Case:** Empty template useful for users who want full control over category creation without pre-populated suggestions.
- **Next Steps:** After this story completes, UserStory-3.3 will add Blazor UI for template selection and comprehensive E2E tests.
