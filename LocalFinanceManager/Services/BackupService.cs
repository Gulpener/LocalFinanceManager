using LocalFinanceManager.Data;
using LocalFinanceManager.DTOs;
using LocalFinanceManager.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LocalFinanceManager.Services;

public class BackupService : IBackupService
{
    private const string SupportedVersion = "1.0";

    private readonly AppDbContext _db;
    private readonly IUserContext _userContext;
    private readonly ILogger<BackupService> _logger;

    public BackupService(AppDbContext db, IUserContext userContext, ILogger<BackupService> logger)
    {
        _db = db;
        _userContext = userContext;
        _logger = logger;
    }

    public async Task<BackupData> CreateBackupAsync(Guid userId)
    {
        _logger.LogInformation("Creating backup for user {UserId}", userId);

        var accounts = await _db.Accounts
            .AsNoTracking()
            .Where(a => a.UserId == userId && !a.IsArchived)
            .ToListAsync();

        var accountIds = accounts.Select(a => a.Id).ToHashSet();

        var budgetPlans = await _db.BudgetPlans
            .AsNoTracking()
            .Where(bp => bp.UserId == userId && !bp.IsArchived && accountIds.Contains(bp.AccountId))
            .ToListAsync();

        var budgetPlanIds = budgetPlans.Select(bp => bp.Id).ToHashSet();

        var categories = await _db.Categories
            .AsNoTracking()
            .Where(c => c.UserId == userId && !c.IsArchived && budgetPlanIds.Contains(c.BudgetPlanId))
            .ToListAsync();

        var categoryIds = categories.Select(c => c.Id).ToHashSet();

        var budgetLines = await _db.BudgetLines
            .AsNoTracking()
            .Where(bl => !bl.IsArchived && budgetPlanIds.Contains(bl.BudgetPlanId))
            .ToListAsync();

        var budgetLineIds = budgetLines.Select(bl => bl.Id).ToHashSet();

        var transactions = await _db.Transactions
            .AsNoTracking()
            .Where(t => t.UserId == userId && !t.IsArchived && accountIds.Contains(t.AccountId))
            .ToListAsync();

        var transactionIds = transactions.Select(t => t.Id).ToHashSet();

        var transactionSplits = await _db.TransactionSplits
            .AsNoTracking()
            .Where(ts => !ts.IsArchived && transactionIds.Contains(ts.TransactionId))
            .ToListAsync();

        return new BackupData
        {
            ExportedAt = DateTime.UtcNow,
            Version = SupportedVersion,
            Accounts = accounts.Select(a => new BackupAccountDto
            {
                Id = a.Id,
                Label = a.Label,
                AccountType = a.Type.ToString(),
                Currency = a.Currency,
                IBAN = a.IBAN,
                StartingBalance = a.StartingBalance,
                CreatedAt = a.CreatedAt,
                UpdatedAt = a.UpdatedAt
            }).ToList(),
            BudgetPlans = budgetPlans.Select(bp => new BackupBudgetPlanDto
            {
                Id = bp.Id,
                AccountId = bp.AccountId,
                Year = bp.Year,
                Name = bp.Name,
                CreatedAt = bp.CreatedAt,
                UpdatedAt = bp.UpdatedAt
            }).ToList(),
            Categories = categories.Select(c => new BackupCategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                CategoryType = c.Type.ToString(),
                BudgetPlanId = c.BudgetPlanId,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt
            }).ToList(),
            BudgetLines = budgetLines.Select(bl => new BackupBudgetLineDto
            {
                Id = bl.Id,
                BudgetPlanId = bl.BudgetPlanId,
                CategoryId = bl.CategoryId,
                MonthlyAmountsJson = bl.MonthlyAmountsJson,
                Notes = bl.Notes,
                CreatedAt = bl.CreatedAt,
                UpdatedAt = bl.UpdatedAt
            }).ToList(),
            Transactions = transactions.Select(t => new BackupTransactionDto
            {
                Id = t.Id,
                Amount = t.Amount,
                Date = t.Date,
                Description = t.Description,
                Counterparty = t.Counterparty,
                AccountId = t.AccountId,
                ExternalId = t.ExternalId,
                ImportBatchId = t.ImportBatchId,
                SourceFileName = t.SourceFileName,
                ImportedAt = t.ImportedAt,
                CreatedAt = t.CreatedAt,
                UpdatedAt = t.UpdatedAt
            }).ToList(),
            TransactionSplits = transactionSplits.Select(ts => new BackupTransactionSplitDto
            {
                Id = ts.Id,
                TransactionId = ts.TransactionId,
                BudgetLineId = ts.BudgetLineId,
                Amount = ts.Amount,
                Note = ts.Note,
                CreatedAt = ts.CreatedAt,
                UpdatedAt = ts.UpdatedAt
            }).ToList()
        };
    }

    public async Task<BackupValidationResultDto> ValidateBackupAsync(BackupData backup, Guid userId)
    {
        var errors = new List<string>();

        if (backup.Version != SupportedVersion)
        {
            errors.Add($"Unsupported backup version '{backup.Version}'. Expected '{SupportedVersion}'.");
            return new BackupValidationResultDto { IsValid = false, Errors = errors };
        }

        // Internal referential integrity checks
        var backupAccountIds = backup.Accounts.Select(a => a.Id).ToHashSet();
        var backupBudgetPlanIds = backup.BudgetPlans.Select(bp => bp.Id).ToHashSet();
        var backupCategoryIds = backup.Categories.Select(c => c.Id).ToHashSet();
        var backupBudgetLineIds = backup.BudgetLines.Select(bl => bl.Id).ToHashSet();
        var backupTransactionIds = backup.Transactions.Select(t => t.Id).ToHashSet();

        foreach (var bp in backup.BudgetPlans)
        {
            if (!backupAccountIds.Contains(bp.AccountId))
                errors.Add($"BudgetPlan '{bp.Id}' references Account '{bp.AccountId}' which is not in the backup.");
        }

        foreach (var bl in backup.BudgetLines)
        {
            if (!backupBudgetPlanIds.Contains(bl.BudgetPlanId))
                errors.Add($"BudgetLine '{bl.Id}' references BudgetPlan '{bl.BudgetPlanId}' which is not in the backup.");
            if (!backupCategoryIds.Contains(bl.CategoryId))
                errors.Add($"BudgetLine '{bl.Id}' references Category '{bl.CategoryId}' which is not in the backup.");
        }

        foreach (var ts in backup.TransactionSplits)
        {
            if (!backupTransactionIds.Contains(ts.TransactionId))
                errors.Add($"TransactionSplit '{ts.Id}' references Transaction '{ts.TransactionId}' which is not in the backup.");
            if (!backupBudgetLineIds.Contains(ts.BudgetLineId))
                errors.Add($"TransactionSplit '{ts.Id}' references BudgetLine '{ts.BudgetLineId}' which is not in the backup.");
        }

        foreach (var t in backup.Transactions)
        {
            if (!backupAccountIds.Contains(t.AccountId))
                errors.Add($"Transaction '{t.Id}' references Account '{t.AccountId}' which is not in the backup.");
        }

        // IBAN conflict check: backup accounts whose Id doesn't match existing local accounts
        var backupIbans = backup.Accounts
            .Where(a => !string.IsNullOrEmpty(a.IBAN))
            .ToDictionary(a => a.IBAN!, a => a.Id);

        if (backupIbans.Count > 0)
        {
            var existingAccounts = await _db.Accounts
                .AsNoTracking()
                .Where(a => a.UserId == userId && !a.IsArchived)
                .Select(a => new { a.Id, a.IBAN })
                .ToListAsync();

            foreach (var existing in existingAccounts)
            {
                if (!string.IsNullOrEmpty(existing.IBAN)
                    && backupIbans.TryGetValue(existing.IBAN, out var backupOwningId)
                    && backupOwningId != existing.Id)
                {
                    errors.Add($"IBAN '{existing.IBAN}' is already used by account '{existing.Id}' but the backup assigns it to a different account '{backupOwningId}'.");
                }
            }
        }

        return new BackupValidationResultDto { IsValid = errors.Count == 0, Errors = errors };
    }

    public async Task<BackupRestoreResultDto> RestoreBackupAsync(Guid userId, BackupData backup, ConflictResolutionStrategy strategy)
    {
        var validation = await ValidateBackupAsync(backup, userId);
        if (!validation.IsValid)
        {
            return new BackupRestoreResultDto { Success = false, Errors = validation.Errors };
        }

        var result = new BackupRestoreResultDto();

        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            if (strategy == ConflictResolutionStrategy.Overwrite)
            {
                await ExecuteOverwriteAsync(userId, backup, result);
            }
            else if (strategy == ConflictResolutionStrategy.Merge)
            {
                await ExecuteMergeAsync(userId, backup, result);
                if (!result.Success && result.Errors.Count > 0)
                {
                    await tx.RollbackAsync();
                    return result;
                }
            }
            else // Skip
            {
                await ExecuteSkipAsync(userId, backup, result);
            }

            await _db.SaveChangesAsync();
            await tx.CommitAsync();
            result.Success = true;
            _logger.LogInformation("Restore completed for user {UserId} with strategy {Strategy}", userId, strategy);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Restore failed for user {UserId}", userId);
            await tx.RollbackAsync();
            result.Success = false;
            result.Errors.Add($"Restore failed: {ex.Message}");
        }

        return result;
    }

    private async Task ExecuteOverwriteAsync(Guid userId, BackupData backup, BackupRestoreResultDto result)
    {
        // Hard-delete in reverse FK order scoped to userId
        await _db.TransactionSplits
            .Where(ts => _db.Transactions.Any(t => t.Id == ts.TransactionId && t.UserId == userId))
            .ExecuteDeleteAsync();

        await _db.Transactions
            .Where(t => t.UserId == userId)
            .ExecuteDeleteAsync();

        await _db.BudgetLines
            .Where(bl => _db.BudgetPlans.Any(bp => bp.Id == bl.BudgetPlanId && bp.UserId == userId))
            .ExecuteDeleteAsync();

        await _db.Categories
            .Where(c => c.UserId == userId)
            .ExecuteDeleteAsync();

        await _db.BudgetPlans
            .Where(bp => bp.UserId == userId)
            .ExecuteDeleteAsync();

        await _db.Accounts
            .Where(a => a.UserId == userId)
            .ExecuteDeleteAsync();

        // Insert in FK order
        foreach (var dto in backup.Accounts)
        {
            _db.Accounts.Add(MapToAccount(dto, userId));
            result.AccountsImported++;
        }

        foreach (var dto in backup.BudgetPlans)
        {
            _db.BudgetPlans.Add(MapToBudgetPlan(dto, userId));
            result.BudgetPlansImported++;
        }

        foreach (var dto in backup.Categories)
        {
            _db.Categories.Add(MapToCategory(dto, userId));
            result.CategoriesImported++;
        }

        foreach (var dto in backup.BudgetLines)
        {
            _db.BudgetLines.Add(MapToBudgetLine(dto));
            result.BudgetLinesImported++;
        }

        foreach (var dto in backup.Transactions)
        {
            _db.Transactions.Add(MapToTransaction(dto, userId));
            result.TransactionsImported++;
        }

        foreach (var dto in backup.TransactionSplits)
        {
            _db.TransactionSplits.Add(MapToTransactionSplit(dto));
            result.TransactionSplitsImported++;
        }
    }

    private async Task ExecuteMergeAsync(Guid userId, BackupData backup, BackupRestoreResultDto result)
    {
        var existingAccountIds = await _db.Accounts
            .Where(a => a.UserId == userId)
            .Select(a => new { a.Id, a.UpdatedAt, a.IBAN })
            .ToDictionaryAsync(a => a.Id);

        // Detect IBAN conflicts for Merge
        var backupIbans = backup.Accounts
            .Where(a => !string.IsNullOrEmpty(a.IBAN))
            .ToDictionary(a => a.IBAN!, a => a.Id);

        foreach (var existing in existingAccountIds.Values)
        {
            if (!string.IsNullOrEmpty(existing.IBAN)
                && backupIbans.TryGetValue(existing.IBAN, out var backupId)
                && backupId != existing.Id)
            {
                result.Errors.Add($"IBAN conflict: '{existing.IBAN}' is assigned to a different account in the backup. Import aborted.");
                result.Success = false;
                return;
            }
        }

        // Accounts
        foreach (var dto in backup.Accounts)
        {
            if (existingAccountIds.TryGetValue(dto.Id, out var existing))
            {
                if (dto.UpdatedAt > existing.UpdatedAt)
                {
                    var entity = await _db.Accounts.FindAsync(dto.Id);
                    if (entity != null) ApplyAccountUpdate(entity, dto);
                    result.AccountsUpdated++;
                }
                else
                {
                    result.AccountsSkipped++;
                }
            }
            else
            {
                _db.Accounts.Add(MapToAccount(dto, userId));
                result.AccountsImported++;
            }
        }

        var existingBudgetPlanIds = await _db.BudgetPlans
            .Where(bp => bp.UserId == userId)
            .Select(bp => new { bp.Id, bp.UpdatedAt })
            .ToDictionaryAsync(bp => bp.Id);

        foreach (var dto in backup.BudgetPlans)
        {
            if (existingBudgetPlanIds.TryGetValue(dto.Id, out var existing))
            {
                if (dto.UpdatedAt > existing.UpdatedAt)
                {
                    var entity = await _db.BudgetPlans.FindAsync(dto.Id);
                    if (entity != null) ApplyBudgetPlanUpdate(entity, dto);
                    result.BudgetPlansUpdated++;
                }
                else
                {
                    result.BudgetPlansSkipped++;
                }
            }
            else
            {
                _db.BudgetPlans.Add(MapToBudgetPlan(dto, userId));
                result.BudgetPlansImported++;
            }
        }

        var existingCategoryIds = await _db.Categories
            .Where(c => c.UserId == userId)
            .Select(c => new { c.Id, c.UpdatedAt })
            .ToDictionaryAsync(c => c.Id);

        foreach (var dto in backup.Categories)
        {
            if (existingCategoryIds.TryGetValue(dto.Id, out var existing))
            {
                if (dto.UpdatedAt > existing.UpdatedAt)
                {
                    var entity = await _db.Categories.FindAsync(dto.Id);
                    if (entity != null) ApplyCategoryUpdate(entity, dto);
                    result.CategoriesUpdated++;
                }
                else
                {
                    result.CategoriesSkipped++;
                }
            }
            else
            {
                _db.Categories.Add(MapToCategory(dto, userId));
                result.CategoriesImported++;
            }
        }

        var existingBudgetLineIds = await _db.BudgetLines
            .Where(bl => _db.BudgetPlans.Any(bp => bp.Id == bl.BudgetPlanId && bp.UserId == userId))
            .Select(bl => new { bl.Id, bl.UpdatedAt })
            .ToDictionaryAsync(bl => bl.Id);

        foreach (var dto in backup.BudgetLines)
        {
            if (existingBudgetLineIds.TryGetValue(dto.Id, out var existing))
            {
                if (dto.UpdatedAt > existing.UpdatedAt)
                {
                    var entity = await _db.BudgetLines.FindAsync(dto.Id);
                    if (entity != null) ApplyBudgetLineUpdate(entity, dto);
                    result.BudgetLinesUpdated++;
                }
                else
                {
                    result.BudgetLinesSkipped++;
                }
            }
            else
            {
                _db.BudgetLines.Add(MapToBudgetLine(dto));
                result.BudgetLinesImported++;
            }
        }

        var existingTransactionIds = await _db.Transactions
            .Where(t => t.UserId == userId)
            .Select(t => new { t.Id, t.UpdatedAt })
            .ToDictionaryAsync(t => t.Id);

        foreach (var dto in backup.Transactions)
        {
            if (existingTransactionIds.TryGetValue(dto.Id, out var existing))
            {
                if (dto.UpdatedAt > existing.UpdatedAt)
                {
                    var entity = await _db.Transactions.FindAsync(dto.Id);
                    if (entity != null) ApplyTransactionUpdate(entity, dto);
                    result.TransactionsUpdated++;
                }
                else
                {
                    result.TransactionsSkipped++;
                }
            }
            else
            {
                _db.Transactions.Add(MapToTransaction(dto, userId));
                result.TransactionsImported++;
            }
        }

        var existingTransactionSplitIds = await _db.TransactionSplits
            .Where(ts => _db.Transactions.Any(t => t.Id == ts.TransactionId && t.UserId == userId))
            .Select(ts => new { ts.Id, ts.UpdatedAt })
            .ToDictionaryAsync(ts => ts.Id);

        foreach (var dto in backup.TransactionSplits)
        {
            if (existingTransactionSplitIds.TryGetValue(dto.Id, out var existing))
            {
                if (dto.UpdatedAt > existing.UpdatedAt)
                {
                    var entity = await _db.TransactionSplits.FindAsync(dto.Id);
                    if (entity != null) ApplyTransactionSplitUpdate(entity, dto);
                    result.TransactionSplitsUpdated++;
                }
                else
                {
                    result.TransactionSplitsSkipped++;
                }
            }
            else
            {
                _db.TransactionSplits.Add(MapToTransactionSplit(dto));
                result.TransactionSplitsImported++;
            }
        }
    }

    private async Task ExecuteSkipAsync(Guid userId, BackupData backup, BackupRestoreResultDto result)
    {
        var existingAccountIds = await _db.Accounts
            .Where(a => a.UserId == userId)
            .Select(a => a.Id)
            .ToHashSetAsync();

        foreach (var dto in backup.Accounts)
        {
            if (existingAccountIds.Contains(dto.Id))
                result.AccountsSkipped++;
            else
            {
                _db.Accounts.Add(MapToAccount(dto, userId));
                result.AccountsImported++;
            }
        }

        var existingBudgetPlanIds = await _db.BudgetPlans
            .Where(bp => bp.UserId == userId)
            .Select(bp => bp.Id)
            .ToHashSetAsync();

        foreach (var dto in backup.BudgetPlans)
        {
            if (existingBudgetPlanIds.Contains(dto.Id))
                result.BudgetPlansSkipped++;
            else
            {
                _db.BudgetPlans.Add(MapToBudgetPlan(dto, userId));
                result.BudgetPlansImported++;
            }
        }

        var existingCategoryIds = await _db.Categories
            .Where(c => c.UserId == userId)
            .Select(c => c.Id)
            .ToHashSetAsync();

        foreach (var dto in backup.Categories)
        {
            if (existingCategoryIds.Contains(dto.Id))
                result.CategoriesSkipped++;
            else
            {
                _db.Categories.Add(MapToCategory(dto, userId));
                result.CategoriesImported++;
            }
        }

        var existingBudgetLineIds = await _db.BudgetLines
            .Where(bl => _db.BudgetPlans.Any(bp => bp.Id == bl.BudgetPlanId && bp.UserId == userId))
            .Select(bl => bl.Id)
            .ToHashSetAsync();

        foreach (var dto in backup.BudgetLines)
        {
            if (existingBudgetLineIds.Contains(dto.Id))
                result.BudgetLinesSkipped++;
            else
            {
                _db.BudgetLines.Add(MapToBudgetLine(dto));
                result.BudgetLinesImported++;
            }
        }

        var existingTransactionIds = await _db.Transactions
            .Where(t => t.UserId == userId)
            .Select(t => t.Id)
            .ToHashSetAsync();

        foreach (var dto in backup.Transactions)
        {
            if (existingTransactionIds.Contains(dto.Id))
                result.TransactionsSkipped++;
            else
            {
                _db.Transactions.Add(MapToTransaction(dto, userId));
                result.TransactionsImported++;
            }
        }

        var existingTransactionSplitIds = await _db.TransactionSplits
            .Where(ts => _db.Transactions.Any(t => t.Id == ts.TransactionId && t.UserId == userId))
            .Select(ts => ts.Id)
            .ToHashSetAsync();

        foreach (var dto in backup.TransactionSplits)
        {
            if (existingTransactionSplitIds.Contains(dto.Id))
                result.TransactionSplitsSkipped++;
            else
            {
                _db.TransactionSplits.Add(MapToTransactionSplit(dto));
                result.TransactionSplitsImported++;
            }
        }
    }

    // --- Mappers ---

    private static Account MapToAccount(BackupAccountDto dto, Guid userId) => new()
    {
        Id = dto.Id,
        Label = dto.Label,
        Type = Enum.Parse<AccountType>(dto.AccountType),
        Currency = dto.Currency,
        IBAN = dto.IBAN ?? "",
        StartingBalance = dto.StartingBalance,
        CreatedAt = dto.CreatedAt,
        UpdatedAt = dto.UpdatedAt,
        UserId = userId,
        IsArchived = false
    };

    private static void ApplyAccountUpdate(Account entity, BackupAccountDto dto)
    {
        entity.Label = dto.Label;
        entity.Type = Enum.Parse<AccountType>(dto.AccountType);
        entity.Currency = dto.Currency;
        entity.IBAN = dto.IBAN ?? "";
        entity.StartingBalance = dto.StartingBalance;
        entity.UpdatedAt = dto.UpdatedAt;
    }

    private static BudgetPlan MapToBudgetPlan(BackupBudgetPlanDto dto, Guid userId) => new()
    {
        Id = dto.Id,
        AccountId = dto.AccountId,
        Year = dto.Year,
        Name = dto.Name,
        CreatedAt = dto.CreatedAt,
        UpdatedAt = dto.UpdatedAt,
        UserId = userId,
        IsArchived = false
    };

    private static void ApplyBudgetPlanUpdate(BudgetPlan entity, BackupBudgetPlanDto dto)
    {
        entity.Year = dto.Year;
        entity.Name = dto.Name;
        entity.UpdatedAt = dto.UpdatedAt;
    }

    private static Category MapToCategory(BackupCategoryDto dto, Guid userId) => new()
    {
        Id = dto.Id,
        Name = dto.Name,
        Type = Enum.Parse<CategoryType>(dto.CategoryType),
        BudgetPlanId = dto.BudgetPlanId,
        CreatedAt = dto.CreatedAt,
        UpdatedAt = dto.UpdatedAt,
        UserId = userId,
        IsArchived = false
    };

    private static void ApplyCategoryUpdate(Category entity, BackupCategoryDto dto)
    {
        entity.Name = dto.Name;
        entity.Type = Enum.Parse<CategoryType>(dto.CategoryType);
        entity.UpdatedAt = dto.UpdatedAt;
    }

    private static BudgetLine MapToBudgetLine(BackupBudgetLineDto dto) => new()
    {
        Id = dto.Id,
        BudgetPlanId = dto.BudgetPlanId,
        CategoryId = dto.CategoryId,
        MonthlyAmountsJson = dto.MonthlyAmountsJson,
        Notes = dto.Notes,
        CreatedAt = dto.CreatedAt,
        UpdatedAt = dto.UpdatedAt,
        IsArchived = false
    };

    private static void ApplyBudgetLineUpdate(BudgetLine entity, BackupBudgetLineDto dto)
    {
        entity.MonthlyAmountsJson = dto.MonthlyAmountsJson;
        entity.Notes = dto.Notes;
        entity.UpdatedAt = dto.UpdatedAt;
    }

    private static Transaction MapToTransaction(BackupTransactionDto dto, Guid userId) => new()
    {
        Id = dto.Id,
        Amount = dto.Amount,
        Date = dto.Date,
        Description = dto.Description,
        Counterparty = dto.Counterparty,
        AccountId = dto.AccountId,
        ExternalId = dto.ExternalId,
        ImportBatchId = dto.ImportBatchId,
        SourceFileName = dto.SourceFileName,
        ImportedAt = dto.ImportedAt,
        CreatedAt = dto.CreatedAt,
        UpdatedAt = dto.UpdatedAt,
        UserId = userId,
        IsArchived = false
    };

    private static void ApplyTransactionUpdate(Transaction entity, BackupTransactionDto dto)
    {
        entity.Amount = dto.Amount;
        entity.Date = dto.Date;
        entity.Description = dto.Description;
        entity.Counterparty = dto.Counterparty;
        entity.ExternalId = dto.ExternalId;
        entity.UpdatedAt = dto.UpdatedAt;
    }

    private static TransactionSplit MapToTransactionSplit(BackupTransactionSplitDto dto) => new()
    {
        Id = dto.Id,
        TransactionId = dto.TransactionId,
        BudgetLineId = dto.BudgetLineId,
        Amount = dto.Amount,
        Note = dto.Note,
        CreatedAt = dto.CreatedAt,
        UpdatedAt = dto.UpdatedAt,
        IsArchived = false
    };

    private static void ApplyTransactionSplitUpdate(TransactionSplit entity, BackupTransactionSplitDto dto)
    {
        entity.Amount = dto.Amount;
        entity.Note = dto.Note;
        entity.BudgetLineId = dto.BudgetLineId;
        entity.UpdatedAt = dto.UpdatedAt;
    }
}
