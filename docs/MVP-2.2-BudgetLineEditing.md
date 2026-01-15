# MVP 2.2 — BudgetLine Inline Editing

Doel

- Gebruikers kunnen bestaande budgetregels inline bewerken zonder archiveren en opnieuw aanmaken.

Acceptatiecriteria

- UI: Edit-modus in `BudgetPlanEdit.razor` met Edit/Save/Cancel knoppen per budgetregel.
- Tabelrij transformeert naar invoervelden bij edit-modus.
- `PUT /api/budgetplans/{planId}/lines/{lineId}` endpoint gebruikt (reeds bestaand).
- 409 Conflict handling met reload-prompt.
- Uniform amount feature beschikbaar in edit-modus.

Data model

- **Geen wijzigingen nodig:** `BudgetLine` model en `UpdateBudgetLineDto` bestaan al met `MonthlyAmounts` (decimal[12] JSON array), `Notes`, `RowVersion`.

Business regels

- Edit-modus is per budgetregel; slechts één regel tegelijk bewerkbaar.
- Wijzigingen worden pas opgeslagen bij expliciet klikken op Save.
- Cancel-knop herstelt originele waarden (discard unsaved changes).
- Uniform amount optie: checkbox "Uniform bedrag" vult alle 12 maanden met zelfde waarde.
- RowVersion concurrency: bij 409 Conflict toont UI dialog met optie om laatste versie te herladen.

API contract & voorbeelden

**Bestaand endpoint (geen wijzigingen nodig):**
- `PUT /api/budgetplans/{planId}/lines/{lineId}`
  - Request body: `UpdateBudgetLineDto { "categoryId":"...", "monthlyAmounts":[...12 waarden...], "notes":"...", "rowVersion":"..." }`
  - Response: `BudgetLineDto` met bijgewerkte data + nieuwe RowVersion
  - Statuscodes: 200 OK, 400 Bad Request, 404 Not Found, 409 Conflict (RowVersion mismatch)

**409 Conflict response voorbeeld:**
```json
{
  "title": "Concurrency conflict",
  "status": 409,
  "currentState": {
    "id": "...",
    "categoryId": "...",
    "monthlyAmounts": [...],
    "rowVersion": "nieuwe-versie"
  }
}
```

UI aanwijzingen (Blazor)

**Updates in Components/Pages/BudgetPlanEdit.razor:**

1. **Tabel structuur:**
   - Standaard modus: toon categorie naam, 12 maand-kolommen (read-only waarden), notes, year total, Actions (Edit, Archive)
   - Edit modus: zelfde rij toont:
     - Category dropdown (alle actieve categorieën)
     - 12 `<input type="number">` velden voor maanden
     - Notes `<input type="text">`
     - Checkbox "Uniform bedrag" met `<input type="number">` (bij aanvinken: vul alle 12 maanden)
     - Actions: Save, Cancel knoppen (geen Edit/Archive)

2. **State management:**
   ```csharp
   private Guid? _editingLineId = null;
   private BudgetLineDto? _editingLineSnapshot = null; // originele waarden voor cancel
   private decimal[] _editMonthlyAmounts = new decimal[12];
   private Guid _editCategoryId;
   private string? _editNotes;
   private bool _useUniformAmount = false;
   private decimal _uniformAmount = 0;
   ```

3. **Edit flow:**
   - Klik "Edit" → Set `_editingLineId`, sla originele waarden op in `_editingLineSnapshot`, laad data in edit velden
   - Wijzig maandwaarden, categorie, notes
   - Uniform amount checkbox:
     - Bij aanvinken: toon extra input field
     - Bij waarde wijzigen: `_editMonthlyAmounts = Enumerable.Repeat(_uniformAmount, 12).ToArray()`
   - Klik "Save" → roep `PUT /api/budgetplans/{planId}/lines/{lineId}` aan
   - Bij 200 OK: reload budget plan data, exit edit mode
   - Bij 409 Conflict: toon dialog "Budgetregel is gewijzigd. Wilt u de laatste versie herladen? (Uw wijzigingen gaan verloren.)" → Reload/Annuleer
   - Klik "Cancel" → herstel `_editingLineSnapshot` waarden, exit edit mode

