using HotelManagement.Services.Interface;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelManagement.Models.Entities
{
    public class InvoiceDetail
	{
		[Key]
		public int Id { get; set; } // EF PK

		[Required]
		public int InvoiceNo { get; set; } // FK to Invoice
		public Invoice Invoice { get; set; }

		public int LineNumber { get; set; } // Auto-increment per invoice
				
        public int ItemId { get; set; }
		
		//Non-persistent property for Umbraco data
        [NotMapped]
        public ItemDto? Item { get; set; }

        [MaxLength(250)]
		public string? Note { get; set; }

		public DateTime? CheckIn { get; set; }
		public DateTime? CheckOut { get; set; }

		[Column(TypeName = "decimal(18,2)")]
		public decimal Quantity { get; set; }

		[Column(TypeName = "decimal(18,2)")]
		public decimal UnitPrice { get; set; }

		[Column(TypeName = "decimal(18,2)")]
		public decimal Amount { get; set; }
	}
}
