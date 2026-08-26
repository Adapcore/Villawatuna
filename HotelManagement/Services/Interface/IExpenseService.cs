using HotelManagement.Models.Entities;
using X.PagedList;

namespace HotelManagement.Services.Interfaces
{
    public interface IExpenseService
    {
        Task<IEnumerable<Expense>> GetAllAsync();
        Task<IEnumerable<Expense>> GetAllAsync(DateTime? startDate, DateTime? endDate, int? expenseTypeId, string? payeeName = null);

        Task<(IPagedList<Expense> Items, decimal TotalAmount)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            DateTime? startDate = null,
            DateTime? endDate = null,
            int? expenseTypeId = null,
            string? payeeName = null);

        Task<Expense?> GetByIdAsync(int id);
        Task AddAsync(Expense expense);
        Task UpdateAsync(Expense expense);
        Task DeleteAsync(int id);
    }
}
