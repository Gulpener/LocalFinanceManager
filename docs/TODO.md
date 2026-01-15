# TODO Backlog — MVP-gebaseerde roadmap

Laatste update: 2026-01-14

## MVP 0 — Infrastructure Setup (Foundation) [✓ COMPLETED]

See detailed spec in `docs/MVP-0-Infrastructure.md`. This phase must be completed before starting any feature MVP (MVP 1-6).

- [x] Run complete solution scaffolding via CLI (dotnet new sln, all 5 projects, packages)
- [x] Create `BaseEntity` abstract class (Guid Id, byte[] RowVersion, DateTime CreatedAt/UpdatedAt)
- [x] Setup `AppDbContext` with automatic migration on startup via `Database.MigrateAsync()`
- [x] Configure EF Core `RowVersion` property with `.IsRowVersion()` on BaseEntity
- [x] Implement `IRepository<T>` generic pattern with `.Where(x => !x.IsArchived)` soft-delete filtering
- [x] Implement `DbUpdateConcurrencyException` handler in repositories (last-write-wins reload, return HTTP 409 Conflict)
- [x] Setup `AppDbContext.SeedAsync()` method for Development-only seeding (check existing data to prevent duplicates)
- [x] Create `TestDbContextFactory` for in-memory SQLite (`:memory:`) in `LocalFinanceManager.Tests`
- [x] Create `PlaywrightFixture` base class for E2E tests with `WebApplicationFactory` + test SQLite database
- [x] Create `TestDataBuilder` for shared seed data (MLModel for testing)
- [x] Create `LocalFinanceManager.ML` class library with ML.NET package references
- [x] Create `LocalFinanceManager.ML.Tests` project with fixture models directory structure
- [x] Create `MLModel` entity for database storage (byte[] ModelBytes, int Version, DateTime TrainedAt, string Metrics JSON)
- [x] Verify all projects compile without errors; folder structure complete
- [x] All infrastructure tests passing (10 unit tests, 2 E2E smoke tests)
- [ ] Create `MLModel` entity for database storage (byte[] ModelBytes, int Version, DateTime TrainedAt, string Metrics JSON)
- [ ] Verify all projects compile without errors; folder structure complete

## MVP 1 — Accounts (CRUD) [✓ COMPLETED]

**Note:** MVP-0 (Infrastructure Setup) must be completed first. These tasks implement Account-specific functionality.

- [x] Entity: Account (extends BaseEntity, Label, Type enum, Currency ISO-4217, IBAN, StartingBalance, IsArchived, RowVersion)
- [x] Create `IAccountRepository` with soft-delete filtering encapsulation
- [x] IBAN validation via IbanNet NuGet in FluentValidation rules
- [x] Account CRUD API endpoints: GET /accounts, GET /accounts/{id}, POST, PUT, DELETE (archive)
- [x] Blazor pages: /accounts (list), /accounts/new, /accounts/{id}/edit
- [x] Concurrency: Handle RowVersion mismatch → return 409 Conflict with current entity state
- [x] Unit tests: Account CRUD, IBAN validation, RowVersion conflict handling (MockAccountRepository)
- [x] Integration tests: DbContext migrations, Account persistence, archive filtering (in-memory SQLite)
- [x] E2E tests: Account create/edit/archive/list workflows via Playwright (skeleton created, marked for manual testing)
- [x] Seed data: 3 sample accounts (EUR Checking, USD Savings, EUR Credit) in Development environment
- [x] Definition of Done: All test projects populated, RowVersion working, archived accounts filtered correctly

## MVP 2 — Budgetplan per account (jaarlijks) [✓ COMPLETED]

