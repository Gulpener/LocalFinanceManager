using System;
using System.ComponentModel.DataAnnotations;

namespace LocalFinanceManager.Blazor.Models
{
    public enum AccountType { Checking = 0, Savings = 1, Credit = 2, Other = 3 }

    public class AccountDto
    {
        public Guid Id { get; set; }
        public string Label { get; set; } = string.Empty;
        public AccountType Type { get; set; }
        public string Currency { get; set; } = string.Empty;
        public string IBAN { get; set; } = string.Empty;
        public decimal StartingBalance { get; set; }
        public bool IsArchived { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    public class CreateAccountDto
    {
        [Required]
        [StringLength(100)]
        public string Label { get; set; } = string.Empty;
        public AccountType Type { get; set; } = AccountType.Checking;
        [Required]
        [StringLength(3)]
        public string Currency { get; set; } = string.Empty;
        [StringLength(34)]
        public string IBAN { get; set; } = string.Empty;
        public decimal StartingBalance { get; set; }
    }

    public class UpdateAccountDto : CreateAccountDto
    {
        public Guid Id { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }
}
