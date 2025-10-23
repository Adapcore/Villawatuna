using HotelManagement.Enums;
using HotelManagement.Models.Entities;
using X.PagedList;

namespace HotelManagement.Models.ViewModels
{

    public class InvoiceIndexViewModel
    {
        public int CustomerId { get; set; }
        public IPagedList<Invoice> Invoices { get; set; }
    }
}