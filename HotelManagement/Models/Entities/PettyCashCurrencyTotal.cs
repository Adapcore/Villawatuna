using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelManagement.Models.Entities
{
    public class PettyCashCurrencyTotal
    {
        [MaxLength(3)]
        public string Currency { get; set; } = "LKR";

        [Column(TypeName = "decimal(18,2)")]
        public decimal Total { get; set; }
    }
}
