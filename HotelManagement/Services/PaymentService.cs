using HotelManagement.Data;
using HotelManagement.Models.Entities;
using HotelManagement.Enums;
using HotelManagement.Services.Interfaces;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.EntityFrameworkCore;

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
                .OrderByDescending(p => p.Date)
                .ToListAsync();
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

        public async Task<Payment> AddPaymentForInvoiceAsync(int invoiceNo, decimal amount, InvoicePaymentType type, string? reference)
        {
            var payment = new Payment
            {
                Date = DateTime.UtcNow,
                InvoiceNo = invoiceNo,
                Amount = amount,
                Type = type,
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