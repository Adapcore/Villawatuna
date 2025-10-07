using HotelManagement.Data;
using HotelManagement.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Services
{
	public class EmployeeService : IEmployeeService
	{
		private readonly HotelContext _dbContext;

		public EmployeeService(HotelContext dbContext)
		{
			_dbContext = dbContext;
		}

		public async Task<List<Employee>> GetAllAsync()
		{
			return await _dbContext.Employees
				.OrderBy(e => e.FirstName)
				.ToListAsync();
		}

		public async Task<Employee?> GetByIdAsync(int id)
		{
			return await _dbContext.Employees.FindAsync(id);
		}

		public async Task<bool> EmailExistsAsync(string email, int? excludeId = null)
		{
			if (string.IsNullOrWhiteSpace(email))
			{
				return false;
			}

			IQueryable<Employee> query = _dbContext.Employees.Where(e => e.Email == email);
			if (excludeId.HasValue)
			{
				query = query.Where(e => e.ID != excludeId.Value);
			}

			return await query.AnyAsync();
		}

		public async Task<Employee> CreateAsync(Employee employee)
		{
			_dbContext.Employees.Add(employee);
			await _dbContext.SaveChangesAsync();
			return employee;
		}

		public async Task UpdateAsync(Employee employee)
		{
			_dbContext.Entry(employee).State = EntityState.Modified;
			await _dbContext.SaveChangesAsync();
		}
	}
}


