using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Models.Entities
{
    [Index(nameof(Email), IsUnique = true)]  // Email must be unique
    public class Customer : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }

        [Required, MaxLength(200)]
        [EmailAddress]
        public string Email { get; set; }

        [Required, MaxLength(100)]
        [Display(Name = "First Name")]
        public string FirstName { get; set; }

        [Required, MaxLength(100)]
        [Display(Name = "Last Name")]
        public string LastName { get; set; }

        [Required, MaxLength(50)]
        [Display(Name = "Passport No / NIC")]
        public string PassportNo { get; set; }

        [Required, MaxLength(15)]
        [Display(Name = "Contact No")]
        public string ContactNo { get; set; }

        [MaxLength(250)]
        public string? Address { get; set; }  // optional

        [MaxLength(10)]
        public string? Country { get; set; }

        [MaxLength(5)]
        [Display(Name = "Room No")]
        [RegularExpression(@"^[0-9]+$", ErrorMessage = "Room No must contain only numeric digits.")]
        public string? RoomNo { get; set; }

        public bool Active { get; set; } = true;
    }
}
