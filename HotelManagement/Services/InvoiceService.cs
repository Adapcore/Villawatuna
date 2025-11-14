using HotelManagement.Data;
using HotelManagement.Enums;
using HotelManagement.Models.Entities;
using HotelManagement.Services.Interface;
using Microsoft.EntityFrameworkCore;
using X.PagedList;
using X.PagedList.Extensions;

namespace HotelManagement.Services
{
    public class InvoiceService : IInvoiceService
    {
        private readonly HotelContext _context;
        private readonly IMenuService _menuService;
        private readonly IRoomService _roomsService;
        private readonly IOtherTypeService _otherTypeService;
        private readonly ITourTypeService _tourTypeService;
        private readonly ILaundryService _laundryService;

        public InvoiceService(HotelContext context,
            IMenuService menuService,
            IRoomService roomService,
            IOtherTypeService otherTypeService,
            ITourTypeService tourTypeService,
             ILaundryService laundryService)
        {
            _context = context;
            _menuService = menuService;
            _roomsService = roomService;
            _otherTypeService = otherTypeService;
            _tourTypeService = tourTypeService;
            _laundryService = laundryService;
        }

        public async Task<List<Invoice>> GetAllInvoicesAsync()
        {
            return await _context.Invoices
                .Include(i => i.Customer)
                .OrderByDescending(i => i.Date).ThenByDescending(i => i.InvoiceNo)
                .ToListAsync();
        }

        public async Task<IPagedList<Invoice>> GetPagedInvoicesAsync(
            int pageNumber,
            int pageSize,
            int customerId = 0,
            InvoiceStatus? invoiceStatus = null)
        {
            var query = _context.Invoices
                .Include(i => i.Customer)
                .AsQueryable();

            if (customerId > 0)
                query = query.Where(x => x.CustomerId == customerId);

            if (invoiceStatus.HasValue)
                query = query.Where(x => x.Status == invoiceStatus.Value);

            query = query
                .OrderByDescending(i => i.InvoiceNo);

            return query.ToPagedList(pageNumber, pageSize); ;
        }


        public async Task<int> GetPagedInvoicesCountAsync(int customerId = 0, InvoiceStatus? invoiceStatus = null)
        {
            var query = _context.Invoices
                .Include(i => i.Customer).AsQueryable();

            if (customerId > 0)
                query = query.Where(x => x.CustomerId == customerId);

            if (invoiceStatus != null)
                query = query.Where(x => x.Status == invoiceStatus);

            return await query.CountAsync();
        }

        public async Task<Invoice?> GetByIdAsync(int invoiceNo)
        {
            if (invoiceNo <= 0)
                return null;

            Invoice invoice = await _context.Invoices
                .Include(i => i.Customer)
                .Include(i => i.InvoiceDetails)
                .Include(i => i.Payments)
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

                case InvoiceType.Laundry:
                    menuItems = await _laundryService.GetItemsAsync();
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
