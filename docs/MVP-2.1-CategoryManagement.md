# MVP 2.1 — Category Management (Volledige CRUD UI)

Doel

- Volledige Category CRUD UI implementeren zodat gebruikers categorieën kunnen beheren (aanmaken, bekijken, bewerken/hernoemen, archiveren) zonder directe API-aanroepen.

Acceptatiecriteria

- API: `PUT /api/categories/{id}` toegevoegd voor update/rename functionaliteit.
- Blazor UI: volledige lijst, create, edit/rename, archive functionaliteit.
- CategoryService: `UpdateAsync` methode toegevoegd.
- NavigatieMenu bevat "Categorieën" link.
- E2E tests volledig werkend (niet meer genegeerd).

Data model

- Category (extends `BaseEntity`) — **reeds bestaand, geen wijzigingen nodig**
  - Id: GUID (inherited from BaseEntity)
  - Name: string(100), not null
  - IsArchived: bool (default false, soft-delete flag)
  - CreatedAt: DateTime (inherited from BaseEntity)
  - UpdatedAt: DateTime (inherited from BaseEntity)
  - RowVersion: byte[] (inherited from BaseEntity, configured for optimistic concurrency)

DB / EF details

- **Migrations:** Geen nieuwe migraties nodig; Category model bestaat al sinds MVP 2.
- **Concurrency:** `RowVersion` byte[] reeds geconfigureerd met EF Core `.IsRowVersion()` voor optimistic concurrency; `DbUpdateConcurrencyException` handled met last-write-wins reload strategie, HTTP 409 Conflict response.
- **Soft-delete filtering:** Alle Category queries moeten expliciet `.Where(c => !c.IsArchived)` bevatten; encapsulated via `ICategoryRepository` pattern.

API contract details

**Bestaande endpoints (geen wijzigingen):**
- `GET /api/categories` — lijst van actieve categorieën
- `GET /api/categories/{id}` — ophalen per ID
- `POST /api/categories` — nieuwe categorie aanmaken
- `DELETE /api/categories/{id}` — archiveren (soft-delete)

**Nieuw toe te voegen endpoint:**
- `PUT /api/categories/{id}` — update/rename categorie
  - Request body: `UpdateCategoryDto { "name":"Nieuwe Naam", "rowVersion":"..." }`
  - Response: `CategoryDto` met bijgewerkte data
  - Statuscodes: 200 OK, 400 Bad Request (validatie), 404 Not Found, 409 Conflict (RowVersion mismatch)

**DTO specificaties:**

```csharp
// Toe te voegen aan BudgetDTOs.cs of nieuwe CategoryDTOs.cs
public record UpdateCategoryDto
{
    public string Name { get; init; } = string.Empty;
    public byte[]? RowVersion { get; init; }
}
```

**Validator specificaties:**

```csharp
// UpdateCategoryDtoValidator.cs in DTOs/Validators/
public class UpdateCategoryDtoValidator : AbstractValidator<UpdateCategoryDto>
{
    public UpdateCategoryDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Naam is verplicht")
            .MaximumLength(100).WithMessage("Naam mag maximaal 100 tekens bevatten");
        
        RuleFor(x => x.RowVersion)
            .NotNull().WithMessage("RowVersion is verplicht voor concurrency controle");
    }
}
```

UI aanwijzingen (Blazor)

**Nieuwe/bijgewerkte pages:**

1. **Components/Pages/Categories.razor** (lijst-overzicht)
   - URL: `/categories`
   - Tabel met kolommen: Name, Actions (Edit, Archive)
   - "Nieuwe Categorie" knop navigeert naar `/categories/new`
   - Archive-knop met bevestigingsmelding
   - Filter om gearchiveerde categorieën te tonen (optioneel)

2. **Components/Pages/CategoryCreate.razor** (nieuwe categorie)
   - URL: `/categories/new`
   - Formulier: Name (text input, required)
   - Client-side + server-side validatie via FluentValidation
   - Submit roept `POST /api/categories` aan
   - Na succes: navigeer naar `/categories` met success toast

3. **Components/Pages/CategoryEdit.razor** (hernoemen)
   - URL: `/categories/{id}/edit`
   - Laad bestaande categorie via `GET /api/categories/{id}`
   - Formulier: Name (text input, pre-filled, required)
   - Hidden field voor RowVersion
   - Submit roept `PUT /api/categories/{id}` aan
   - Bij 409 Conflict: toon dialog "Categorie is gewijzigd door een ander proces. Wilt u de laatste versie herladen?" met Reload/Annuleer knoppen
   - Na succes: navigeer naar `/categories` met success toast

