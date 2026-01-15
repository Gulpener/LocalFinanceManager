# MVP 2.3 — CategoryType Enum (Inkomsten vs Uitgaven)

Doel

- Voeg `CategoryType` enum toe aan `Category` model om onderscheid te maken tussen Inkomsten en Uitgaven categorieën voor betere budget-analyse.

Acceptatiecriteria

- `Category` model bevat `Type` property met enum `CategoryType { Income, Expense }`.
- EF Core migratie: `AddCategoryTypeField` wordt automatisch aangemaakt en toegepast bij app startup.
- Alle DTOs (CategoryDto, CreateCategoryDto, UpdateCategoryDto) bevatten `Type` property.
- Validators vereisen `Type` property.
- UI: Category create/edit formulieren bevatten Type selectie (radio buttons of dropdown).
- Seed data bevat Type voor alle sample categorieën.
- Optioneel: Budget editor groepeert budgetregels per Income/Expense.

Data model

**Category model updates (Models/Category.cs):**

```csharp
namespace LocalFinanceManager.Models;

/// <summary>
/// Category type: Income or Expense.
/// </summary>
public enum CategoryType
{
    Income,
    Expense
}

/// <summary>
/// Represents a budget category for transaction classification.
/// </summary>
public class Category : BaseEntity
{
    /// <summary>
    /// Category name (e.g., "Groceries", "Rent", "Salary").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Category type (Income or Expense).
    /// </summary>
    public CategoryType Type { get; set; }
}
```

DB / EF details

- **Migration:** EF Core genereert automatisch migratie `AddCategoryTypeField`:
  - Voegt kolom `Type` toe aan `Categories` tabel als `INTEGER` (enum opgeslagen als int)
  - Default waarde: `Expense` (0) voor backwards compatibility met bestaande categorieën
- **EF Core configuratie (AppDbContext.cs OnModelCreating):**
  ```csharp
  modelBuilder.Entity<Category>()
      .Property(c => c.Type)
      .HasConversion<int>() // Store enum as int
      .HasDefaultValue(CategoryType.Expense);
  ```
- **Automatic migration:** Migratie wordt toegepast bij app startup via `Database.MigrateAsync()` in `Program.cs`.
- **Bestaande data:** Bestaande categorieën krijgen default `Expense` na migratie; handmatig updaten indien nodig of via seed update.

API contract details

**DTO updates:**

```csharp
// CategoryDto (in BudgetDTOs.cs of CategoryDTOs.cs)
public record CategoryDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public CategoryType Type { get; init; } // NIEUW
}

// CreateCategoryDto
public record CreateCategoryDto
{
    public string Name { get; init; } = string.Empty;
    public CategoryType Type { get; init; } // NIEUW
}

// UpdateCategoryDto (MVP 2.1)
public record UpdateCategoryDto
{
    public string Name { get; init; } = string.Empty;
    public CategoryType Type { get; init; } // NIEUW
    public byte[]? RowVersion { get; init; }
}
```

**Validator updates:**

```csharp
// CreateCategoryDtoValidator update
public class CreateCategoryDtoValidator : AbstractValidator<CreateCategoryDto>
{
    public CreateCategoryDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Naam is verplicht")
            .MaximumLength(100).WithMessage("Naam mag maximaal 100 tekens bevatten");

        RuleFor(x => x.Type)
            .IsInEnum().WithMessage("Type moet Income of Expense zijn"); // NIEUW
    }
}

// UpdateCategoryDtoValidator update
public class UpdateCategoryDtoValidator : AbstractValidator<UpdateCategoryDto>
{
    public UpdateCategoryDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Naam is verplicht")
            .MaximumLength(100).WithMessage("Naam mag maximaal 100 tekens bevatten");

        RuleFor(x => x.Type)
            .IsInEnum().WithMessage("Type moet Income of Expense zijn"); // NIEUW

        RuleFor(x => x.RowVersion)
            .NotNull().WithMessage("RowVersion is verplicht voor concurrency controle");
    }
}
```

**API responses:**

- `GET /api/categories` response voorbeeld:

  ```json
  [
    { "id": "...", "name": "Salaris", "type": "Income" },
    { "id": "...", "name": "Huur", "type": "Expense" }
  ]
  ```

- `POST /api/categories` request voorbeeld:
  ```json
  { "name": "Freelance Inkomsten", "type": "Income" }
  ```

Business regels

- **Default Type:** Bij create is `Type` verplicht (geen default in DTO); UI moet expliciet selectie forceren.
- **Type wijzigen:** Bestaande categorieën mogen van type wisselen (Income ↔ Expense); heeft impact op budget-analyse maar is toegestaan.
- **Filtering per type:** API mag optioneel `/api/categories?type=Income` query parameter ondersteunen (toekomstige uitbreiding, niet verplicht voor MVP 2.3).

UI aanwijzingen (Blazor)

**Updates in Components/Pages/CategoryCreate.razor en CategoryEdit.razor:**

1. **Type selectie input:**

   ```razor
   <div class="form-group">
       <label for="type">Type</label>
       <div>
           <input type="radio" id="typeIncome" name="type" value="@CategoryType.Income" @bind="_categoryType" />
           <label for="typeIncome">Inkomsten</label>

           <input type="radio" id="typeExpense" name="type" value="@CategoryType.Expense" @bind="_categoryType" />
           <label for="typeExpense">Uitgaven</label>
       </div>
   </div>
   ```

