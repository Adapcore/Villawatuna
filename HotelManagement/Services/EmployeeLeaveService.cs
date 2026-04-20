using HotelManagement.Enums;
using HotelManagement.Models.Entities;
using HotelManagement.Models.ViewModels;
using HotelManagement.Repositories;
using HotelManagement.Services.Interface;
using Microsoft.EntityFrameworkCore;
using X.PagedList;
using X.PagedList.Extensions;

namespace HotelManagement.Services
{
	public class EmployeeLeaveService : IEmployeeLeaveService
	{
		private readonly IEmployeeLeaveRepository _repository;

		public EmployeeLeaveService(IEmployeeLeaveRepository repository)
		{
			_repository = repository;
		}

		public async Task ApplyAsync(ApplyLeaveViewModel model, int createdByUserId, CancellationToken cancellationToken = default)
		{
			Validate(model);

			await EnsureNoOverlapAsync(
				employeeId: model.EmployeeId,
				fromDate: model.FromDate.Date,
				toDate: model.ToDate.Date,
				excludeLeaveId: null,
				cancellationToken: cancellationToken);

			var leave = new EmployeeLeave
			{
				EmployeeId = model.EmployeeId,
				Type = model.Type,
				RequestDate = DateTime.UtcNow,
				FromDate = model.FromDate.Date,
				ToDate = model.ToDate.Date,
				Duration = model.Duration,
				HalfDaySession = ResolveHalfDaySession(model),
				NoOfDays = CalculateNoOfDays(model),
				Reason = model.Reason.Trim(),
				Comment = string.IsNullOrWhiteSpace(model.Comment) ? null : model.Comment.Trim(),
				Status = LeaveStatus.Open,
				CreatedBy = createdByUserId,
				CreatedDate = DateTime.UtcNow
			};

			await _repository.AddAsync(leave, cancellationToken);
		}

		public async Task<(IPagedList<EmployeeLeave> Leaves, decimal TotalDays, decimal OpenDays, decimal ApprovedDays, decimal RejectedDays)> GetPagedAsync(
			int pageNumber,
			int pageSize,
			int? employeeId = null,
			DateTime? fromDate = null,
			DateTime? toDate = null,
			LeaveStatus? status = null,
			CancellationToken cancellationToken = default)
		{
			var query = _repository.Query()
				.AsNoTracking()
				.Include(l => l.Employee)
				.AsQueryable();

			if (employeeId.HasValue && employeeId.Value > 0)
			{
				query = query.Where(l => l.EmployeeId == employeeId.Value);
			}

			if (status.HasValue)
			{
				query = query.Where(l => l.Status == status.Value);
			}

			if (fromDate.HasValue)
			{
				var f = fromDate.Value.Date;
				query = query.Where(l => l.FromDate.Date >= f);
			}

			if (toDate.HasValue)
			{
				var t = toDate.Value.Date;
				query = query.Where(l => l.ToDate.Date <= t);
			}

			query = query.OrderBy(l => l.FromDate);

			var totalDays = await query.SumAsync(l => l.NoOfDays, cancellationToken);
			var openDays = await query.Where(l => l.Status == LeaveStatus.Open).SumAsync(l => l.NoOfDays, cancellationToken);
			var approvedDays = await query.Where(l => l.Status == LeaveStatus.Approved).SumAsync(l => l.NoOfDays, cancellationToken);
			var rejectedDays = await query.Where(l => l.Status == LeaveStatus.Rejected).SumAsync(l => l.NoOfDays, cancellationToken);
			var paged = query.ToPagedList(pageNumber, pageSize);

			return (paged, totalDays, openDays, approvedDays, rejectedDays);
		}

