using HotelManagement.Data;
using HotelManagement.Enums;
using HotelManagement.Services.Interface;
using HotelManagement.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Services;

namespace HotelManagement.Controllers
{
    [Authorize]
    [Route("Reports")]
    public class ReportsController : Controller
    {
        private readonly HotelContext _context;
        private readonly IMenuService _menuService;
        private readonly IRoomService _roomService;
        private readonly IOtherTypeService _otherTypeService;
        private readonly ITourTypeService _tourService;
        private readonly ILaundryService _laundryService;
        private readonly IMemberManager _memberManager;
        private readonly IMemberService _memberService;

        public ReportsController(
            HotelContext context,
            IMenuService menuService,
            IRoomService roomService,
            IOtherTypeService otherTypeService,
            ITourTypeService tourService,
            ILaundryService laundryService,
            IMemberManager memberManager,
            IMemberService memberService)
        {
            _context = context;
            _menuService = menuService;
            _roomService = roomService;
            _otherTypeService = otherTypeService;
            _tourService = tourService;
            _laundryService = laundryService;
            _memberManager = memberManager;
            _memberService = memberService;
        }

        private bool IsAdmin()
        {
            if (User?.Identity?.Name == null)
                return false;

            var memberIdentity = _memberManager.FindByNameAsync(User.Identity.Name).GetAwaiter().GetResult();
            if (memberIdentity == null)
                return false;

            var member = _memberService.GetByKey(memberIdentity.Key);
            if (member == null)
                return false;

            var rawType = member.GetValue<string>("userType") ?? "";
            var userType = rawType.Replace("[", "").Replace("]", "").Replace("\"", "").Trim();
            return string.Equals(userType, "Admin", StringComparison.OrdinalIgnoreCase);
        }

        [HttpGet("ItemWiseSales")]
        public IActionResult ItemWiseSales()
        {
            if (!IsAdmin())
            {
                return RedirectToAction("AccessDenied", "Account");
            }
            return View();
        }

        [HttpGet("GetItemOptions")]
        public async Task<IActionResult> GetItemOptions()
        {
            if (!IsAdmin())
            {
                return Json(new { success = false, message = "Access denied" });
            }

            // Get distinct item ids that appear in Dining/TakeAway invoice details only
            var usedItemIds = await _context.InvoiceDetails
                .Include(d => d.Invoice)
                .Where(d => d.Invoice.Type == InvoiceType.Dining || d.Invoice.Type == InvoiceType.TakeAway)
                .Select(d => d.ItemId)
                .Distinct()
                .ToListAsync();

            var itemNameDict = await GetAllItemsDictionaryAsync();

            var items = usedItemIds
                .Select(id => new
                {
                    id,
                    name = itemNameDict.TryGetValue(id, out var name) ? name : $"Item {id}"
                })
                .OrderBy(x => x.name)
                .ToList();

            return Json(new { success = true, items });
        }

        [HttpGet("GetItemWiseSales")]
        public async Task<IActionResult> GetItemWiseSales(
            int itemId = 0,
            string? fromDate = null,
            string? toDate = null,
            string? category = null,
            int page = 1,
            int pageSize = 50)
        {
            if (!IsAdmin())
            {
                return Json(new { success = false, message = "Access denied" });
            }

            DateTime? fromDateParsed = null;
            DateTime? toDateParsed = null;

            if (!string.IsNullOrWhiteSpace(fromDate) && DateTime.TryParse(fromDate, out var fromVal))
            {
                fromDateParsed = fromVal.Date;
            }

            if (!string.IsNullOrWhiteSpace(toDate) && DateTime.TryParse(toDate, out var toVal))
            {
                toDateParsed = toVal.Date;
            }

            var query = _context.InvoiceDetails
                .Include(d => d.Invoice)
                .Where(d => d.Invoice.Status != InvoiceStatus.InProgress) // exclude open / in-progress invoices
                .AsQueryable();

            // Map category filter to invoice types
            var cat = (category ?? "All").Trim();
            var allowedTypes = new List<InvoiceType>();

            switch (cat.ToLowerInvariant())
            {
                case "restaurant":
                    allowedTypes.Add(InvoiceType.Dining);
                    allowedTypes.Add(InvoiceType.TakeAway);
                    break;
                case "rooms":
                    allowedTypes.Add(InvoiceType.Stay);
                    break;
                case "tours":
                    allowedTypes.Add(InvoiceType.Tour);
                    break;
                case "laundry":
                    allowedTypes.Add(InvoiceType.Laundry);
                    break;
                case "other":
                    allowedTypes.Add(InvoiceType.Other);
                    break;
                case "all":
                default:
                    allowedTypes.AddRange(new[]
                    {
                        InvoiceType.Dining,
                        InvoiceType.TakeAway,
                        InvoiceType.Stay,
                        InvoiceType.Tour,
                        InvoiceType.Laundry,
                        InvoiceType.Other
                    });
                    break;
            }

            if (allowedTypes.Any())
            {
                query = query.Where(d => allowedTypes.Contains(d.Invoice.Type));
            }

            if (itemId > 0)
            {
                query = query.Where(d => d.ItemId == itemId);
            }

            if (fromDateParsed.HasValue)
            {
                query = query.Where(d => d.Invoice.Date >= fromDateParsed.Value);
            }

            if (toDateParsed.HasValue)
            {
                query = query.Where(d => d.Invoice.Date <= toDateParsed.Value);
            }

            var grouped = await query
                .GroupBy(d => d.ItemId)
                .Select(g => new
                {
                    ItemId = g.Key,
                    Quantity = g.Sum(x => x.Quantity),
                    Total = g.Sum(x => x.Amount)
                })
                .ToListAsync();

            var itemNameDict = await GetAllItemsDictionaryAsync();

            var rows = grouped
                .Select(g => new
                {
                    itemId = g.ItemId,
                    description = itemNameDict.TryGetValue(g.ItemId, out var name) ? name : $"Item {g.ItemId}",
                    quantity = g.Quantity,
                    total = g.Total
                })
                .OrderBy(r => r.description)
                .ToList();

            var grandTotalQty = rows.Sum(r => r.quantity);
            var grandTotalAmount = rows.Sum(r => r.total);

            // Paging (default pageSize = 50)
            if (page < 1)
            {
                page = 1;
            }
            if (pageSize <= 0)
            {
                pageSize = 50;
            }

            var totalItems = rows.Count;
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            if (page > totalPages && totalPages > 0)
            {
                page = totalPages;
            }

            var pagedRows = rows
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return Json(new
            {
                success = true,
                items = pagedRows,
                totals = new
                {
                    quantity = grandTotalQty,
                    total = grandTotalAmount
                },
                paging = new
                {
                    page,
                    pageSize,
                    totalItems,
                    totalPages
                }
            });
        }

