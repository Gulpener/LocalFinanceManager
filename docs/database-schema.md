# Database Schema — LocalFinanceManager

## Overview

LocalFinanceManager uses **SQLite** as its local database provider with **Entity Framework Core 10** for ORM and migrations. This document describes the complete database schema, relationships, and design decisions.

---

## Entity Relationship Diagram

```
┌─────────────────┐         ┌──────────────────┐
│    Account      │         │   Transaction    │
├─────────────────┤         ├──────────────────┤
│ Id (PK)         │◄────┐   │ Id (PK)          │
│ Name            │     │   │ AccountId (FK)   │───┐
│ AccountType     │     └───│ Date             │   │
│ InitialBalance  │         │ Amount           │   │
│ IsActive        │         │ Description      │   │
│ CreatedAt       │         │ CategoryId (FK)  │───┼───┐
│ UpdatedAt       │         │ EnvelopeId (FK)  │───┼───┼───┐
└─────────────────┘         │ OriginalCsv      │   │   │   │
                            │ ImportHash       │   │   │   │
                            │ Tags (JSON)      │   │   │   │
                            │ IsSplit          │   │   │   │
                            │ ParentTransId(FK)│   │   │   │
                            │ CreatedAt        │   │   │   │
                            │ UpdatedAt        │   │   │   │
                            └──────────────────┘   │   │   │
                                    │              │   │   │
                                    │              │   │   │
┌─────────────────────┐             │              │   │   │
│  TransactionSplit   │             │              │   │   │
├─────────────────────┤             │              │   │   │
│ Id (PK)             │             │              │   │   │
│ TransactionId (FK)  │◄────────────┘              │   │   │
│ CategoryId (FK)     │                            │   │   │
│ EnvelopeId (FK)     │                            │   │   │
│ Amount              │                            │   │   │
│ Description         │                            │   │   │
└─────────────────────┘                            │   │   │
                                                   │   │   │
┌─────────────────┐                                │   │   │
│    Category     │◄───────────────────────────────┘   │   │
├─────────────────┤                                    │   │
│ Id (PK)         │                                    │   │
│ Name            │                                    │   │
│ ParentId (FK)   │───┐                                │   │
│ MonthlyBudget   │   │                                │   │
│ IconName        │   │                                │   │
│ Color           │   │                                │   │
│ IsActive        │   │                                │   │
│ CreatedAt       │   │                                │   │
│ UpdatedAt       │   │                                │   │
└─────────────────┘   │                                │   │
         ▲            │                                │   │
         └────────────┘ (self-reference for hierarchy) │   │
                                                       │   │
┌────────────────────────────┐                         │   │
│ CategoryLearningProfile    │                         │   │
├────────────────────────────┤                         │   │
│ Id (PK)                    │                         │   │
│ CategoryId (FK)            │◄────────────────────────┘   │
│ WordFrequency (JSON)       │                             │
│ IbanFrequency (JSON)       │                             │
│ AmountBucketFreq (JSON)    │                             │
│ RecurrenceFreq (JSON)      │                             │
│ TotalSamples               │                             │
│ LastUpdated                │                             │
└────────────────────────────┘                             │
                                                           │
┌─────────────────┐                                        │
│    Envelope     │◄───────────────────────────────────────┘
├─────────────────┤
│ Id (PK)         │
│ Name            │
│ Balance         │
│ MonthlyAlloc    │
│ IsActive        │
│ CreatedAt       │
│ UpdatedAt       │
└─────────────────┘

┌─────────────────┐
│      Rule       │
├─────────────────┤
│ Id (PK)         │
│ Name            │
│ MatchType       │ (Contains, Regex, Exact, IBAN)
│ Pattern         │
│ TargetCatId(FK) │───┐
│ TargetEnvId(FK) │───┼───┐
│ AddLabels (JSON)│   │   │
│ Priority        │   │   │
│ IsActive        │   │   │
│ CreatedAt       │   │   │
│ UpdatedAt       │   │   │
└─────────────────┘   │   │
         │            │   │
         └────────────┴───┼───► (FK to Category and Envelope)
                          │
                          └───────────────────────────────┐
┌─────────────────┐                                       │
│     Budget      │                                       │
├─────────────────┤                                       │
│ Id (PK)         │                                       │
│ CategoryId (FK) │───────────────────────────────────────┘
│ EnvelopeId (FK) │
│ Month (Date)    │
│ PlannedAmount   │
│ ActualAmount    │
│ CreatedAt       │
│ UpdatedAt       │
└─────────────────┘
```

