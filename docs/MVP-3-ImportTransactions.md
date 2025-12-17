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

- UI/JSON schema waarmee gebruiker kolomnamen mappt naar transactievenvelden.
- Voorstel-heuristieken: automatisch detecteer Date en Amount kolommen.

Deduplicatie

- Strategies:
  - exact: match op (Date, Amount, ExternalId)
  - fuzzy: description similarity + amount + counterparty (threshold configurable)
- Config: prioriteit order (externalId -> exact -> fuzzy).

Storage

- Transactie-entiteit bevat `OriginalImport` (string, nvarchar(max)), `ImportedAt`, `SourceFileName`, `ImportBatchId`.

Error handling & UX

- Per-row errors reported with line numbers; option voor partial import (skip errors) of all-or-nothing.

Performance

- Stream parsing voor grote bestanden; batch inserts; background job for heavy dedupe.

Tests

- Parser edgecases: escaped commas, quotes, different date formats.
- Deduplicate tests: exact, fuzzy thresholds, collision edgecases.

Definition of Done

- End-to-end: upload CSV → parsed → preview with mapping → import with dedupe, transactions saved with `OriginalImport`.
