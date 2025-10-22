using HotelManagement.Enums;
using HotelManagement.Models.Entities;
using HotelManagement.Models.ViewModels;
using X.PagedList;

namespace HotelManagement.Services.Interface
{
    public interface IInvoiceService
    {
        Task<List<Invoice>> GetAllInvoicesAsync();
        Task<IPagedList<Invoice>> GetPagedInvoicesAsync(int pageNumber, int pageSize, int customerId = 0, InvoiceStatus? invoiceStatus = null);
        Task<int> GetPagedInvoicesCountAsync(int customerId = 0, InvoiceStatus? invoiceStatus = null);
        Task<Invoice?> GetByIdAsync(int invoiceNo);
        Task<Invoice> CreateAsync(Invoice invoice);
        Task UpdateInvoiceAsync(Invoice invoice);
        Task DeleteInvoiceAsync(int invoiceNo);
        Task DeleteInvoiceDetailsAsync(int invoiceId);
    }
}
