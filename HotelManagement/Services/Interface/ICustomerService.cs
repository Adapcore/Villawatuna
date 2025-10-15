using HotelManagement.Models.Entities;

namespace HotelManagement.Services.Interface
{
    public interface ICustomerService
    {
        Task<List<Customer>> GetAllAsync();
        Task<Customer?> GetByIdAsync(int id);
        Task<bool> EmailExistsAsync(string email, int? excludeId = null);
        Task<Customer> CreateAsync(Customer customer);
        Task UpdateAsync(Customer customer);
    }
}
