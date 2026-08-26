using HotelManagement.Data;
using HotelManagement.Enums;
using HotelManagement.Helper;
using HotelManagement.Models.Entities;
using HotelManagement.Services.Interface;
using Microsoft.EntityFrameworkCore;
using X.PagedList;
using X.PagedList.Extensions;

namespace HotelManagement.Services
{
    public class PettyCashBookService : IPettyCashBookService
    {
        private readonly HotelContext _context;
        private readonly IExpenseTypeService _expenseTypeService;

        public PettyCashBookService(HotelContext context, IExpenseTypeService expenseTypeService)
        {
            _context = context;
            _expenseTypeService = expenseTypeService;
        }

        public async Task<IEnumerable<PettyCashBook>> GetAllAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            return await BuildFilteredQuery(startDate, endDate)
                .OrderByDescending(e => e.Date)
                .ToListAsync();
        }

        public Task<IPagedList<PettyCashBook>> GetPagedAsync(
            int pageNumber,
            int pageSize,
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            var paged = BuildFilteredQuery(startDate, endDate)
                .OrderByDescending(e => e.Date)
                .ToPagedList(pageNumber, pageSize);

            return Task.FromResult(paged);
        }

        private IQueryable<PettyCashBook> BuildFilteredQuery(DateTime? startDate, DateTime? endDate)
        {
            var query = _context.PettyCashBooks
                .AsNoTracking()
                .AsQueryable();

            if (startDate.HasValue)
                query = query.Where(e => e.Date >= startDate.Value.Date);

            if (endDate.HasValue)
                query = query.Where(e => e.Date <= endDate.Value.Date);

            return query;
        }

        public async Task<PettyCashBook?> GetByIdAsync(int id)
        {
            var book = await _context.PettyCashBooks
                .FirstOrDefaultAsync(e => e.Id == id);
            if (book != null)
                HydrateRefills(book);
            return book;
        }

        public async Task<PettyCashBook?> GetByDateAsync(DateTime date)
        {
            var day = date.Date;
            var book = await _context.PettyCashBooks
                .FirstOrDefaultAsync(e => e.Date == day);
            if (book != null)
                HydrateRefills(book);
            return book;
        }

        private static void HydrateRefills(PettyCashBook book)
        {
            book.Refills ??= new List<PettyCashRefillLine>();
            if (book.Refills.Count == 0 && book.PettyCashRefillOpening > 0)
            {
                book.Refills.Add(new PettyCashRefillLine
                {
                    Id = 1,
                    Title = "Refill",
                    Amount = book.PettyCashRefillOpening
                });
            }
        }

        public async Task<decimal> GetPreviousCloseBalanceAsync(DateTime date)
        {
            var day = date.Date;
            var previousClose = await _context.PettyCashBooks
                .Where(e => e.Date < day)
                .OrderByDescending(e => e.Date)
                .Select(e => (decimal?)e.ClosingBalance)
                .FirstOrDefaultAsync();

            return previousClose ?? 0m;
        }

        public async Task<List<PettyCashIncomeLine>> GetIncomeLinesFromPaymentsAsync(DateTime date)
        {
            var day = date.Date;
            var nextDay = day.AddDays(1);

            var payments = await _context.Payments
                .AsNoTracking()
                .Include(p => p.Invoice)
                    .ThenInclude(i => i.Customer)
                .Where(p => p.Date >= day && p.Date < nextDay
                    && p.Type == InvoicePaymentType.Cash)
                .OrderBy(p => p.Date)
                .ThenBy(p => p.ID)
                .ToListAsync();

            var lines = new List<PettyCashIncomeLine>();
            int lineId = 1;
            foreach (var payment in payments)
            {
                var invoice = payment.Invoice;
                var customer = invoice?.Customer;
                var customerId = invoice?.CustomerId ?? 0;
                var customerName = customer == null
                    ? string.Empty
                    : $"{customer.FirstName} {customer.LastName}".Trim();
                var invoiceType = invoice != null
                    ? EnumHelper.GetDisplayName(invoice.Type)
                    : string.Empty;

                var title = $"#{payment.InvoiceNo} - #{customerId} - {customerName} - {invoiceType}";
                if (title.Length > 250)
                    title = title.Substring(0, 250);

                string currency;
                decimal amount;
                if (payment.PaidCurrency == PaidCurrency.SelectedCurrency)
                {
                    currency = NormalizeCurrency(invoice?.Currency);
                    amount = payment.CurryAmount.GetValueOrDefault(payment.Amount);
                }
                else
                {
                    currency = "LKR";
                    amount = payment.Amount;
                }

                lines.Add(new PettyCashIncomeLine
                {
                    Id = lineId++,
                    Title = title,
                    Currency = currency,
                    Amount = Math.Round(amount, 2),
                    InvoiceTypeCode = invoice?.Type.ToString()
                });
            }

            return lines;
        }

