# Post-MVP-4: Enforce Account-Budget Plan Matching

## Objective

Enforce data integrity by validating that transaction splits reference budget lines from budget plans associated with the transaction's account. Uses cached lookup service for performance optimization.

## Requirements

### Data Model Changes

- Enforce `BudgetLineId` as required on `TransactionSplit` (remove nullable)
- Remove `CategoryId` property from `TransactionSplit` (category accessed via `BudgetLine.Category`)
- Database reset allowed (no migration of existing data)

### Validation Infrastructure

- Implement `IBudgetAccountLookupService` with `IMemoryCache` (60-minute TTL, expiration-only invalidation)
- Batch validation optimization for multiple splits using `GetAccountIdsForBudgetLinesAsync(budgetLineIds[])`
- Validate `Transaction.AccountId == BudgetLine.BudgetPlan.AccountId` for all splits

### API Changes

- Update DTOs to replace `CategoryId` with required `BudgetLineId` in assignment requests
- Update FluentValidation validators to perform async account-budget plan consistency checks
- Include account labels in error messages for clarity

### Scope Exclusions

- UI workflow for assignment (separate user story)
- UI warnings for uncategorized transactions (separate user story)

## Cache Configuration

### Registration in Program.cs

```csharp
builder.Services.AddMemoryCache(options =>
{
    options.SizeLimit = 1000; // Maximum 1000 cached entries
    options.CompactionPercentage = 0.25; // Remove 25% when limit reached
});

builder.Services.AddSingleton<ICacheKeyTracker, CacheKeyTracker>();
builder.Services.AddScoped<IBudgetAccountLookupService, BudgetAccountLookupService>();

builder.Services.Configure<CacheOptions>(builder.Configuration.GetSection("Caching"));
```

### Configuration Class (Configuration/CacheOptions.cs)

```csharp
public class CacheOptions
{
    public int AbsoluteExpirationMinutes { get; set; } = 5;
    public int SlidingExpirationMinutes { get; set; } = 2;
    public int SizeLimit { get; set; } = 1000;
}
```

### appsettings.json Configuration

```json
{
  "Caching": {
    "AbsoluteExpirationMinutes": 5,
    "SlidingExpirationMinutes": 2,
    "SizeLimit": 1000
  }
}
```

### Cache Key Pattern

- Pattern: `BudgetPlanValidation:{AccountId}:{CategoryId}`
- Example: `BudgetPlanValidation:3fa85f64-5717-4562-b3fc-2c963f66afa6:8b45e8d2-9c1a-4f3b-a7e1-5d6c9f2e4a3b`
- Absolute expiration: 5 minutes (configurable)
- Sliding expiration: 2 minutes (resets on access)
- Size per entry: 1 (for eviction policy)
- Priority: `CacheItemPriority.Normal` (frequently accessed accounts can use `CacheItemPriority.High`)

### Cache Key Tracking

Since `IMemoryCache` doesn't support pattern-based removal natively, a `ConcurrentDictionary<string, byte>` is used to track active cache keys:

```csharp
public interface ICacheKeyTracker
{
    void AddKey(string key);
    void RemoveKey(string key);
    IEnumerable<string> GetKeysMatchingPattern(string pattern);
    void Clear();
}

public class CacheKeyTracker : ICacheKeyTracker
{
    private readonly ConcurrentDictionary<string, byte> _keys = new();

    public void AddKey(string key) => _keys.TryAdd(key, 0);
    public void RemoveKey(string key) => _keys.TryRemove(key, out _);

    public IEnumerable<string> GetKeysMatchingPattern(string pattern)
    {
        // Pattern uses * as wildcard: "BudgetPlanValidation:account-id:*"
        var regex = new Regex("^" + Regex.Escape(pattern).Replace("\\*", ".*") + "$");
        return _keys.Keys.Where(k => regex.IsMatch(k));
    }

    public void Clear() => _keys.Clear();
}
```

### Cache Entry Creation Example

