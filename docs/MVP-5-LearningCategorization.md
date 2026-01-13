# MVP 5 — Leerfunctie (categorisatie)

Doel

- Een leerbare engine (rule-based + ML-hybride) die transacties automatisch categoriseert en verbetert op basis van user-correcties.

Scope & acceptatiecriteria

- Basis prototype met feature-extraction en een eenvoudig model (logistic regression / decision tree) of rule-scoring.
- UI: suggesties per transactie met confidence-score; user kan accept/override.

Data & features

- Input features:
  - Description: tokenization (word-level, case-insensitive)
  - Amount: binning into 5 ranges (micro <10, small 10-100, medium 100-1000, large 1000-10000, xlarge >10000)
  - Temporal features: weekday, month, quarter patterns
  - Counterparty, booking account, merchant patterns
- Label: CategoryId / BudgetLineId from user assignments.

Training / update flow

- Offline training job using `FastTreeBinaryClassificationTrainer` for multi-class categorization via one-vs-rest approach.
- Weekly retraining on labeled data within 90-day rolling window.
- Model versioning: store model metadata, createdAt, sample-size, metrics, trainer configuration.
- Manual retraining trigger available for immediate updates.

Performance & metrics

- Track precision, recall, F1 score, top-N accuracy, suggestion acceptance rate.
- Minimum 10 labeled examples per category threshold before auto-assignment considered (MVP-6).

Configuration & Settings

(via appsettings.json + IOptions<MLOptions>):
- Minimum labeled examples threshold: **10 per category** (setting: MLOptions.MinLabeledExamplesPerCategory)
- Training window: **90 days** rolling (setting: MLOptions.TrainingWindowDays)
- Retraining frequency: **weekly** (setting: MLOptions.RetrainingScheduleCron = "0 2 * * 0") // Sunday 2 AM UTC
- Feature importance top N: **3** features (setting: MLOptions.TopFeaturesCount)
- Model approval threshold (F1 score): **> 0.85** (setting: MLOptions.MinF1ScoreForApproval)

Example IOptions class:
```csharp
public class MLOptions
{
    public int MinLabeledExamplesPerCategory { get; set; } = 10;
    public int TrainingWindowDays { get; set; } = 90;
    public string RetrainingScheduleCron { get; set; } = "0 2 * * 0";
    public int TopFeaturesCount { get; set; } = 3;
    public decimal MinF1ScoreForApproval { get; set; } = 0.85m;
}
```

Explainability

- Return the top contributing features for a suggestion (e.g., keyword matches, counterparty).

Integration/UI

- API: `GET /suggestions?transactionId=...` -> `{ categoryId, score, explanation }`
- UI: show suggestions with confidence and accept/override buttons; track user action for training.

Storage

- Store user-corrections as labeled examples; keep audit trail linking suggestion -> final label -> user.
- **ML Model storage:** Train models using `LocalFinanceManager.ML` class library (separate from main app); serialize trained model as `MLModel` entity (byte[] ModelBytes + metadata in database). Enable versioning without filesystem dependencies.
- **Fixture models:** Pre-trained `.bin` model files committed to `LocalFinanceManager.ML.Tests/fixtures/` for fast <100ms test startup. CI job retrains fixture models monthly to detect data drift.

Error Handling

(see `Implementation-Guidelines.md` Error Response Format section):
- **Insufficient labeled data:** Return 400 Bad Request with detail: "Category {categoryId} has only {count} labeled examples, minimum required is {threshold}"
- **Model training timeout:** Return 503 Service Unavailable with detail: "Model training exceeded timeout, please retry"
- **Feature extraction failure:** Log error and return 500 Internal Server Error
- **Suggestion API errors:** Return 400 Bad Request if transactionId not found, 500 if prediction fails

Example error response for insufficient data:
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Insufficient training data",
  "status": 400,
  "detail": "Category 'Groceries' has only 5 labeled examples, minimum required is 10"
}
```

Logging Strategy

(see `Implementation-Guidelines.md` Logging Strategy section):
- Use `ILogger<MLService>` and `ILogger<SuggestionService>` for operations
- Log levels:
  - `LogInformation`: Training started, model saved, new model approved, suggestion generated
  - `LogWarning`: Category below minimum threshold, confidence below expected range
  - `LogError`: Training failed, feature extraction error, model serialization failure
- Example: `_logger.LogInformation("Model v{Version} trained with F1 score {F1Score}, sample size {SampleSize}", version, f1Score, sampleSize);`

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
