# 🏦 Personal Finance Manager – Local WebApp (.NET 10)

**Description:**
LocalFinanceManager is a locally hosted web application (ASP.NET Core 10, Blazor Server) for managing personal finances. The app supports multiple accounts, budgets, envelopes, transaction import, and score-based automatic categorization. All data is stored locally in SQLite.

**Audience:** Anyone who wants to manage personal finances locally without cloud dependence.

**Technology:**

- Backend: **.NET 10 / ASP.NET Core**
- Frontend: **Blazor Server / Razor Components**
- Database: **SQLite**
- Scaffolding: **dotnet CLI** (`dotnet new`, `dotnet ef migrations add`, `dotnet ef database update`, etc.)

---

## 🚀 Quick Start

Follow these steps on first use:

1. Create a budget plan per account number.
   - Create an `Account` and define the monthly budgets for that account, either per category or per account.
2. Import bank transactions for that account(s) via CSV, TSV, MT940, or JSON.
3. Assign imported transactions to existing budget categories or envelopes, and allocate amounts.
4. Review automatic categorization suggestions and correct them where needed — the scoring model learns from your corrections and improves future categorizations.

Note: Budgets attached to a specific `Account` (account number) provide per-account limits; category budgets track spending across all accounts.

---

## 🎯 Features

### 1. Account management

- Multiple accounts (checking, savings, credit card, cash)
- Opening balance + transactions
- Balance auto-calculated
- Archive/hide accounts

### 2. Transaction management

- Add transactions manually
- Import transactions via CSV, TSV, MT940, or JSON
- Deduplication on import
- Split transactions across multiple categories/envelopes
- **Store original CSV string with each transaction**

### 3. Score-based automatic categorization

- Learns from previously manually categorized transactions
- Scoring based on:

  - Words in the description
  - Counterparty / IBAN
  - Amount clusters
  - Recurring patterns (monthly/weekly payments)

- Score formula picks the most likely category
- User can correct suggestions → model updates
- Uncertainty threshold: if score is low, the system asks the user to confirm

### 4. Budgets

- Monthly budgets per category and optionally per account
- Compare planned vs actual spending (scopes: category / envelope / account)
- Visual progress bars per category or per account
- Budgets can be created with scope: `Category`, `Envelope`, or `Account`.
  - **Category budgets** track spending per category across all accounts.
  - **Envelope budgets** track allocations to envelopes.
  - **Account budgets** track all transactions for a specific account (optionally filtered by category).
- Display & priority: when both account and category budgets exist, the UI shows both; account budgets provide per-account limits while category budgets help category-driven planning.

### 5. Envelopes

- Create envelopes: “Cash”, “Groceries”, “Vacation”, etc.
- Monthly automatic allocation
- Link transactions to envelopes
- Dashboard with remaining balance per envelope

### 6. Dashboards & Reporting

- Monthly income / expense overview
- Yearly overview with trends
- Charts: pie, bar, line
- Category distribution & envelope status

### 7. Local storage & privacy

- SQLite database stored locally
- Optional AES-256 encryption
- No cloud connections
- Automatic backups to local files

---

## 🏛️ Architecture

### Backend

- ASP.NET Core 10
- EF Core 10 + SQLite provider
- Clean Architecture:

  - **Domain:** Entities, Aggregates
  - **Application:** Services (TransactionService, CategoryService, RuleEngineService, BudgetService)
  - **Infrastructure:** Repositories, EF Core migrations
  - **Presentation:** Blazor UI

### Frontend

- Blazor Server (recommended)
- Alternative: Razor Pages or React + minimal APIs

### Dependency Injection

- All services registered via the DI container (`builder.Services.AddScoped<...>`)

---

## 📦 Data model

```csharp
public class Account {
    public int Id { get; set; }
    public string Name { get; set; }
    public string AccountType { get; set; }
    public decimal InitialBalance { get; set; }
    public bool IsActive { get; set; }
}

public class Transaction {
    public int Id { get; set; }
    public int AccountId { get; set; }
    public DateTime Date { get; set; }
    public decimal Amount { get; set; }
    public string Description { get; set; }
    public int CategoryId { get; set; }
    public int? EnvelopeId { get; set; }
    public List<string> Tags { get; set; } = new();
    public string OriginalCsv { get; set; } // original CSV string
}

public class Category {
    public int Id { get; set; }
    public string Name { get; set; }
    public int? ParentCategoryId { get; set; }
    public decimal MonthlyBudget { get; set; }
}

public class Envelope {
    public int Id { get; set; }
    public string Name { get; set; }
    public decimal Balance { get; set; }
    public decimal MonthlyAllocation { get; set; }
}

public class Rule {
    public int Id { get; set; }
    public string MatchType { get; set; }
    public string Pattern { get; set; }
    public int? TargetCategoryId { get; set; }
    public int? TargetEnvelopeId { get; set; }
    public List<string> AddLabels { get; set; } = new();
    public int Priority { get; set; }
}

public class CategoryLearningProfile {
    public int Id { get; set; }
    public int CategoryId { get; set; }
    public Dictionary<string,int> WordFrequency { get; set; } = new();
    public Dictionary<string,int> IbanFrequency { get; set; } = new();
    public Dictionary<string,int> AmountBucketFrequency { get; set; } = new();
    public Dictionary<string,int> RecurrenceFrequency { get; set; } = new();
}

public class Budget {
    public int Id { get; set; }
    public int? CategoryId { get; set; }    // Budget scoped to a category (optional)
    public int? EnvelopeId { get; set; }    // Budget scoped to an envelope (optional)
    public int? AccountId { get; set; }     // NEW: Budget scoped to a specific account (optional)
    public DateTime Month { get; set; }     // Which month this budget applies to (e.g. 2025-01-01)
    public decimal PlannedAmount { get; set; }
    public decimal ActualAmount { get; set; }
}
```

---

## 🔧 Technology & Libraries

- **.NET 10 / ASP.NET Core**
- **Blazor Server / Razor Components**
- **Entity Framework Core 10** + SQLite
- **AutoMapper**
- **FluentValidation**
- **MediatR** (optional)
- **Chart.js** integration or Blazor Charts component
- **Hangfire Lite** (optional for scheduled tasks)

---

## 🔨 Scaffolding via dotnet CLI

- `dotnet new webapp -n LocalFinanceManager`
- `dotnet new classlib` (for services and domain)
- `dotnet ef migrations add InitialCreate`
- `dotnet ef database update`
- `dotnet new razorcomponent` (UI scaffolding)

Copilot can generate scaffolding templates for these steps.

---

## 🧪 Testing strategy

- Unit tests for RuleEngine & BudgetEngine
- Integration tests with SQLite + EF Core
- UI tests via Playwright or BUnit for Blazor

---

## 🚀 Future enhancements

- PSD2 integration (local token)
- Export to Excel/PDF
- Multi-user (offline auth)
- Offline-first PWA option
- Plugin system for extensions