```csharp
var cacheEntryOptions = new MemoryCacheEntryOptions
{
    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_cacheOptions.AbsoluteExpirationMinutes),
    SlidingExpiration = TimeSpan.FromMinutes(_cacheOptions.SlidingExpirationMinutes),
    Size = 1, // Required when SizeLimit is set
    Priority = CacheItemPriority.Normal
};

_memoryCache.Set(cacheKey, accountId, cacheEntryOptions);
_cacheKeyTracker.AddKey(cacheKey); // Track for pattern-based removal
```

## Cache Invalidation Rules

### Invalidation Triggers (Synchronous)

Cache entries are invalidated **synchronously** when related entities change to ensure immediate consistency:

#### 1. Account.CurrentBudgetPlanId Changes

**Trigger:** When an account switches to a different budget plan  
**Action:** Invalidate all keys matching pattern `BudgetPlanValidation:{AccountId}:*`

```csharp
public async Task UpdateAccountBudgetPlanAsync(Guid accountId, Guid newBudgetPlanId)
{
    var account = await _context.Accounts.FindAsync(accountId);
    account.CurrentBudgetPlanId = newBudgetPlanId;
    await _context.SaveChangesAsync();

    // Invalidate cache synchronously
    _budgetAccountLookupService.InvalidateAccountCache(accountId);
}
```

#### 2. Category.BudgetPlanId Changes

**Trigger:** When a category is moved to a different budget plan  
**Action:** Invalidate all keys matching pattern `BudgetPlanValidation:*:{CategoryId}`

```csharp
public async Task UpdateCategoryBudgetPlanAsync(Guid categoryId, Guid newBudgetPlanId)
{
    var category = await _context.Categories.FindAsync(categoryId);
    category.BudgetPlanId = newBudgetPlanId;
    await _context.SaveChangesAsync();

    // Invalidate cache synchronously
    _budgetAccountLookupService.InvalidateCategoryCache(categoryId);
}
```

#### 3. BudgetPlan Deletion

**Trigger:** When a budget plan is deleted  
**Action:** Clear all cached validation entries (full cache clear)

```csharp
public async Task DeleteBudgetPlanAsync(Guid budgetPlanId)
{
    var budgetPlan = await _context.BudgetPlans.FindAsync(budgetPlanId);
    _context.BudgetPlans.Remove(budgetPlan);
    await _context.SaveChangesAsync();

    // Full cache clear for safety
    _budgetAccountLookupService.ClearAllCache();
}
```

### Implementation in Lookup Service

```csharp
public class BudgetAccountLookupService : IBudgetAccountLookupService
{
    private readonly IMemoryCache _memoryCache;
    private readonly ICacheKeyTracker _cacheKeyTracker;

    public void InvalidateAccountCache(Guid accountId)
    {
        var pattern = $"BudgetPlanValidation:{accountId}:*";
        var keysToRemove = _cacheKeyTracker.GetKeysMatchingPattern(pattern);

        foreach (var key in keysToRemove)
        {
            _memoryCache.Remove(key);
            _cacheKeyTracker.RemoveKey(key);
        }
    }

    public void InvalidateCategoryCache(Guid categoryId)
    {
        var pattern = $"BudgetPlanValidation:*:{categoryId}";
        var keysToRemove = _cacheKeyTracker.GetKeysMatchingPattern(pattern);

        foreach (var key in keysToRemove)
        {
            _memoryCache.Remove(key);
            _cacheKeyTracker.RemoveKey(key);
        }
    }

    public void ClearAllCache()
    {
        var allKeys = _cacheKeyTracker.GetKeysMatchingPattern("BudgetPlanValidation:*");

        foreach (var key in allKeys)
        {
            _memoryCache.Remove(key);
        }

        _cacheKeyTracker.Clear();
    }
}
```

### Why Synchronous Invalidation?

