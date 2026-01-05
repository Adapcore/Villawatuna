using HotelManagement.Models.Entities;
using HotelManagement.Enums;

namespace HotelManagement.Services.Interfaces
{
    public interface IPaymentService
    {
        Task<IEnumerable<Payment>> GetAllAsync();
        Task<Payment?> GetByIdAsync(int id);
        Task<Payment> CreateAsync(Payment payment);
        Task<Payment> AddPaymentForInvoiceAsync(int invoiceNo, decimal amount, InvoicePaymentType method, string? reference, decimal? curryLastPaid = null, PaidCurrency paidCurrency = PaidCurrency.BaseCurrency);
        Task DeleteAsync(int id);
    }
}