---

## Table Definitions

### Account

Represents a financial account (bank account, credit card, cash, savings, etc.)

| Column           | Type          | Constraints                         | Description                                       |
| ---------------- | ------------- | ----------------------------------- | ------------------------------------------------- |
| `Id`             | INTEGER       | PRIMARY KEY, AUTO INCREMENT         | Unique identifier                                 |
| `Name`           | TEXT          | NOT NULL                            | Account name (e.g., "ING Checking")               |
| `AccountType`    | TEXT          | NOT NULL                            | Type: `Checking`, `Savings`, `CreditCard`, `Cash` |
| `InitialBalance` | DECIMAL(18,2) | NOT NULL                            | Starting balance                                  |
| `IsActive`       | BOOLEAN       | NOT NULL, DEFAULT 1                 | Whether account is active/visible                 |
| `CreatedAt`      | DATETIME      | NOT NULL, DEFAULT CURRENT_TIMESTAMP |                                                   |
| `UpdatedAt`      | DATETIME      | NOT NULL, DEFAULT CURRENT_TIMESTAMP |                                                   |

**Indexes:**

- `IX_Account_IsActive` on `IsActive`

---

### Transaction

Represents a single financial transaction

| Column                | Type          | Constraints                         | Description                                              |
| --------------------- | ------------- | ----------------------------------- | -------------------------------------------------------- |
| `Id`                  | INTEGER       | PRIMARY KEY, AUTO INCREMENT         | Unique identifier                                        |
| `AccountId`           | INTEGER       | FOREIGN KEY → Account.Id, NOT NULL  | Associated account                                       |
| `Date`                | DATETIME      | NOT NULL                            | Transaction date                                         |
| `Amount`              | DECIMAL(18,2) | NOT NULL                            | Amount (positive = income, negative = expense)           |
| `Description`         | TEXT          | NOT NULL                            | Transaction description                                  |
| `CategoryId`          | INTEGER       | FOREIGN KEY → Category.Id, NULL     | Assigned category                                        |
| `EnvelopeId`          | INTEGER       | FOREIGN KEY → Envelope.Id, NULL     | Assigned envelope                                        |
| `OriginalCsv`         | TEXT          | NULL                                | **Original CSV/import row** for traceability             |
| `ImportHash`          | TEXT          | NULL                                | SHA256 hash for deduplication (Date+Amount+Desc+Account) |
| `Tags`                | TEXT          | NULL                                | **JSON array** of string tags                            |
| `IsSplit`             | BOOLEAN       | NOT NULL, DEFAULT 0                 | Whether this is a split transaction                      |
| `ParentTransactionId` | INTEGER       | FOREIGN KEY → Transaction.Id, NULL  | Parent if this is a split part                           |
| `CreatedAt`           | DATETIME      | NOT NULL, DEFAULT CURRENT_TIMESTAMP |                                                          |
| `UpdatedAt`           | DATETIME      | NOT NULL, DEFAULT CURRENT_TIMESTAMP |                                                          |

**Indexes:**

- `IX_Transaction_AccountId` on `AccountId`
- `IX_Transaction_CategoryId` on `CategoryId`
- `IX_Transaction_EnvelopeId` on `EnvelopeId`
- `IX_Transaction_Date` on `Date`
- `IX_Transaction_ImportHash` on `ImportHash` (UNIQUE where NOT NULL)
- `IX_Transaction_ParentTransactionId` on `ParentTransactionId`

**JSON Columns:**

- `Tags`: Stored as JSON text, e.g., `["groceries", "weekly"]`
  - EF Core value converter handles serialization

---

### TransactionSplit

Represents a split part of a transaction when divided across multiple categories/envelopes