- **Immediate Consistency:** Users see correct validation results immediately after changes
- **Simplicity:** No background job coordination or eventual consistency complexity
- **Low Overhead:** Invalidation is fast (pattern matching + dictionary removal)
- **Future Enhancement:** Async invalidation can be added later if performance becomes an issue

## Data Migration Strategy

**Database recreation is sufficient for this user story.** Existing data migration is out of scope.

### Implementation Approach

1. Developers should **drop and recreate the database** after implementing schema changes
2. Run: `dotnet ef database drop --force` followed by `dotnet ef database update`
3. Seed data will be recreated automatically via `AppDbContext.SeedAsync()` in Development environment

### Migration File Warning

The generated EF Core migration file should include this comment:

```csharp
public partial class EnforceBudgetLineIdRequired : Migration
{
    // WARNING: This migration requires database recreation.
    // Run: dotnet ef database drop --force
    // Then: dotnet ef database update

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Make BudgetLineId required, remove CategoryId
        migrationBuilder.DropColumn(
            name: "CategoryId",
            table: "TransactionSplits");

        migrationBuilder.AlterColumn<Guid>(
            name: "BudgetLineId",
            table: "TransactionSplits",
            nullable: false,
            oldNullable: true);
    }
}
```

### Rationale

- **Simplicity:** No complex data migration logic to maintain
- **MVP Scope:** Early development phase where database reset is acceptable
- **Test Data:** Seed data ensures consistent test environment after reset
- **Future Work:** Production-ready migrations will be implemented in later user stories (US-9 Multi-User Authentication)

## Implementation Tasks

### 1. Cache Infrastructure

- [ ] Register `IMemoryCache` with `MemoryCacheOptions.SizeLimit = 1000` and `CompactionPercentage = 0.25` in `Program.cs`
- [ ] Create `Configuration/CacheOptions.cs` with properties: `AbsoluteExpirationMinutes` (default 5), `SlidingExpirationMinutes` (default 2), `SizeLimit` (default 1000)
- [ ] Add `Caching` section to `appsettings.json` with `AbsoluteExpirationMinutes: 5`, `SlidingExpirationMinutes: 2`, `SizeLimit: 1000`
- [ ] Create `ICacheKeyTracker` interface and `CacheKeyTracker` implementation using `ConcurrentDictionary<string, byte>` for pattern-based key tracking
- [ ] Register `ICacheKeyTracker` as singleton service in `Program.cs`
- [ ] Register `IBudgetAccountLookupService` as scoped service in `Program.cs`
- [ ] Bind `CacheOptions` from `appsettings.json:Caching` section using `IOptions<CacheOptions>` pattern

### 2. Data Model Updates

- [ ] Update `Models/TransactionSplit.cs`: Make `BudgetLineId` required, remove `CategoryId` property
- [ ] Update `Data/AppDbContext.cs`: Configure `BudgetLineId` as required FK, remove `CategoryId` relationship
- [ ] Update `DTOs/TransactionDTOs.cs`: Replace `CategoryId` with required `BudgetLineId` in `AssignTransactionRequest` and `SplitAllocationDto`
- [ ] Generate new EF Core migration for schema changes

### 3. Lookup Service Implementation

- [ ] Create `Services/BudgetAccountLookupService.cs` implementing `IBudgetAccountLookupService`
- [ ] Inject `IMemoryCache`, `ICacheKeyTracker`, `IOptions<CacheOptions>`, and repository dependencies
- [ ] Implement `GetAccountIdForBudgetLineAsync(Guid budgetLineId)` with caching (key pattern: `BudgetPlanValidation:{accountId}:{categoryId}`)
- [ ] Implement `GetAccountIdsForBudgetLinesAsync(IEnumerable<Guid> budgetLineIds)` for batch queries
- [ ] Batch method queries uncached IDs in single database call and populates cache
- [ ] Set cache entry options: `Size = 1`, `Priority = CacheItemPriority.Normal`, absolute/sliding expiration from `CacheOptions`
- [ ] Track cache keys using `ICacheKeyTracker.AddKey()` on each cache write
- [ ] Implement `InvalidateAccountCache(Guid accountId)` method: use `ICacheKeyTracker.GetKeysMatchingPattern($"BudgetPlanValidation:{accountId}:*")` to find and remove all matching keys
- [ ] Implement `InvalidateCategoryCache(Guid categoryId)` method: use pattern `BudgetPlanValidation:*:{categoryId}` to find and remove matching keys
- [ ] Implement `ClearAllCache()` method: clear all keys matching `BudgetPlanValidation:*` from both `IMemoryCache` and `ICacheKeyTracker`
- [ ] (Optional) Implement priority-based eviction: use `CacheItemPriority.High` for frequently accessed account validations