		public async Task<List<EmployeeLeave>> GetCalendarLeavesInRangeAsync(
			DateTime fromDate,
			DateTime toDate,
			bool showOpen,
			bool showApproved,
			bool showRejected,
			CancellationToken cancellationToken = default)
		{
			var from = fromDate.Date;
			var to = toDate.Date;

			// If user unchecks everything, default back to "all" (requested default: all checked).
			if (!showOpen && !showApproved && !showRejected)
			{
				showOpen = true;
				showApproved = true;
				showRejected = true;
			}

			var allowedStatuses = new List<LeaveStatus>();
			if (showOpen) allowedStatuses.Add(LeaveStatus.Open);
			if (showApproved) allowedStatuses.Add(LeaveStatus.Approved);
			if (showRejected) allowedStatuses.Add(LeaveStatus.Rejected);

			return await _repository.Query()
				.AsNoTracking()
				.Include(l => l.Employee)
				.Where(l => allowedStatuses.Contains(l.Status))
				.Where(l => l.FromDate.Date <= to && l.ToDate.Date >= from) // overlap
				.OrderBy(l => l.FromDate)
				.ToListAsync(cancellationToken);
		}

		public async Task<EmployeeLeave?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
		{
			return await _repository.Query()
				.AsNoTracking()
				.Include(l => l.Employee)
				.FirstOrDefaultAsync(l => l.ID == id, cancellationToken);
		}

		public async Task UpdateAsync(EditLeaveViewModel model, CancellationToken cancellationToken = default)
		{
			// Only Open leaves should be editable (as per requirement)
			var entity = await _repository.GetForUpdateAsync(model.ID, cancellationToken);
			if (entity == null) throw new InvalidOperationException("Leave not found.");
			if (entity.Status != LeaveStatus.Open) throw new InvalidOperationException("Only Open leaves can be edited.");

			// Basic validation
			if (model.FromDate.Date > model.ToDate.Date)
				throw new InvalidOperationException("From Date cannot be greater than To Date.");

			if (model.Duration == LeaveDuration.HalfDay)
			{
				if (!model.HalfDaySession.HasValue)
					throw new InvalidOperationException("Half Day Session is required for Half Day leave.");
				if (model.FromDate.Date != model.ToDate.Date)
					throw new InvalidOperationException("For Half Day leave, From Date and To Date must be the same.");
			}

			await EnsureNoOverlapAsync(
				employeeId: entity.EmployeeId,
				fromDate: model.FromDate.Date,
				toDate: model.ToDate.Date,
				excludeLeaveId: entity.ID,
				cancellationToken: cancellationToken);

			entity.Type = model.Type;
			entity.FromDate = model.FromDate.Date;
			entity.ToDate = model.ToDate.Date;
			entity.Duration = model.Duration;
			entity.HalfDaySession = model.Duration == LeaveDuration.HalfDay ? model.HalfDaySession : null;
			entity.NoOfDays = model.Duration == LeaveDuration.HalfDay
				? 0.5m
				: CountInclusiveDaysIncludingWeekends(entity.FromDate.Date, entity.ToDate.Date);
			entity.Reason = model.Reason.Trim();
			entity.Comment = string.IsNullOrWhiteSpace(model.Comment) ? null : model.Comment.Trim();

			await _repository.SaveChangesAsync(cancellationToken);
		}

		private async Task EnsureNoOverlapAsync(
			int employeeId,
			DateTime fromDate,
			DateTime toDate,
			int? excludeLeaveId,
			CancellationToken cancellationToken)
		{
			// Overlap rule (inclusive):
			// existing.From <= new.To AND existing.To >= new.From
			var query = _repository.Query()
				.AsNoTracking()
				.Where(l => l.EmployeeId == employeeId)
				.Where(l => l.Status == LeaveStatus.Open || l.Status == LeaveStatus.Approved)
				.Where(l => l.FromDate.Date <= toDate && l.ToDate.Date >= fromDate);

			if (excludeLeaveId.HasValue)
			{
				query = query.Where(l => l.ID != excludeLeaveId.Value);
			}

			var exists = await query.AnyAsync(cancellationToken);
			if (exists)
			{
				throw new InvalidOperationException("This employee already has an Open/Approved leave in the selected date period.");
			}
		}

		public async Task ApproveAsync(int id, int adminUserId, CancellationToken cancellationToken = default)
		{
			var entity = await _repository.GetForUpdateAsync(id, cancellationToken);
			if (entity == null) throw new InvalidOperationException("Leave not found.");
			if (entity.Status != LeaveStatus.Open) throw new InvalidOperationException("Only Open leaves can be approved.");

			entity.Status = LeaveStatus.Approved;
			entity.ApproveRejectAt = DateTime.UtcNow;
			entity.ApproveRejectUserId = adminUserId;

			await _repository.SaveChangesAsync(cancellationToken);
		}

