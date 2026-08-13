using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelManagement.Models.Entities
{
    public class PettyCashExpenseLine
    {
        public int Id { get; set; }

        [MaxLength(100)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(3)]
        public string Currency { get; set; } = "LKR";

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }
    }
}
