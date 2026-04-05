namespace LocalFinanceManager.DTOs;

public class BackupData
{
    public DateTime ExportedAt { get; set; }
    public string Version { get; set; } = "1.0";
    public List<BackupAccountDto> Accounts { get; set; } = [];
    public List<BackupBudgetPlanDto> BudgetPlans { get; set; } = [];
    public List<BackupCategoryDto> Categories { get; set; } = [];
    public List<BackupBudgetLineDto> BudgetLines { get; set; } = [];
    public List<BackupTransactionDto> Transactions { get; set; } = [];
    public List<BackupTransactionSplitDto> TransactionSplits { get; set; } = [];
}

public class BackupAccountDto
{
    public Guid Id { get; set; }
    public string Label { get; set; } = "";
    public string AccountType { get; set; } = "";
    public string Currency { get; set; } = "";
    public string? IBAN { get; set; }
    public decimal StartingBalance { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class BackupBudgetPlanDto
{
    public Guid Id { get; set; }
    public Guid AccountId { get; set; }
    public int Year { get; set; }
    public string Name { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class BackupCategoryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string CategoryType { get; set; } = "";
    public Guid BudgetPlanId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class BackupBudgetLineDto
{
    public Guid Id { get; set; }
    public Guid BudgetPlanId { get; set; }
    public Guid CategoryId { get; set; }
    public string MonthlyAmountsJson { get; set; } = "[]";
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class BackupTransactionDto
{
    public Guid Id { get; set; }
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }
    public string Description { get; set; } = "";
    public string? Counterparty { get; set; }
    public Guid AccountId { get; set; }
    public string? ExternalId { get; set; }
    public Guid? ImportBatchId { get; set; }
    public string? SourceFileName { get; set; }
    public DateTime? ImportedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class BackupTransactionSplitDto
{
    public Guid Id { get; set; }
    public Guid TransactionId { get; set; }
    public Guid BudgetLineId { get; set; }
    public decimal Amount { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public enum ConflictResolutionStrategy
{
    Merge,
    Overwrite,
    Skip
}

public class RestoreRequest
{
    public BackupData Backup { get; set; } = null!;
    public ConflictResolutionStrategy Strategy { get; set; } = ConflictResolutionStrategy.Merge;
}

public class BackupRestoreResultDto
{
    public bool Success { get; set; }
    public int AccountsImported { get; set; }
    public int AccountsUpdated { get; set; }
    public int AccountsSkipped { get; set; }
    public int BudgetPlansImported { get; set; }
    public int BudgetPlansUpdated { get; set; }
    public int BudgetPlansSkipped { get; set; }
    public int CategoriesImported { get; set; }
    public int CategoriesUpdated { get; set; }
    public int CategoriesSkipped { get; set; }
    public int BudgetLinesImported { get; set; }
    public int BudgetLinesUpdated { get; set; }
    public int BudgetLinesSkipped { get; set; }
    public int TransactionsImported { get; set; }
    public int TransactionsUpdated { get; set; }
    public int TransactionsSkipped { get; set; }
    public int TransactionSplitsImported { get; set; }
    public int TransactionSplitsUpdated { get; set; }
    public int TransactionSplitsSkipped { get; set; }
    public List<string> Errors { get; set; } = [];
}

public class BackupValidationResultDto
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = [];
}
