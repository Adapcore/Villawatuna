using HotelManagement.Enums;
using HotelManagement.Helper;
using HotelManagement.Models.ViewModels;
using HotelManagement.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
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
				TempData["SuccessMessage"] = "Leave approved.";
			}
			catch (Exception ex)
			{
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
				TempData["SuccessMessage"] = "Leave rejected.";
			}
			catch (Exception ex)
			{
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
		public async Task<IActionResult> Apply()
		{
			var model = new ApplyLeaveViewModel
			{
				FromDate = DateTime.UtcNow.Date,
				ToDate = DateTime.UtcNow.Date,
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

