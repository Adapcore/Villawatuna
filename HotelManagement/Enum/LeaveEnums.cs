using System.ComponentModel.DataAnnotations;

namespace HotelManagement.Enums
{
	public enum LeaveType
	{
		[Display(Name = "Planned")]
		Planned = 1,

		[Display(Name = "Unplanned")]
		Unplanned = 2
	}

	public enum LeaveDuration
	{
		[Display(Name = "Full Day")]
		FullDay = 1,

		[Display(Name = "Half Day")]
		HalfDay = 2
	}

	public enum HalfDaySession
	{
		[Display(Name = "First Half")]
		FirstHalf = 1,

		[Display(Name = "Second Half")]
		SecondHalf = 2
	}

	public enum LeaveStatus
	{
		[Display(Name = "Open")]
		Open = 1,

		[Display(Name = "Approved")]
		Approved = 2,

		[Display(Name = "Rejected")]
		Rejected = 3,

		[Display(Name = "Cancelled")]
		Cancelled = 4
	}
}

