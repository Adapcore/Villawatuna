using HotelManagement.Models.Entities;
using X.PagedList;

namespace HotelManagement.Services.Interface
{
    public interface IPettyCashBookService
    {
        Task<IEnumerable<PettyCashBook>> GetAllAsync(DateTime? startDate = null, DateTime? endDate = null);

        Task<IPagedList<PettyCashBook>> GetPagedAsync(
            int pageNumber,
            int pageSize,
            DateTime? startDate = null,
            DateTime? endDate = null);

        Task<PettyCashBook?> GetByIdAsync(int id);
        Task<PettyCashBook?> GetByDateAsync(DateTime date);
        Task<decimal> GetPreviousCloseBalanceAsync(DateTime date);
        Task<List<PettyCashIncomeLine>> GetIncomeLinesFromPaymentsAsync(DateTime date);
        Task<List<PettyCashExpenseLine>> GetExpenseLinesFromExpensesAsync(DateTime date);
        void CalculateTotals(PettyCashBook book);
        Task<PettyCashBook> CreateAsync(PettyCashBook book);
        Task<PettyCashBook> UpdateAsync(PettyCashBook book);
        Task DeleteAsync(int id);
    }
}