| Column          | Type          | Constraints                            | Description                              |
| --------------- | ------------- | -------------------------------------- | ---------------------------------------- |
| `Id`            | INTEGER       | PRIMARY KEY, AUTO INCREMENT            | Unique identifier                        |
| `TransactionId` | INTEGER       | FOREIGN KEY → Transaction.Id, NOT NULL | Parent transaction                       |
| `CategoryId`    | INTEGER       | FOREIGN KEY → Category.Id, NULL        | Split category                           |
| `EnvelopeId`    | INTEGER       | FOREIGN KEY → Envelope.Id, NULL        | Split envelope                           |
| `Amount`        | DECIMAL(18,2) | NOT NULL                               | Split amount (must sum to parent amount) |
| `Description`   | TEXT          | NULL                                   | Optional description for this split      |

**Constraints:**

- Sum of `Amount` in splits must equal parent `Transaction.Amount` (enforced by application logic)

**Indexes:**

- `IX_TransactionSplit_TransactionId` on `TransactionId`

---

### Category

Represents expense/income categories with optional hierarchy

| Column             | Type          | Constraints                         | Description                      |
| ------------------ | ------------- | ----------------------------------- | -------------------------------- |
| `Id`               | INTEGER       | PRIMARY KEY, AUTO INCREMENT         | Unique identifier                |
| `Name`             | TEXT          | NOT NULL, UNIQUE                    | Category name                    |
| `ParentCategoryId` | INTEGER       | FOREIGN KEY → Category.Id, NULL     | Parent category for hierarchy    |
| `MonthlyBudget`    | DECIMAL(18,2) | NOT NULL, DEFAULT 0                 | Default monthly budget           |
| `IconName`         | TEXT          | NULL                                | Icon identifier for UI           |
| `Color`            | TEXT          | NULL                                | Hex color code (e.g., `#FF5733`) |
| `IsActive`         | BOOLEAN       | NOT NULL, DEFAULT 1                 | Whether category is active       |
| `CreatedAt`        | DATETIME      | NOT NULL, DEFAULT CURRENT_TIMESTAMP |                                  |
| `UpdatedAt`        | DATETIME      | NOT NULL, DEFAULT CURRENT_TIMESTAMP |                                  |

**Indexes:**

- `IX_Category_ParentCategoryId` on `ParentCategoryId`
- `IX_Category_Name` on `Name` (UNIQUE)

**Self-Reference:**

- Categories can have subcategories via `ParentCategoryId`
- Example: `Groceries` (parent) → `Vegetables`, `Meat`, `Dairy` (children)

---

### CategoryLearningProfile

Stores machine learning data for automatic categorization

| Column                  | Type     | Constraints                                 | Description                              |
| ----------------------- | -------- | ------------------------------------------- | ---------------------------------------- |
| `Id`                    | INTEGER  | PRIMARY KEY, AUTO INCREMENT                 | Unique identifier                        |
| `CategoryId`            | INTEGER  | FOREIGN KEY → Category.Id, NOT NULL, UNIQUE | Associated category                      |
| `WordFrequency`         | TEXT     | NOT NULL                                    | **JSON dictionary** `{"word": count}`    |
| `IbanFrequency`         | TEXT     | NOT NULL                                    | **JSON dictionary** `{"IBAN": count}`    |
| `AmountBucketFrequency` | TEXT     | NOT NULL                                    | **JSON dictionary** `{"bucket": count}`  |
| `RecurrenceFrequency`   | TEXT     | NOT NULL                                    | **JSON dictionary** `{"pattern": count}` |
| `TotalSamples`          | INTEGER  | NOT NULL, DEFAULT 0                         | Total transactions learned               |
| `LastUpdated`           | DATETIME | NOT NULL, DEFAULT CURRENT_TIMESTAMP         |                                          |

**JSON Structure Examples:**

```json
// WordFrequency
{"albert": 15, "heijn": 15, "supermarket": 20, "groceries": 8}

// IbanFrequency
{"NL01RABO0123456789": 5, "NL02INGB0987654321": 3}

// AmountBucketFrequency (bucketed by ranges)
{"0-10": 5, "10-50": 20, "50-100": 10, "100+": 2}

// RecurrenceFrequency
{"monthly": 12, "weekly": 4}
```

