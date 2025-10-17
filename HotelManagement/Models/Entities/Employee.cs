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

		[Required, MaxLength(100)]
		public string Username { get; set; }

		[Required, MaxLength(100)]
		public string Password { get; set; }

		[MaxLength(100)]
		public string FirstName { get; set; }

		[MaxLength(100)]
		public string LastName { get; set; }

		[EmailAddress, MaxLength(200)]
		public string Email { get; set; }

		[MaxLength(20)]
		public string ContactNo { get; set; }

		[MaxLength(250)]
		public string Address { get; set; }

		[MaxLength(12)]
		public string NIC { get; set; }

		public DateTime? JoinedDate { get; set; }

		public DateTime DOB { get; set; }
	}
}
