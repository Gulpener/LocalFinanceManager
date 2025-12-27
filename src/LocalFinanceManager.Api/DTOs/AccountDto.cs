using System;
using LocalFinanceManager.Core.Models;

namespace LocalFinanceManager.Api.DTOs
{
    public class AccountDto
    {
        public Guid Id { get; set; }
        public string Label { get; set; } = string.Empty;
        public AccountType Type { get; set; }
        public string Currency { get; set; } = string.Empty;
        public string IBAN { get; set; } = string.Empty;
        public decimal StartingBalance { get; set; }
        public bool IsArchived { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }
}
