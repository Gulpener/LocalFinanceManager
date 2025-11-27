# Test Data Fixtures — LocalFinanceManager

## Overview

This directory contains sample data files used throughout the test suite for the LocalFinanceManager application. These fixtures support unit tests, integration tests, and development workflows.

---

## File Inventory

### `sample-transactions.csv`

**Purpose:** Standard CSV format for transaction import testing

**Format:**

```csv
Date,Amount,Description,CounterAccount,AccountNumber
2025-01-15,100.50,Albert Heijn supermarket,NL01RABO0123456789,NL99INGB9876543210
2025-01-16,-45.00,Shell gas station,NL02SHELL0011223344,NL99INGB9876543210
2025-01-17,2500.00,Salary deposit,NL03ACME0099887766,NL99INGB9876543210
```

**Usage:**

- CSV import parser tests
- Deduplication logic validation
- `OriginalCsv` preservation verification

**Test Coverage:**

- ✅ Positive amounts (income)
- ✅ Negative amounts (expenses)
- ✅ Various date formats
- ✅ Special characters in descriptions
- ✅ IBAN validation

---

### `sample-transactions.json`

**Purpose:** JSON format for alternative import method testing

**Format:**

```json
[
  {
    "date": "2025-01-15T00:00:00",
    "amount": 100.5,
    "description": "Albert Heijn supermarket",
    "counterAccount": "NL01RABO0123456789",
    "accountNumber": "NL99INGB9876543210"
  },
  {
    "date": "2025-01-16T00:00:00",
    "amount": -45.0,
    "description": "Shell gas station",
    "counterAccount": "NL02SHELL0011223344",
    "accountNumber": "NL99INGB9876543210"
  }
]
```

**Usage:**

- JSON import parser tests
- Alternative data format validation
- Schema compatibility testing

---

### `sample-transactions-mt940.txt`

**Purpose:** MT940 bank statement format testing (SWIFT standard)

**Format:** (Simplified MT940)

```
:20:TRANSACTION-REF-001
:25:NL99INGB9876543210
:28C:00001/001
:60F:C250101EUR1000.00
:61:2501150115C100,50NTRFNONREF//Albert Heijn supermarket
:86:COUNTER ACCOUNT: NL01RABO0123456789
:61:2501160116D45,00NTRFNONREF//Shell gas station
:86:COUNTER ACCOUNT: NL02SHELL0011223344
:62F:C250116EUR1055,50
```

**Usage:**

- MT940 parser implementation (future)
- SWIFT format validation
- European banking standard compliance

**Note:** MT940 support is planned but not yet implemented. This file serves as a reference for future development.

---

### `duplicate-transactions.csv`

**Purpose:** Testing deduplication logic with intentional duplicates

**Format:**

```csv
Date,Amount,Description,CounterAccount,AccountNumber
2025-01-15,100.50,Albert Heijn supermarket,NL01RABO0123456789,NL99INGB9876543210
2025-01-15,100.50,Albert Heijn supermarket,NL01RABO0123456789,NL99INGB9876543210
2025-01-16,100.50,Albert Heijn supermarket,NL01RABO0123456789,NL99INGB9876543210
2025-01-15,100.51,Albert Heijn supermarket,NL01RABO0123456789,NL99INGB9876543210
```

**Test Scenarios:**

1. **Exact duplicates** — Line 1 and 2 (should be detected)
2. **Same amount, different date** — Line 1 and 3 (not duplicates)
3. **Almost identical** — Line 1 and 4, differ by 1 cent (not duplicates)

**Usage:**

- `DeduplicationService` unit tests
- Import hash collision testing
- Preview UI validation

---

### `edge-cases.csv`

**Purpose:** Boundary values, special characters, and unusual formats

**Format:**

```csv
Date,Amount,Description,CounterAccount,AccountNumber
2025-01-01,0.00,Zero amount transaction,NL01TEST0000000001,NL99INGB9876543210
2025-01-02,0.01,Minimum positive amount,NL01TEST0000000002,NL99INGB9876543210
2025-01-03,-0.01,Minimum negative amount,NL01TEST0000000003,NL99INGB9876543210
2025-01-04,999999.99,Very large amount,NL01TEST0000000004,NL99INGB9876543210
2025-01-05,50.00,"Description with ""quotes"" and, commas",NL01TEST0000000005,NL99INGB9876543210
2025-01-06,30.00,Ümlauts ëñ dëscríptíøn,NL01TEST0000000006,NL99INGB9876543210
2025-12-31,100.00,Last day of year,NL01TEST0000000007,NL99INGB9876543210
2025-02-29,100.00,Invalid leap day (2025 is not a leap year),NL01TEST0000000008,NL99INGB9876543210
```

**Test Scenarios:**

- ✅ Zero amounts
- ✅ Minimum decimal values (0.01)
- ✅ Large amounts (overflow protection)
- ✅ Special characters (quotes, commas, Unicode)
- ✅ Date edge cases (year boundaries, invalid dates)
- ✅ CSV escaping rules

**Usage:**

- Input validation tests
- Parser robustness verification
- Error handling validation

---

### `learning-profiles.json`

**Purpose:** Pre-trained category learning profiles for scoring tests

**Format:**