2. **CategoryList.razor (Components/Pages/Categories.razor) update:**

   - Voeg kolom "Type" toe aan tabel:
     ```razor
     <thead>
         <tr>
             <th>Naam</th>
             <th>Type</th> <!-- NIEUW -->
             <th>Acties</th>
         </tr>
     </thead>
     <tbody>
         @foreach (var category in _categories)
         {
             <tr>
                 <td>@category.Name</td>
                 <td>@(category.Type == CategoryType.Income ? "Inkomsten" : "Uitgaven")</td> <!-- NIEUW -->
                 <td>...</td>
             </tr>
         }
     </tbody>
     ```

3. **Optioneel: Budget editor grouping (BudgetPlanEdit.razor):**
   - Groepeer budgetregels per categorie-type:

     ```razor
     <h4>Inkomsten</h4>
     <table>
         @foreach (var line in _budgetLines.Where(l => l.CategoryType == CategoryType.Income))
         { ... }
     </table>

     <h4>Uitgaven</h4>
     <table>
         @foreach (var line in _budgetLines.Where(l => l.CategoryType == CategoryType.Expense))
         { ... }
     </table>

     <div>
         <strong>Totaal Inkomsten:</strong> @_incomeTotal
         <strong>Totaal Uitgaven:</strong> @_expenseTotal
         <strong>Netto:</strong> @(_incomeTotal - _expenseTotal)
     </div>
     ```

   - Hiervoor moet `BudgetLineDto` uitgebreid worden met `CategoryType`:
     ```csharp
     public record BudgetLineDto
     {
         // ... bestaande properties ...
         public CategoryType CategoryType { get; init; } // NIEUW (via join met Category)
     }
     ```

Edgecases

- **Bestaande categorieën na migratie:** Alle bestaande categorieën krijgen `Expense`; gebruiker moet handmatig inkomsten-categorieën updaten.
- **Type wijzigen van veel-gebruikte categorie:** Toegestaan; heeft impact op budget analyse maar geen data-integriteit issues.
- **Enum serialisatie:** JSON serialiseert enum als string ("Income", "Expense") voor leesbaarheid; EF Core slaat op als int (0, 1).

Tests

**Unit tests** (`LocalFinanceManager.Tests`):

- `CreateCategoryDtoValidator` test suite update:
  - Missing Type → validation error
  - Invalid enum value (e.g., 99) → validation error
  - Valid Income/Expense → passes validation
- `UpdateCategoryDtoValidator` test suite update (idem)
- Enum serialisation test:
  - `CategoryType.Income` → JSON "Income"
  - JSON "Expense" → `CategoryType.Expense`

**Integration tests** (`LocalFinanceManager.Tests` met in-memory SQLite):

- Create category with Type = Income → verify persistence
- Create category with Type = Expense → verify persistence
- Update category Type from Expense to Income → verify update
- Query categories, filter by type (indien query parameter geïmplementeerd)
- Migration test: run migration, verify Type column exists with default value

**E2E tests** (`LocalFinanceManager.E2E` met Playwright):

- **Test:** Create Income category workflow
  - Navigate `/categories/new` → Enter name "Salaris" → Select "Inkomsten" radio → Submit → Verify appears in list with type "Inkomsten"
- **Test:** Create Expense category workflow
  - Navigate `/categories/new` → Enter name "Boodschappen" → Select "Uitgaven" radio → Submit → Verify appears in list with type "Uitgaven"
- **Test:** Edit category type
  - Create category "Test" with Expense → Navigate edit → Change to Income → Save → Verify type updated in list
- **Test:** Category list shows type column
  - Create mix of Income/Expense categories → Navigate `/categories` → Verify Type column displays correct values

Deployment/env

- Geen nieuwe environment variabelen nodig.
- **Automatic migrations:** `AddCategoryTypeField` migratie wordt automatisch toegepast bij app startup.

Voorbeeld seed-data

**Update in `AppDbContext.SeedAsync()`:**

```csharp
if (!context.Categories.Any())
{
    var categories = new List<Category>
    {
        // Inkomsten
        new() { Id = Guid.NewGuid(), Name = "Salaris", Type = CategoryType.Income, IsArchived = false },
        new() { Id = Guid.NewGuid(), Name = "Freelance", Type = CategoryType.Income, IsArchived = false },
        new() { Id = Guid.NewGuid(), Name = "Dividend", Type = CategoryType.Income, IsArchived = false },

        // Uitgaven
        new() { Id = Guid.NewGuid(), Name = "Huur", Type = CategoryType.Expense, IsArchived = false },
        new() { Id = Guid.NewGuid(), Name = "Boodschappen", Type = CategoryType.Expense, IsArchived = false },
        new() { Id = Guid.NewGuid(), Name = "Transport", Type = CategoryType.Expense, IsArchived = false },
        new() { Id = Guid.NewGuid(), Name = "Entertainment", Type = CategoryType.Expense, IsArchived = false },
        new() { Id = Guid.NewGuid(), Name = "Utilities", Type = CategoryType.Expense, IsArchived = false }
    };
    await context.Categories.AddRangeAsync(categories);
    await context.SaveChangesAsync();
}
```

Definition of Done

- `CategoryType` enum toegevoegd aan `Category` met waarden `Income`, `Expense`.
- `Type` property toegevoegd en geconfigureerd in EF Core.
- Migratie `AddCategoryTypeField` automatisch gegenereerd en toegepast bij app startup.
- Alle DTOs (CategoryDto, CreateCategoryDto, UpdateCategoryDto) bevatten `Type` property.
- Validators vereisen en valideren `Type` enum.
- Category create/edit UI bevat Type selectie (radio buttons).
- Category lijst toont Type kolom.
- Seed data bevat mix van Income/Expense categorieën.
- Optioneel: Budget editor groepeert income/expense en toont netto.
- Unit tests voor enum validation en serialisation.
- Integration tests voor create/update met Type.
- E2E tests voor create Income/Expense categories en type editing.
- Bestaande categorieën hebben default `Expense` na migratie.
