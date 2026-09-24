using HotelManagement.Models.Entities;
using X.PagedList;

namespace HotelManagement.Services.Interface
{
    public interface ICurrencyExchangeService
    {
        Task<IEnumerable<CurrencyExchange>> GetAllAsync(
            DateTime? startDate = null,
            DateTime? endDate = null,
            int? customerId = null,
            string? foreignCurrency = null);

        Task<(IPagedList<CurrencyExchange> Items, decimal TotalLkr)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            DateTime? startDate = null,
            DateTime? endDate = null,
            int? customerId = null,
            string? foreignCurrency = null);

        Task<CurrencyExchange?> GetByIdAsync(int id);
        Task AddAsync(CurrencyExchange exchange);
        Task AddForInvoiceBalanceSettlementAsync(CurrencyExchange exchange, Invoice sourceInvoice);
        Task UpdateAsync(CurrencyExchange exchange);
        Task DeleteAsync(int id);
    }
}
