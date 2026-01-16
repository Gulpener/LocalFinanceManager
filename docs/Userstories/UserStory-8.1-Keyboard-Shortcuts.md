# UserStory-8.1: Keyboard Shortcuts & Accessibility

## Objective

Implement comprehensive keyboard navigation shortcuts for transaction assignment workflows to enhance accessibility and support power users. Provide keyboard-driven alternatives for all modal interactions, transaction selection, and help documentation. Ensure WCAG 2.1 Level AA compliance with focus management, logical tab order, and screen reader compatibility.

## Requirements

- Implement keyboard navigation shortcuts (Tab, Enter, Esc, Space, Ctrl+A, Ctrl+D, /) for assignment workflows
- Create keyboard shortcut documentation modal (triggered by `?` key)
- Add keyboard event handlers to all assignment modals (TransactionAssignModal, SplitEditor, BulkAssignModal)
- Implement transaction list keyboard navigation (arrow keys, space for checkbox toggle)
- Ensure WCAG 2.1 Level AA compliance (focus indicators, logical tab order, keyboard trap prevention)
- Support mobile/touch devices (disable keyboard shortcuts, show touch-optimized help)
- Pass Playwright AxeCore accessibility scans (0 critical/serious violations)

## Prerequisites (Hard Dependencies)

This user story **CANNOT** start until all of the following are complete:

### Required Components from US-5 (Basic Assignment UI):

- ✅ `CategorySelector.razor` exists and is functional
- ✅ `TransactionAssignModal.razor` exists and is functional
- ✅ `Transactions.razor` has assignment UI (assign buttons, status column)
- ✅ All US-5 E2E tests passing

### Required Components from US-6 (Split/Bulk Assignment):

- ✅ `SplitEditor.razor` exists and is functional
- ✅ `BulkAssignModal.razor` exists and is functional
- ✅ Checkbox selection in transaction list implemented
- ✅ All US-6 E2E tests passing

### Required Components from US-7 (ML Suggestions):

- ✅ `MLSuggestionBadge.razor` exists and is functional
- ✅ ML suggestion integration in transaction list
- ✅ All US-7 E2E tests passing

### Required Infrastructure from US-5.1 (E2E Infrastructure):

- ✅ `PlaywrightFixture` base class ready
- ✅ `SeedDataHelper` utility available
- ✅ PageObjectModels infrastructure complete

**Verification Checklist:** Before starting implementation, manually verify all checkboxes above. If any component is missing or tests are failing, **DO NOT PROCEED** with this user story.

## Pattern Adherence

This story **enhances** existing assignment components without modifying their core logic:

### Components Enhanced

- `TransactionAssignModal.razor` from US-5 - Add keyboard event handlers (Tab, Enter, Esc)
- `SplitEditor.razor` from US-6 - Add keyboard navigation through split rows
- `BulkAssignModal.razor` from US-6 - Add keyboard shortcuts for bulk operations
- `Transactions.razor` from US-5/6/7 - Add keyboard selection, arrow navigation, focus management

### New Components Created

- `ShortcutHelp.razor` - Keyboard shortcut documentation modal
- `KeyboardService.cs` (optional) - Centralized keyboard event handling and device detection

### UX Patterns

- Keyboard shortcuts follow industry standards (Tab navigation, Enter submit, Esc cancel)
- Focus indicators visible and WCAG compliant (2px outline, high contrast)
- Keyboard traps prevented (focus cycles properly in modals)
- Mobile detection disables keyboard shortcuts on touch devices
- Help modal accessible via `?` key with dismissible overlay

## Implementation Tasks

### 1. Keyboard Shortcut Help Modal

- [ ] Create `ShortcutHelp.razor` component in `Components/Shared/`
- [ ] Add modal with keyboard shortcut documentation table:
  - **Tab:** Navigate between form fields in modals
  - **Enter:** Submit assignment/split/bulk modal (when save button focused)
  - **Esc:** Close modal without saving
  - **Space:** Toggle transaction checkbox selection
  - **Ctrl+A / Cmd+A:** Select all visible transactions
  - **Ctrl+D / Cmd+D:** Deselect all transactions
  - **/:** Focus search/filter input
  - **?:** Show this help modal
  - **Arrow Up/Down:** Navigate between transaction rows
  - **Enter (on row):** Open assignment modal for focused row
