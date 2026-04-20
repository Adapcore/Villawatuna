using HotelManagement.Models.ViewModels;
using HotelManagement.Enums;
using HotelManagement.Models.Entities;
using X.PagedList;

namespace HotelManagement.Services.Interface
{
	public interface IEmployeeLeaveService
	{
		Task ApplyAsync(ApplyLeaveViewModel model, int createdByUserId, CancellationToken cancellationToken = default);
		Task<EmployeeLeave?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
		Task UpdateAsync(EditLeaveViewModel model, CancellationToken cancellationToken = default);
		Task ApproveAsync(int id, int adminUserId, CancellationToken cancellationToken = default);
		Task RejectAsync(int id, int adminUserId, CancellationToken cancellationToken = default);
		Task CancelAsync(int id, int userId, CancellationToken cancellationToken = default);
		Task<(IPagedList<EmployeeLeave> Leaves, decimal TotalDays, decimal OpenDays, decimal ApprovedDays, decimal RejectedDays)> GetPagedAsync(
			int pageNumber,
			int pageSize,
			int? employeeId = null,
			DateTime? fromDate = null,
			DateTime? toDate = null,
			LeaveStatus? status = null,
			CancellationToken cancellationToken = default);
	}
}