		public async Task RejectAsync(int id, int adminUserId, CancellationToken cancellationToken = default)
		{
			var entity = await _repository.GetForUpdateAsync(id, cancellationToken);
			if (entity == null) throw new InvalidOperationException("Leave not found.");
			if (entity.Status != LeaveStatus.Open) throw new InvalidOperationException("Only Open leaves can be rejected.");

			entity.Status = LeaveStatus.Rejected;
			entity.ApproveRejectAt = DateTime.UtcNow;
			entity.ApproveRejectUserId = adminUserId;

			await _repository.SaveChangesAsync(cancellationToken);
		}

		public async Task CancelAsync(int id, int userId, CancellationToken cancellationToken = default)
		{
			var entity = await _repository.GetForUpdateAsync(id, cancellationToken);
			if (entity == null) throw new InvalidOperationException("Leave not found.");
			if (entity.Status != LeaveStatus.Open) throw new InvalidOperationException("Only Open leaves can be cancelled.");
			if (entity.CreatedBy != userId) throw new InvalidOperationException("Only the user who created this leave can cancel it.");

			entity.Status = LeaveStatus.Cancelled;

			await _repository.SaveChangesAsync(cancellationToken);
		}

		private static void Validate(ApplyLeaveViewModel model)
		{
			if (model.FromDate.Date > model.ToDate.Date)
			{
				throw new InvalidOperationException("From Date cannot be greater than To Date.");
			}

			if (model.Duration == LeaveDuration.HalfDay)
			{
				if (!model.HalfDaySession.HasValue)
				{
					throw new InvalidOperationException("Half Day Session is required for Half Day leave.");
				}

				if (model.FromDate.Date != model.ToDate.Date)
				{
					throw new InvalidOperationException("For Half Day leave, From Date and To Date must be the same.");
				}

				if (model.LastDayIsHalfDay)
				{
					throw new InvalidOperationException("Last day is half day applies only to Full Day leave with a date range.");
				}
			}

			if (model.LastDayIsHalfDay)
			{
				if (model.Duration != LeaveDuration.FullDay)
				{
					throw new InvalidOperationException("Last day is half day applies only when Duration is Full Day.");
				}

				if (model.FromDate.Date >= model.ToDate.Date)
				{
					throw new InvalidOperationException("Last day is half day requires From Date to be before To Date (at least two calendar days in the range).");
				}

				if (!model.HalfDaySession.HasValue)
				{
					throw new InvalidOperationException("Half Day Session is required when Last day is half day is selected (session applies to the last day).");
				}
			}
		}

		private static HalfDaySession? ResolveHalfDaySession(ApplyLeaveViewModel model)
		{
			if (model.Duration == LeaveDuration.HalfDay)
			{
				return model.HalfDaySession;
			}

			if (model.Duration == LeaveDuration.FullDay && model.LastDayIsHalfDay)
			{
				return model.HalfDaySession;
			}

			return null;
		}

		private static decimal CalculateNoOfDays(ApplyLeaveViewModel model)
		{
			var from = model.FromDate.Date;
			var to = model.ToDate.Date;

			if (model.Duration == LeaveDuration.HalfDay)
			{
				return 0.5m;
			}

			// Every calendar day in the range counts, including Saturday and Sunday (no weekend skip).
			var inclusive = CountInclusiveDaysIncludingWeekends(from, to);

			if (model.LastDayIsHalfDay)
			{
				return inclusive - 0.5m;
			}

			return inclusive;
		}

		/// <summary>
		/// Inclusive day count from <paramref name="from"/> through <paramref name="to"/>.
		/// Saturday and Sunday are treated as work days for leave (always included).
		/// </summary>
		private static int CountInclusiveDaysIncludingWeekends(DateTime from, DateTime to)
		{
			var count = 0;
			for (var d = from; d <= to; d = d.AddDays(1))
			{
				count++;
			}

			return count;
		}
	}
}