- [ ] Add mobile/touch device section:
  - Detect touch device: `navigator.maxTouchPoints > 0` via JSInterop
  - Show touch gestures instead of keyboard shortcuts on mobile
  - Touch gestures: Tap (select), Swipe (dismiss modal), Long press (context menu)
- [ ] Add global keyboard event listener in `App.razor` or `MainLayout.razor`
- [ ] Implement `?` key handler to show help modal (prevent default browser behavior)
- [ ] Add `Esc` key handler to close help modal
- [ ] Style help modal with responsive design (mobile-friendly, scrollable)
- [ ] Add "Print" button to help modal (print-friendly CSS)

### 2. Device Detection Service (Optional)

- [ ] Create `DeviceDetectionService.cs` in `Services/`
- [ ] Implement `IsTouchDeviceAsync()` method:
  - Use IJSRuntime to check `navigator.maxTouchPoints > 0`
  - Cache result for session (avoid repeated JSInterop calls)
- [ ] Implement `GetOperatingSystemAsync()` method:
  - Detect Windows/Mac/Linux via user agent
  - Return OS enum for conditional shortcut display (Ctrl vs Cmd)
- [ ] Register service as scoped in `Program.cs`

### 3. TransactionAssignModal Keyboard Navigation

- [ ] Update `TransactionAssignModal.razor` to handle keyboard events:
  - Add `@onkeydown` event handler on modal container
  - Implement Tab navigation (cycle through category → budget line → note → save → cancel)
  - Prevent Tab from escaping modal (keyboard trap prevention with focus wrap)
  - Implement Enter to submit when save button focused (check `event.target`)
  - Implement Esc to close modal without saving (call CloseModal method)
- [ ] Add visual focus indicators:
  - CSS class `.focus-visible:focus` with 2px solid outline
  - High contrast color (e.g., `#0078d4` or `#005a9e`)
  - Ensure focus indicator visible on all interactive elements
- [ ] Ensure keyboard shortcuts don't conflict with browser defaults:
  - Use `event.preventDefault()` for custom shortcuts
  - Allow browser shortcuts (Ctrl+C, Ctrl+V) to work normally
- [ ] Add focus management on modal open:
  - Set initial focus to category selector
  - Announce modal to screen readers (`role="dialog"`, `aria-modal="true"`)

### 4. SplitEditor Keyboard Navigation

- [ ] Update `SplitEditor.razor` to handle keyboard events:
  - Add `@onkeydown` event handler on editor container
  - Implement Tab navigation through split rows (cycle through categories, amounts)
  - Implement Enter to submit when save button focused
  - Implement Esc to close editor without saving
- [ ] Add keyboard shortcuts for split row management:
  - Ctrl+Plus (+) to add new split row
  - Ctrl+Minus (-) to remove focused split row
  - Tab through category → amount → add/remove buttons
- [ ] Add visual focus indicators on split rows
- [ ] Ensure focus returns to trigger element when editor closes

### 5. BulkAssignModal Keyboard Navigation

- [ ] Update `BulkAssignModal.razor` to handle keyboard events:
  - Add `@onkeydown` event handler on modal container
  - Implement Tab navigation through category selector → assign button → cancel
  - Implement Enter to start bulk assignment
  - Implement Esc to close modal without saving
- [ ] Add progress indicator keyboard accessibility:
  - Announce progress updates to screen readers (`aria-live="polite"`)
  - Allow Esc to cancel in-progress bulk operation
- [ ] Add visual focus indicators on modal elements

### 6. Transaction List Keyboard Shortcuts

- [ ] Update `Transactions.razor` to handle keyboard events:
  - Add `@onkeydown` event handler on transaction table
  - Implement Space key to toggle transaction checkbox when row focused
  - Implement Ctrl+A to select all visible transactions (prevent browser default)
  - Implement Ctrl+D to deselect all transactions
  - Implement `/` key to focus search/filter input (prevent browser default)
- [ ] Add keyboard focus management:
  - Arrow Up/Down keys to navigate between transaction rows
  - Enter key to open assignment modal for focused row
  - Home key to focus first row, End key to focus last row
- [ ] Add visual focus indicator for focused transaction row:
  - CSS class `.transaction-row:focus` with background highlight
  - Ensure focus indicator contrasts with row background
- [ ] Ensure keyboard navigation works with pagination:
  - Focus resets to first row on page change
  - Announce page change to screen readers ("Page 2 of 10 loaded")

### 7. Global Keyboard Event Coordination

