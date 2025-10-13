using HotelManagement.Models.Entities;
using HotelManagement.Models.ViewModels;

namespace HotelManagement.Services
{
    public interface IInvoiceService
    {
        Task<List<Invoice>> GetAllInvoicesAsync();
        Task<Invoice?> GetByIdAsync(int invoiceNo);
        Task<Invoice> CreateAsync(Invoice invoice);
        //Task<int> CreateInvoiceAsync(CreateInvoiceViewModel model);
        Task UpdateInvoiceAsync(Invoice invoice);
        Task DeleteInvoiceAsync(int invoiceNo);
        Task DeleteInvoiceDetailsAsync(int invoiceId);
    }
}
