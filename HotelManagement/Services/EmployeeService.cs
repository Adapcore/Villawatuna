using HotelManagement.Data;
using HotelManagement.Models.Entities;
using HotelManagement.Services.Interface;
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
			Employee? existing = await _dbContext.Employees.FindAsync(employee.ID);
			if (existing == null)
			{
				throw new InvalidOperationException($"Employee {employee.ID} was not found.");
			}

			// Personal
			existing.FirstName = employee.FirstName;
			existing.LastName = employee.LastName;
			existing.NameWithInitials = employee.NameWithInitials;
			existing.Email = employee.Email;
			existing.ContactNo = employee.ContactNo;
			existing.PermanentAddress = employee.PermanentAddress;
			existing.CurrentAddress = employee.CurrentAddress;
			existing.NIC = employee.NIC;
			existing.DOB = employee.DOB;
			existing.Gender = employee.Gender;
			existing.MaritalStatus = employee.MaritalStatus;

			// Emergency
			existing.EmergencyContactName = employee.EmergencyContactName;
			existing.EmergencyContactRelationship = employee.EmergencyContactRelationship;
			existing.EmergencyContactMobile = employee.EmergencyContactMobile;
			existing.EmergencyContactAddress = employee.EmergencyContactAddress;

			// Employment
			existing.JobTitle = employee.JobTitle;
			existing.Department = employee.Department;
			existing.JoinedDate = employee.JoinedDate;
			existing.EmploymentType = employee.EmploymentType;
			existing.AgreedSalary = employee.AgreedSalary;
			existing.WorkingLocation = employee.WorkingLocation;
			existing.WorkingDays = employee.WorkingDays;
			existing.WorkingDaysOther = employee.WorkingDaysOther;
			existing.WorkingHours = employee.WorkingHours;
			existing.EPFNo = employee.EPFNo;

			// Bank
			existing.BankAccountHolderName = employee.BankAccountHolderName;
			existing.BankName = employee.BankName;
			existing.BankBranchName = employee.BankBranchName;
			existing.BankAccountNumber = employee.BankAccountNumber;

			existing.LastModifiedDate = DateTime.UtcNow;

			await _dbContext.SaveChangesAsync();
		}
	}
}
