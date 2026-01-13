# MVP 6 — Automatisering bij voldoende zekerheid

Doel

- Automatisch toepassen van categorisatieresultaten wanneer de confidence hoog genoeg, met undo/confirm flows en audit.

Acceptatiecriteria

- Configurable confidence threshold for auto-tagging.
- Background worker that processes suggestions, applies tags, records audit and exposes undo.

Background job design

- `IHostedService` configured in `Program.cs` with dual schedule:
  - Retraining job: **Sunday 2 AM UTC** weekly
  - Auto-apply job: **daily 6 AM UTC**
- Calls `IMLService.RetrainAsync()` to train new model on latest labeled data.
- **Threshold-based approval:** New model must exceed **F1 score > 0.85** before active model swap; rejected improvements logged for monitoring.
- Idempotent auto-apply job processes suggestion batches; retries with exponential backoff: 1s → 2s → 4s → 8s.
- Store `AutoAppliedBy`, `AutoAppliedAt`, `Confidence`, `ModelVersion` in audit trail for each auto-applied assignment.

Rollout & safety

- Feature flags / gradual rollout (percent of transactions or specific users/accounts).
- Undo UI: user can revert automated tag within **30-day retention window**; audit log records automated actions and reverts.
- **Safety gates:** Auto-apply only after MVP-5 retraining completes with threshold approval; monitored for undo rate.
- **Alert threshold:** Trigger alert if **undo rate exceeds 20% in 7-day window**, indicating potential model quality issues.

Configuration & Feature Flags

(via appsettings.json + IOptions<AutomationOptions>):

- Auto-apply confidence threshold: **0.85** (setting: AutomationOptions.ConfidenceThreshold)
- Retraining schedule: **Sunday 2 AM UTC** (setting: AutomationOptions.RetrainingScheduleCron = "0 2 \* \* 0")
- Auto-apply schedule: **daily 6 AM UTC** (setting: AutomationOptions.AutoApplyScheduleCron = "0 6 \* \* \*")
- Undo retention window: **30 days** (setting: AutomationOptions.UndoRetentionDays)
- Undo rate alert threshold: **>20% in 7-day window** (setting: AutomationOptions.UndoRateAlertThreshold)
- Max retries with exponential backoff: **5 retries** (1s, 2s, 4s, 8s, 16s)
- Per-account feature flag: auto-apply enabled/disabled toggle

Example IOptions class:

```csharp
public class AutomationOptions
{
    public decimal ConfidenceThreshold { get; set; } = 0.85m;
    public string RetrainingScheduleCron { get; set; } = "0 2 * * 0";
    public string AutoApplyScheduleCron { get; set; } = "0 6 * * *";
    public int UndoRetentionDays { get; set; } = 30;
    public decimal UndoRateAlertThreshold { get; set; } = 0.20m;
    public int MaxRetries { get; set; } = 5;
}
```

Error Handling

(see `Implementation-Guidelines.md` Error Response Format section):

- **Retraining failure:** Log error, retain previous model, alert operations team
- **Auto-apply failure:** Retry with exponential backoff (1s → 2s → 4s → 8s → 16s), log all attempts, skip transaction after max retries
- **Undo operation failure:** Return 409 Conflict if transaction was re-edited, 500 if database error
- **Feature flag retrieval failure:** Default to safe mode (auto-apply disabled)

Example error response for undo conflict:

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.4.9",
  "title": "Conflict - Cannot undo",
  "status": 409,
  "detail": "Transaction was modified after auto-apply. Manual review required."
}
```

Logging Strategy

(see `Implementation-Guidelines.md` Logging Strategy section):

- Use `ILogger<MLRetrainingService>` and `ILogger<AutoApplyService>` for background jobs
- Log levels:
  - `LogInformation`: Retraining started/completed, model approved/rejected, auto-apply batch started/completed, undo operation
  - `LogWarning`: High undo rate detected, confidence drift detected, retraining skipped due to insufficient data
  - `LogError`: Retraining failure, auto-apply crash, database transaction rollback
- Example: `_logger.LogInformation("Auto-applied {Count} transactions with avg confidence {Confidence}", count, avgConfidence);`
- Monitor: job execution time, transaction processing rate, error frequency, undo rate

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
