# MVP 6 — Automatisering bij voldoende zekerheid

Doel

- Automatisch toepassen van categorisatieresultaten wanneer de confidence hoog genoeg, met undo/confirm flows en audit.

Acceptatiecriteria

- Configurable confidence threshold for auto-tagging.
- Background worker that processes suggestions, applies tags, records audit and exposes undo.

Background job design

- Idempotent jobs that process suggestion batches; retries with exponential backoff.
- Store `AutoAppliedBy`, `AutoAppliedAt`, `Confidence`, `ModelVersion`.

Rollout & safety

- Feature flags / gradual rollout (percent of transactions or specific users/accounts).
- Undo UI: user can revert automated tag within retention window; audit log records automated actions.

Security & monitoring

- Authorization checks for automated actions.
- Monitoring: job failures, auto-apply rate, undo rate, confidence drift alerts.

Tests

- End-to-end: suggestion -> background apply -> audit -> undo.
- Load tests for job throughput if many transactions.

Definition of Done

- Automated apply worker running in dev with configurable threshold, audit + undo, monitoring hooks.
