using HotelManagement.Data;
using HotelManagement.Models.Entities;
using HotelManagement.Services.Interface;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Services
{
    public class PettyCashBookService : IPettyCashBookService
    {
        private readonly HotelContext _context;

        public PettyCashBookService(HotelContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<PettyCashBook>> GetAllAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.PettyCashBooks.AsQueryable();

            if (startDate.HasValue)
                query = query.Where(e => e.Date >= startDate.Value.Date);

            if (endDate.HasValue)
                query = query.Where(e => e.Date <= endDate.Value.Date);

            return await query
                .OrderByDescending(e => e.Date)
                .ToListAsync();
        }

        public async Task<PettyCashBook?> GetByIdAsync(int id)
        {
            return await _context.PettyCashBooks.FindAsync(id);
        }

        public async Task<PettyCashBook?> GetByDateAsync(DateTime date)
        {
            var day = date.Date;
            return await _context.PettyCashBooks
                .FirstOrDefaultAsync(e => e.Date == day);
        }

        public async Task<decimal> GetPreviousCloseBalanceAsync(DateTime date)
        {
            var day = date.Date;
            var previous = await _context.PettyCashBooks
                .Where(e => e.Date < day)
                .OrderByDescending(e => e.Date)
                .FirstOrDefaultAsync();

            return previous?.ClosingBalance ?? 0m;
        }

        public void CalculateTotals(PettyCashBook book)
        {
            book.Income ??= new List<PettyCashIncomeLine>();
            book.Expences ??= new List<PettyCashExpenseLine>();

            // Strip empty rows
            book.Income = book.Income
                .Where(i => !string.IsNullOrWhiteSpace(i.Title) || i.Amount != 0)
                .ToList();
            book.Expences = book.Expences
                .Where(e => !string.IsNullOrWhiteSpace(e.Title) || e.Amount != 0)
                .ToList();

            // Normalize currency codes and assign line ids
            int incomeId = 1;
            foreach (var line in book.Income)
            {
                line.Id = incomeId++;
                line.Title = (line.Title ?? string.Empty).Trim();
                line.Currency = NormalizeCurrency(line.Currency);
                line.Amount = Math.Round(line.Amount, 2);
            }

            // Expenses are LKR only — no foreign currency, no conversion
            int expenseId = 1;
            foreach (var line in book.Expences)
            {
                line.Id = expenseId++;
                line.Title = (line.Title ?? string.Empty).Trim();
                line.Currency = "LKR";
                line.Amount = Math.Round(line.Amount, 2);
            }

            // Group income by currency (no conversion — each currency kept separately)
            book.TotalIncome = book.Income
                .GroupBy(i => i.Currency)
                .Select(g => new PettyCashCurrencyTotal
                {
                    Currency = g.Key,
                    Total = Math.Round(g.Sum(x => x.Amount), 2)
                })
                .OrderBy(t => t.Currency)
                .ToList();

            // LKR income total only (used for LKR balance sheet fields)
            book.TotalIncomeBaseCurry = book.TotalIncome
                .FirstOrDefault(t => t.Currency == "LKR")?.Total ?? 0m;

            book.TotalExpences = Math.Round(book.Expences.Sum(e => e.Amount), 2);

            // MoneyInHand hidden in UI for now — keep at 0
            book.MoneyInHand = 0m;
            book.OpeningBalance = Math.Round(book.OpeningBalance, 2);
            book.HandedOver = Math.Round(book.HandedOver, 2);

            // Balance = OpeningBalance + LKR income - LKR expenses
            book.Balance = Math.Round(book.OpeningBalance + book.TotalIncomeBaseCurry - book.TotalExpences, 2);

            // ClosingBalance = Balance - HandedOver
            book.ClosingBalance = Math.Round(book.Balance - book.HandedOver, 2);
        }

        public async Task<PettyCashBook> CreateAsync(PettyCashBook book)
        {
            book.Date = book.Date.Date;

            var existing = await GetByDateAsync(book.Date);
            if (existing != null)
                throw new InvalidOperationException($"A petty cash entry already exists for {book.Date:yyyy-MM-dd}.");

            book.OpeningBalance = await GetPreviousCloseBalanceAsync(book.Date);
            CalculateTotals(book);

            _context.PettyCashBooks.Add(book);
            await _context.SaveChangesAsync();
            return book;
        }

        public async Task<PettyCashBook> UpdateAsync(PettyCashBook book)
        {
            var existing = await _context.PettyCashBooks.FindAsync(book.Id)
                ?? throw new InvalidOperationException("Petty cash entry not found.");

            book.Date = book.Date.Date;

            var duplicate = await _context.PettyCashBooks
                .FirstOrDefaultAsync(e => e.Date == book.Date && e.Id != book.Id);
            if (duplicate != null)
                throw new InvalidOperationException($"A petty cash entry already exists for {book.Date:yyyy-MM-dd}.");

            // Opening balance is fixed at create time (no cascade to future days)
            existing.Date = book.Date;
            existing.HandedOver = book.HandedOver;
            existing.Income = book.Income ?? new List<PettyCashIncomeLine>();
            existing.Expences = book.Expences ?? new List<PettyCashExpenseLine>();

            CalculateTotals(existing);

            await _context.SaveChangesAsync();
            return existing;
        }

        public async Task DeleteAsync(int id)
        {
            var entity = await _context.PettyCashBooks.FindAsync(id);
            if (entity == null)
                return;

            _context.PettyCashBooks.Remove(entity);
            await _context.SaveChangesAsync();
        }

        private static string NormalizeCurrency(string? currency)
        {
            if (string.IsNullOrWhiteSpace(currency))
                return "LKR";
            return currency.Trim().ToUpperInvariant();
        }
    }
}
