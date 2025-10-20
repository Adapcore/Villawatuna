using HotelManagement.Models.Entities;

namespace HotelManagement.Services.Interfaces
{
    public interface IExpenseService
    {
        Task<IEnumerable<Expense>> GetAllAsync();
        Task<Expense?> GetByIdAsync(int id);
        Task AddAsync(Expense expense);
        Task UpdateAsync(Expense expense);
        Task DeleteAsync(int id);
    }
}
