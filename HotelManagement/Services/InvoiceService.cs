using HotelManagement.Data;
using HotelManagement.Enums;
using HotelManagement.Models.Entities;
using HotelManagement.Services.Interface;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Services
{
    public class InvoiceService : IInvoiceService
    {
        private readonly HotelContext _context;
        private readonly IMenuService _menuService;
        private readonly IRoomService _roomsService;
        private readonly IOtherTypeService _otherTypeService;
        private readonly ITourTypeService _tourTypeService;

        public InvoiceService(HotelContext context, IMenuService menuService, IRoomService roomService, IOtherTypeService otherTypeService, ITourTypeService tourTypeService)
        {
            _context = context;
            _menuService = menuService;
            _roomsService = roomService;
            _otherTypeService = otherTypeService;
            _tourTypeService = tourTypeService;
        }

        public async Task<List<Invoice>> GetAllInvoicesAsync()
        {
            return await _context.Invoices
                .Include(i => i.Customer)
                .OrderByDescending(i => i.Date).ThenByDescending(i => i.InvoiceNo)
                .ToListAsync();
        }

        public async Task<Invoice?> GetByIdAsync(int invoiceNo)
        {
            Invoice invoice = await _context.Invoices
                .Include(i => i.Customer)
                .Include(i => i.InvoiceDetails)
                .FirstOrDefaultAsync(i => i.InvoiceNo == invoiceNo);

            IEnumerable<ItemDto> menuItems = new List<ItemDto>();

            switch (invoice.Type)
            {
                case InvoiceType.Dining:
                case InvoiceType.TakeAway:
                    menuItems = await _menuService.GetItemsAsync();
                    break;

                case InvoiceType.Stay:
                    menuItems = await _roomsService.GetRoomCategoriesAsync();
                    break;

                case InvoiceType.Other:
                    menuItems = await _otherTypeService.GetItemsAsync();
                    break;

                case InvoiceType.Tour:
                    menuItems = await _tourTypeService.GetItemsAsync();
                    break;
            }

            foreach (var detail in invoice.InvoiceDetails)
            {
                detail.Item = menuItems.FirstOrDefault(m => m.Id == detail.ItemId);
            }

            return invoice;
        }

        public async Task<Invoice> CreateAsync(Invoice invoice)
        {
            // Set line numbers for invoice details
            int lineNumber = 1;
            foreach (var detail in invoice.InvoiceDetails)
            {
                detail.LineNumber = lineNumber++;
            }

            _context.Invoices.Add(invoice);
            await _context.SaveChangesAsync();
            return invoice;
        }

        public async Task UpdateInvoiceAsync(Invoice invoice)
        {

            _context.Entry(invoice).State = EntityState.Modified;
            await _context.SaveChangesAsync();        
        }

        public async Task DeleteInvoiceAsync(int invoiceNo)
        {
            var invoice = await _context.Invoices.FindAsync(invoiceNo);
            if (invoice != null)
            {
                _context.Invoices.Remove(invoice);
                await _context.SaveChangesAsync();
            }
        }

        public async Task DeleteInvoiceDetailsAsync(int invoiceNo)
        {
            var details = await _context.InvoiceDetails
                .Where(d => d.InvoiceNo == invoiceNo)
                .ToListAsync();

            if (details.Any())
            {
                _context.InvoiceDetails.RemoveRange(details);
                await _context.SaveChangesAsync();
            }
        }
    }
}
