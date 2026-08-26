using HotelManagement.Models.Entities;
using HotelManagement.Enums;
using X.PagedList;

namespace HotelManagement.Services.Interfaces
{
    public interface IPaymentService
    {
        Task<IEnumerable<Payment>> GetAllAsync();

        Task<IPagedList<Payment>> GetPagedAsync(
            int pageNumber,
            int pageSize,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            InvoicePaymentType? type = null);

        Task<Payment?> GetByIdAsync(int id);
        Task<Payment> CreateAsync(Payment payment);
        Task<Payment> AddPaymentForInvoiceAsync(int invoiceNo, decimal amount, InvoicePaymentType method, string? reference, decimal? curryLastPaid = null, PaidCurrency paidCurrency = PaidCurrency.BaseCurrency);
        Task DeleteAsync(int id);
    }
}
