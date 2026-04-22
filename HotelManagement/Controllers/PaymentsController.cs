using HotelManagement.Models.DTO;
using HotelManagement.Models.Entities;
using HotelManagement.Helper;
using HotelManagement.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Options;
using X.PagedList.Extensions;

namespace HotelManagement.Controllers
{
    [Authorize]
    public class PaymentsController : Controller
    {
        private readonly IPaymentService _paymentService;
        private readonly int _pageSize;

        public PaymentsController(IPaymentService paymentService,
            IOptions<PaginationSettings> paginationSettings)
        {
            _paymentService = paymentService;
            _pageSize = paginationSettings.Value.DefaultPageSize;
        }

        public async Task<IActionResult> Index(int page = 1)
        {
            ViewBag.PaymentTypes = Enum.GetValues(typeof(HotelManagement.Enums.InvoicePaymentType))
                .Cast<HotelManagement.Enums.InvoicePaymentType>()
                .Select(s => new SelectListItem
                {
                    Text = EnumHelper.GetDisplayName(s),
                    Value = ((int)s).ToString()
                }).ToList();

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetPayments(int page = 1, string? fromDate = null, string? toDate = null, HotelManagement.Enums.InvoicePaymentType? type = null)
        {
            int pageNumber = page < 1 ? 1 : page;

            var payments = await _paymentService.GetAllAsync();
            var query = payments.AsQueryable();

            // Parse dates (expect yyyy-MM-dd)
            DateTime? fromParsed = null;
            DateTime? toParsed = null;
            if (!string.IsNullOrWhiteSpace(fromDate) && DateTime.TryParse(fromDate, out var fd)) fromParsed = fd.Date;
            if (!string.IsNullOrWhiteSpace(toDate) && DateTime.TryParse(toDate, out var td)) toParsed = td.Date;

            if (fromParsed.HasValue) query = query.Where(p => p.Date.Date >= fromParsed.Value);
            if (toParsed.HasValue) query = query.Where(p => p.Date.Date <= toParsed.Value);
            if (type.HasValue) query = query.Where(p => p.Type == type.Value);

            // Keep original ordering from view (Date desc, ID desc)
            query = query.OrderByDescending(p => p.Date).ThenByDescending(p => p.ID);

            var pagedList = query.ToPagedList(pageNumber, _pageSize);

            var items = pagedList.Select(p => new
            {
                id = p.ID,
                date = p.Date.ToString("dd MMM yyyy"),
                invoiceNo = p.InvoiceNo,
                typeDisplay = EnumHelper.GetDisplayName(p.Type),
                reference = p.Reference ?? "-",
                amount = p.Amount,
                curryAmount = p.CurryAmount,
                currency = p.Invoice != null ? (p.Invoice.Currency ?? "USD") : null,
                paidCurrency = p.PaidCurrency.ToString(),
                createdByName = p.CreatedByMember != null ? p.CreatedByMember.Name : null
            }).ToList();

            return Json(new
            {
                success = true,
                payments = items,
                pagination = new
                {
                    pageNumber = pagedList.PageNumber,
                    pageCount = pagedList.PageCount,
                    totalItemCount = pagedList.TotalItemCount,
                    pageSize = pagedList.PageSize,
                    hasPreviousPage = pagedList.HasPreviousPage,
                    hasNextPage = pagedList.HasNextPage
                },
                filters = new
                {
                    fromDate,
                    toDate,
                    type = type.HasValue ? ((int)type.Value).ToString() : null
                }
            });
        }

        public async Task<IActionResult> Details(int id)
        {
            var payment = await _paymentService.GetByIdAsync(id);
            if (payment == null)
                return NotFound();

            return View(payment);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Payment payment)
        {
            // Skip validation for the non-persistent property
            ModelState.Remove("CreatedByMember");

            if (!ModelState.IsValid)
                return View(payment);

            await _paymentService.CreateAsync(payment);
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int id)
        {
            await _paymentService.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }
    }
}
