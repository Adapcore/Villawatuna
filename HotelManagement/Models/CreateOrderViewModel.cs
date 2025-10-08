namespace HotelManagement.Models
{
    using System.ComponentModel.DataAnnotations;

    public class CreateOrderViewModel
    {
        [Required]
        public DateTime Date { get; set; }

        [Required]
        public int CustomerId { get; set; }

        [Required]
        public int TableNo { get; set; }

        public bool IsFreeOfCharge { get; set; }
        public bool Dining { get; set; }
        public string? Notes { get; set; }

        [Display(Name = "Sub Total")]
        public decimal SubTotal { get; set; }

        [Display(Name = "Service Charge (10%)")]
        public decimal ServiceCharge { get; set; }

        [Display(Name = "Gross Amount")]
        public decimal GrossAmount { get; set; }

        public List<OrderItemViewModel> OrderItems { get; set; } = new();
    }

    public class OrderItemViewModel
    {
        [Required]
        public int ItemId { get; set; }

        public string? Comments { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int Qty { get; set; } = 1;

        [Required]
        [Range(0, double.MaxValue)]
        public decimal UnitPrice { get; set; }

        public decimal Amount { get; set; }
    }

}