- [x] Entity: BudgetPlan (extends BaseEntity, AccountId FK, Year int, Name, RowVersion)
- [x] Entity: BudgetLine (extends BaseEntity, BudgetPlanId FK, CategoryId, MonthlyAmounts decimal[12] as JSON, RowVersion)
- [x] Create `IBudgetPlanRepository` and `IBudgetLineRepository` with soft-delete filtering
- [x] BudgetPlan CRUD API: GET /accounts/{id}/budgetplans, POST, PUT, DELETE
- [x] BudgetLine CRUD API: POST /budgetplans/{id}/lines, PUT, DELETE
- [x] Aggregation: Sum MonthlyAmounts JSON array → YearTotal computed property
- [x] Blazor UI: Budget editor table (rows = categories, columns = months Jan-Dec + Year-total)
- [x] Bulk uniform value assignment: Copy single month value to all 12 months
- [x] Concurrency: RowVersion on budget edits → 409 Conflict + reload prompts
- [x] Unit tests: MonthlyAmounts JSON storage/retrieval, aggregation calculations, RowVersion conflict
- [x] Integration tests: End-to-end plan + lines creation, monthly aggregation queries
- [x] E2E tests: Budget editor UI, per-month entry, bulk uniform assignment, persistence
- [x] Seed data: Sample budget plan for test account with 5 budget lines + monthly allocations
- [x] Definition of Done: JSON storage working, aggregations accurate, RowVersion enforced

## MVP 3 — Import transacties [High Priority]

- [ ] Entity: Transaction (extends BaseEntity, Amount, Date, Description, Counterparty, OriginalImport string, ImportBatchId, SourceFileName, ImportedAt, RowVersion)
- [ ] Entity: Category (Id, Name, IsArchived) - foundational for transaction categorization
- [ ] CSV parser with configurable column mapping (auto-detect Date/Amount heuristics)
- [ ] JSON import support (array of objects)
- [ ] Deduplication: Exact match (Date + Amount + ExternalId) strategy
- [ ] Deduplication: Fuzzy match (description similarity + counterparty + amount) with configurable threshold
- [ ] Mapping configuration UI: allow user to map CSV columns to Transaction fields
- [ ] Import preview UI: show parsed transactions, column mapping, errors per row
- [ ] Partial import option: skip errors vs. all-or-nothing mode
- [ ] **Important:** Import bypass RowVersion checks (batch non-interactive operation); RowVersion applied only to post-import manual edits
- [ ] Store original CSV/JSON import string in `OriginalImport` field for audit
- [ ] Batch processing with error reporting per row (line number + error detail)
- [ ] Unit tests: CSV/JSON parsing edge cases (escaped commas, quotes, date formats)
- [ ] Unit tests: Deduplication exact + fuzzy matching logic, collision edge cases
- [ ] Integration tests: End-to-end import flow with deduplication against existing transactions
- [ ] E2E tests: Upload → preview mapping → import workflow, transaction list verification
- [ ] Definition of Done: Import succeeds with audit trail, deduplication accurate, RowVersion bypass documented

## MVP 4 — Koppel transacties aan budgetcategorieën [Medium Priority]

- [ ] Entity: TransactionSplit (extends BaseEntity, TransactionId FK, BudgetLineId FK, Amount, Note, RowVersion)
- [ ] Update Transaction: Add optional AssignedParts collection (null/empty = unsplit)
- [ ] Single assign API: POST /transactions/{id}/assign body: { budgetLineId }
- [ ] Bulk assign API: POST /transactions/bulk-assign body: { transactionIds[], budgetLineId }
- [ ] Split API: POST /transactions/{id}/split body: { splits: [{budgetLineId, amount}, ...] }
- [ ] Rounding tolerance configuration: validate Sum(splits) ≤ Amount + tolerance
- [ ] Computed property: Transaction.EffectiveAmount (sum of splits or base amount)
- [ ] Computed property: Transaction.IsSplit (true if splits exist)
- [ ] Concurrency: RowVersion enforced on all transaction edits → 409 Conflict handling
- [ ] Audit trail: ChangedBy, ChangedAt, before/after values for all assignments
- [ ] Undo functionality: revert last N assignment actions per transaction
- [ ] Unit tests: Split sum validation, rounding tolerance, RowVersion conflict detection
- [ ] Integration tests: Assign/split workflows with RowVersion mismatch → 409 Conflict verification, atomic bulk operations
- [ ] E2E tests: Assign UI workflow, split editor (add/remove rows, validation), bulk assign preview, undo
- [ ] Definition of Done: Splits sum validation working, RowVersion enforced, audit trail complete

## MVP 5 — Leerfunctie (categorisatie) [Medium Priority]

