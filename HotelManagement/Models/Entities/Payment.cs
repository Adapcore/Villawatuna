using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HotelManagement.Enums;

namespace HotelManagement.Models.Entities
{
    public class Payment : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }

        [Required]
        public DateTime Date { get; set; }

        [Required]
        public int InvoiceNo { get; set; } //FK
        public Invoice Invoice { get; set; }


        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Required]
        public InvoicePaymentType Type { get; set; }

        [MaxLength(200)]
        public string? Reference { get; set; }
    }
}