4. **Concurrency conflict dialog:**
   ```razor
   @if (_showConflictDialog)
   {
       <div class="modal">
           <div class="modal-dialog">
               <div class="modal-content">
                   <div class="modal-header">
                       <h5>Concurrency Conflict</h5>
                   </div>
                   <div class="modal-body">
                       De budgetregel is gewijzigd door een ander proces. 
                       Wilt u de laatste versie herladen? (Uw wijzigingen gaan verloren.)
                   </div>
                   <div class="modal-footer">
                       <button @onclick="ReloadBudgetPlan">Herladen</button>
                       <button @onclick="CloseConflictDialog">Annuleer</button>
                   </div>
               </div>
           </div>
       </div>
   }
   ```

5. **Validation:**
   - Client-side: alle 12 maanden moeten numeric zijn (HTML5 input validation)
   - Server-side: `UpdateBudgetLineDtoValidator` handelt validatie af (reeds bestaand)
   - Category dropdown mag niet leeg zijn

Edgecases

- **Edit tijdens gelijktijdige wijziging:** RowVersion mismatch → 409 → reload prompt.
- **Switch tussen uniform/per-maand:** Uniform amount checkbox uit → behoud huidige maandwaarden; aan → overschrijf alles met uniform waarde (waarschuw gebruiker).
- **Annuleren tijdens edit:** Geen server-call; restore snapshot lokaal.
- **Edit van gearchiveerde categorie:** Dropdown toont alle actieve categorieën; bestaande gearchiveerde categorie blijft zichtbaar in view-mode (legacy data).

Tests

**Unit tests** (`LocalFinanceManager.Tests`):
- `UpdateBudgetLineDtoValidator` test suite (reeds bestaand):
  - MonthlyAmounts array length ≠ 12 → validation error
  - Negative monthly amounts → validation error (optioneel, afhankelijk van business regel)
  - Null RowVersion → validation error
- Uniform amount logic test (UI logic, potentieel in code-behind unit test):
  - `SetUniformAmount(100)` → alle 12 maanden = 100
  - Toggle uniform off → maandwaarden blijven behouden

**Integration tests** (`LocalFinanceManager.Tests` met in-memory SQLite):
- Create budget line → edit monthly amounts → verify persistence
- Edit with stale RowVersion → 409 Conflict response met current state
- Edit category en monthly amounts tegelijk → verify both updated
- Edit archived budget plan line → verify updates allowed (historische wijzigingen)

**E2E tests** (`LocalFinanceManager.E2E` met Playwright):
- **Test:** Edit budget line workflow
  - Create budget plan met één line → Navigate edit page → Click "Edit" op budgetregel → Change month 1 van 100 naar 150 → Click "Save" → Verify updated value persisted (reload page, check value)
- **Test:** Cancel edit workflow
  - Edit budget line → Change values → Click "Cancel" → Verify original values restored (no API call)
- **Test:** Uniform amount feature
  - Edit budget line → Check "Uniform bedrag" → Enter 200 → Verify all 12 months = 200 → Save → Verify persistence
- **Test:** Concurrency conflict handling
  - Open budget line edit in two browser contexts → Update in context A → Save in context B → Verify 409 conflict dialog → Click "Herladen" → Verify latest data loaded
- **Test:** Category change during edit
  - Edit budget line → Change category dropdown → Save → Verify category updated in list

Deployment/env

- Geen nieuwe environment variabelen of configuratie nodig.
- **Automatic migrations:** Geen nieuwe migraties; bestaand schema voldoende.

Definition of Done

- "Edit" knop per budgetregel transformeert rij naar invoervelden.
- Save roept `PUT /api/budgetplans/{planId}/lines/{lineId}` aan en handelt 200/409 responses af.
- Cancel herstelt originele waarden zonder server-call.
- Uniform amount checkbox vult alle 12 maanden met zelfde waarde in edit-modus.
- 409 Conflict toont reload-prompt dialog.
- Unit tests voor validator en uniform amount logic.
- Integration tests voor edit met concurrency conflicts.
- E2E tests voor edit, cancel, uniform amount, concurrency workflows volledig werkend.
- Gebruikers kunnen budgetregels bewerken zonder archiveren/opnieuw aanmaken.
