using HotelManagement.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Repositories
{
	public interface IEmployeeLeaveRepository
	{
		Task<EmployeeLeave> AddAsync(EmployeeLeave leave, CancellationToken cancellationToken = default);
		Task<EmployeeLeave?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
		Task<EmployeeLeave?> GetForUpdateAsync(int id, CancellationToken cancellationToken = default);
		Task SaveChangesAsync(CancellationToken cancellationToken = default);
		IQueryable<EmployeeLeave> Query();
	}
}

