using LocalFinanceManager.Core.Models;

namespace LocalFinanceManager.Api.DTOs
{
    public class CreateAccountDto
    {
        public string Label { get; set; } = string.Empty;
        public AccountType Type { get; set; }
        public string Currency { get; set; } = string.Empty;
        public string IBAN { get; set; } = string.Empty;
        public decimal StartingBalance { get; set; }
    }
}
