using HotelManagement.Models.Entities;

namespace HotelManagement.Services.Interface
{
    public interface ICurrencyExchangeService
    {
        Task<IEnumerable<CurrencyExchange>> GetAllAsync(DateTime? startDate = null, DateTime? endDate = null, int? customerId = null, int? foreignCurrencyId = null);
        Task<CurrencyExchange?> GetByIdAsync(int id);
        Task AddAsync(CurrencyExchange exchange);
        Task UpdateAsync(CurrencyExchange exchange);
        Task DeleteAsync(int id);
    }
}
