using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelManagement.Models.Entities
{
	[Index(nameof(Email), IsUnique = true)]   // <-- Enforces UNIQUE constraint
	public class Employee : BaseEntity
    {
		[Key]
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public int ID { get; set; }

		[Required]
		[MaxLength(100)]
		[Display(Name = "Username")]
		public string Username { get; set; }

		[Required]
		[MaxLength(100)]
		[Display(Name = "Password")]
		public string Password { get; set; }

		[MaxLength(100)]
		[Display(Name = "First Name")]
		public string FirstName { get; set; }

		[MaxLength(100)]
		[Display(Name = "Last Name")]
		public string LastName { get; set; }

		[EmailAddress]
		[MaxLength(200)]
		[Display(Name = "Email")]
		public string Email { get; set; }

		[MaxLength(20)]
		[Display(Name = "Contact No")]
		public string ContactNo { get; set; }

		[MaxLength(250)]
		[Display(Name = "Address")]
		public string Address { get; set; }

		[MaxLength(12)]
		[Display(Name = "NIC")]
		public string NIC { get; set; }

		[Display(Name = "Joined Date")]
		public DateTime? JoinedDate { get; set; }

		[Display(Name = "Date of Birth")]
		public DateTime DOB { get; set; }
	}
}
