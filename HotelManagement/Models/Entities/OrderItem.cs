using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelManagement.Models.Entities
{
	public class OrderItem
	{
		[Key]
		public int Id { get; set; } // Primary key for EF

		[Required]
		public int OrderNo { get; set; } // Foreign key
		public Order Order { get; set; }

		public int LineNumber { get; set; } // Auto-increment per order

		[Required]
		public int ItemId { get; set; } // select from menu item

		[MaxLength(250)]
		public string Comments { get; set; }

		[Required]
		public int Qty { get; set; }

		[Column(TypeName = "decimal(18,2)")]
		public decimal UnitPrice { get; set; }

		[Column(TypeName = "decimal(18,2)")]
		public decimal Amount { get; set; }
	}
}
