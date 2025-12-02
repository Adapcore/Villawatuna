using HotelManagement.Data;
using HotelManagement.Enums;
using HotelManagement.Models.Entities;
using HotelManagement.Models.DTO;
using HotelManagement.Services.Interface;
using Microsoft.EntityFrameworkCore;
using X.PagedList;
using X.PagedList.Extensions;
using Umbraco.Cms.Core.Services;

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
        private readonly IMemberService _memberService;

        public InvoiceService(HotelContext context,
            IMenuService menuService,
            IRoomService roomService,
            IOtherTypeService otherTypeService,
            ITourTypeService tourTypeService,
            ILaundryService laundryService,
            IMemberService memberService)
        {
            _context = context;
            _menuService = menuService;
            _roomsService = roomService;
            _otherTypeService = otherTypeService;
            _tourTypeService = tourTypeService;
            _laundryService = laundryService;
            _memberService = memberService;
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
            InvoiceStatus? invoiceStatus = null,
            InvoiceType? invoiceType = null,
            DateTime? fromDate = null,
            DateTime? toDate = null)
        {
            var query = _context.Invoices
                .Include(i => i.Customer)
                .AsQueryable();

            if (customerId > 0)
                query = query.Where(x => x.CustomerId == customerId);

            if (invoiceStatus.HasValue)
                query = query.Where(x => x.Status == invoiceStatus.Value);

            if (invoiceType.HasValue)
                query = query.Where(x => x.Type == invoiceType.Value);

            if (fromDate.HasValue)
                query = query.Where(x => x.Date >= fromDate.Value.Date);

            if (toDate.HasValue)
                query = query.Where(x => x.Date <= toDate.Value.Date.AddDays(1).AddTicks(-1));

            query = query
                .OrderByDescending(i => i.InvoiceNo);

            var pagedList = query.ToPagedList(pageNumber, pageSize);
            
            // Load creator data from Umbraco members (CreatedBy stores Umbraco Member IDs)
            var createdByIds = pagedList.Where(i => i.CreatedBy > 0).Select(i => i.CreatedBy).Distinct().ToList();
            
            if (createdByIds.Any())
            {
                // Load Umbraco members by ID
                var memberDict = new Dictionary<int, MemberDTO>();
                
                foreach (var memberId in createdByIds)
                {
                    try
                    {
                        var member = _memberService.GetById(memberId);
                        if (member != null)
                        {
                            memberDict[memberId] = new MemberDTO
                            {
                                Id = member.Id,
                                Name = member.Name ?? member.Username ?? "",
                                Username = member.Username ?? "",
                                Email = member.Email ?? ""
                            };
                        }
                    }
                    catch
                    {
                        // Member not found, skip
                    }
                }
                
                // Assign member data to invoices
                foreach (var invoice in pagedList)
                {
                    if (invoice.CreatedBy > 0 && memberDict.TryGetValue(invoice.CreatedBy, out var member))
                    {
                        invoice.CreatedByMember = member;
                    }
                }
            }
            
            return pagedList;
        }


        public async Task<int> GetPagedInvoicesCountAsync(int customerId = 0, InvoiceStatus? invoiceStatus = null, InvoiceType? invoiceType = null, DateTime? fromDate = null, DateTime? toDate = null)
        {
            var query = _context.Invoices
                .Include(i => i.Customer).AsQueryable();

            if (customerId > 0)
                query = query.Where(x => x.CustomerId == customerId);

            if (invoiceStatus != null)
                query = query.Where(x => x.Status == invoiceStatus);

            if (invoiceType.HasValue)
                query = query.Where(x => x.Type == invoiceType.Value);

            if (fromDate.HasValue)
                query = query.Where(x => x.Date >= fromDate.Value.Date);

            if (toDate.HasValue)
                query = query.Where(x => x.Date <= toDate.Value.Date.AddDays(1).AddTicks(-1));

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
            var invoice = await _context.Invoices
                .Include(i => i.InvoiceDetails)
                .Include(i => i.Payments)
                .FirstOrDefaultAsync(i => i.InvoiceNo == invoiceNo);
            
            if (invoice != null)
            {
                // Delete related entities explicitly (cascade should handle this, but being explicit)
                if (invoice.InvoiceDetails != null && invoice.InvoiceDetails.Any())
                {
                    _context.InvoiceDetails.RemoveRange(invoice.InvoiceDetails);
                }
                
                if (invoice.Payments != null && invoice.Payments.Any())
                {
                    _context.Payments.RemoveRange(invoice.Payments);
                }
                
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
