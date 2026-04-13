# BugReport-12: TransactionImport Preview Not Updated On Mapping Change

## Status

- [ ] Open

## Summary

When importing transactions, changing the column mapping does not refresh the preview. The preview keeps showing values based on the previous mapping, which can lead to incorrect import verification.

## Environment

- Version: latest
- Scope: transaction import flow
- Frequency: always reproducible

## Steps to Reproduce

1. Open the transaction import page.
2. Upload a CSV file with multiple columns.
3. Configure an initial mapping (for example: Date, Description, Amount).
4. Observe the preview table values.
5. Change one or more mapping selections to different CSV columns.
6. Observe the preview table again.

## Expected Behaviour

After each mapping change, the preview is recalculated and immediately shows values according to the new mapping.

## Actual Behaviour

The preview is not updated after mapping changes and continues to display values from the previous mapping state.

## Workaround

- No reliable in-flow workaround confirmed.
- Users may need to restart the import flow (re-open page or re-upload file) to get a correct preview.

## Impact

- Users cannot trust the preview after changing mappings.
- Increases risk of importing incorrectly mapped transaction data.
- Slows down import workflow and causes confusion.
- Severity: Medium (workaround possible but disruptive).

## Related Reports

- `docs/BugReports/Archive/BugReport-1-Currency-Symbol-Incorrect.md` (same import preview area, but this is not a duplicate)

## Suspected Scope

Likely state update/refresh issue in import mapping and preview rendering flow:

- `LocalFinanceManager/Components/Pages/TransactionImport.razor`
- Related import parsing/mapping services used by the page

Possible cause: mapping state changes do not trigger preview recomputation or component re-render with the updated mapping model.

## Tasks

- [ ] Reproduce locally with a deterministic sample CSV and capture mapping state before/after change
- [ ] Verify event handlers for mapping dropdown changes are invoked consistently
- [ ] Ensure mapping change triggers preview recomputation with current selected mappings
- [ ] Ensure component UI refreshes with recalculated preview rows
- [ ] Add/update regression test for preview refresh after mapping changes
- [ ] Verify no regression in import performance and existing mapping behavior

## Acceptance Criteria

- [ ] Changing a mapping immediately updates preview rows based on the new selection
- [ ] Preview no longer shows stale values from an old mapping state
- [ ] Regression test added/updated and passing
- [ ] Import confirmation uses the same mapping state shown in preview