**NavMenu update:**
- Voeg toe aan `Components/Layout/NavMenu.razor`:
  ```razor
  <div class="nav-item px-3">
      <NavLink class="nav-link" href="categories">
          <span class="bi bi-tags-fill" aria-hidden="true"></span> Categorieën
      </NavLink>
  </div>
  ```

Business/edge-cases

- **Duplicate names:** Validatie mag optioneel duplicate namen blokkeren (configurable); standaard toegestaan voor flexibiliteit.
- **Archiveren met actieve budgetregels:** Categorie mag gearchiveerd worden; bestaande `BudgetLine` referenties blijven intact (historische data behouden).
- **Concurrency conflicts:** UI toont reload-optie; last-write-wins strategie na reload.
- **Naam wijzigen van vaak gebruikte categorie:** Wijziging is onmiddellijk zichtbaar in alle budget editors en lijsten.

Tests

**Project Structure:** Shared test infrastructure from MVP-1 en MVP-2 (`LocalFinanceManager.Tests` en `LocalFinanceManager.E2E`).

**Unit tests** (`LocalFinanceManager.Tests`):
- `UpdateCategoryDtoValidator` test suite:
  - Empty name → validation error
  - Name > 100 characters → validation error
  - Null RowVersion → validation error
  - Valid input → passes validation
- `CategoryService.UpdateAsync` logic tests:
  - Update category name → entity updated
  - Non-existent category ID → returns null/throws
  - RowVersion mismatch simulation → `DbUpdateConcurrencyException` path

**Integration tests** (`LocalFinanceManager.Tests` met in-memory SQLite):
- Create category → update name → verify persistence
- Update with stale RowVersion → 409 Conflict response met huidige entity state
- Update archived category → 404 Not Found (service filters archived entities)
- Concurrent update scenario: twee simultane updates → laatste wint na reload

**E2E tests** (`LocalFinanceManager.E2E` met Playwright):
- **Test:** Create category workflow
  - Navigate `/categories` → Click "Nieuwe Categorie" → Fill name "Boodschappen" → Submit → Verify appears in list
- **Test:** Edit/rename category workflow
  - Create category "Transport" → Navigate edit → Change to "Vervoer" → Save → Verify updated name in list
- **Test:** Archive category workflow
  - Create category "Test" → Click Archive → Confirm → Verify removed from active list
- **Test:** Concurrency conflict handling
  - Open category edit in two browser contexts → Update in context A → Attempt update in context B → Verify 409 conflict dialog → Reload → Verify latest data shown
- **Verwijder `[Ignore]` attribute** van alle category E2E tests

Deployment/env

- Geen nieuwe environment variabelen nodig.
- **Automatic migrations:** Geen nieuwe migraties; bestaand schema voldoende.

Voorbeeld seed-data

**Uitbreiding van bestaande seed in `AppDbContext.SeedAsync()`:**

```csharp
if (!context.Categories.Any())
{
    var categories = new List<Category>
    {
        new() { Id = Guid.NewGuid(), Name = "Huur", IsArchived = false },
        new() { Id = Guid.NewGuid(), Name = "Boodschappen", IsArchived = false },
        new() { Id = Guid.NewGuid(), Name = "Transport", IsArchived = false },
        new() { Id = Guid.NewGuid(), Name = "Entertainment", IsArchived = false },
        new() { Id = Guid.NewGuid(), Name = "Utilities", IsArchived = false }
    };
    await context.Categories.AddRangeAsync(categories);
    await context.SaveChangesAsync();
}
```

Definition of Done

- `PUT /api/categories/{id}` endpoint werkend met RowVersion concurrency controle.
- `CategoryService.UpdateAsync` methode geïmplementeerd.
- `UpdateCategoryDto` en `UpdateCategoryDtoValidator` aanwezig.
- Blazor pages: `/categories` (lijst), `/categories/new` (create), `/categories/{id}/edit` (rename) volledig functioneel.
- NavMenu bevat "Categorieën" link.
- 409 Conflict handling in UI toont reload-dialog.
- Unit tests voor validator en service update logica.
- Integration tests voor update met concurrency conflicts.
- E2E tests voor create, edit, archive workflows volledig werkend (niet meer ignored).
- Gebruikers kunnen volledige category lifecycle beheren zonder API-tools.
