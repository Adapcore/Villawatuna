using HotelManagement.Models.Entities;

namespace HotelManagement.Services.Interface
{
    public interface IEmployeeService
    {
        Task<List<Employee>> GetAllAsync();
        Task<Employee?> GetByIdAsync(int id);
        Task<bool> EmailExistsAsync(string email, int? excludeId = null);
        Task<Employee> CreateAsync(Employee employee);
        Task UpdateAsync(Employee employee);
    }
}


