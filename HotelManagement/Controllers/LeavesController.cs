using HotelManagement.Enums;
using HotelManagement.Helper;
using HotelManagement.Models.ViewModels;
using HotelManagement.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Globalization;
using X.PagedList;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Services;

namespace HotelManagement.Controllers
{
	[Authorize]
	public class LeavesController : Controller
	{
		private readonly IEmployeeService _employeeService;
		private readonly IEmployeeLeaveService _leaveService;
		private readonly IMemberManager _memberManager;
		private readonly IMemberService _memberService;
		private readonly int _pageSize = 10;

		public LeavesController(IEmployeeService employeeService, IEmployeeLeaveService leaveService, IMemberManager memberManager, IMemberService memberService)
		{
			_employeeService = employeeService;
			_leaveService = leaveService;
			_memberManager = memberManager;
			_memberService = memberService;
		}

		[HttpGet]
		public async Task<IActionResult> Index(
			int page = 1,
			int? employeeId = null,
			DateTime? fromDate = null,
			DateTime? toDate = null,
			LeaveStatus? status = null,
			string? range = null)
		{
			ViewBag.IsAdmin = IsAdminUser();
			var today = DateTime.UtcNow.Date;

			if (!string.IsNullOrWhiteSpace(range))
			{
				switch (range.Trim().ToLowerInvariant())
				{
					case "today":
						fromDate = today;
						toDate = today;
						break;
					case "yesterday":
						fromDate = today.AddDays(-1);
						toDate = today.AddDays(-1);
						break;
					case "month":
						fromDate = new DateTime(today.Year, today.Month, 1);
						toDate = fromDate.Value.AddMonths(1).AddDays(-1);
						break;
					case "year":
						fromDate = new DateTime(today.Year, 1, 1);
						toDate = new DateTime(today.Year, 12, 31);
						break;
				}
			}

			var pageNumber = page < 1 ? 1 : page;
			var (leaves, totalDays, openDays, approvedDays, rejectedDays) = await _leaveService.GetPagedAsync(
				pageNumber: pageNumber,
				pageSize: _pageSize,
				employeeId: employeeId,
				fromDate: fromDate,
				toDate: toDate,
				status: status);

			var model = new LeaveIndexViewModel
			{
				EmployeeId = employeeId,
				FromDate = fromDate,
				ToDate = toDate,
				Status = status,
				Leaves = leaves,
				TotalDays = totalDays,
				OpenDays = openDays,
				ApprovedDays = approvedDays,
				RejectedDays = rejectedDays
			};

			await PopulateEmployeesAsync(model);
			PopulateStatuses(model);

			return View(model);
		}

		[HttpGet]
		public async Task<IActionResult> GetLeaves(
			int page = 1,
			int? employeeId = null,
			LeaveStatus? status = null,
			string? fromDate = null,
			string? toDate = null,
			string? range = null)
		{
			var isAdmin = IsAdminUser();
			var today = DateTime.UtcNow.Date;

			DateTime? fromParsed = null;
			DateTime? toParsed = null;

			// Accept yyyy-MM-dd (and fallback to current culture parsing).
			if (!string.IsNullOrWhiteSpace(fromDate))
			{
				if (DateTime.TryParseExact(fromDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var fd)
					|| DateTime.TryParse(fromDate, out fd))
				{
					fromParsed = fd.Date;
				}
			}
			if (!string.IsNullOrWhiteSpace(toDate))
			{
				if (DateTime.TryParseExact(toDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var td)
					|| DateTime.TryParse(toDate, out td))
				{
					toParsed = td.Date;
				}
			}

			// If range is provided, it overrides explicit dates (same behavior as server-rendered page).
			if (!string.IsNullOrWhiteSpace(range))
			{
				switch (range.Trim().ToLowerInvariant())
				{
					case "today":
						fromParsed = today;
						toParsed = today;
						break;
					case "yesterday":
						fromParsed = today.AddDays(-1);
						toParsed = today.AddDays(-1);
						break;
					case "month":
						fromParsed = new DateTime(today.Year, today.Month, 1);
						toParsed = fromParsed.Value.AddMonths(1).AddDays(-1);
						break;
					case "year":
						fromParsed = new DateTime(today.Year, 1, 1);
						toParsed = new DateTime(today.Year, 12, 31);
						break;
				}
			}

			var pageNumber = page < 1 ? 1 : page;
			var (leaves, totalDays, openDays, approvedDays, rejectedDays) = await _leaveService.GetPagedAsync(
				pageNumber: pageNumber,
				pageSize: _pageSize,
				employeeId: employeeId,
				fromDate: fromParsed,
				toDate: toParsed,
				status: status);

			var currentUserId = GetCurrentUserId();

			var items = leaves.Select(l => new
			{
				id = l.ID,
				createdBy = l.CreatedBy,
				employeeId = l.EmployeeId,
				employeeName = l.Employee != null ? $"{l.Employee.FirstName} {l.Employee.LastName}".Trim() : l.EmployeeId.ToString(),
				requestDate = l.RequestDate.ToString("yyyy-MM-dd"),
				fromDate = l.FromDate.ToString("yyyy-MM-dd"),
				toDate = l.ToDate.ToString("yyyy-MM-dd"),
				noOfDays = l.NoOfDays,
				reason = l.Reason,
				status = l.Status.ToString(),
				statusValue = (int)l.Status,
				canEdit = l.Status == LeaveStatus.Open,
				canApproveReject = isAdmin && l.Status == LeaveStatus.Open,
				canCancel = l.Status == LeaveStatus.Open && l.CreatedBy == currentUserId
			}).ToList();

			return Json(new
			{
				success = true,
				isAdmin,
				leaves = items,
				totals = new
				{
					totalDays,
					openDays,
					approvedDays,
					rejectedDays
				},
				pagination = new
				{
					pageNumber = leaves.PageNumber,
					pageCount = leaves.PageCount,
					totalItemCount = leaves.TotalItemCount,
					pageSize = leaves.PageSize,
					hasPreviousPage = leaves.HasPreviousPage,
					hasNextPage = leaves.HasNextPage
				},
				filters = new
				{
					employeeId,
					status = status?.ToString(),
					fromDate = fromParsed?.ToString("yyyy-MM-dd"),
					toDate = toParsed?.ToString("yyyy-MM-dd"),
					range = (range ?? "").Trim().ToLowerInvariant()
				}
			});
		}

