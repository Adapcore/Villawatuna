using HotelManagement.Data;
using HotelManagement.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Repositories
{
	public class EmployeeLeaveRepository : IEmployeeLeaveRepository
	{
		private readonly HotelContext _dbContext;

		public EmployeeLeaveRepository(HotelContext dbContext)
		{
			_dbContext = dbContext;
		}

		public async Task<EmployeeLeave> AddAsync(EmployeeLeave leave, CancellationToken cancellationToken = default)
		{
			_dbContext.EmployeeLeaves.Add(leave);
			await _dbContext.SaveChangesAsync(cancellationToken);
			return leave;
		}

		public async Task<EmployeeLeave?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
		{
			return await _dbContext.EmployeeLeaves
				.AsNoTracking()
				.FirstOrDefaultAsync(l => l.ID == id, cancellationToken);
		}

		public async Task<EmployeeLeave?> GetForUpdateAsync(int id, CancellationToken cancellationToken = default)
		{
			return await _dbContext.EmployeeLeaves
				.FirstOrDefaultAsync(l => l.ID == id, cancellationToken);
		}

		public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
		{
			await _dbContext.SaveChangesAsync(cancellationToken);
		}

		public IQueryable<EmployeeLeave> Query()
		{
			return _dbContext.EmployeeLeaves.AsQueryable();
		}
	}
}

