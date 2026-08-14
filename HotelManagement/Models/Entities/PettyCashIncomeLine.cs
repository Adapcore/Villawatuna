using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelManagement.Models.Entities
{
    public class PettyCashIncomeLine
    {
        public int Id { get; set; }

        [MaxLength(250)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(3)]
        public string Currency { get; set; } = "LKR";

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        /// <summary>Invoice type enum name (e.g. Stay, Dining) used for row coloring.</summary>
        [MaxLength(50)]
        public string? InvoiceTypeCode { get; set; }
    }
}