```json
[
  {
    "categoryId": 1,
    "categoryName": "Groceries",
    "wordFrequency": {
      "albert": 15,
      "heijn": 15,
      "jumbo": 10,
      "supermarket": 25,
      "groceries": 8
    },
    "ibanFrequency": {
      "NL01RABO0123456789": 15,
      "NL02JUMBO0011223344": 10
    },
    "amountBucketFrequency": {
      "0-10": 5,
      "10-50": 20,
      "50-100": 15,
      "100+": 5
    },
    "recurrenceFrequency": {
      "weekly": 30,
      "biweekly": 10
    }
  },
  {
    "categoryId": 2,
    "categoryName": "Fuel",
    "wordFrequency": {
      "shell": 20,
      "bp": 10,
      "esso": 8,
      "gas": 15,
      "fuel": 12
    },
    "ibanFrequency": {
      "NL02SHELL0011223344": 20,
      "NL03BP0099887766": 10
    },
    "amountBucketFrequency": {
      "30-60": 25,
      "60-90": 10
    },
    "recurrenceFrequency": {
      "weekly": 15,
      "biweekly": 20
    }
  }
]
```

**Usage:**

- `ScoringEngine` unit tests
- Pre-seeding test databases
- Performance benchmarking with realistic data

---

## Data Generation

### Manual Creation

Most test data is hand-crafted to ensure specific edge cases are covered.

### Automated Generation (Future)

Consider using libraries like **Bogus** for generating large datasets:

```csharp
using Bogus;

var transactionFaker = new Faker<Transaction>()
    .RuleFor(t => t.Date, f => f.Date.Between(DateTime.Now.AddYears(-1), DateTime.Now))
    .RuleFor(t => t.Amount, f => f.Finance.Amount(-500, 5000))
    .RuleFor(t => t.Description, f => f.Company.CompanyName())
    .RuleFor(t => t.CounterAccount, f => f.Finance.Iban());

var transactions = transactionFaker.Generate(1000);
```

---

## File Formats Supported

| Format | Extension        | Status         | Priority |
| ------ | ---------------- | -------------- | -------- |
| CSV    | `.csv`           | ✅ Implemented | High     |
| TSV    | `.tsv`           | ✅ Implemented | Medium   |
| JSON   | `.json`          | ✅ Implemented | Medium   |
| MT940  | `.txt`, `.mt940` | 🚧 Planned     | Low      |
| OFX    | `.ofx`           | 📋 Future      | Low      |
| QIF    | `.qif`           | 📋 Future      | Low      |

---

## CSV Format Specifications

### Delimiter Support

- **Comma (`,`)** — Default
- **Semicolon (`;`)** — European standard
- **Tab (`\t`)** — TSV files
- **Pipe (`|`)** — Alternative

### Header Variations

The importer supports multiple header naming conventions:

| Standard         | Aliases                                      |
| ---------------- | -------------------------------------------- |
| `Date`           | `Datum`, `Transaction Date`, `Boekingsdatum` |
| `Amount`         | `Bedrag`, `Value`, `TransactionAmount`       |
| `Description`    | `Omschrijving`, `Memo`, `Details`            |
| `CounterAccount` | `Tegenrekening`, `IBAN`, `Counterparty`      |
| `AccountNumber`  | `Rekening`, `Account`, `OwnAccount`          |

### Encoding

- **UTF-8** (with or without BOM) — Preferred
- **ISO-8859-1** (Latin-1) — Fallback for legacy files
- **Windows-1252** — European banks

---

## Test Data Guidelines

### When Creating New Test Files

1. **Name descriptively** — `scenario-name.format` (e.g., `split-transactions.csv`)
2. **Document in this README** — Add a section describing the file
3. **Include comments** — Use CSV comments where possible (lines starting with `#`)
4. **Minimize size** — Keep files small (< 100 rows) for fast tests
5. **Version control** — Commit all test data files

### Example: Adding a New Fixture

````markdown
### `split-transactions.csv`

**Purpose:** Testing transaction splitting functionality

**Format:**

```csv
Date,Amount,Description,CounterAccount,AccountNumber
2025-01-15,150.00,Walmart - groceries and household,NL01WALMART123,NL99INGB9876543210
```
````

**Expected Splits:**

- $100 → Groceries
- $50 → Household

**Usage:**

- Split transaction UI tests
- Amount validation (sum equals parent)

```

```

---

## Data Privacy

⚠️ **Important:** Test data should NEVER contain real financial information.

- ✅ Use fake IBANs (valid checksum but fictional banks)
- ✅ Use generic merchant names
- ✅ Use realistic but fictional amounts
- ❌ Never commit real account numbers
- ❌ Never commit personal data

---

## Test Data Refresh

### When to Update

- When adding new features that require new scenarios
- When bugs reveal missing edge cases
- When import format specifications change
- Quarterly review for relevance

### Update Process

1. Create/modify fixture file
2. Update this README with documentation
3. Update related tests to use the new data
4. Verify all tests pass
5. Commit with descriptive message

---

## Maintenance

### Validation Scripts

Run these commands to validate test data integrity:

```powershell
# Check CSV format validity
dotnet run --project tests/DataValidation -- --check-csv tests/TestData/*.csv

# Verify IBAN checksums
dotnet run --project tests/DataValidation -- --check-iban tests/TestData/*.csv

# Check for duplicate entries
dotnet run --project tests/DataValidation -- --check-duplicates tests/TestData/sample-transactions.csv
```

### Cleanup

Remove unused test data files during sprint retrospectives. Keep this directory lean and well-documented.

---

## Related Documentation

- [Testing Strategy](../../docs/testing-strategy.md) — Overall testing approach
- [Database Schema](../../docs/database-schema.md) — Entity definitions
- [CONTRIBUTING.md](../../CONTRIBUTING.md) — Contribution guidelines

---

**Last Updated:** November 27, 2025  
**Maintained By:** Development Team
