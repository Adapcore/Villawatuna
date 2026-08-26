using HotelManagement.Data;
using HotelManagement.Models.Entities;
using HotelManagement.Enums;
using HotelManagement.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using X.PagedList;
using X.PagedList.Extensions;

namespace HotelManagement.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly HotelContext _context;

        public PaymentService(HotelContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Payment>> GetAllAsync()
        {
            return await _context.Payments
                .AsNoTracking()
                .Include(p => p.Invoice)
                .OrderByDescending(p => p.Date)
                .ThenByDescending(p => p.ID)
                .ToListAsync();
        }

        public Task<IPagedList<Payment>> GetPagedAsync(
            int pageNumber,
            int pageSize,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            InvoicePaymentType? type = null)
        {
            var query = BuildFilteredQuery(fromDate, toDate, type)
                .OrderByDescending(p => p.Date)
                .ThenByDescending(p => p.ID);

            var paged = query.ToPagedList(pageNumber, pageSize);
            return Task.FromResult(paged);
        }

        private IQueryable<Payment> BuildFilteredQuery(
            DateTime? fromDate,
            DateTime? toDate,
            InvoicePaymentType? type)
        {
            var query = _context.Payments
                .AsNoTracking()
                .Include(p => p.Invoice)
                .AsQueryable();

            if (fromDate.HasValue)
                query = query.Where(p => p.Date >= fromDate.Value.Date);

            if (toDate.HasValue)
                query = query.Where(p => p.Date < toDate.Value.Date.AddDays(1));

            if (type.HasValue)
                query = query.Where(p => p.Type == type.Value);

            return query;
        }

        public async Task<Payment?> GetByIdAsync(int id)
        {
            return await _context.Payments.FindAsync(id);
        }

        public async Task<Payment> CreateAsync(Payment payment)
        {
            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();
            return payment;
        }

        public async Task<Payment> AddPaymentForInvoiceAsync(int invoiceNo, decimal amount, InvoicePaymentType type, string? reference, decimal? curryLastPaid = null, PaidCurrency paidCurrency = PaidCurrency.BaseCurrency)
        {
            var payment = new Payment
            {
                Date = DateTime.UtcNow,
                InvoiceNo = invoiceNo,
                Amount = amount,
                CurryAmount = curryLastPaid,
                Type = type,
                PaidCurrency = paidCurrency,
                Reference = reference
            };

            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();

            return payment;
        }

        public async Task DeleteAsync(int id)
        {
            var payment = await _context.Payments.FindAsync(id);
            if (payment != null)
            {
                _context.Payments.Remove(payment);
                await _context.SaveChangesAsync();
            }
        }
    }
}
