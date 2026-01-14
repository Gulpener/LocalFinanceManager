using LocalFinanceManager.Models;

namespace LocalFinanceManager.DTOs;

/// <summary>
/// Request DTO for creating a new account.
/// </summary>
public class CreateAccountRequest
{
    public string Label { get; set; } = string.Empty;
    public AccountType Type { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string IBAN { get; set; } = string.Empty;
    public decimal StartingBalance { get; set; }
}

/// <summary>
/// Request DTO for updating an existing account.
/// </summary>
public class UpdateAccountRequest
{
    public string Label { get; set; } = string.Empty;
    public AccountType Type { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string IBAN { get; set; } = string.Empty;
    public decimal StartingBalance { get; set; }
}

/// <summary>
/// Response DTO for account data.
/// </summary>
public class AccountResponse
{
    public Guid Id { get; set; }
    public string Label { get; set; } = string.Empty;
    public AccountType Type { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string IBAN { get; set; } = string.Empty;
    public decimal StartingBalance { get; set; }
    public decimal CurrentBalance { get; set; }
    public bool IsArchived { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public byte[]? RowVersion { get; set; }

    public static AccountResponse FromEntity(Account account)
    {
        return new AccountResponse
        {
            Id = account.Id,
            Label = account.Label,
            Type = account.Type,
            Currency = account.Currency,
            IBAN = account.IBAN,
            StartingBalance = account.StartingBalance,
            CurrentBalance = account.CurrentBalance,
            IsArchived = account.IsArchived,
            CreatedAt = account.CreatedAt,
            UpdatedAt = account.UpdatedAt,
            RowVersion = account.RowVersion
        };
    }
}
