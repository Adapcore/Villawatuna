using HotelManagement.Data;
using HotelManagement.Models.Entities;
using HotelManagement.Services.Interface;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Services
{
    public class CurrencyExchangeService : ICurrencyExchangeService
    {
        private readonly HotelContext _context;
        private readonly ICurrencyService _currencyService;

        public CurrencyExchangeService(HotelContext context, ICurrencyService currencyService)
        {
            _context = context;
            _currencyService = currencyService;
        }

        public async Task<IEnumerable<CurrencyExchange>> GetAllAsync(
            DateTime? startDate = null,
            DateTime? endDate = null,
            int? customerId = null,
            int? foreignCurrencyId = null)
        {
            var query = _context.CurrencyExchanges
                .Include(e => e.Customer)
                .AsQueryable();

            if (startDate.HasValue)
                query = query.Where(e => e.Date >= startDate.Value.Date);

            if (endDate.HasValue)
                query = query.Where(e => e.Date < endDate.Value.Date.AddDays(1));

            if (customerId.HasValue && customerId.Value > 0)
                query = query.Where(e => e.CustomerId == customerId.Value);

            if (foreignCurrencyId.HasValue && foreignCurrencyId.Value > 0)
                query = query.Where(e => e.ForeignCurrencyId == foreignCurrencyId.Value);

            var exchanges = await query
                .OrderByDescending(e => e.Id)
                .ToListAsync();

            var currencies = (await _currencyService.GetCurencyTypesAsync()).ToList();
            foreach (var item in exchanges)
            {
                item.ForeignCurrency = currencies.FirstOrDefault(c => c.Id == item.ForeignCurrencyId);
            }

            return exchanges;
        }

        public async Task<CurrencyExchange?> GetByIdAsync(int id)
        {
            var exchange = await _context.CurrencyExchanges
                .Include(e => e.Customer)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (exchange == null)
                return null;

            var currencies = await _currencyService.GetCurencyTypesAsync();
            exchange.ForeignCurrency = currencies.FirstOrDefault(c => c.Id == exchange.ForeignCurrencyId);
            return exchange;
        }

        public async Task AddAsync(CurrencyExchange exchange)
        {
            PrepareForSave(exchange);
            _context.CurrencyExchanges.Add(exchange);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(CurrencyExchange exchange)
        {
            var existing = await _context.CurrencyExchanges.FirstOrDefaultAsync(e => e.Id == exchange.Id);
            if (existing == null)
                throw new InvalidOperationException("Currency exchange not found.");

            existing.Date = exchange.Date.Date;
            existing.CustomerId = exchange.CustomerId;
            existing.ForeignCurrencyId = exchange.ForeignCurrencyId;
            existing.ExchangeRate = exchange.ExchangeRate;
            existing.ForeignAmount = exchange.ForeignAmount;
            existing.LKRAmount = CalculateLkrAmount(exchange.ExchangeRate, exchange.ForeignAmount);
            existing.Remarks = string.IsNullOrWhiteSpace(exchange.Remarks) ? null : exchange.Remarks.Trim();

            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var existing = await _context.CurrencyExchanges.FindAsync(id);
            if (existing == null)
                return;

            _context.CurrencyExchanges.Remove(existing);
            await _context.SaveChangesAsync();
        }

        private static void PrepareForSave(CurrencyExchange exchange)
        {
            exchange.Customer = null;
            exchange.ForeignCurrency = null;
            exchange.Date = exchange.Date.Date;
            exchange.Remarks = string.IsNullOrWhiteSpace(exchange.Remarks) ? null : exchange.Remarks.Trim();
            exchange.LKRAmount = CalculateLkrAmount(exchange.ExchangeRate, exchange.ForeignAmount);
        }

        private static decimal CalculateLkrAmount(decimal exchangeRate, decimal foreignAmount)
        {
            return Math.Round(exchangeRate * foreignAmount, 2, MidpointRounding.AwayFromZero);
        }
    }
}
