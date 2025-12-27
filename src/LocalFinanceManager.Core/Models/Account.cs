using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LocalFinanceManager.Core.Models
{
    public enum AccountType { Checking, Savings, Credit, Other }

    public class Account
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Label { get; set; } = string.Empty;

        public AccountType Type { get; set; }

        [Required]
        [StringLength(3)]
        public string Currency { get; set; } = string.Empty;

        [Required]
        [MaxLength(34)]
        public string IBAN { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal StartingBalance { get; set; }

        public bool IsArchived { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        [Timestamp]
        public byte[]? RowVersion { get; set; }
    }
}