        public async Task<List<PettyCashExpenseLine>> GetExpenseLinesFromExpensesAsync(DateTime date)
        {
            var day = date.Date;
            var nextDay = day.AddDays(1);

            var expenses = await _context.Expenses
                .AsNoTracking()
                .Where(e => e.Date >= day && e.Date < nextDay
                    && e.PaymentMethod == PaymentMethod.Cash)
                .OrderBy(e => e.Date)
                .ThenBy(e => e.ID)
                .ToListAsync();

            var expenseTypes = (await _expenseTypeService.GetExpenseTypesAsync()).ToList();
            var typeLookup = expenseTypes.ToDictionary(t => t.Id, t => t.Name ?? string.Empty);

            var lines = new List<PettyCashExpenseLine>();
            int lineId = 1;
            foreach (var expense in expenses)
            {
                var expenseTypeName = typeLookup.TryGetValue(expense.ExpenseTypeID, out var name)
                    ? name
                    : string.Empty;
                var payee = (expense.PayeeName ?? string.Empty).Trim();

                var title = $"#{expense.ID} - {expenseTypeName} - {payee}";
                if (title.Length > 250)
                    title = title.Substring(0, 250);

                lines.Add(new PettyCashExpenseLine
                {
                    Id = lineId++,
                    Title = title,
                    Currency = "LKR",
                    Amount = Math.Round(expense.Amount, 2)
                });
            }

            return lines;
        }

        public void CalculateTotals(PettyCashBook book)
        {
            book.Income ??= new List<PettyCashIncomeLine>();
            book.Expences ??= new List<PettyCashExpenseLine>();
            book.Refills ??= new List<PettyCashRefillLine>();

            // Strip empty rows
            book.Income = book.Income
                .Where(i => !string.IsNullOrWhiteSpace(i.Title) || i.Amount != 0)
                .ToList();
            book.Expences = book.Expences
                .Where(e => !string.IsNullOrWhiteSpace(e.Title) || e.Amount != 0)
                .ToList();
            book.Refills = book.Refills
                .Where(r => !string.IsNullOrWhiteSpace(r.Title) || r.Amount != 0)
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

            int refillId = 1;
            foreach (var line in book.Refills)
            {
                line.Id = refillId++;
                line.Title = (line.Title ?? string.Empty).Trim();
                line.Amount = Math.Max(0, Math.Round(line.Amount, 2));
            }

            book.OpeningBalance = Math.Round(book.OpeningBalance, 2);
            book.PettyCashRefillOpening = Math.Round(book.Refills.Sum(r => r.Amount), 2);
            book.PettyCashRefillClosing = Math.Max(0, Math.Round(book.PettyCashRefillClosing, 2));
            book.HandedOver = Math.Max(0, Math.Round(book.HandedOver, 2));

            // Only one of Handed Over or Refill (Closing) may be greater than zero
            if (book.HandedOver > 0)
                book.PettyCashRefillClosing = 0;
            else if (book.PettyCashRefillClosing > 0)
                book.HandedOver = 0;

            // Daily Balance = Opening + Refill Opening + LKR income - LKR expenses
            book.Balance = Math.Round(
                book.OpeningBalance + book.PettyCashRefillOpening + book.TotalIncomeBaseCurry - book.TotalExpences, 2);

            // Petty Cash (Closing) = Daily Balance - Handed Over + Refill (Closing)
            book.ClosingBalance = Math.Round(
                book.Balance - book.HandedOver + book.PettyCashRefillClosing, 2);
        }

        public async Task<PettyCashBook> CreateAsync(PettyCashBook book)
        {
            book.Date = book.Date.Date;

            var existing = await GetByDateAsync(book.Date);
            if (existing != null)
                throw new InvalidOperationException($"A petty cash entry already exists for {book.Date:yyyy-MM-dd}.");

            book.OpeningBalance = await GetPreviousCloseBalanceAsync(book.Date);
            book.Income ??= new List<PettyCashIncomeLine>();
            book.Expences ??= new List<PettyCashExpenseLine>();
            book.Refills ??= new List<PettyCashRefillLine>();
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
            existing.PettyCashRefillOpening = book.PettyCashRefillOpening;
            existing.PettyCashRefillClosing = book.PettyCashRefillClosing;
            existing.HandedOver = book.HandedOver;
            existing.Income = book.Income ?? new List<PettyCashIncomeLine>();
            existing.Expences = book.Expences ?? new List<PettyCashExpenseLine>();
            existing.Refills = book.Refills ?? new List<PettyCashRefillLine>();

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
