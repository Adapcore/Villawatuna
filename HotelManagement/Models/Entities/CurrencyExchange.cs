using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HotelManagement.Models.DTO;

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

        [Required]
        [Display(Name = "Foreign Currency")]
        public int ForeignCurrencyId { get; set; }

        [NotMapped]
        public CurrencyDTO? ForeignCurrency { get; set; }

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

        [MaxLength(500)]
        [Display(Name = "Remarks")]
        public string? Remarks { get; set; }
    }
}
