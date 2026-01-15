# Post-MVP-3: Restructure Category Ownership

## Objective

Restructure categories from global entities to budget-plan-scoped entities with editable starter templates.

## Requirements

- Add `BudgetPlanId` foreign key to `Category` entity
- Update `AppDbContext` to configure navigation properties
- Create starter templates (Personal, Business, Household)
- Make categories editable after template application
- Add `BudgetPlanService.CreateFromTemplate()` method

## Implementation Tasks

- [ ] Add `BudgetPlanId` property to `Category.cs`
- [ ] Configure one-to-many relationship in `AppDbContext.cs`
- [ ] Create migration for schema changes
- [ ] Create `CategoryTemplate` entity or seed data structure
- [ ] Implement three starter templates:
  - Personal (Income, Housing, Transportation, Food, Entertainment, Savings)
  - Business (Revenue, COGS, Operating Expenses, Marketing, Payroll)
  - Household (Income, Rent/Mortgage, Utilities, Groceries, Childcare, Healthcare)
- [ ] Add `BudgetPlanService.CreateFromTemplate(budgetPlanId, templateName)` method
- [ ] Update category CRUD endpoints to scope queries by budget plan
- [ ] Update Blazor UI to show template selection during budget plan creation
- [ ] Add UI for editing categories after creation

## Database Schema Changes

```csharp
public class Category : BaseEntity
{
    public string Name { get; set; }
    public CategoryType Type { get; set; }
    public Guid BudgetPlanId { get; set; }  // NEW
    public BudgetPlan BudgetPlan { get; set; }  // NEW
    public bool IsArchived { get; set; }
}
```

## Testing

- Unit tests for template application
- Integration tests for category-budget plan relationship
- Verify categories are isolated per budget plan
- Verify category editing after template application

## Success Criteria

- Categories are scoped to budget plans
- Users can select from predefined templates
- Categories are fully editable after creation
- No cross-budget-plan category access
- Migration applies successfully