### 4. Repository Extensions

- [ ] Add `GetByIdWithAccountAsync()` to `Data/Repositories/TransactionRepository.cs` (eager-load Account)
- [ ] Add `GetAccountMappingsAsync(IEnumerable<Guid> budgetLineIds)` to `Data/Repositories/BudgetLineRepository.cs`
- [ ] Returns `Dictionary<Guid, Guid>` mapping BudgetLineId → AccountId using single query

### 5. Validation Updates

- [ ] Update `DTOs/Validators/TransactionValidators.cs`: Inject `IBudgetAccountLookupService`, `ITransactionRepository`, `IAccountRepository`
- [ ] Add async validation rule in `AssignTransactionRequestValidator` checking account-budget plan consistency
- [ ] Add async validation rule in `SplitAllocationDtoValidator` using batch lookup for split collections
- [ ] Include account labels in error messages: "Category '{CategoryName}' belongs to budget plan for account '{AccountLabel}', but transaction is for account '{TransactionAccountLabel}'"
- [ ] Update `Services/TransactionAssignmentService.cs`: Add validation in `AssignToSingleAsync()` and `SplitAsync()` methods
- [ ] Throw `InvalidOperationException` with descriptive messages on validation failure

### 6. Query Updates

- [ ] Update all `TransactionSplit` queries to eager-load `BudgetLine.Category` instead of separate `Category` navigation
- [ ] Update `TransactionSplitRepository` queries to use new relationship path

### 7. Testing

#### Unit Tests

- [ ] Create `LocalFinanceManager.Tests/Services/BudgetAccountLookupServiceTests.cs`
- [ ] Test cache hit scenario: Call `GetAccountIdForBudgetLineAsync()` twice, verify second call doesn't hit database
- [ ] Test cache miss scenario: Call with new budget line ID, verify database query executed
- [ ] Test batch query optimization: Call `GetAccountIdsForBudgetLinesAsync()` with 50 IDs, verify single database query
- [ ] Test cache key tracking: Verify `ICacheKeyTracker.AddKey()` called on cache writes
- [ ] Test `InvalidateAccountCache()`: Verify pattern matching removes correct keys
- [ ] Test `InvalidateCategoryCache()`: Verify suffix pattern matching works
- [ ] Test `ClearAllCache()`: Verify all cache entries and tracked keys removed

#### Integration Tests

- [ ] Create `LocalFinanceManager.Tests/Integration/CrossAccountValidationTests.cs`
- [ ] Test validator rejection: Assign budget line from Account A to transaction on Account B, verify HTTP 400 with descriptive error message
- [ ] Test service-level rejection: Call `TransactionAssignmentService.AssignToSingleAsync()` with mismatched account, verify `InvalidOperationException` thrown
- [ ] Test error message format: Verify error includes account labels (e.g., "Category 'Groceries' belongs to budget plan for account 'Savings Account', but transaction is for 'Checking Account'")
- [ ] Test split validation: Assign 10 splits with mixed valid/invalid budget lines, verify all invalid splits reported in single error response
- [ ] Test `BudgetLineId` required constraint: Attempt to save `TransactionSplit` with null `BudgetLineId`, verify database constraint violation

#### Performance Tests

