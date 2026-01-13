# MVP 6 — Automatisering bij voldoende zekerheid

Doel

- Automatisch toepassen van categorisatieresultaten wanneer de confidence hoog genoeg, met undo/confirm flows en audit.

Acceptatiecriteria

- Configurable confidence threshold for auto-tagging.
- Background worker that processes suggestions, applies tags, records audit and exposes undo.

Background job design

- `IHostedService` configured in `Program.cs` runs on configurable schedule (e.g., weekly).
- Calls `IMLService.RetrainAsync()` to train new model on latest labeled data.
- **Threshold-based approval:** New model must exceed metric threshold (e.g., F1 score > 0.85) before active model swap; rejected improvements logged for monitoring.
- Idempotent auto-apply job processes suggestion batches; retries with exponential backoff.
- Store `AutoAppliedBy`, `AutoAppliedAt`, `Confidence`, `ModelVersion` in audit trail for each auto-applied assignment.

Rollout & safety

- Feature flags / gradual rollout (percent of transactions or specific users/accounts).
- Undo UI: user can revert automated tag within retention window (configurable, e.g., 30 days); audit log records automated actions and reverts.
- **Safety gates:** Auto-apply only after MVP-5 retraining completes with threshold approval; monitored for undo rate (alert if > 20% of auto-applies reversed).

Security & monitoring

- Authorization checks for automated actions.
- Monitoring: job failures, auto-apply rate, undo rate, confidence drift alerts.

Tests

**Project Structure:** Shared test infrastructure (`LocalFinanceManager.Tests`, `LocalFinanceManager.E2E`, `LocalFinanceManager.ML.Tests`).

- **Unit tests** (`LocalFinanceManager.Tests`): background job idempotency, retry logic with exponential backoff, threshold-based model approval logic, undo operation atomicity.
- **Integration tests** (`LocalFinanceManager.Tests`): end-to-end retraining job → model approval → auto-apply job → audit trail verification using in-memory SQLite with fixture models.
- **E2E tests** (`LocalFinanceManager.E2E`): auto-apply workflow, undo UI, rollout feature flag toggles, monitoring dashboard (job failures, auto-apply rate, undo rate, confidence drift alerts).
- **Load tests** (optional): job throughput with large transaction batches; focus on idempotency under retries.

Definition of Done

- `IHostedService` background job running on configurable schedule (e.g., weekly).
- ML retraining triggered automatically; new models require threshold approval (F1 > 0.85) before active swap; rejected improvements logged.
- Auto-apply worker processes suggestions at configurable confidence threshold; stores audit trail (AutoAppliedBy, AutoAppliedAt, Confidence, ModelVersion).
- Undo functionality working within retention window; UI displays undo option with timestamp and reason.
- Monitoring dashboard shows job failure count, auto-apply rate, undo rate, confidence drift alerts.
- Feature flags control gradual rollout per account/user; can disable auto-apply without redeployment.
- All retraining and auto-apply operations fully audited with complete before/after state for compliance.