- [ ] Create `LocalFinanceManager.ML.IMLService` interface in main project, implementation in ML library
- [ ] Feature extraction: tokenize description, counterparty, amount binning, temporal patterns (weekday/month)
- [ ] Rule-based engine: simple scoring based on keywords, counterparty patterns, amount ranges
- [ ] ML.NET model training: logistic regression or decision tree on labeled examples
- [ ] Feature importance extraction: return top contributing features for each suggestion
- [ ] Suggestion API: GET /suggestions?transactionId → { categoryId, confidence, explanation[], topFeatures[] }
- [ ] User feedback loop: track accept/override actions as labeled training data in audit trail
- [ ] Store labeled examples: persist user corrections linking suggestion → final label → user
- [ ] Offline retraining job: configurable trigger (daily/weekly/manual) trains new model on labeled data
- [ ] MLModel entity storage: serialize trained model as byte[] + metadata (version, createdAt, metrics JSON)
- [ ] Model versioning: support multiple versions in database without filesystem dependencies
- [ ] Fixture models: pre-trained `.bin` files committed to `LocalFinanceManager.ML.Tests/fixtures/`
- [ ] Fixture model strategy: use for fast <100ms test startup; separate CI job retrains monthly
- [ ] Metrics tracking: precision, recall, F1 score, acceptance rate per category
- [ ] Minimum labeled examples threshold: enforce 10+ per category before auto-assignment consideration (MVP-6)
- [ ] Basic metrics dashboard: show precision, recall, acceptance rate, labeled examples count
- [ ] Unit tests: feature extraction (tokenization, binning, patterns), rule scoring logic
- [ ] ML tests: model training, evaluation on holdout set, metric validation, feature importance, serialization/deserialization
- [ ] Integration tests: end-to-end suggestion flow, labeled example storage, retraining trigger
- [ ] E2E tests: suggestion display UI, accept/override workflows, confidence score visibility, top features explanation
- [ ] Definition of Done: Suggestion API working, ML.NET model trained, feedback loop captured, metrics dashboard basic

## MVP 6 — Automatisering bij voldoende zekerheid [Medium Priority]

- [ ] `IHostedService` configured in `Program.cs` for background ML retraining (configurable schedule, e.g., weekly)
- [ ] Retraining job calls `IMLService.RetrainAsync()` on latest labeled data
- [ ] **Threshold-based model approval:** New model must exceed metric threshold (e.g., F1 > 0.85) before active model swap
- [ ] Rejected improvements logged to audit trail with reason (failed threshold check)
- [ ] Auto-apply worker: processes suggestion batches at configurable confidence threshold
- [ ] Auto-apply idempotency: safe to retry without duplicate assignments
- [ ] Exponential backoff retry logic: handle transient failures gracefully
- [ ] Auto-apply audit trail: store AutoAppliedBy, AutoAppliedAt, Confidence, ModelVersion per assignment
- [ ] Undo functionality: user can revert auto-applied assignment within retention window (configurable, e.g., 30 days)
- [ ] Undo UI: display undo option with timestamp, reason, and revert action
- [ ] Feature flags: gradual rollout control (percent of transactions or specific accounts/users)
- [ ] Feature flags: can disable auto-apply without redeployment
- [ ] **Safety gates:** Auto-apply only after retraining completes with threshold approval
- [ ] Monitoring dashboard: job failure count, auto-apply rate, undo rate, confidence drift alerts
- [ ] Undo rate monitoring: alert if >20% of auto-applies reversed (quality warning)
- [ ] Confidence drift detection: warn if average suggestion confidence drops unexpectedly
- [ ] Unit tests: job idempotency, retry logic, threshold approval logic, undo atomicity
- [ ] Integration tests: end-to-end retraining → approval → auto-apply → audit → undo verification
- [ ] E2E tests: auto-apply workflow, undo UI, feature flag toggles, monitoring dashboard validation
- [ ] Load tests (optional): job throughput with large transaction batches, idempotency under retries
- [ ] Definition of Done: IHostedService working, threshold approval enforced, undo functional, monitoring operational

## Post-MVP / Nice-to-have [Low Priority]

- [ ] MT940 bank statement parser
- [ ] Encrypted SQLite support (AES-256 key management)
- [ ] Backup/restore functionality + UI
- [ ] Charts & dashboards (spending trends, category breakdowns)
- [ ] CI/CD templates (GitHub Actions for testing, fixture model retraining)
- [ ] Advanced undo UI (merge/diff for conflicting changes, not just reload)
- [ ] Multi-user support + authentication (ASP.NET Core Identity)
- [ ] API documentation improvements (OpenAPI/Swagger enhancements)