- [ ] Create `LocalFinanceManager.Tests/Services/BudgetAccountLookupServicePerformanceTests.cs`
- [ ] **Setup:** Seed test database with:
  - 10 accounts (each with unique budget plan)
  - 50 categories per budget plan (500 total categories)
  - 1000 transactions across all accounts (100 per account)
- [ ] **Test Scenario:** Execute 10,000 validation calls with 50/50 cache hit/miss ratio:
  - Warm up cache: Call `GetAccountIdForBudgetLineAsync()` for 250 random budget lines (50% of total)
  - Performance test: Call for 10,000 random budget lines (expect ~50% cache hits)
- [ ] **Measurements (using `Stopwatch`):**
  - Cache hit rate: Count hits vs misses, verify **>95% hit rate** after warmup
  - Cache hit latency: Measure p95 (95th percentile), verify **<5ms**
  - Cache miss latency: Measure p95 for database queries, verify **<50ms**
  - Batch validation: Call `GetAccountIdsForBudgetLinesAsync()` with 100 budget line IDs, verify **<100ms total**
- [ ] **Assertion Examples:**

  ```csharp
  var hitLatencies = /* collect hit latencies */;
  var p95HitLatency = hitLatencies.OrderBy(x => x).ElementAt((int)(hitLatencies.Count * 0.95));
  Assert.That(p95HitLatency.TotalMilliseconds, Is.LessThan(5), "Cache hit p95 latency");

  var cacheHitRate = (double)cacheHits / totalCalls;
  Assert.That(cacheHitRate, Is.GreaterThan(0.95), "Cache hit rate");
  ```

- [ ] **Tools:** Use `System.Diagnostics.Stopwatch` for measurement (BenchmarkDotNet optional for advanced profiling post-MVP)

## Validation Rules

1. `TransactionSplit.BudgetLineId` is **required** (cannot be null)
2. `BudgetLine.BudgetPlan.AccountId` MUST equal `Transaction.AccountId` for all splits
3. Validation performed at both DTO validator level (FluentValidation) and service level
4. Batch validation used for multiple splits to optimize performance

## Technical Specifications

### Cache Configuration

See **Cache Configuration** section above for complete setup details including:

- `IMemoryCache` registration with size limits
- `CacheOptions` configuration class
- `appsettings.json` configuration
- `ICacheKeyTracker` for pattern-based invalidation

### Cache Key Pattern

- Pattern: `BudgetPlanValidation:{AccountId}:{CategoryId}`
- Absolute expiration: 5 minutes (configurable via `CacheOptions.AbsoluteExpirationMinutes`)
- Sliding expiration: 2 minutes (configurable via `CacheOptions.SlidingExpirationMinutes`)
- Size limit: 1000 entries (configurable via `CacheOptions.SizeLimit`)
- Thread-safe via `IMemoryCache` and `ConcurrentDictionary<string, byte>`

### Error Response Format (RFC 7231)

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Validation error",
  "status": 400,
  "detail": "Category assignment validation failed",
  "errors": {
    "BudgetLineId": [
      "Category 'Groceries' belongs to budget plan '2026 Household Budget' for account 'Savings Account', but transaction is associated with 'Checking Account'"
    ]
  }
}
```

### Performance Targets

- Cache hit p95 latency: <5ms per validation
- Cache miss p95 latency: <50ms per validation (includes database query)
- Batch validation (100 splits): <100ms total
- Cache hit rate: >95% after warmup (measured in performance tests)
- Database query reduction: ~95% with >95% cache hit rate
- Cache invalidation latency: <10ms for pattern-based removal (synchronous)

## Success Criteria

- `TransactionSplit.BudgetLineId` is required (database constraint enforced)
- Cannot assign budget lines from wrong account's budget plan
- Validation enforced at both validator and service layers
- Error messages include account and budget plan labels for clarity
- Cache reduces database queries by ~98%
- Batch validation performs efficiently for bulk operations
- All unit and integration tests pass
