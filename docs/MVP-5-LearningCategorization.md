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

Tests

- Unit: feature extraction, rule engine scoring.
- Model tests: validate that retrained model improves metrics on holdout.

Definition of Done

- Suggestion API with a simple model + feedback loop; example trainer; metrics dashboard basic.
