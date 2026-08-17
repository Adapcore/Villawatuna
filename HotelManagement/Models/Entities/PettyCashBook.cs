using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelManagement.Models.Entities
{
    public class PettyCashBook : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public DateTime Date { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal OpeningBalance { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal ClosingBalance { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal PettyCashRefillClosing { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalIncomeBaseCurry { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalExpences { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Balance { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal HandedOver { get; set; }
        

        public List<PettyCashCurrencyTotal> TotalIncome { get; set; } = new();

        public List<PettyCashIncomeLine> Income { get; set; } = new();

        public List<PettyCashExpenseLine> Expences { get; set; } = new();

        public List<PettyCashRefillLine> Refills { get; set; } = new();

        [NotMapped]
        public decimal RefillOpeningTotal =>
            Math.Round((Refills ?? new List<PettyCashRefillLine>()).Sum(r => r.Amount), 2);
    }
}
