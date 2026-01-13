# Implementation Guidelines

## Overview

This document specifies technical decisions for LocalFinanceManager development. All MVPs (0-6) must follow these guidelines.

---

## Technology Stack

| Component | Choice | Details |
|-----------|--------|---------|
| **.NET Version** | net10.0 | Latest, modern features |
| **Logging** | Built-in ILogger | Use Microsoft.Extensions.Logging |
| **Error Responses** | RFC 7231 Problem Details | Standard API error format |
| **Configuration** | appsettings.json + environment-specific | IOptions<T> for DI |
| **CORS** | Not needed | No cross-origin calls required (Blazor Server, same-origin) |
| **Async Patterns** | Async all the way | All I/O operations async/await |
| **DI Conventions** | Scoped services, I<Name> interfaces | See conventions below |
| **Database** | SQLite file | `localfinancemanager.db` in project root |
| **Validation Errors** | Standard RFC 7231 format | See error response examples below |
| **Code Style** | Nullable enabled, warnings not-as-errors | Modern C# safety features |

---

## .NET Version (net10.0)

- **Target Framework:** `<TargetFramework>net10.0</TargetFramework>` in all `.csproj` files
- **Language Version:** default (C# 14 or latest)
- **Nullable Reference Types:** Enabled in main project and libraries (`<Nullable>enable</Nullable>`)

---

## Logging Strategy (Built-in ILogger)

Use Microsoft.Extensions.Logging (ships with .NET):

```csharp
// In Program.cs
builder.Services.AddLogging(config =>
{
    config.AddConsole();
    // Add file logging if needed later
});

// In services
public class AccountService
{
    private readonly ILogger<AccountService> _logger;

    public AccountService(ILogger<AccountService> logger)
    {
        _logger = logger;
    }

    public async Task<Account> CreateAccountAsync(CreateAccountRequest request)
    {
        _logger.LogInformation("Creating account: {Label}", request.Label);
        // implementation
    }
}
```

**Log Levels:**
- `LogError`: Exceptions, data integrity issues
- `LogWarning`: Validation failures, edge cases
- `LogInformation`: Operation starts/completions
- `LogDebug`: State changes, data values (dev only)

---

## Error Response Format (RFC 7231 Problem Details)

All API errors return standardized JSON structure:

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "detail": "The request contains invalid data. See errors object.",
  "errors": {
    "Label": ["Label is required"],
    "IBAN": ["Invalid IBAN format"]
  }
}
```

**Status codes by scenario:**
- `200 OK`: Successful GET/PUT/DELETE
- `201 Created`: Successful POST
- `204 No Content`: Successful DELETE with no response body
- `400 Bad Request`: Validation failures, malformed request
- `404 Not Found`: Resource not found
- `409 Conflict`: RowVersion mismatch (optimistic concurrency)
- `500 Internal Server Error`: Unhandled exceptions

**Exception handling middleware example:**

```csharp
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var exceptionHandlerPathFeature = context.Features.Get<IExceptionHandlerPathFeature>();
        var exception = exceptionHandlerPathFeature?.Error;

        context.Response.ContentType = "application/problem+json";

        if (exception is FluentValidation.ValidationException validationEx)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new
            {
                type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                title = "One or more validation errors occurred.",
                status = 400,
                errors = validationEx.Errors.GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())
            });
        }
        else if (exception is DbUpdateConcurrencyException concurrencyEx)
        {
            context.Response.StatusCode = StatusCodes.Status409Conflict;
            await context.Response.WriteAsJsonAsync(new
            {
                type = "https://tools.ietf.org/html/rfc7231#section-6.4.9",
                title = "Conflict - Resource was modified.",
                status = 409,
                detail = "The resource was modified by another request. Please reload and try again."
            });
        }
        else
        {
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsJsonAsync(new
            {
                type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                title = "Internal Server Error",
                status = 500
            });
        }
    });
});
```

---

## Configuration (appsettings.json + Environment-Specific)

**File structure:**
```
LocalFinanceManager/
├── appsettings.json              (shared config)
├── appsettings.Development.json  (dev overrides)
├── appsettings.Production.json   (prod overrides)
└── Program.cs
```

**appsettings.json example:**

```json
{
  "ConnectionStrings": {
    "Default": "Data Source=localfinancemanager.db"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning"
    }
  },
  "Features": {
    "AutoApplyEnabled": false,
    "AutoApplyConfidenceThreshold": 0.85,
    "UndoRetentionDays": 30,
    "MaxImportFileSizeMB": 100
  }
}
```

**appsettings.Development.json:**

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft": "Information"
    }
  },
  "Features": {
    "AutoApplyEnabled": false
  }
}
```

**Program.cs usage with IOptions<T>:**

```csharp
var builder = WebApplication.CreateBuilder(args);

// Configuration automatically loaded from appsettings.json + environment-specific
builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

// Register strongly-typed options
builder.Services.Configure<FeaturesOptions>(builder.Configuration.GetSection("Features"));
builder.Services.AddScoped(sp => sp.GetRequiredService<IOptions<FeaturesOptions>>().Value);
```

**FeaturesOptions class:**

```csharp
public class FeaturesOptions
{
    public bool AutoApplyEnabled { get; set; }
    public decimal AutoApplyConfidenceThreshold { get; set; }
    public int UndoRetentionDays { get; set; }
    public int MaxImportFileSizeMB { get; set; }
}
```

---

## No CORS

Blazor Server runs same-origin as the API, so CORS not needed. If external API clients are added later:

