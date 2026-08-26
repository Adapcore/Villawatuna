using System.ComponentModel.DataAnnotations;

namespace HotelManagement.Enums
{
	public enum Gender
	{
		[Display(Name = "Male")]
		Male = 1,

		[Display(Name = "Female")]
		Female = 2,

		[Display(Name = "Other")]
		Other = 3
	}

	public enum MaritalStatus
	{
		[Display(Name = "Single")]
		Single = 1,

		[Display(Name = "Married")]
		Married = 2
	}

	public enum EmploymentType
	{
		[Display(Name = "Permanent")]
		Permanent = 1,

		[Display(Name = "Temporary")]
		Temporary = 2,

		[Display(Name = "Part-Time")]
		PartTime = 3,

		[Display(Name = "Casual/Daily")]
		CasualDaily = 4
	}
}
