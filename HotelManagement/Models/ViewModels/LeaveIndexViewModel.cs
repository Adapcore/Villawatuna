using System.ComponentModel.DataAnnotations;
using HotelManagement.Enums;
using HotelManagement.Models.Entities;
using Microsoft.AspNetCore.Mvc.Rendering;
using X.PagedList;

namespace HotelManagement.Models.ViewModels
{
	public class LeaveIndexViewModel
	{
		// Filters
		[Display(Name = "Employee")]
		public int? EmployeeId { get; set; }

		[DataType(DataType.Date)]
		[Display(Name = "From Date")]
		public DateTime? FromDate { get; set; }

		[DataType(DataType.Date)]
		[Display(Name = "To Date")]
		public DateTime? ToDate { get; set; }

		[Display(Name = "Status")]
		public LeaveStatus? Status { get; set; }

		// Data
		public IPagedList<EmployeeLeave> Leaves { get; set; } = new PagedList<EmployeeLeave>(Enumerable.Empty<EmployeeLeave>(), 1, 1);

		// Sum for full filtered result (not only current page)
		public decimal TotalDays { get; set; }
		public decimal OpenDays { get; set; }
		public decimal ApprovedDays { get; set; }
		public decimal RejectedDays { get; set; }

		// Dropdowns
		public List<SelectListItem> Employees { get; set; } = new();
		public List<SelectListItem> Statuses { get; set; } = new();

		// Calendar (current month)
		public DateTime CalendarMonthStart { get; set; }
		public DateTime CalendarMonthEnd { get; set; }
		public List<EmployeeLeave> CalendarLeaves { get; set; } = new();

		// Calendar filters
		public bool CalendarShowOpen { get; set; } = true;
		public bool CalendarShowApproved { get; set; } = true;
		public bool CalendarShowRejected { get; set; } = true;
	}
}

