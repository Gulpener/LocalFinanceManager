# MVP 5 — Leerfunctie (categorisatie)

Doel

- Een leerbare engine (rule-based + ML-hybride) die transacties automatisch categoriseert en verbetert op basis van user-correcties.

Scope & acceptatiecriteria

- Basis prototype met feature-extraction en een eenvoudig model (logistic regression / decision tree) of rule-scoring.
- UI: suggesties per transactie met confidence-score; user kan accept/override.

Data & features

- Input features: description (tokenized), counterparty, amount (binned), booking account, weekday/month, merchant patterns.
- Label: CategoryId / BudgetLineId from user assignments.

Training / update flow

- Offline training job that uses persisted user-corrections as labeled data.
- Model versioning: store model metadata, createdAt, sample-size, metrics.
- Retrain frequency configurable (daily/weekly or manual trigger).

Performance & metrics

- Track precision, recall, top-N accuracy, suggestion acceptance rate.
- Minimum labeled examples per category before auto-assign considered (configurable threshold).

Explainability

- Return the top contributing features for a suggestion (e.g., keyword matches, counterparty).

Integration/UI

- API: `GET /suggestions?transactionId=...` -> `{ categoryId, score, explanation }`
- UI: show suggestions with confidence and accept/override buttons; track user action for training.

Storage

- Store user-corrections as labeled examples; keep audit trail linking suggestion -> final label -> user.
- **ML Model storage:** Train models using `LocalFinanceManager.ML` class library (separate from main app); serialize trained model as `MLModel` entity (byte[] ModelBytes + metadata in database). Enable versioning without filesystem dependencies.
- **Fixture models:** Pre-trained `.bin` model files committed to `LocalFinanceManager.ML.Tests/fixtures/` for fast <100ms test startup. CI job retrains fixture models monthly to detect data drift.

Tests

**Project Structure:** Three test projects with dedicated ML test isolation (`LocalFinanceManager.Tests`, `LocalFinanceManager.E2E`, `LocalFinanceManager.ML.Tests`).

- **Unit tests** (`LocalFinanceManager.Tests`): feature extraction (tokenization, binning, temporal patterns), rule engine scoring, suggestion ranking logic.
- **ML tests** (`LocalFinanceManager.ML.Tests`): model training, evaluation on holdout set, metric validation (precision, recall, F1), feature importance extraction, model serialization/deserialization using fixture models.
- **Integration tests** (`LocalFinanceManager.Tests`): end-to-end suggestion flow with in-memory SQLite; verify labeled examples stored after user corrections; test retraining trigger.
- **E2E tests** (`LocalFinanceManager.E2E`): suggestion display UI, accept/override workflows, confidence score display, explanation (top features) visibility.

Definition of Done

- Suggestion API with ML.NET model (logistic regression / decision tree) + feedback loop working.
- Suggestion endpoint returns `{ categoryId, confidence, explanation }` with top contributing features visible.
- User accept/override tracked as labeled training data in audit trail.
- Offline retraining job (configurable frequency) trains new model on labeled examples; stores result in `MLModel` table with version + metrics.
- Fixture models committed to repo; CI job retrains monthly to prevent data drift.
- Basic metrics dashboard showing precision, recall, acceptance rate, labeled examples count per category.
- Minimum labeled examples threshold (e.g., 10 per category) enforced before model considers category for auto-assignment (MVP-6).
