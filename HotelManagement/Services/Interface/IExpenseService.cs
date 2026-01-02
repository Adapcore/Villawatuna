using HotelManagement.Models.Entities;

namespace HotelManagement.Services.Interfaces
{
    public interface IExpenseService
    {
        Task<IEnumerable<Expense>> GetAllAsync();
        Task<IEnumerable<Expense>> GetAllAsync(DateTime? startDate, DateTime? endDate, int? expenseTypeId, string payeeName = null);
        Task<Expense?> GetByIdAsync(int id);
        Task AddAsync(Expense expense);
        Task UpdateAsync(Expense expense);
        Task DeleteAsync(int id);
    }
}
