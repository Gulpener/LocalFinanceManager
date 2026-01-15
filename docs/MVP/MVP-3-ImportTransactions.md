# MVP 3 — Import transacties

Doel

- Betrouwbare importpipeline voor transacties vanuit CSV/JSON met deduplicatie en opslag van originele import-string.

Acceptatiecriteria

- Upload endpoint + UI voor CSV/JSON.
- Configurabele kolom-mapping, validatie per rij, duidelijke foutmeldingen.
- Deduplicatie strategy implementatie: exact en fuzzy matching.

Import specificaties

- CSV: configurable headers, encoding UTF-8 (option BOM), voorbeeldmappings: Date, Amount, Description, Counterparty, AccountId, ExternalId.
- JSON: array van objects met gelijkwaardige velden.

Mapping config

- UI/JSON schema waarmee gebruiker kolomnamen mappt naar transactievelden.
- Voorstel-heuristieken: automatisch detecteer Date en Amount kolommen.

Deduplicatie

- Strategies:
  - exact: match op (Date, Amount, ExternalId)
  - fuzzy: Jaccard similarity algorithm on description (threshold 0.65) + amount match + counterparty similarity
- Config: prioriteit order (externalId -> exact -> fuzzy)
- Auto-detection heuristics for Date, Amount, Description column mapping
- Batch processing: 1000 transactions per batch
- Max file size: 100 MB

Storage

- Transactie-entiteit bevat `OriginalImport` (string, nvarchar(max)), `ImportedAt`, `SourceFileName`, `ImportBatchId`.
- **Note:** Import process **bypasses RowVersion conflict checks** (import is batch non-interactive operation); RowVersion validation applied only to post-import manual edits (MVP-4+).

Error handling & UX

- Per-row errors reported with line numbers; option voor partial import (skip errors) of all-or-nothing.
- Error responses follow RFC 7231 Problem Details format (see `../Implementation-Guidelines.md` Error Response Format section)
- Per-row errors include: line number, field name, and error message
- API returns 400 Bad Request for validation failures with structured errors
- Example: `{ "type": "...", "status": 400, "errors": { "Date": ["Invalid date format"] } }`

Performance

- Stream parsing voor grote bestanden; batch inserts; background job for heavy dedupe.
- Configuration (via appsettings.json + IOptions<ImportOptions>):

  - Batch size: **1000 rows** per transaction (setting: ImportOptions.BatchSize)
  - Max file size: **100 MB** (setting: ImportOptions.MaxFileSizeMB)
  - Timeout: **60 seconds per 10k rows** (setting: ImportOptions.TimeoutSecondsPerBatch)
  - Fuzzy threshold: **0.65** Jaccard similarity (setting: ImportOptions.FuzzyMatchThreshold)

  Example IOptions class:

  ```csharp
  public class ImportOptions
  {
      public int BatchSize { get; set; } = 1000;
      public int MaxFileSizeMB { get; set; } = 100;
      public int TimeoutSecondsPerBatch { get; set; } = 60;
      public decimal FuzzyMatchThreshold { get; set; } = 0.65m;
  }
  ```

Tests

**Project Structure:** Shared test infrastructure from MVP-1 (`LocalFinanceManager.Tests` and `LocalFinanceManager.E2E`).

- **Unit tests** (`LocalFinanceManager.Tests`): Parser edgecases (escaped commas, quotes, different date formats), deduplicate tests (exact, fuzzy thresholds, collision edgecases), mapping configuration validation.
- **Integration tests** (`LocalFinanceManager.Tests`): end-to-end import flow with in-memory SQLite, deduplication against existing transactions, original import string storage verification.
- **E2E tests** (`LocalFinanceManager.E2E`): Upload UI → preview mapping → import flow; verify imported transactions appear in list and are excludable from duplicates on re-import.

Logging Strategy

(see `../Implementation-Guidelines.md` Logging Strategy section):

- Use `ILogger<ImportService>` for import operations
- Log levels:
  - `LogInformation`: Import started, batch completed, total rows imported
  - `LogWarning`: Validation failures per row, deduplication matches skipped
  - `LogError`: Critical failures (file parse error, database error, timeout)
- Example: `_logger.LogInformation("Imported {RowCount} transactions in batch {BatchId}", rowCount, batchId);`

Definition of Done

- End-to-end: upload CSV → parsed → preview with mapping → import with dedupe, transactions saved with `OriginalImport`.
- Import succeeds with comprehensive audit trail (original file, batch ID, timestamp, error log per row).
- Deduplication (exact + fuzzy) working correctly across existing and new transactions.
- RowVersion concurrency checks **not applied** during import; manual post-import edits respect RowVersion validation.