        [HttpGet("GetItemInvoices")]
        public async Task<IActionResult> GetItemInvoices(int itemId, string? fromDate = null, string? toDate = null, string? category = null)
        {
            if (!IsAdmin())
            {
                return Json(new { success = false, message = "Access denied" });
            }

            if (itemId <= 0)
            {
                return Json(new { success = false, items = Array.Empty<object>() });
            }

            DateTime? fromDateParsed = null;
            DateTime? toDateParsed = null;

            if (!string.IsNullOrWhiteSpace(fromDate) && DateTime.TryParse(fromDate, out var fromVal))
            {
                fromDateParsed = fromVal.Date;
            }

            if (!string.IsNullOrWhiteSpace(toDate) && DateTime.TryParse(toDate, out var toVal))
            {
                toDateParsed = toVal.Date;
            }

            var query = _context.InvoiceDetails
                .Include(d => d.Invoice)
                .ThenInclude(i => i.Customer)
                .Where(d => d.Invoice.Status != InvoiceStatus.InProgress)
                .Where(d => d.ItemId == itemId)
                .AsQueryable();

            // Apply same category filter logic as GetItemWiseSales
            var cat = (category ?? "All").Trim();
            var allowedTypes = new List<InvoiceType>();

            switch (cat.ToLowerInvariant())
            {
                case "restaurant":
                    allowedTypes.Add(InvoiceType.Dining);
                    allowedTypes.Add(InvoiceType.TakeAway);
                    break;
                case "rooms":
                    allowedTypes.Add(InvoiceType.Stay);
                    break;
                case "tours":
                    allowedTypes.Add(InvoiceType.Tour);
                    break;
                case "laundry":
                    allowedTypes.Add(InvoiceType.Laundry);
                    break;
                case "other":
                    allowedTypes.Add(InvoiceType.Other);
                    break;
                case "all":
                default:
                    allowedTypes.AddRange(new[]
                    {
                        InvoiceType.Dining,
                        InvoiceType.TakeAway,
                        InvoiceType.Stay,
                        InvoiceType.Tour,
                        InvoiceType.Laundry,
                        InvoiceType.Other
                    });
                    break;
            }

            if (allowedTypes.Any())
            {
                query = query.Where(d => allowedTypes.Contains(d.Invoice.Type));
            }

            if (fromDateParsed.HasValue)
            {
                query = query.Where(d => d.Invoice.Date >= fromDateParsed.Value);
            }

            if (toDateParsed.HasValue)
            {
                query = query.Where(d => d.Invoice.Date <= toDateParsed.Value);
            }

            var items = await query
                .OrderBy(d => d.Invoice.Date)
                .Select(d => new
                {
                    invoiceNo = d.InvoiceNo,
                    date = d.Invoice.Date.ToString("yyyy-MM-dd"),
                    customer = d.Invoice.Customer != null
                        ? (d.Invoice.Customer.FirstName + " " + d.Invoice.Customer.LastName)
                        : string.Empty,
                    roomNo = d.Invoice.Customer != null ? d.Invoice.Customer.RoomNo : null,
                    quantity = d.Quantity,
                    amount = d.Amount
                })
                .ToListAsync();

            return Json(new { success = true, items });
        }

        private async Task<Dictionary<int, string>> GetAllItemsDictionaryAsync()
        {
            var result = new Dictionary<int, string>();

            // Restaurant (Dining / TakeAway)
            var menuItems = await _menuService.GetItemsAsync();
            foreach (var item in menuItems)
            {
                if (!result.ContainsKey(item.Id))
                    result[item.Id] = item.Name;
            }

            // Rooms (Stay)
            var roomCategories = await _roomService.GetRoomCategoriesAsync();
            foreach (var item in roomCategories)
            {
                if (!result.ContainsKey(item.Id))
                    result[item.Id] = item.Name;
            }

            // Tours
            var tourItems = await _tourService.GetItemsAsync();
            foreach (var item in tourItems)
            {
                if (!result.ContainsKey(item.Id))
                    result[item.Id] = item.Name;
            }

            // Laundry
            var laundryItems = await _laundryService.GetItemsAsync();
            foreach (var item in laundryItems)
            {
                if (!result.ContainsKey(item.Id))
                    result[item.Id] = item.Name;
            }

            // Other
            var otherItems = await _otherTypeService.GetItemsAsync();
            foreach (var item in otherItems)
            {
                if (!result.ContainsKey(item.Id))
                    result[item.Id] = item.Name;
            }

            return result;
        }
    }
}


