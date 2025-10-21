using HotelManagement.Models.Entities;

namespace HotelManagement.Services.Interfaces
{
    public interface IPaymentService
    {
        Task<IEnumerable<Payment>> GetAllAsync();
        Task<Payment?> GetByIdAsync(int id);
        Task<Payment> CreateAsync(Payment payment);
        Task<Payment> AddPaymentForInvoiceAsync(int invoiceNo, decimal amount);
        Task DeleteAsync(int id);
    }
}