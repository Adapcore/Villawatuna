using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HotelManagement.Enums;
using HotelManagement.Models.DTO;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Models.Entities
{

    public class Expense : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }

        [Required]
        public DateTime Date { get; set; }

        [Required]
        public int ExpenseTypeID { get; set; }

        //Non-persistent property for Umbraco data
        [NotMapped]
        public ExpenseTypeDTO  ExpenseType { get; set; }

        public string? PayeeName { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Required]
        public PaymentMethod PaymentMethod { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }
        
        [MaxLength(500)]
        public string? Attachment { get; set; }
    }
}
