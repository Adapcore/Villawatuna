using System.ComponentModel.DataAnnotations;
using HotelManagement.Enums;

namespace HotelManagement.Models.ViewModels
{
	public class EditLeaveViewModel
	{
		public int ID { get; set; }
		public int CreatedBy { get; set; }

		[Display(Name = "Employee")]
		public string EmployeeName { get; set; } = string.Empty;

		[Display(Name = "Employee ID")]
		public int EmployeeId { get; set; }

		[Required]
		[Display(Name = "Leave Type")]
		public LeaveType Type { get; set; }

		[Required]
		[DataType(DataType.Date)]
		[Display(Name = "From Date")]
		public DateTime FromDate { get; set; }

		[Required]
		[DataType(DataType.Date)]
		[Display(Name = "To Date")]
		public DateTime ToDate { get; set; }

		[Required]
		[Display(Name = "Duration")]
		public LeaveDuration Duration { get; set; }

		[Display(Name = "Half Day Session")]
		public HalfDaySession? HalfDaySession { get; set; }

		[Required]
		[MaxLength(100)]
		public string Reason { get; set; } = string.Empty;

		[MaxLength(100)]
		public string? Comment { get; set; }

		[Display(Name = "Status")]
		public LeaveStatus Status { get; set; }

		[Display(Name = "No. of days")]
		public decimal NoOfDays { get; set; }
	}
}

