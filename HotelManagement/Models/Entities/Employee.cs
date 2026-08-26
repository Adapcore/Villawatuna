using HotelManagement.Enums;
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

		[MaxLength(50)]
		[Display(Name = "EPF No")]
		public string? EPFNo { get; set; }

		[MaxLength(100)]
		[Display(Name = "First Name")]
		public string FirstName { get; set; }

		[MaxLength(100)]
		[Display(Name = "Last Name")]
		public string LastName { get; set; }

		[MaxLength(150)]
		[Display(Name = "Name with Initials")]
		public string? NameWithInitials { get; set; }

		[EmailAddress]
		[MaxLength(200)]
		[Display(Name = "Email Address")]
		public string Email { get; set; }

		[MaxLength(20)]
		[Display(Name = "Mobile Number")]
		public string ContactNo { get; set; }

		[MaxLength(250)]
		[Display(Name = "Permanent Address")]
		public string PermanentAddress { get; set; }

		[MaxLength(250)]
		[Display(Name = "Current Address")]
		public string? CurrentAddress { get; set; }

		[MaxLength(12)]
		[Display(Name = "NIC")]
		public string NIC { get; set; }

		[Display(Name = "Date of Birth")]
		public DateTime DOB { get; set; }

		[Display(Name = "Gender")]
		public Gender? Gender { get; set; }

		[Display(Name = "Marital Status")]
		public MaritalStatus? MaritalStatus { get; set; }

		[Display(Name = "Joining Date")]
		public DateTime? JoinedDate { get; set; }

		// Emergency contact
		[MaxLength(150)]
		[Display(Name = "Contact Person's Name")]
		public string? EmergencyContactName { get; set; }

		[MaxLength(100)]
		[Display(Name = "Relationship")]
		public string? EmergencyContactRelationship { get; set; }

		[MaxLength(20)]
		[Display(Name = "Mobile Number")]
		public string? EmergencyContactMobile { get; set; }

		[MaxLength(250)]
		[Display(Name = "Address")]
		public string? EmergencyContactAddress { get; set; }

		// Employment
		[MaxLength(100)]
		[Display(Name = "Position / Job Title")]
		public string? JobTitle { get; set; }

		[MaxLength(100)]
		[Display(Name = "Department")]
		public string? Department { get; set; }

		[Display(Name = "Employment Type")]
		public EmploymentType? EmploymentType { get; set; }

		[Display(Name = "Agreed Salary / Wage")]
		[Column(TypeName = "decimal(18,2)")]
		public decimal? AgreedSalary { get; set; }

		[MaxLength(150)]
		[Display(Name = "Working Location")]
		public string? WorkingLocation { get; set; }

		[MaxLength(20)]
		[Display(Name = "Working Days")]
		public string? WorkingDays { get; set; }

		[MaxLength(100)]
		[Display(Name = "Working Days (Other)")]
		public string? WorkingDaysOther { get; set; }

		[MaxLength(100)]
		[Display(Name = "Working Hours")]
		public string? WorkingHours { get; set; }

		// Bank account
		[MaxLength(150)]
		[Display(Name = "Account Holder's Name")]
		public string? BankAccountHolderName { get; set; }

		[MaxLength(100)]
		[Display(Name = "Bank Name")]
		public string? BankName { get; set; }

		[MaxLength(100)]
		[Display(Name = "Branch Name")]
		public string? BankBranchName { get; set; }

		[MaxLength(50)]
		[Display(Name = "Account Number")]
		public string? BankAccountNumber { get; set; }
	}
}
