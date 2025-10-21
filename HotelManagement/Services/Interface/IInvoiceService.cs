using HotelManagement.Models.Entities;
using HotelManagement.Models.ViewModels;

namespace HotelManagement.Services.Interface
{
    public interface IInvoiceService
    {
        Task<List<Invoice>> GetAllInvoicesAsync();
        Task<Invoice?> GetByIdAsync(int invoiceNo);
        Task<Invoice> CreateAsync(Invoice invoice);
        Task UpdateInvoiceAsync(Invoice invoice);
        Task DeleteInvoiceAsync(int invoiceNo);
        Task DeleteInvoiceDetailsAsync(int invoiceId);
    }
}
