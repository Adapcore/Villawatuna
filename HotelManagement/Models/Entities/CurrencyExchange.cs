using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelManagement.Models.Entities
{
    public class CurrencyExchange : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Display(Name = "Exchange Transaction No")]
        public int Id { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Exchange Date")]
        public DateTime Date { get; set; }

        [Required]
        [Display(Name = "Customer")]
        public int CustomerId { get; set; }

        public Customer? Customer { get; set; }

        /// <summary>
        /// Foreign currency code stored like Invoice.Currency (e.g. USD, GBP).
        /// </summary>
        [Required]
        [MaxLength(5)]
        [Display(Name = "Foreign Currency")]
        public string ForeignCurrency { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Exchange Rate")]
        [Column(TypeName = "decimal(18,4)")]
        public decimal ExchangeRate { get; set; }

        [Required]
        [Display(Name = "Foreign Amount")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal ForeignAmount { get; set; }

        [Display(Name = "LKR Amount")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal LKRAmount { get; set; }

        public bool IsInvoiceBalanceExchange { get; set; } = false;

        [MaxLength(500)]
        [Display(Name = "Remarks")]
        public string? Remarks { get; set; }

        public int? InvoiceNo { get; set; }
        public Invoice? Invoice { get; set; }

        /// <summary>
        /// Original invoice whose foreign-currency customer change was settled in LKR.
        /// </summary>
        public int? InvoiceId { get; set; }
        public Invoice? SourceInvoice { get; set; }

        public int? ExpenseId { get; set; }
        public Expense? Expense { get; set; }
    }
}
