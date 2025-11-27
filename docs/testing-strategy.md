# Testing Strategy — LocalFinanceManager

## Overview

This document outlines the comprehensive testing approach for the LocalFinanceManager application. Our strategy emphasizes automated testing at multiple levels to ensure reliability, maintainability, and confidence in the financial calculations and data integrity.

---

## Testing Principles

1. **Test Early, Test Often** — Tests are written alongside implementation
2. **Isolation** — Unit tests don't depend on external resources (database, filesystem)
3. **Fast Feedback** — Quick test execution to support rapid development
4. **Confidence in Financials** — Extra scrutiny on money calculations and data integrity
5. **TDD-Friendly** — Tests can be written before implementation when appropriate

---

## Test Levels

### 1. Unit Tests

**Scope:** Individual classes, methods, and business logic in isolation

**Tools:**

- xUnit
- Moq (mocking framework)
- FluentAssertions

**Coverage Areas:**

#### Domain Layer

- Entity validation rules
- Value object behavior
- Domain model invariants

#### Application Layer

- **ScoringEngine** — Category suggestion calculations
  - Word frequency scoring
  - IBAN pattern matching
  - Amount bucket clustering
  - Recurrence pattern detection
  - Score combination formulas
- **RuleEngine** — Rule evaluation logic
  - Pattern matching (regex, contains, exact)
  - Priority ordering
  - Multiple rule application
- **DeduplicationService** — Transaction duplicate detection
  - Hash computation (Date + Amount + Description + Account)
  - Threshold-based matching
  - Edge cases (same-day similar amounts)
- **BudgetService** — Budget calculations
  - Monthly spent vs. budget
  - Category rollups
  - Envelope allocations
- **LearningService** — Profile updates
  - Frequency increment logic
  - Profile normalization
- **Validators** (FluentValidation)
  - Transaction validation rules
  - Account validation
  - Category/Envelope validation

#### Infrastructure Layer

- Repository implementations (using in-memory SQLite or EF Core InMemory provider)
- Import parsers (CSV, JSON, MT940)
- Data mapping/conversion logic

**Example Test Structure:**

```csharp
public class ScoringEngineTests
{
    [Fact]
    public void ComputeScore_WithMatchingWords_ReturnsHighScore()
    {
        // Arrange
        var profile = CreateProfileWithWords("groceries", "supermarket");
        var transaction = CreateTransaction("Albert Heijn supermarket");
        var engine = new ScoringEngine();

        // Act
        var score = engine.ComputeScore(transaction, profile);

        // Assert
        score.Should().BeGreaterThan(0.7);
    }
}
```

---

### 2. Integration Tests

**Scope:** Multiple components working together, including database interactions

**Tools:**

- xUnit
- SQLite (in-memory or temporary file-based)
- EF Core
- WebApplicationFactory (for API testing)

**Coverage Areas:**

#### Repository Integration

- CRUD operations with actual EF Core context
- Transaction integrity
- Concurrency handling
- Complex queries

#### End-to-End Workflows

- Import → Dedupe → Auto-categorize pipeline
- Manual transaction entry → Learning update
- Rule application → Transaction categorization
- Budget calculation with real transaction data

#### Database Migrations

- Migration application success
- Schema validation
- Data seeding

**Example:**

```csharp
public class TransactionRepositoryIntegrationTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly ITransactionRepository _repository;

    public TransactionRepositoryIntegrationTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite("DataSource=:memory:")
            .Options;
        _context = new ApplicationDbContext(options);
        _context.Database.OpenConnection();
        _context.Database.EnsureCreated();
        _repository = new EfTransactionRepository(_context);
    }

    [Fact]
    public async Task AddTransaction_SavesOriginalCsv()
    {
        // Arrange
        var transaction = new Transaction
        {
            Description = "Test",
            OriginalCsv = "2025-01-01,100,Test,NL01BANK..."
        };

        // Act
        await _repository.AddAsync(transaction);
        var retrieved = await _repository.GetByIdAsync(transaction.Id);

        // Assert
        retrieved.OriginalCsv.Should().Be(transaction.OriginalCsv);
    }

    public void Dispose() => _context.Dispose();
}
```

---

### 3. UI Tests (Blazor Components)

**Scope:** Blazor component rendering, user interactions, and UI logic

**Tools:**

- bUnit (Blazor component testing)
- AngleSharp (HTML parsing/assertions)

**Coverage Areas:**

- Transaction entry form validation
- Import preview display
- Budget dashboard calculations
- Category selection dropdowns
- Split transaction UI

**Example:**

```csharp
public class TransactionAddPageTests : TestContext
{
    [Fact]
    public void AddTransactionForm_ValidInput_EnablesSubmit()
    {
        // Arrange
        var component = RenderComponent<TransactionAdd>();

        // Act
        component.Find("#amount").Change("100.50");
        component.Find("#description").Change("Groceries");

        // Assert
        var submitButton = component.Find("button[type=submit]");
        submitButton.IsDisabled().Should().BeFalse();
    }
}
```

---

### 4. End-to-End Tests (Optional)