```csharp
// Only add if needed
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocalhost", policy =>
    {
        policy.WithOrigins("https://localhost:5001")
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

app.UseCors("AllowLocalhost");
```

For now, omit CORS middleware entirely.

---

## Async Patterns

All I/O operations must be async:

**Repository pattern (async):**

```csharp
public interface IRepository<T> where T : BaseEntity
{
    Task<T?> GetByIdAsync(Guid id);
    Task<List<T>> GetActiveAsync();
    Task AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(Guid id);
}

public class Repository<T> : IRepository<T> where T : BaseEntity
{
    private readonly AppDbContext _context;

    public async Task<T?> GetByIdAsync(Guid id)
    {
        return await _context.Set<T>()
            .Where(x => !x.IsArchived)
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task AddAsync(T entity)
    {
        await _context.Set<T>().AddAsync(entity);
        await _context.SaveChangesAsync();
    }
}
```

**Service pattern (async):**

```csharp
public interface IAccountService
{
    Task<AccountDto> CreateAccountAsync(CreateAccountRequest request);
    Task<List<AccountDto>> GetAccountsAsync();
}

public class AccountService : IAccountService
{
    private readonly IRepository<Account> _repository;

    public async Task<AccountDto> CreateAccountAsync(CreateAccountRequest request)
    {
        // validation, business logic
        var account = new Account { ... };
        await _repository.AddAsync(account);
        return _mapper.Map<AccountDto>(account);
    }

    public async Task<List<AccountDto>> GetAccountsAsync()
    {
        var accounts = await _repository.GetActiveAsync();
        return _mapper.Map<List<AccountDto>>(accounts);
    }
}
```

**Controller pattern (async):**

```csharp
[ApiController]
[Route("api/[controller]")]
public class AccountsController : ControllerBase
{
    private readonly IAccountService _service;

    [HttpPost]
    public async Task<IActionResult> CreateAccount([FromBody] CreateAccountRequest request)
    {
        var result = await _service.CreateAccountAsync(request);
        return CreatedAtAction(nameof(GetAccount), new { id = result.Id }, result);
    }
}
```

**Program.cs Main signature:**

```csharp
var app = builder.Build();
await app.RunAsync();  // Async main
```

---

## Dependency Injection Conventions

**Service naming:**
- Interfaces: `IAccountRepository`, `IAccountService`, `IMLService`
- Implementations: `AccountRepository`, `AccountService`, `MLService`

**Lifetime management:**
- **Scoped** (default for most services): Created per HTTP request
- **Singleton**: Shared across requests (configuration, logging factories)
- **Transient**: New instance every time (rarely used)

**Registration pattern:**

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<IAccountRepository, AccountRepository>();
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<IMLService, MLService>();

// DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Default")));

// Background jobs (singleton)
builder.Services.AddSingleton<IHostedService, MLRetrainingBackgroundService>();

var app = builder.Build();
```

---

## Database (SQLite File)

**Connection string:**

```json
{
  "ConnectionStrings": {
    "Default": "Data Source=localfinancemanager.db"
  }
}
```

**AppDbContext configuration:**

```csharp
protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
{
    if (!optionsBuilder.IsConfigured)
    {
        optionsBuilder.UseSqlite("Data Source=localfinancemanager.db");
    }
}
```

**Migrations at startup:**

```csharp
// Program.cs
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await dbContext.Database.MigrateAsync();
    
    if (app.Environment.IsDevelopment())
    {
        await dbContext.SeedAsync();
    }
}

await app.RunAsync();
```

**Database file location:** `localfinancemanager.db` in project root (same directory as `.csproj`)

---

## Validation Error Responses

**Standard format (RFC 7231 with property errors):**

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "detail": "The request contains invalid data.",
  "errors": {
    "Label": [
      "Label is required"
    ],
    "IBAN": [
      "Invalid IBAN format"
    ]
  }
}
```

**FluentValidation integration:**

```csharp
// In Program.cs
builder.Services
    .AddFluentValidationAutoValidation()
    .AddValidatorsFromAssemblyContaining<Program>();

// Custom validator
public class CreateAccountRequestValidator : AbstractValidator<CreateAccountRequest>
{
    public CreateAccountRequestValidator()
    {
        RuleFor(x => x.Label)
            .NotEmpty().WithMessage("Label is required")
            .MaximumLength(100).WithMessage("Label cannot exceed 100 characters");

        RuleFor(x => x.IBAN)
            .NotEmpty().WithMessage("IBAN is required")
            .Must(ValidateIban).WithMessage("Invalid IBAN format");
    }

    private bool ValidateIban(string iban)
    {
        // Use IbanNet library
        var ibanValidator = new IbanValidator();
        return ibanValidator.Validate(iban).IsValid;
    }
}
```

---

## Code Style

**Nullable Reference Types:** Enabled

```csharp
<PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
</PropertyGroup>
```

**Example with nullability:**

```csharp
public class Account : BaseEntity
{
    public required string Label { get; set; }  // Non-nullable, required
    public string? Notes { get; set; }          // Nullable, optional
}

public async Task<Account?> GetAccountAsync(Guid id)  // May return null
{
    return await _repository.GetByIdAsync(id);
}
```

**Warnings as errors:** Disabled (pragmatism over strictness for MVPs)

```csharp
<!-- Disabled for now, can enable later -->
<!-- <TreatWarningsAsErrors>true</TreatWarningsAsErrors> -->
```

---

## Summary

All code must follow these guidelines. If any ambiguity arises during implementation, refer back to this document. Update this document if new patterns or decisions emerge.

Last updated: 2026-01-13
