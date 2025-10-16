using HotelManagement.Enums;
using System.ComponentModel.DataAnnotations;

namespace HotelManagement.Models.ViewModels
{
    public class CreateInvoiceViewModel
    {
        public int InvoiceNo { get; set; }

        [Required]
        public DateTime Date { get; set; }

        [Required]
        public int Type { get; set; }

        public string? ReferenceNo { get; set; }
        public int Status { get; set; }

        [Required]
        public int CustomerId { get; set; }

        [StringLength(500)]
        public string? Note { get; set; }

        public decimal SubTotal { get; set; }
        public decimal ServiceCharge { get; set; }
        public decimal GrossAmount { get; set; }
        public decimal Paid { get; set; }
        public decimal Balance { get; set; }
        public List<CreateInvoiceDetailViewModel> InvoiceDetails { get; set; } = new();
    }

    public class CreateInvoiceDetailViewModel
    {
        public int ItemId { get; set; }
        public string? Note { get; set; }
        public DateTime? CheckIn { get; set; }
        public DateTime? CheckOut { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Amount { get; set; }
    }
}