**Scope:** Complete user workflows through the UI

**Tools:**

- Playwright or Selenium
- xUnit as test runner

**Coverage Areas:**

- Import CSV → Review → Confirm workflow
- Manual categorization → Learning feedback
- Budget setup → Transaction posting → Budget update
- Rule creation → Automatic application

**Note:** E2E tests are slower and more brittle. Focus on critical happy paths and edge cases that can't be caught by lower-level tests.

---

## Test Data Management

### Fixtures & Test Data Location

All test data is stored in `tests/TestData/` with the following structure:

```
tests/TestData/
├── README.md                    # Documentation (see separate file)
├── sample-transactions.csv      # Standard CSV import format
├── sample-transactions.json     # JSON import format
├── sample-transactions-mt940.txt # MT940 format
├── duplicate-transactions.csv   # For deduplication testing
├── edge-cases.csv              # Boundary values, special characters
└── learning-profiles.json      # Pre-trained category profiles
```

### Test Data Builders

Use the Builder pattern for complex domain objects:

```csharp
public class TransactionBuilder
{
    private Transaction _transaction = new();

    public TransactionBuilder WithAmount(decimal amount)
    {
        _transaction.Amount = amount;
        return this;
    }

    public Transaction Build() => _transaction;
}

// Usage:
var transaction = new TransactionBuilder()
    .WithAmount(100)
    .WithDescription("Test")
    .Build();
```

---

## Code Coverage Goals

| Layer                | Target Coverage |
| -------------------- | --------------- |
| Domain               | 90%+            |
| Application Services | 85%+            |
| Infrastructure       | 70%+            |
| Web/UI               | 60%+            |
| **Overall**          | **75%+**        |

**Tools:** Coverlet + ReportGenerator

**Commands:**

```powershell
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura
reportgenerator -reports:coverage.cobertura.xml -targetdir:coverage-report
```

---

## Continuous Integration

### PR Checks (GitHub Actions / Azure DevOps)

1. Build all projects
2. Run all unit tests
3. Run integration tests
4. Generate coverage report
5. Enforce minimum coverage thresholds
6. Run code quality checks (dotnet format, StyleCop)

### Required for Merge

- ✅ All tests passing
- ✅ Coverage meets thresholds
- ✅ No build warnings
- ✅ Code review approved

---

## Testing Workflow

### When Adding a New Feature

1. **Write Tests First (TDD)** — Define expected behavior
2. **Implement Minimal Code** — Make tests pass
3. **Refactor** — Improve code while keeping tests green
4. **Add Integration Tests** — Verify component interactions
5. **Manual Testing** — UI smoke test
6. **Update Coverage** — Check and document

### When Fixing a Bug

1. **Write Failing Test** — Reproduce the bug
2. **Fix the Bug** — Make test pass
3. **Verify** — Ensure related tests still pass
4. **Document** — Add comment explaining edge case

---

## Special Testing Considerations

### Financial Accuracy

- Use `decimal` type for all money calculations (never `float` or `double`)
- Test rounding behavior explicitly
- Verify transaction splits sum to original amount
- Test edge cases: zero amounts, negative amounts, very large amounts

### Deduplication Logic

- Test identical transactions
- Test similar but non-duplicate transactions
- Test hash collisions (if any)
- Test across multiple accounts

### Learning Engine

- Test profile updates after manual corrections
- Test score degradation over time (if implemented)
- Test cold-start scenarios (no training data)
- Test with conflicting patterns

### Data Import

- Test various CSV formats (different delimiters, encodings)
- Test malformed input (missing columns, invalid dates)
- Test large file imports (performance)
- Verify `OriginalCsv` preservation

---

## Performance Testing (Future)

### Load Testing

- Import 10,000+ transactions
- Calculate budgets for 12-month period
- Score 1,000 uncategorized transactions

### Benchmark Tools

- BenchmarkDotNet for critical algorithms
- SQLite performance profiling

---

## Accessibility & Usability Testing

### Manual Checks

- Keyboard navigation
- Screen reader compatibility (ARIA labels)
- Color contrast (WCAG AA)
- Responsive design (mobile/tablet/desktop)

---

## Testing Anti-Patterns to Avoid

❌ **Don't** test implementation details — test behavior  
❌ **Don't** rely on test execution order  
❌ **Don't** use `Thread.Sleep` — use proper async patterns  
❌ **Don't** share mutable state between tests  
❌ **Don't** mock what you don't own (e.g., EF Core DbContext)  
❌ **Don't** write tests without clear assertions

---

## Resources

- [xUnit Documentation](https://xunit.net/)
- [bUnit Documentation](https://bunit.dev/)
- [FluentAssertions Documentation](https://fluentassertions.com/)
- [EF Core Testing Guide](https://learn.microsoft.com/en-us/ef/core/testing/)

---

## Maintenance

This testing strategy should be reviewed and updated:

- At the start of each sprint
- When major architectural changes occur
- When new testing tools or patterns are adopted
- When coverage goals are consistently not met

**Last Updated:** November 27, 2025  
**Owned By:** Development Team
