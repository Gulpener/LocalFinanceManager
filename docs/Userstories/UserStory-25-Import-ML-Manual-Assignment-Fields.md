# UserStory-25: Transaction Type and Notes for ML and Manual Assignment

## Status

- [ ] Not Started

## Description

As a user, I want the import fields `Transaction Type` and `Notes` to be stored and used, so I can assign transactions more effectively by hand and the ML model can provide more accurate suggestions.

## Acceptance Criteria

- [ ] During import, `Transaction Type` and `Notes` are correctly mapped and stored on the transaction
- [ ] For transactions without values in these columns, empty and null values are handled safely without import errors
- [ ] In manual assignment, `Transaction Type` and `Notes` are visible per transaction
- [ ] In manual assignment, users can filter by `Transaction Type` and search in `Notes`
- [ ] ML training uses `Transaction Type` and `Notes` as additional features alongside existing features
- [ ] ML inference uses the same additional features for suggestions on new transactions
- [ ] Existing import, assignment, and suggestion workflows remain backward compatible

## Tasks

- [ ] Extend the `Transaction` model with fields for `Transaction Type` and `Notes`
- [ ] Add an EF Core migration for the new transaction fields
- [ ] Update import mapping in `CsvImportParser` and any JSON parser variants so both fields are populated
- [ ] Add validation and normalization for empty/whitespace values (for example trimming and null-safe storage)
- [ ] Extend `FeatureExtractor` in `LocalFinanceManager.ML` with features based on `Transaction Type` and `Notes`
- [ ] Update ML training and prediction flow so the same feature set is used consistently
- [ ] Show both fields in the manual assignment transaction view
- [ ] Add filters in manual assignment: exact/select filter for `Transaction Type` and text filter for `Notes`
- [ ] Add unit tests for parser mapping and ML feature extraction
- [ ] Add integration tests for import and persistence of both fields
- [ ] Add a UI/component or e2e test that verifies visibility and filtering in manual assignment

## Notes

- Scope is focused on existing import flows and the existing manual assignment flow
- Out of scope: new bank formats, a completely new ML model type, and relabeling historical data outside regular migration
- Privacy consideration: `Notes` may contain personal data; evaluate masking/retention as a separate follow-up story
