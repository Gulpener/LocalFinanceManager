# UserStory-17: Transaction Audit Trail UI

**Als** gebruiker van Local Finance Manager  
**Wil ik** de volledige auditgeschiedenis van een transactie bekijken  
**Zodat ik** kan zien wie de transactie wanneer heeft gewijzigd, inclusief automatische toewijzingen en handmatige aanpassingen

## Business Value

- **Transparantie:** Gebruikers kunnen alle wijzigingen aan een transactie traceren
- **Vertrouwen:** Duidelijk inzicht in automatische vs handmatige toewijzingen
- **Debugging:** Helpdesk/admin kan begrijpen waarom een transactie een bepaalde staat heeft
- **Compliance:** Audittrail voldoet aan financiële administratie-eisen

## Acceptance Criteria

### UI Requirements

- [x] Database heeft `TransactionAudit` tabel met volledige audit data (reeds geïmplementeerd)
- [ ] Nieuwe pagina `/transactions/{id}/audit` toont auditgeschiedenis
- [ ] Transactielijst heeft "Audit Trail" link/icoon per transactie
- [ ] Audit entries tonen in chronologische volgorde (nieuwste eerst)
- [ ] Elke audit entry toont:
  - **Timestamp:** ChangedAt (relatieve tijd + absolute timestamp tooltip)
  - **Actor:** ChangedBy (bijv. "AutoApplyService", "User123", "Manual")
  - **Action Type:** AutoAssign, ManualAssign, Undo, Split, BulkAssign, etc.
  - **Indicator:** "Auto-applied" badge voor `IsAutoApplied=true` entries
  - **Confidence:** Model confidence percentage voor ML-toewijzingen
  - **Model Version:** Getoonde versienummer voor ML-toewijzingen
  - **Changes:** Before/After state diff (optioneel: JSON diff viewer)
  - **Reason:** Tekstuele uitleg (bijv. "Reverted auto-applied assignment")

### Auto-Applied Indicator

- [ ] Entries met `IsAutoApplied=true` tonen prominente badge (bijv. "🤖 Auto-applied")
- [ ] Badge toont confidence score: "Auto-applied (85% confidence)"
- [ ] Model version zichtbaar: "Model v1.2"
- [ ] Verschillend kleuren scheme voor auto vs manual entries

### State Changes

- [ ] Before/After state diff toont specifieke wijzigingen:
  - **Category:** "None → Groceries"
  - **Budget Line:** "Unassigned → Food Budget (€500)"
  - **Split:** "Single → Split (3 parts)"
- [ ] JSON state raw viewer beschikbaar via toggle/expand

### Performance

- [ ] Pagina laadt binnen 200ms voor transacties met <100 audit entries
- [ ] Pagination voor transacties met >50 audit entries (toon 20 per pagina)
- [ ] Lazy loading van Before/After JSON details

### Responsiveness

- [ ] Mobile-friendly layout (stacked kaarten op smalle schermen)
- [ ] Touch-friendly tap targets voor expand/collapse

## Technical Implementation

### API Endpoint

```csharp
// GET /api/transactions/{id}/audit
[HttpGet("{id}/audit")]
public async Task<ActionResult<List<TransactionAuditDto>>> GetTransactionAudit(Guid id)
{
    var audits = await _context.TransactionAudits
        .Where(a => a.TransactionId == id)
        .OrderByDescending(a => a.ChangedAt)
        .Select(a => new TransactionAuditDto
        {
            Id = a.Id,
            ActionType = a.ActionType,
            ChangedBy = a.ChangedBy,
            ChangedAt = a.ChangedAt,
            IsAutoApplied = a.IsAutoApplied,
            Confidence = a.Confidence,
            ModelVersion = a.ModelVersion,
            BeforeState = a.BeforeState,
            AfterState = a.AfterState,
            Reason = a.Reason
        })
        .ToListAsync();

    return Ok(audits);
}
```

### DTO

```csharp
public class TransactionAuditDto
{
    public Guid Id { get; set; }
    public string ActionType { get; set; } = string.Empty;
    public string ChangedBy { get; set; } = string.Empty;
    public DateTime ChangedAt { get; set; }
    public bool IsAutoApplied { get; set; }
    public float? Confidence { get; set; }
    public int? ModelVersion { get; set; }
    public string? BeforeState { get; set; }
    public string? AfterState { get; set; }
    public string? Reason { get; set; }
}
```

### Blazor Page

**Route:** `/transactions/{id}/audit`

**File:** `LocalFinanceManager/Components/Pages/TransactionAudit.razor`

**Components:**

- Timeline layout met verticale lijn
- Badge component voor "Auto-applied" indicator
- Collapsible JSON diff viewer
- Relative timestamp component (bijv. "2 hours ago" met tooltip)

**data-testid attributes:**

- `audit-trail-container`
- `audit-entry` (met index of id)
- `audit-timestamp`
- `audit-actor`
- `audit-action-type`
- `auto-applied-badge`
- `confidence-score`
- `model-version`
- `before-state`
- `after-state`
- `audit-reason`

### Link from Transaction List

