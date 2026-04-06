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
    private readonly IUserContext _userContext;
    private readonly ILogger<AccountService> _logger;

    public AccountService(IAccountRepository accountRepository, IUserContext userContext, ILogger<AccountService> logger)
    {
        _accountRepository = accountRepository;
        _userContext = userContext;
        _logger = logger;
    }

    /// <summary>
    /// Get all active accounts.
    /// </summary>
    public async Task<List<AccountResponse>> GetAllActiveAsync()
    {
        _logger.LogInformation("Retrieving all active accounts");
        var accounts = await _accountRepository.GetAllActiveAsync();
        return accounts.Select(a => BuildResponse(a)).ToList();
    }

    /// <summary>
    /// Get an account by ID.
    /// </summary>
    public async Task<AccountResponse?> GetByIdAsync(Guid id)
    {
        _logger.LogInformation("Retrieving account with ID: {AccountId}", id);
        var account = await _accountRepository.GetReadableByIdAsync(id);
        if (account == null) return null;
        return BuildResponse(account);
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
            UserId = _userContext.GetCurrentUserId(),
            Label = request.Label,
            Type = request.Type,
            Currency = request.Currency.ToUpperInvariant(),
            IBAN = request.IBAN.Replace(" ", ""), // Normalize IBAN (remove spaces)
            StartingBalance = request.StartingBalance,
            IsArchived = false
        };

        await _accountRepository.AddAsync(account);

        _logger.LogInformation("Account created successfully with ID: {AccountId}", account.Id);
        var response = AccountResponse.FromEntity(account);
        response.PermissionLevel = Models.PermissionLevel.Owner;
        return response;
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

        try
        {
            await _accountRepository.UpdateAsync(account);
            _logger.LogInformation("Account updated successfully: {AccountId}", id);
            // UpdateAsync uses owner-only GetByIdAsync, so the current user is always the owner
            var response = AccountResponse.FromEntity(account);
            response.PermissionLevel = Models.PermissionLevel.Owner;
            return response;
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

    /// <summary>
    /// Returns the count of active (non-archived) accounts for the current user.
    /// Used to determine whether to redirect a new user to the onboarding wizard.
    /// </summary>
    public async Task<int> GetActiveAccountCountAsync()
    {
        return await _accountRepository.CountActiveAsync();
    }

    /// <summary>
    /// Builds an <see cref="AccountResponse"/> from an entity and populates <see cref="AccountResponse.PermissionLevel"/>
    /// based on whether the current user owns the account or has been granted shared access.
    /// </summary>
    private AccountResponse BuildResponse(Account account)
    {
        var response = AccountResponse.FromEntity(account);
        var currentUserId = _userContext.GetCurrentUserId();

        if (account.UserId == currentUserId)
        {
            response.PermissionLevel = Models.PermissionLevel.Owner;
        }
        else
        {
            var share = account.Shares?.FirstOrDefault(s =>
                s.SharedWithUserId == currentUserId &&
                s.Status == Models.ShareStatus.Accepted &&
                !s.IsArchived);
            response.PermissionLevel = share?.Permission ?? Models.PermissionLevel.Viewer;
        }

        return response;
    }
}