		[HttpGet]
		public async Task<IActionResult> Calendar(
			int? year = null,
			int? month = null,
			bool? showOpen = null,
			bool? showApproved = null,
			bool? showRejected = null)
		{
			ViewBag.IsAdmin = IsAdminUser();

			var today = DateTime.UtcNow.Date;
			var y = year ?? today.Year;
			var m = month ?? today.Month;

			// Basic bounds (avoid invalid month values)
			if (m < 1) m = 1;
			if (m > 12) m = 12;

			var monthStart = new DateTime(y, m, 1);
			var monthEnd = monthStart.AddMonths(1).AddDays(-1);

			// default: all checked (first load). If any filter value is provided, treat missing ones as false.
			var anySpecified = showOpen.HasValue || showApproved.HasValue || showRejected.HasValue;
			var open = anySpecified ? (showOpen ?? false) : true;
			var approved = anySpecified ? (showApproved ?? false) : true;
			var rejected = anySpecified ? (showRejected ?? false) : true;

			var model = new LeaveIndexViewModel
			{
				CalendarMonthStart = monthStart,
				CalendarMonthEnd = monthEnd,
				CalendarShowOpen = open,
				CalendarShowApproved = approved,
				CalendarShowRejected = rejected,
				CalendarLeaves = await _leaveService.GetCalendarLeavesInRangeAsync(monthStart, monthEnd, open, approved, rejected)
			};

			return View(model);
		}

		[HttpGet]
		public async Task<IActionResult> CalendarDayLeaves(
			DateTime date,
			bool? showOpen = null,
			bool? showApproved = null,
			bool? showRejected = null)
		{
			// Use same filter defaults as Calendar()
			var anySpecified = showOpen.HasValue || showApproved.HasValue || showRejected.HasValue;
			var open = anySpecified ? (showOpen ?? false) : true;
			var approved = anySpecified ? (showApproved ?? false) : true;
			var rejected = anySpecified ? (showRejected ?? false) : true;

			var day = date.Date;
			var leaves = await _leaveService.GetCalendarLeavesInRangeAsync(day, day, open, approved, rejected);

			var items = leaves.Select(l =>
			{
				var empName = l.Employee != null
					? $"{l.Employee.FirstName} {l.Employee.LastName}".Trim()
					: l.EmployeeId.ToString();

				var isHalf = IsHalfDayOnDate(l, day) || IsLastDayHalfOnDate(l, day);
				var session = isHalf ? l.HalfDaySession : null;
				var sessionText = session == HalfDaySession.FirstHalf ? "First Half" :
					session == HalfDaySession.SecondHalf ? "Second Half" :
					"";

				return new
				{
					id = l.ID,
					employeeName = empName,
					status = l.Status.ToString(),
					fromDate = l.FromDate.ToString("yyyy-MM-dd"),
					toDate = l.ToDate.ToString("yyyy-MM-dd"),
					reason = l.Reason,
					isHalfDay = isHalf,
					halfDaySession = sessionText
				};
			}).ToList();

			return Json(new { success = true, date = day.ToString("yyyy-MM-dd"), leaves = items });
		}