Update `Transactions.razor` om "Audit Trail" link toe te voegen:

```razor
<td>
    <a href="/transactions/@transaction.Id/audit"
       class="btn btn-sm btn-outline-secondary"
       data-testid="audit-trail-link">
        <i class="bi bi-clock-history"></i> Audit Trail
    </a>
</td>
```

## Testing Strategy

### Unit Tests (LocalFinanceManager.Tests)

- [ ] `TransactionsController.GetTransactionAudit_ReturnsAuditHistory`
- [ ] `TransactionsController.GetTransactionAudit_OrdersByDateDescending`
- [ ] `TransactionsController.GetTransactionAudit_IncludesAutoAppliedFlag`
- [ ] `TransactionsController.GetTransactionAudit_NotFound_Returns404`

### E2E Tests (LocalFinanceManager.E2E)

- [ ] `TransactionAudit_PageLoads_WithAuditHistory`
- [ ] `TransactionAudit_AutoAppliedBadge_VisibleForMLAssignments`
- [ ] `TransactionAudit_ConfidenceScore_DisplayedCorrectly`
- [ ] `TransactionAudit_Timeline_OrderedChronologically`
- [ ] `TransactionAudit_StateChanges_DisplayDifferences`
- [ ] `TransactionAudit_Link_FromTransactionList_NavigatesToAuditPage`

**Page Object Model:** `TransactionAuditPageModel.cs`

```csharp
public class TransactionAuditPageModel
{
    private readonly IPage Page;

    public async Task NavigateAsync(Guid transactionId);
    public async Task<int> GetAuditEntryCountAsync();
    public async Task<bool> HasAutoAppliedBadgeAsync(int entryIndex);
    public async Task<string> GetConfidenceScoreAsync(int entryIndex);
    public async Task<string> GetActionTypeAsync(int entryIndex);
    public async Task<string> GetActorAsync(int entryIndex);
    public async Task<DateTime> GetTimestampAsync(int entryIndex);
    public async Task ExpandStateChangesAsync(int entryIndex);
    public async Task<string> GetBeforeStateAsync(int entryIndex);
    public async Task<string> GetAfterStateAsync(int entryIndex);
}
```

## Definition of Done

- [ ] `/transactions/{id}/audit` pagina geïmplementeerd met timeline layout
- [ ] API endpoint `/api/transactions/{id}/audit` werkt en retourneert audit data
- [ ] "Audit Trail" link toegevoegd aan transactielijst
- [ ] Auto-applied badge zichtbaar voor ML-toewijzingen met confidence score
- [ ] Before/After state diff wordt correct weergegeven
- [ ] Alle unit tests (4) passed
- [ ] Alle E2E tests (6) passed
- [ ] Mobile-responsive layout getest
- [ ] data-testid attributes toegevoegd voor E2E testing
- [ ] Code review compleet
- [ ] User story gearchiveerd naar `docs/Userstories/Archive/`

## UI Mockup (ASCII)

```
┌─────────────────────────────────────────────────────────┐
│ Transaction Audit Trail                       [← Back]  │
├─────────────────────────────────────────────────────────┤
│ Transaction: "Albert Heijn - Groceries" (-€42.50)      │
│                                                         │
│ ┌─ 2 hours ago ────────────────────────────────────┐   │
│ │ 🤖 Auto-applied (85% confidence) · Model v1.2   │   │
│ │ AutoApplyService                                 │   │
│ │ Action: AutoAssign                               │   │
│ │ Changes: None → Groceries (Food Budget)         │   │
│ │ Reason: Auto-applied by ML model                │   │
│ │ [▼ View State Changes]                           │   │
│ └──────────────────────────────────────────────────┘   │
│                                                         │
│ ┌─ 1 day ago ──────────────────────────────────────┐   │
│ │ 👤 Manual · Admin User                          │   │
│ │ Action: ManualAssign                             │   │
│ │ Changes: Groceries → Transport (Monthly Pass)   │   │
│ │ [▼ View State Changes]                           │   │
│ └──────────────────────────────────────────────────┘   │
│                                                         │
│ ┌─ 3 days ago ─────────────────────────────────────┐   │
│ │ ⚡ System Import                                 │   │
│ │ Action: Import                                   │   │
│ │ Changes: Created transaction                     │   │
│ └──────────────────────────────────────────────────┘   │
│                                                         │
│                            [Load More] (23 more entries)│
└─────────────────────────────────────────────────────────┘
```

## Dependencies

- ✅ `TransactionAudit` model en DbContext setup (reeds geïmplementeerd)
- ✅ `IsAutoApplied`, `Confidence`, `ModelVersion` velden in database
- ⚠️ Transactielijst pagina moet link/icoon toevoegen
- ⚠️ Bootstrap Icons voor timeline/badge UI elementen

## Notes

- Overweeg later uitbreiding met filters (bijv. "Toon alleen auto-applied", "Toon alleen undone")
- Mogelijk export functie voor audit trail (CSV/JSON download)
- Future: Diff viewer met syntax highlighting voor JSON state changes
- Future: Realtime updates via SignalR als audit entries worden toegevoegd terwijl pagina open is
