using LocalFinanceManager.Core.Models;

namespace LocalFinanceManager.Api.DTOs
{
    public class UpdateAccountDto
    {
        public string Label { get; set; } = string.Empty;
        public AccountType Type { get; set; }
        public string Currency { get; set; } = string.Empty;
        public string IBAN { get; set; } = string.Empty;
        public decimal StartingBalance { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }
}