		private static bool IsHalfDayOnDate(Models.Entities.EmployeeLeave l, DateTime date)
		{
			return l.Duration == LeaveDuration.HalfDay
			       && l.FromDate.Date == date.Date
			       && l.ToDate.Date == date.Date;
		}

		// For "Last day is half day" requests we store Duration=FullDay and HalfDaySession != null.
		private static bool IsLastDayHalfOnDate(Models.Entities.EmployeeLeave l, DateTime date)
		{
			return l.Duration == LeaveDuration.FullDay
			       && l.HalfDaySession.HasValue
			       && l.ToDate.Date == date.Date;
		}

		[HttpGet]
		public async Task<IActionResult> Details(int id)
		{
			ViewBag.IsAdmin = IsAdminUser();
			ViewBag.CurrentUserId = GetCurrentUserId();
			var leave = await _leaveService.GetByIdAsync(id);
			if (leave == null) return NotFound();
			return View(leave);
		}

		[HttpGet]
		public async Task<IActionResult> Edit(int id)
		{
			ViewBag.IsAdmin = IsAdminUser();
			ViewBag.CurrentUserId = GetCurrentUserId();
			var leave = await _leaveService.GetByIdAsync(id);
			if (leave == null) return NotFound();
			if (leave.Status != LeaveStatus.Open)
			{
				TempData["ErrorMessage"] = "Approved, Rejected, or Cancelled leaves cannot be edited.";
				return RedirectToAction(nameof(Details), new { id });
			}

			var model = new EditLeaveViewModel
			{
				ID = leave.ID,
				CreatedBy = leave.CreatedBy,
				EmployeeId = leave.EmployeeId,
				EmployeeName = leave.Employee != null ? $"{leave.Employee.FirstName} {leave.Employee.LastName}".Trim() : leave.EmployeeId.ToString(),
				Type = leave.Type,
				FromDate = leave.FromDate.Date,
				ToDate = leave.ToDate.Date,
				Duration = leave.Duration,
				HalfDaySession = leave.HalfDaySession,
				Reason = leave.Reason,
				Comment = leave.Comment,
				Status = leave.Status,
				NoOfDays = leave.NoOfDays
			};

			return View(model);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Edit(EditLeaveViewModel model)
		{
			ViewBag.IsAdmin = IsAdminUser();
			ViewBag.CurrentUserId = GetCurrentUserId();
			if (!ModelState.IsValid) return View(model);

			try
			{
				await _leaveService.UpdateAsync(model);
				TempData["SuccessMessage"] = "Leave updated successfully.";
				return RedirectToAction(nameof(Edit), new { id = model.ID });
			}
			catch (Exception ex)
			{
				ModelState.AddModelError(string.Empty, ex.Message);
				return View(model);
			}
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		[AuthorizeUserType("Admin")]
		public async Task<IActionResult> Approve(int id)
		{
			try
			{
				await _leaveService.ApproveAsync(id, adminUserId: GetCurrentUserId());
				if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
				{
					return Json(new { success = true, message = "Leave approved." });
				}

				TempData["SuccessMessage"] = "Leave approved.";
			}
			catch (Exception ex)
			{
				if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
				{
					return Json(new { success = false, message = ex.Message });
				}
				TempData["ErrorMessage"] = ex.Message;
			}

			return RedirectToAction(nameof(Index));
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		[AuthorizeUserType("Admin")]
		public async Task<IActionResult> Reject(int id)
		{
			try
			{
				await _leaveService.RejectAsync(id, adminUserId: GetCurrentUserId());
				if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
				{
					return Json(new { success = true, message = "Leave rejected." });
				}

				TempData["SuccessMessage"] = "Leave rejected.";
			}
			catch (Exception ex)
			{
				if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
				{
					return Json(new { success = false, message = ex.Message });
				}
				TempData["ErrorMessage"] = ex.Message;
			}

			return RedirectToAction(nameof(Index));
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Cancel(int id)
		{
			try
			{
				await _leaveService.CancelAsync(id, userId: GetCurrentUserId());
				TempData["SuccessMessage"] = "Leave cancelled.";
			}
			catch (Exception ex)
			{
				TempData["ErrorMessage"] = ex.Message;
			}

			return RedirectToAction(nameof(Index));
		}

		[HttpGet]
		public async Task<IActionResult> Apply(DateTime? fromDate = null, DateTime? toDate = null)
		{
			// If a date is provided from the calendar, prefill the form.
			// When only one side is provided, mirror it to keep the range valid.
			var from = (fromDate ?? toDate)?.Date;
			var to = (toDate ?? fromDate)?.Date;

			if (from.HasValue && to.HasValue && to.Value < from.Value)
			{
				to = from;
			}

			var model = new ApplyLeaveViewModel
			{
				FromDate = from ?? DateTime.UtcNow.Date,
				ToDate = to ?? DateTime.UtcNow.Date,
				Type = LeaveType.Planned,
				Duration = LeaveDuration.FullDay
			};

			await PopulateEmployeesAsync(model);
			return View(model);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Apply(ApplyLeaveViewModel model)
		{
			await PopulateEmployeesAsync(model);

			// To must not be before From; align To to From when the range would be invalid.
			if (model.ToDate.Date < model.FromDate.Date)
			{
				model.ToDate = model.FromDate;
				ModelState.Remove(nameof(ApplyLeaveViewModel.ToDate));
			}

			// Half day is only valid for a single date; collapse To to From if needed.
			if (model.Duration == LeaveDuration.HalfDay && model.ToDate.Date != model.FromDate.Date)
			{
				model.ToDate = model.FromDate;
				ModelState.Remove(nameof(ApplyLeaveViewModel.ToDate));
			}

			if (!ModelState.IsValid)
			{
				return View(model);
			}

			try
			{
				// For now, we store 0 if we can't resolve the logged-in user id.
				// (Your DbContext audit also relies on user id; we can wire this properly when we connect member->employee.)
				var createdByUserId = 0;

				await _leaveService.ApplyAsync(model, createdByUserId);
				TempData["SuccessMessage"] = "Leave applied successfully.";
				return RedirectToAction(nameof(Apply));
			}
			catch (Exception ex)
			{
				ModelState.AddModelError(string.Empty, ex.Message);
				return View(model);
			}
		}

		private async Task PopulateEmployeesAsync(ApplyLeaveViewModel model)
		{
			var employees = await _employeeService.GetAllAsync();
			model.Employees = employees
				.Select(e => new SelectListItem
				{
					Value = e.ID.ToString(),
					Text = $"{e.FirstName} {e.LastName}".Trim(),
					Selected = e.ID == model.EmployeeId
				})
				.ToList();
		}

		private async Task PopulateEmployeesAsync(LeaveIndexViewModel model)
		{
			var employees = await _employeeService.GetAllAsync();
			model.Employees = employees
				.Select(e => new SelectListItem
				{
					Value = e.ID.ToString(),
					Text = $"{e.FirstName} {e.LastName}".Trim(),
					Selected = model.EmployeeId.HasValue && e.ID == model.EmployeeId.Value
				})
				.ToList();
		}

		private static void PopulateStatuses(LeaveIndexViewModel model)
		{
			model.Statuses = new List<SelectListItem>
			{
				new SelectListItem { Value = "", Text = "-- All --", Selected = !model.Status.HasValue }
			};

			foreach (LeaveStatus s in Enum.GetValues(typeof(LeaveStatus)))
			{
				model.Statuses.Add(new SelectListItem
				{
					Value = ((int)s).ToString(),
					Text = s.ToString(),
					Selected = model.Status.HasValue && model.Status.Value == s
				});
			}
		}

		private int GetCurrentUserId()
		{
			// Consistent with HotelContext audit (expects int user id). Currently Umbraco member IDs are int.
			var username = User?.Identity?.Name;
			if (string.IsNullOrWhiteSpace(username)) return 0;
			var memberIdentity = _memberManager.FindByNameAsync(username).GetAwaiter().GetResult();
			if (memberIdentity == null) return 0;
			return int.TryParse(memberIdentity.Id, out var id) ? id : 0;
		}

		private bool IsAdminUser()
		{
			var username = User?.Identity?.Name;
			if (string.IsNullOrWhiteSpace(username)) return false;

			var memberIdentity = _memberManager.FindByNameAsync(username).GetAwaiter().GetResult();
			if (memberIdentity == null) return false;

			var member = _memberService.GetByKey(memberIdentity.Key);
			if (member == null) return false;

			var rawType = member.GetValue<string>("userType") ?? "";
			var userType = rawType.Replace("[", "").Replace("]", "").Replace("\"", "").Trim();
			return string.Equals(userType, "Admin", StringComparison.OrdinalIgnoreCase);
		}
	}
}

