using HotelManagement.Data;
using HotelManagement.Models.Entities;
using HotelManagement.Services.Interfaces;

namespace HotelManagement.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly HotelContext _context;

        public PaymentService(HotelContext context)
        {
            _context = context;
        }

        public async Task AddPaymentAsync(int invoiceNo, decimal amount)
        {
            var payment = new Payment
            {
                Date = DateTime.UtcNow,
                InvoiceNo = invoiceNo,
                Amount = amount,
            };

            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();
        }
    }
}