- [ ] Update `App.razor` or `MainLayout.razor` with global keyboard listener:
  - Listen for `?` key to show ShortcutHelp modal
  - Check if any modal is open before handling global shortcuts
  - Prevent global shortcuts when input fields focused (e.g., don't trigger `/` when typing in text field)
- [ ] Add keyboard event priority system:
  - Modal-level shortcuts take precedence over global shortcuts
  - Input field shortcuts (Ctrl+A in text field) override component shortcuts
- [ ] Disable keyboard shortcuts on mobile/touch devices:
  - Check `DeviceDetectionService.IsTouchDeviceAsync()` on app init
  - Conditionally register keyboard listeners only on non-touch devices

### 8. Focus Management System

- [ ] Create `FocusManagementService.cs` in `Services/` (optional):
  - Store reference to last focused element before modal opens
  - Implement `RestoreFocusAsync()` to return focus after modal closes
  - Implement `TrapFocusAsync(elementRef)` to prevent Tab from escaping modals
- [ ] Ensure modals trap focus properly:
  - Add `tabindex="-1"` on modal container for initial focus
  - Add `@onfocusout` handler to detect Tab wrap-around
  - Cycle focus back to first focusable element when Tab reaches end
- [ ] Ensure focus visible when navigating with keyboard:
  - Add `:focus-visible` CSS styles (not just `:focus` which shows on click)
  - Use CSS `:focus-visible { outline: 2px solid #0078d4; }`

### 9. WCAG 2.1 Level AA Compliance

- [ ] Verify focus indicators meet contrast ratio requirements:
  - Focus outline must have 3:1 contrast ratio with background (WCAG 2.1 AA)
  - Use browser DevTools contrast checker to verify
- [ ] Verify logical tab order:
  - Tab order follows visual order (left to right, top to bottom)
  - Skip navigation links not needed (Blazor Server SPA)
- [ ] Verify keyboard trap prevention:
  - Focus cycles properly in modals (Tab from last element returns to first)
  - Esc key always closes modals (no keyboard traps)
- [ ] Verify screen reader announcements:
  - Modals have `role="dialog"` and `aria-modal="true"`
  - Form fields have proper `<label>` elements or `aria-label`
  - Error messages associated with fields via `aria-describedby`
  - Dynamic content changes announced via `aria-live="polite"`

### 10. Testing - Unit Tests

- [ ] Create `ShortcutHelpTests.cs` in `LocalFinanceManager.Tests/Components/`
- [ ] Test: Help modal displays all keyboard shortcuts
  - Render ShortcutHelp component
  - Assert all 10 shortcuts displayed in table
- [ ] Test: Help modal closes when Esc pressed
  - Render modal, trigger Esc key event
  - Assert modal closed (OnClose callback invoked)
- [ ] Test: Mobile section shown on touch devices
  - Mock DeviceDetectionService.IsTouchDeviceAsync() to return true
  - Assert touch gestures section visible, keyboard shortcuts section hidden

### 11. Testing - E2E Tests (Keyboard Navigation)

> **Note:** Requires US-5.1 E2E infrastructure (PlaywrightFixture, PageObjectModels).

- [ ] Create `KeyboardNavigationTests.cs` in `LocalFinanceManager.E2E/Tests/`
- [ ] Test: Press `Tab` in assignment modal → Focus moves through fields
  - Seed transaction, open assignment modal
  - Press Tab repeatedly
  - Assert focus moves: category → budget line → note → save → cancel → wraps to category
- [ ] Test: Press `Enter` when save button focused → Modal submits
  - Open assignment modal, Tab to save button
  - Press Enter
  - Assert assignment saved, modal closed
- [ ] Test: Press `Esc` in modal → Modal closes without saving
  - Open assignment modal, press Esc
  - Assert modal closed, no assignment saved
- [ ] Test: Press `Space` on transaction row → Checkbox toggles
  - Navigate to transactions page, focus first row
  - Press Space
  - Assert checkbox checked
- [ ] Test: Press `Ctrl+A` → All visible transactions selected
  - Load 10 transactions, press Ctrl+A
  - Assert all 10 checkboxes checked
- [ ] Test: Press `/` → Search input focused
  - Navigate to transactions page, press `/`
  - Assert search input has focus (document.activeElement)
- [ ] Test: Press `?` → Help modal opens
  - Navigate to any page, press `?`
  - Assert ShortcutHelp modal visible

### 12. Testing - E2E Tests (Accessibility Validation)

> **Note:** Uses `Deque.AxeCore.Playwright` for automated accessibility scanning.

- [ ] Install `Deque.AxeCore.Playwright` NuGet package in `LocalFinanceManager.E2E`
- [ ] Create `AccessibilityTests.cs` test class in `LocalFinanceManager.E2E/Tests/`
- [ ] Test: Run axe-core on Transactions page → Zero critical violations
  - Navigate to transactions page
  - Run `await page.RunAxe()` scan
  - Assert zero critical or serious accessibility violations
- [ ] Test: Run axe-core on assignment modal → Zero critical violations
  - Open assignment modal
  - Run axe-core scan
  - Assert zero critical or serious violations
- [ ] Test: Run axe-core on ShortcutHelp modal → Zero critical violations
  - Open help modal (press `?`)
  - Run axe-core scan
  - Assert zero critical or serious violations
- [ ] Test: Tab through assignment modal → Focus order logical
  - Open assignment modal
  - Press Tab 5 times, record focus sequence
  - Assert focus order matches expected (category → budget line → note → save → cancel)
- [ ] Test: Focus indicators visible (manual verification)
  - Open modal, Tab through elements
  - Manually verify focus outlines visible (2px, high contrast)
  - Document in test results (automated detection difficult)

### 13. Documentation

- [ ] Update `README.md` with keyboard shortcuts section:
  - List all 10 keyboard shortcuts with descriptions
  - Note mobile/touch device behavior (shortcuts disabled)
- [ ] Create `docs/ACCESSIBILITY.md`:
  - Document WCAG 2.1 Level AA compliance
  - List keyboard shortcuts and their behavior
  - Describe focus management system
  - Include screen reader testing notes
  - List tested assistive technologies (NVDA, JAWS, VoiceOver)
- [ ] Add inline help tooltips in UI:
  - Tooltip on "?" icon: "Press ? to view keyboard shortcuts"
  - Tooltip on close button in modal: "Press Esc to close"

## Testing

### Keyboard Shortcut Test Scenarios

1. **Global Shortcuts:**

   - Press `?` → Help modal opens
   - Press `/` → Search input focused (if on Transactions page)
   - Press `Esc` in help modal → Help modal closes

2. **Assignment Modal Shortcuts:**

   - Press `Tab` → Focus cycles through category → budget line → note → save → cancel
   - Press `Enter` when save button focused → Assignment saved, modal closed
   - Press `Esc` → Modal closes without saving
   - Open modal → Focus starts on category selector

3. **Split Editor Shortcuts:**

   - Press `Tab` → Focus cycles through split row fields
   - Press `Ctrl+Plus` → New split row added
   - Press `Ctrl+Minus` → Focused split row removed
   - Press `Enter` when save button focused → Split saved, editor closed
   - Press `Esc` → Editor closes without saving

4. **Bulk Assign Modal Shortcuts:**

   - Press `Tab` → Focus cycles through category → assign button → cancel
   - Press `Enter` when assign button focused → Bulk assignment starts
   - Press `Esc` during assignment → Bulk operation cancelled
   - Press `Esc` after completion → Modal closes

5. **Transaction List Shortcuts:**

   - Press `Space` on focused row → Checkbox toggles
   - Press `Ctrl+A` → All visible transactions selected
   - Press `Ctrl+D` → All transactions deselected
   - Press `Arrow Down` → Focus moves to next row
   - Press `Arrow Up` → Focus moves to previous row
   - Press `Enter` on focused row → Assignment modal opens
   - Press `Home` → Focus moves to first row
   - Press `End` → Focus moves to last row

6. **Focus Management:**

   - Open modal → Focus trapped inside modal (Tab doesn't escape)
   - Close modal → Focus returns to trigger element
   - Navigate between pages → Focus persists correctly

7. **Mobile/Touch Behavior:**
   - Detect touch device → Keyboard shortcuts disabled
   - Press `?` on mobile → Help modal shows touch gestures instead of keyboard shortcuts

## Success Criteria

- ✅ Keyboard shortcuts implemented for 10+ actions (Tab, Enter, Esc, Space, Ctrl+A, Ctrl+D, /, ?, Arrow keys)
- ✅ `ShortcutHelp.razor` component displays keyboard shortcut documentation
- ✅ All assignment modals support keyboard navigation (TransactionAssignModal, SplitEditor, BulkAssignModal)
- ✅ Transaction list supports keyboard selection and navigation
- ✅ WCAG 2.1 Level AA compliance verified:
  - ✅ Focus indicators visible (2px outline, 3:1 contrast ratio)
  - ✅ Logical tab order (follows visual order)
  - ✅ Keyboard trap prevention (Esc closes modals, focus cycles properly)
  - ✅ Screen reader support (proper ARIA labels, roles, announcements)
- ✅ Playwright AxeCore scans pass (0 critical/serious violations on Transactions page, modals)
- ✅ Mobile/touch detection working (keyboard shortcuts disabled on touch devices)
- ✅ E2E tests cover all keyboard shortcuts (7 navigation tests + 5 accessibility tests)
- ✅ Unit tests cover ShortcutHelp component (3 tests)
- ✅ Documentation complete (`ACCESSIBILITY.md`, README keyboard shortcuts section)

## Definition of Done

- Keyboard shortcuts implemented in all assignment modals and transaction list (10+ shortcuts)
- `ShortcutHelp.razor` component displays keyboard shortcut documentation (desktop and mobile versions)
- Focus indicators meet WCAG 2.1 Level AA standards (2px outline, 3:1 contrast ratio)
- Focus management prevents keyboard traps (Tab cycles in modals, Esc always closes)
- Screen reader support complete (ARIA roles, labels, live regions)
- E2E tests pass for keyboard navigation (7 tests in `KeyboardNavigationTests.cs`)
- E2E tests pass for accessibility (5 tests in `AccessibilityTests.cs` with AxeCore)
- Unit tests pass for ShortcutHelp component (3 tests)
- Mobile/touch device detection working (keyboard shortcuts disabled on touch devices)
- Documentation complete (`docs/ACCESSIBILITY.md`, README keyboard shortcuts section)
- Code follows Implementation-Guidelines.md (.NET 10.0, async/await, IJSRuntime for device detection)
- All prerequisite components from US-5/6/7 verified before starting

## Dependencies

- **UserStory-5 (Basic Assignment UI):** REQUIRED - Enhances CategorySelector, TransactionAssignModal, Transactions.razor components.
- **UserStory-6 (Split/Bulk Assignment):** REQUIRED - Adds keyboard shortcuts to SplitEditor, BulkAssignModal components.
- **UserStory-7 (ML Suggestions):** REQUIRED - Keyboard shortcuts work with ML suggestion badges and filters.
- **UserStory-5.1 (E2E Infrastructure):** REQUIRED for E2E tests - Must complete US-5.1 before running E2E tests in this story. Uses PlaywrightFixture and PageObjectModels.

## Estimated Effort

**1.5-2 days** (~15 implementation tasks + 12 test tasks = 27 total tasks)

## Roadmap Timing

**Phase 4** (Weeks 9-10, after US-7 complete)

## Notes

- **Keyboard shortcuts follow industry standards:** Tab (navigate), Enter (submit), Esc (cancel) are universal patterns recognized by all users.
- **Fixed shortcuts (non-configurable):** Simplifies implementation and prevents user confusion. Future enhancement could add customization.
- **WCAG 2.1 Level AA compliance:** Ensures accessibility for keyboard-only users, screen reader users, and users with motor disabilities.
- **Focus indicators critical:** Many users rely on visible focus outlines to know where keyboard focus is. 2px solid outline with high contrast meets WCAG standards.
- **Keyboard trap prevention:** Users must always be able to Esc out of modals and Tab through elements without getting stuck.
- **Mobile/touch detection:** `navigator.maxTouchPoints > 0` reliably detects touch devices. Disabling keyboard shortcuts on mobile prevents confusion.
- **Screen reader support:** Proper ARIA roles (`role="dialog"`, `aria-modal="true"`) and labels ensure screen readers announce UI changes correctly.
- **AxeCore automated testing:** Catches ~57% of WCAG violations automatically. Manual testing still needed for keyboard-only navigation and screen reader testing.
- **Focus management on modal close:** Returning focus to trigger element (e.g., "Assign" button) provides seamless keyboard navigation experience.
- **Browser compatibility:** All keyboard shortcuts tested in Chrome, Firefox, Edge, Safari. Ctrl/Cmd key detection handles Mac vs Windows differences.
- **Future enhancements:** Consider adding configurable shortcuts, command palette (Ctrl+K), or keyboard recording feature for user customization.
