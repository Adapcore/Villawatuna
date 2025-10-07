using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Models.Entities
{
	[Index(nameof(Email), IsUnique = true)]  // Email must be unique
	public class Customer
	{
		[Key]
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public int ID { get; set; }

		[Required, MaxLength(200)]
		[EmailAddress]
		public string Email { get; set; }

		[Required, MaxLength(100)]
		public string FirstName { get; set; }

		[Required, MaxLength(100)]
		public string LastName { get; set; }

		[Required, MaxLength(50)]
		public string PassportNo { get; set; }

		[Required, MaxLength(12)]
		public string NIC { get; set; }

		[Required, MaxLength(15)]
		public string ContactNo { get; set; }

		[MaxLength(250)]
		public string? Address { get; set; }  // optional
	}
}
