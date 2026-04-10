# UserStory-23: Budget Line Ordering

## Status

- [ ] Not Started

## Description

As a user managing a budget plan, I want to reorder budget lines so that I can organize them in a meaningful order (e.g., by priority or category group).

## Acceptance Criteria

- [ ] Budget lines are displayed in user-defined order within a budget plan
- [ ] User can move a budget line up or down via arrow buttons in the UI
- [ ] Order is persisted in the database
- [ ] New budget lines are appended at the bottom by default
- [ ] Ordering is preserved across page reloads

## Tasks

- [ ] Add `int SortOrder` property to `BudgetLine` model
- [ ] Create EF Core migration for the new `SortOrder` column
- [ ] Update `BudgetLineDto`, `CreateBudgetLineDto`, and `UpdateBudgetLineDto` to include `SortOrder`
- [ ] Add `PATCH /api/budgetplans/lines/{id}/order` endpoint (body: `{ "direction": "up" | "down" }`)
- [ ] Update `BudgetPlanService.CreateLineAsync` to assign `SortOrder = max existing + 1`
- [ ] Implement swap logic in `BudgetPlanService` for reorder (swap `SortOrder` values with adjacent line)
- [ ] Update `BudgetPlanEdit.razor` to sort lines by `SortOrder` and render up/down arrow buttons per row
- [ ] Disable the "up" button on the first line and the "down" button on the last line
- [ ] Add unit tests for sort order assignment and swap logic in `BudgetPlanServiceTests`
- [ ] Add integration tests for the reorder endpoint in `BudgetPlansControllerTests`

## Notes

- Lines are sorted ascending by `SortOrder`; ties fall back to `CreatedAt` ascending
- The reorder endpoint swaps `SortOrder` values between the target line and its adjacent neighbour
- Up/down buttons are sufficient scope; drag-and-drop can be added in a future story