**Indexes:**

- `IX_CategoryLearningProfile_CategoryId` on `CategoryId` (UNIQUE)

---

### Envelope

Represents budget envelopes/pots for allocation-based budgeting

| Column              | Type          | Constraints                         | Description                                   |
| ------------------- | ------------- | ----------------------------------- | --------------------------------------------- |
| `Id`                | INTEGER       | PRIMARY KEY, AUTO INCREMENT         | Unique identifier                             |
| `Name`              | TEXT          | NOT NULL, UNIQUE                    | Envelope name (e.g., "Groceries", "Vacation") |
| `Balance`           | DECIMAL(18,2) | NOT NULL, DEFAULT 0                 | Current balance                               |
| `MonthlyAllocation` | DECIMAL(18,2) | NOT NULL, DEFAULT 0                 | Amount allocated monthly                      |
| `IsActive`          | BOOLEAN       | NOT NULL, DEFAULT 1                 | Whether envelope is active                    |
| `CreatedAt`         | DATETIME      | NOT NULL, DEFAULT CURRENT_TIMESTAMP |                                               |
| `UpdatedAt`         | DATETIME      | NOT NULL, DEFAULT CURRENT_TIMESTAMP |                                               |

**Indexes:**

- `IX_Envelope_Name` on `Name` (UNIQUE)

**Balance Calculation:**

- Recalculated periodically based on transactions assigned to envelope

---

### Rule

Represents automation rules for categorization and envelope assignment

| Column             | Type     | Constraints                         | Description                          |
| ------------------ | -------- | ----------------------------------- | ------------------------------------ |
| `Id`               | INTEGER  | PRIMARY KEY, AUTO INCREMENT         | Unique identifier                    |
| `Name`             | TEXT     | NOT NULL                            | Rule name for UI                     |
| `MatchType`        | TEXT     | NOT NULL                            | `Contains`, `Regex`, `Exact`, `IBAN` |
| `Pattern`          | TEXT     | NOT NULL                            | Match pattern/regex/IBAN             |
| `TargetCategoryId` | INTEGER  | FOREIGN KEY → Category.Id, NULL     | Auto-assign category                 |
| `TargetEnvelopeId` | INTEGER  | FOREIGN KEY → Envelope.Id, NULL     | Auto-assign envelope                 |
| `AddLabels`        | TEXT     | NULL                                | **JSON array** of tags to add        |
| `Priority`         | INTEGER  | NOT NULL, DEFAULT 0                 | Higher = evaluated first             |
| `IsActive`         | BOOLEAN  | NOT NULL, DEFAULT 1                 | Whether rule is active               |
| `CreatedAt`        | DATETIME | NOT NULL, DEFAULT CURRENT_TIMESTAMP |                                      |
| `UpdatedAt`        | DATETIME | NOT NULL, DEFAULT CURRENT_TIMESTAMP |                                      |

**JSON Columns:**

- `AddLabels`: `["recurring", "subscription"]`

**Indexes:**

- `IX_Rule_Priority` on `Priority` (DESC)
- `IX_Rule_IsActive` on `IsActive`

**Rule Evaluation:**

- Rules are evaluated in priority order (highest first)
- First matching rule wins (unless configured otherwise)

---

### Budget

Represents monthly budget tracking per category/envelope

| Column          | Type          | Constraints                         | Description                                 |
| --------------- | ------------- | ----------------------------------- | ------------------------------------------- |
| `Id`            | INTEGER       | PRIMARY KEY, AUTO INCREMENT         | Unique identifier                           |
| `CategoryId`    | INTEGER       | FOREIGN KEY → Category.Id, NULL     | Category (mutually exclusive with Envelope) |
| `EnvelopeId`    | INTEGER       | FOREIGN KEY → Envelope.Id, NULL     | Envelope (mutually exclusive with Category) |
| `Month`         | DATE          | NOT NULL                            | Budget month (e.g., `2025-01-01`)           |
| `PlannedAmount` | DECIMAL(18,2) | NOT NULL                            | Budgeted amount                             |
| `ActualAmount`  | DECIMAL(18,2) | NOT NULL, DEFAULT 0                 | Actual spent (computed from transactions)   |
| `CreatedAt`     | DATETIME      | NOT NULL, DEFAULT CURRENT_TIMESTAMP |                                             |
| `UpdatedAt`     | DATETIME      | NOT NULL, DEFAULT CURRENT_TIMESTAMP |                                             |

