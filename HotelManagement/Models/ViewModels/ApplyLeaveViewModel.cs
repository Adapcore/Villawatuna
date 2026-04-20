using System.ComponentModel.DataAnnotations;
using HotelManagement.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HotelManagement.Models.ViewModels
{
	public class ApplyLeaveViewModel
	{
		[Required]
		[Display(Name = "Employee")]
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

		/// <summary>
		/// When duration is Full Day and From &lt; To, the leave ends at noon on <see cref="ToDate"/> (half of that day only).
		/// Example: Mon–Wed with last day half = 2.5 days.
		/// </summary>
		[Display(Name = "Last day is half day")]
		public bool LastDayIsHalfDay { get; set; }

		[Required]
		[MaxLength(100)]
		public string Reason { get; set; } = string.Empty;

		[MaxLength(100)]
		public string? Comment { get; set; }

		public List<SelectListItem> Employees { get; set; } = new();
	}
}

