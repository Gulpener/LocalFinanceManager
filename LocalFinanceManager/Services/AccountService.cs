using LocalFinanceManager.Data.Repositories;
using LocalFinanceManager.DTOs;
using LocalFinanceManager.Models;
using Microsoft.EntityFrameworkCore;

namespace LocalFinanceManager.Services;

/// <summary>
/// Service for managing account operations.
/// </summary>
public class AccountService
{
    private readonly IAccountRepository _accountRepository;
    private readonly ILogger<AccountService> _logger;

    public AccountService(IAccountRepository accountRepository, ILogger<AccountService> logger)
    {
        _accountRepository = accountRepository;
        _logger = logger;
    }

    /// <summary>
    /// Get all active accounts.
    /// </summary>
    public async Task<List<AccountResponse>> GetAllActiveAsync()
    {
        _logger.LogInformation("Retrieving all active accounts");
        var accounts = await _accountRepository.GetAllActiveAsync();
        return accounts.Select(AccountResponse.FromEntity).ToList();
    }

    /// <summary>
    /// Get an account by ID.
    /// </summary>
    public async Task<AccountResponse?> GetByIdAsync(Guid id)
    {
        _logger.LogInformation("Retrieving account with ID: {AccountId}", id);
        var account = await _accountRepository.GetByIdAsync(id);
        return account != null ? AccountResponse.FromEntity(account) : null;
    }

    /// <summary>
    /// Create a new account.
    /// </summary>
    public async Task<AccountResponse> CreateAsync(CreateAccountRequest request)
    {
        _logger.LogInformation("Creating new account: {Label}", request.Label);

        var account = new Account
        {
            Id = Guid.NewGuid(),
            Label = request.Label,
            Type = request.Type,
            Currency = request.Currency.ToUpperInvariant(),
            IBAN = request.IBAN.Replace(" ", ""), // Normalize IBAN (remove spaces)
            StartingBalance = request.StartingBalance,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsArchived = false
        };

        await _accountRepository.AddAsync(account);

        _logger.LogInformation("Account created successfully with ID: {AccountId}", account.Id);
        return AccountResponse.FromEntity(account);
    }

    /// <summary>
    /// Update an existing account.
    /// </summary>
    public async Task<AccountResponse?> UpdateAsync(Guid id, UpdateAccountRequest request)
    {
        _logger.LogInformation("Updating account with ID: {AccountId}", id);

        var account = await _accountRepository.GetByIdAsync(id);
        if (account == null)
        {
            _logger.LogWarning("Account not found with ID: {AccountId}", id);
            return null;
        }

        // Check for duplicate label
        if (account.Label != request.Label && await _accountRepository.LabelExistsAsync(request.Label, id))
        {
            _logger.LogWarning("Account label already exists: {Label}", request.Label);
            throw new InvalidOperationException("An account with this label already exists");
        }

        account.Label = request.Label;
        account.Type = request.Type;
        account.Currency = request.Currency.ToUpperInvariant();
        account.IBAN = request.IBAN.Replace(" ", "");
        account.StartingBalance = request.StartingBalance;
        account.UpdatedAt = DateTime.UtcNow;

        try
        {
            await _accountRepository.UpdateAsync(account);
            _logger.LogInformation("Account updated successfully: {AccountId}", id);
            return AccountResponse.FromEntity(account);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Concurrency conflict updating account: {AccountId}", id);
            throw;
        }
    }

    /// <summary>
    /// Archive (soft-delete) an account.
    /// </summary>
    public async Task<bool> ArchiveAsync(Guid id)
    {
        _logger.LogInformation("Archiving account with ID: {AccountId}", id);

        var account = await _accountRepository.GetByIdAsync(id);
        if (account == null)
        {
            _logger.LogWarning("Account not found with ID: {AccountId}", id);
            return false;
        }

        account.IsArchived = true;
        account.UpdatedAt = DateTime.UtcNow;

        try
        {
            await _accountRepository.UpdateAsync(account);
            _logger.LogInformation("Account archived successfully: {AccountId}", id);
            return true;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Concurrency conflict archiving account: {AccountId}", id);
            throw;
        }
    }

    /// <summary>
    /// Unarchive (restore) an account.
    /// </summary>
    public async Task<bool> UnarchiveAsync(Guid id)
    {
        _logger.LogInformation("Unarchiving account with ID: {AccountId}", id);

        var account = await _accountRepository.GetByIdAsync(id);
        if (account == null)
        {
            _logger.LogWarning("Account not found with ID: {AccountId}", id);
            return false;
        }

        account.IsArchived = false;
        account.UpdatedAt = DateTime.UtcNow;

        try
        {
            await _accountRepository.UpdateAsync(account);
            _logger.LogInformation("Account unarchived successfully: {AccountId}", id);
            return true;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Concurrency conflict unarchiving account: {AccountId}", id);
            throw;
        }
    }
}