**Constraints:**

- Either `CategoryId` OR `EnvelopeId` must be set (not both, enforced by application)

**Indexes:**

- `IX_Budget_CategoryId_Month` on `(CategoryId, Month)` (UNIQUE)
- `IX_Budget_EnvelopeId_Month` on `(EnvelopeId, Month)` (UNIQUE)

---

## EF Core Configuration

### Value Conversions

**JSON Columns:**

```csharp
// In ApplicationDbContext.OnModelCreating
modelBuilder.Entity<Transaction>()
    .Property(t => t.Tags)
    .HasConversion(
        v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
        v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions)null)
    );

modelBuilder.Entity<CategoryLearningProfile>()
    .Property(p => p.WordFrequency)
    .HasConversion(
        v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
        v => JsonSerializer.Deserialize<Dictionary<string, int>>(v, (JsonSerializerOptions)null)
    );
```

### Indexes

```csharp
modelBuilder.Entity<Transaction>()
    .HasIndex(t => t.ImportHash)
    .IsUnique()
    .HasFilter("ImportHash IS NOT NULL");

modelBuilder.Entity<Rule>()
    .HasIndex(r => r.Priority)
    .IsDescending();
```

---

## Migration Strategy

### Initial Migration

```powershell
dotnet ef migrations add InitialCreate --project src/Infrastructure --startup-project src/Web
dotnet ef database update --project src/Infrastructure --startup-project src/Web
```

### Migration Naming Convention

- `InitialCreate` — First migration
- `AddOriginalCsvColumn` — Descriptive feature name
- `UpdateCategoryHierarchy` — Schema change description

### Database Location

- **Development:** `App_Data/local.db` (SQLite file)
- **Production:** User-specified path or default to `%LOCALAPPDATA%/LocalFinanceManager/finance.db`

---

## Data Integrity

### Constraints Enforced by Database

- Foreign key relationships with `ON DELETE RESTRICT` (default)
- UNIQUE constraints on import hashes
- NOT NULL on critical fields

### Constraints Enforced by Application

- Transaction split amounts sum to parent
- Budget month uniqueness per category/envelope
- Category hierarchy depth limits
- Decimal precision for money (18,2)

---

## Performance Considerations

### Indexing Strategy

- Foreign keys indexed automatically by EF Core
- Additional indexes on frequently queried columns (`Date`, `ImportHash`)
- Composite indexes for budget lookups

### Query Optimization

- Use `.AsNoTracking()` for read-only queries
- Eager load related entities with `.Include()` when needed
- Avoid N+1 queries with proper navigation properties

### SQLite Optimizations

```sql
PRAGMA journal_mode = WAL;  -- Write-Ahead Logging for concurrency
PRAGMA synchronous = NORMAL;
PRAGMA cache_size = -64000; -- 64MB cache
```

---

## Backup & Recovery

### Automatic Backups

- Daily backup task copies `local.db` to `Backups/` folder
- Retention policy: Keep last 30 days

### Manual Backup

```powershell
Copy-Item "App_Data/local.db" "Backups/manual-backup-$(Get-Date -Format 'yyyyMMdd').db"
```

### Restore

```powershell
Copy-Item "Backups/backup-file.db" "App_Data/local.db" -Force
dotnet ef database update --project src/Infrastructure --startup-project src/Web
```

---

## Encryption (Future)

### AES-256 Encryption

- SQLite database encrypted with user password
- Using `SQLCipher` or `Microsoft.Data.Sqlite` with encryption extension
- Key derivation: PBKDF2 with high iteration count

---

## Schema Versioning

Current schema version: **1.0.0**

Schema versions tracked in `__EFMigrationsHistory` table by EF Core.

---

**Last Updated:** November 27, 2025  
**Maintained By:** Development Team
