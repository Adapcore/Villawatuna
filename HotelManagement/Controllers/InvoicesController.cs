using HotelManagement.Enums;
using HotelManagement.Helper;
using HotelManagement.Models.DTO;
using HotelManagement.Models.Entities;
using HotelManagement.Models.ViewModels;
using HotelManagement.Services.Interface;
using HotelManagement.Services.Interfaces;
using Lucene.Net.Search;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using uSync.Core;
using X.PagedList.Extensions;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Controllers
{
    [Authorize]
    //[AuthorizeUserType("Admin")]
    [Route("Internal/Invoices")]
    public class InvoicesController : Controller
    {
        private readonly IInvoiceService _invoiceService;
        private readonly ICustomerService _customerService;
        private readonly IMenuService _menuService;
        private readonly ICurrencyService _currencyService;
        private readonly IPaymentService _paymentService;
        private readonly int _pageSize;
        private readonly IMemberManager _memberManager;
        private readonly IMemberService _memberService;

        public InvoicesController(
            IInvoiceService invoiceService,
            ICustomerService customerService,
            IMenuService menuService,
            ICurrencyService currencyService,
            IPaymentService paymentService,
            IOptions<PaginationSettings> paginationSettings,
            IMemberManager memberManager,
            IMemberService memberService)
        {
            _invoiceService = invoiceService;
            _customerService = customerService;
            _menuService = menuService;
            _currencyService = currencyService;
            _paymentService = paymentService;
            _pageSize = paginationSettings.Value.DefaultPageSize;
            _memberManager = memberManager;
            _memberService = memberService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            ViewBag.IsAdmin = IsAdminUser();

            var customers = await _customerService.GetAllAsync();
            ViewBag.Customers = customers.Where(c => c.Active).Select(c => new SelectListItem
            {
                Value = c.ID.ToString(),
                Text = !string.IsNullOrWhiteSpace(c.RoomNo) 
                    ? $"#{c.RoomNo} - {c.FirstName} {c.LastName}" 
                    : $"{c.FirstName} {c.LastName}"
            }).ToList();

            ViewBag.InvoiceTypes = Enum.GetValues(typeof(InvoiceType))
                .Cast<InvoiceType>().Select(s => new SelectListItem
                {
                    Text = s.GetType()
                    .GetMember(s.ToString())
                    .First()
                    .GetCustomAttribute<DisplayAttribute>()?
                    .Name ?? s.ToString(),
                    Value = ((int)s).ToString()
                }).ToList();

            return View();
        }

        [HttpGet("GetInvoices")]
        public async Task<IActionResult> GetInvoices(InvoiceStatus? invoiceStatus = null, int customerId = 0, InvoiceType? invoiceType = null, string fromDate = null, string toDate = null, int page = 1)
        {
            int pageNumber = page < 1 ? 1 : page;
            customerId = customerId < 0 ? 0 : customerId;

            // Parse date strings to DateTime? (null if empty)
            DateTime? fromDateParsed = null;
            DateTime? toDateParsed = null;
            
            if (!string.IsNullOrWhiteSpace(fromDate) && DateTime.TryParse(fromDate, out DateTime fromDateValue))
                fromDateParsed = fromDateValue;
                
            if (!string.IsNullOrWhiteSpace(toDate) && DateTime.TryParse(toDate, out DateTime toDateValue))
                toDateParsed = toDateValue;

            var pagedList = await _invoiceService.GetPagedInvoicesAsync(pageNumber, _pageSize, customerId: customerId, invoiceStatus: invoiceStatus, invoiceType: invoiceType, fromDate: fromDateParsed, toDate: toDateParsed);

            // badge counts by status (with filters applied)
            var countAll = await _invoiceService.GetPagedInvoicesCountAsync(customerId: customerId, invoiceStatus: null, invoiceType: invoiceType, fromDate: fromDateParsed, toDate: toDateParsed);
            var countOpen = await _invoiceService.GetPagedInvoicesCountAsync(customerId: customerId, invoiceStatus: InvoiceStatus.InProgress, invoiceType: invoiceType, fromDate: fromDateParsed, toDate: toDateParsed);
            var countComplete = await _invoiceService.GetPagedInvoicesCountAsync(customerId: customerId, invoiceStatus: InvoiceStatus.Complete, invoiceType: invoiceType, fromDate: fromDateParsed, toDate: toDateParsed);
            var countPartial = await _invoiceService.GetPagedInvoicesCountAsync(customerId: customerId, invoiceStatus: InvoiceStatus.PartiallyPaid, invoiceType: invoiceType, fromDate: fromDateParsed, toDate: toDateParsed);
            var countPaid = await _invoiceService.GetPagedInvoicesCountAsync(customerId: customerId, invoiceStatus: InvoiceStatus.Paid, invoiceType: invoiceType, fromDate: fromDateParsed, toDate: toDateParsed);

            var invoices = pagedList.Select(i => new
            {
                invoiceNo = i.InvoiceNo,
                customerName = !string.IsNullOrWhiteSpace(i.Customer?.RoomNo)
                    ? $"#{i.Customer.RoomNo} - {i.Customer.FirstName} {i.Customer.LastName}"
                    : $"{i.Customer?.FirstName} {i.Customer?.LastName}",
                customerRoomNo = i.Customer?.RoomNo,
                customerFirstName = i.Customer?.FirstName,
                customerLastName = i.Customer?.LastName,
                type = i.Type.ToString(),
                typeDisplay = EnumHelper.GetDisplayName(i.Type),
                date = i.Date.ToString("yyyy-MM-dd"),
                // SettledOn: use LastModifiedDate only for Paid invoices; otherwise null
                settledOn = i.Status == InvoiceStatus.Paid && i.LastModifiedDate.HasValue
                    ? i.LastModifiedDate.Value.ToString("yyyy-MM-dd")
                    : null,
                status = i.Status.ToString(),
                statusDisplay = EnumHelper.GetDisplayName(i.Status),
                grossAmount = i.GrossAmount,
                createdByMember = i.CreatedByMember != null ? new
                {
                    name = i.CreatedByMember.Name,
                    username = i.CreatedByMember.Username
                } : null
            }).ToList();

            return Json(new
            {
                success = true,
                invoices = invoices,
                pagination = new
                {
                    pageNumber = pagedList.PageNumber,
                    pageCount = pagedList.PageCount,
                    totalItemCount = pagedList.TotalItemCount,
                    hasPreviousPage = pagedList.HasPreviousPage,
                    hasNextPage = pagedList.HasNextPage,
                    isFirstPage = pagedList.IsFirstPage,
                    isLastPage = pagedList.IsLastPage
                },
                counts = new
                {
                    all = countAll,
                    open = countOpen,
                    complete = countComplete,
                    partial = countPartial,
                    paid = countPaid
                },
                filters = new
                {
                    invoiceStatus = invoiceStatus?.ToString(),
                    customerId = customerId,
                    invoiceType = invoiceType?.ToString(),
                    fromDate = fromDate,
                    toDate = toDate
                }
            });
        }

        [HttpGet("SelectType")]
        public IActionResult SelectType()
        {
            var types = EnumHelper.ToSelectList<InvoiceType>();

            ViewBag.InvoiceTypes = types;
            return View();
        }

        [HttpPost("SelectType")]
        public IActionResult SelectType(string selectedType)
        {
            return RedirectToAction("Create", new { type = selectedType });
        }

        [HttpGet("Create")]
        public async Task<IActionResult> Create(string type)
        {
            if (string.IsNullOrEmpty(type))
                return RedirectToAction("SelectType");

            var model = new CreateInvoiceViewModel
            {
                Date = DateTime.Now,
                Type = int.Parse(type),
                Status = (int)InvoiceStatus.InProgress
            };

            var customers = await _customerService.GetAllAsync();
            ViewBag.Customers = customers.Where(c=> c.Active).Select(c => new SelectListItem
            {
                Value = c.ID.ToString(),
                Text = !string.IsNullOrWhiteSpace(c.RoomNo) 
                    ? $"#{c.RoomNo} - {c.FirstName} {c.LastName}" 
                    : $"{c.FirstName} {c.LastName}"
            }).ToList();

            ViewBag.StatusList = Enum.GetValues(typeof(InvoiceStatus))
               .Cast<InvoiceStatus>().Select(s => new SelectListItem
               {
                   Text = s.GetType()
                   .GetMember(s.ToString())
                   .First()
                   .GetCustomAttribute<DisplayAttribute>()?
                   .Name ?? s.ToString(),
                   Value = ((int)s).ToString()
               }).ToList();

            var currencyTypes = await _currencyService.GetCurencyTypesAsync();
            ViewBag.CurrencyTypes = currencyTypes.Select(c => new SelectListItem
            {
                Value = c.Code.ToString(),
                Text = c.Code.ToString()
            }).ToList();

            // Pass currency data with exchange rates for JavaScript
            ViewBag.CurrencyData = currencyTypes;

            ViewBag.InvoicePaymentTypes = Enum.GetValues(typeof(InvoicePaymentType))
                           .Cast<InvoicePaymentType>().Select(s => new SelectListItem
                           {
                               Text = s.GetType()
                               .GetMember(s.ToString())
                               .First()
                               .GetCustomAttribute<DisplayAttribute>()?
                               .Name ?? s.ToString(),
                               Value = ((int)s).ToString()
                           }).ToList();

            ViewBag.InvoiceTypeName = Enum.GetName(typeof(InvoiceType), Enum.Parse<InvoiceType>(type));
            ViewBag.Mode = "Insert";
            ViewBag.Countries = CountryList.All;
            ViewBag.IsAdmin = IsAdminUser();

            return View(model);
        }

        [HttpGet("Edit/{id}")]
        public async Task<IActionResult> Edit(int id)
        {
            var invoice = await _invoiceService.GetByIdAsync(id);
            if (invoice == null)
                return NotFound();

            var customers = await _customerService.GetAllAsync();

            ViewBag.Customers = customers.Where(c => c.Active).Select(c => new SelectListItem
            {
                Value = c.ID.ToString(),
                Text = !string.IsNullOrWhiteSpace(c.RoomNo) 
                    ? $"#{c.RoomNo} - {c.FirstName} {c.LastName}" 
                    : $"{c.FirstName} {c.LastName}"
            }).ToList();

            ViewBag.StatusList = Enum.GetValues(typeof(InvoiceStatus))
                .Cast<InvoiceStatus>().Select(s => new SelectListItem
                {
                    Text = s.GetType()
                    .GetMember(s.ToString())
                    .First()
                    .GetCustomAttribute<DisplayAttribute>()?
                    .Name ?? s.ToString(),
                    Value = ((int)s).ToString()
                }).ToList();

            var currencyTypes = await _currencyService.GetCurencyTypesAsync();
            ViewBag.CurrencyTypes = currencyTypes.Select(c => new SelectListItem
            {
                Value = c.Code.ToString(),
                Text = c.Code.ToString()
            }).ToList();

            // Pass currency data with exchange rates for JavaScript
            ViewBag.CurrencyData = currencyTypes;

            ViewBag.InvoicePaymentTypes = Enum.GetValues(typeof(InvoicePaymentType))
                          .Cast<InvoicePaymentType>().Select(s => new SelectListItem
                          {
                              Text = s.GetType()
                              .GetMember(s.ToString())
                              .First()
                              .GetCustomAttribute<DisplayAttribute>()?
                              .Name ?? s.ToString(),
                              Value = ((int)s).ToString()
                          }).ToList();

            ViewBag.Mode = "Edit";
            ViewBag.IsAdmin = IsAdminUser();

            CreateInvoiceViewModel model = new CreateInvoiceViewModel(invoice);

            return View("Create", model);
        }

        [HttpGet("Details/{id}")]
        public async Task<IActionResult> Details(int id)
        {
            Invoice invoice = await _invoiceService.GetByIdAsync(id);
            if (invoice == null)
                return NotFound();

            return View("Details", invoice);
        }

        [HttpGet("Print/{id}")]
        public async Task<IActionResult> Print(int id)
        {
            var invoice = await _invoiceService.GetByIdAsync(id);

            if (invoice == null)
                return NotFound();

            return View("PrintThermal", invoice);
        }

        //[HttpGet("PrintThermal/{id}")]
        //public async Task<IActionResult> PrintThermal(int id)
        //{

        //    var invoice = await _invoiceService.GetByIdAsync(id);

        //    if (invoice == null)
        //        return NotFound();

        //    // OPTIONAL: if you have MenuService or MenuController data cached
        //    // you can map the MenuItem details here
        //    // Example:
        //    // foreach (var detail in invoice.InvoiceDetails)
        //    // {
        //    //     detail.MenuItem = await _menuService.GetItemByIdAsync(detail.ItemId);
        //    // }

        //    return View("PrintThermal", invoice);
        //}

        [HttpGet("PrintThermal/{id}")]
        public async Task<IActionResult> PrintThermal(int id)
        {
            //var vm = new PrintDTO(
            //    ReceiptNo : "001",
            //    Date : DateTime.Now,
            //    StoreName: "My Shop",
            //    AddressLine: "123 Main St",
            //    Items: new()
            //    {
            //        new("Item A", 1, 10m, 10m),
            //        new("Item B", 2, 2.50m, 5m)
            //    },
            //    Subtotal: 15m,
            //    Tax: 0m,
            //    Discount: 0m,
            //    Total: 15m,
            //    Paid : 10,
            //    Footer: "Visit again!"
            //);

            //return View("PrintThermalTest", vm);


            var invoice = await _invoiceService.GetByIdAsync(id);

            if (invoice == null)
                return NotFound();

            // OPTIONAL: if you have MenuService or MenuController data cached
            // you can map the MenuItem details here
            // Example:
            // foreach (var detail in invoice.InvoiceDetails)
            // {
            //     detail.MenuItem = await _menuService.GetItemByIdAsync(detail.ItemId);
            // }

            return View("PrintThermal", invoice);
        }

        private async Task<List<Customer>> GetCustomersAsync()
        {
            // Replace with your real service call
            return await Task.FromResult(new List<Customer>());
        }

        [HttpPost("Delete/{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            if (!IsAdminUser())
            {
                return Json(new { success = false, message = "Only admin users can delete invoices." });
            }

            try
            {
                var invoice = await _invoiceService.GetByIdAsync(id);
                if (invoice == null)
                {
                    return Json(new { success = false, message = "Invoice not found." });
                }

                await _invoiceService.DeleteInvoiceAsync(id);
                return Json(new { success = true, message = "Invoice deleted successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error deleting invoice: {ex.Message}" });
            }
        }

        [HttpGet("GetPayments/{invoiceNo}")]
        public async Task<IActionResult> GetPayments(int invoiceNo)
        {
            try
            {
                var payments = await _paymentService.GetAllAsync();
                var invoicePayments = payments
                    .Where(p => p.InvoiceNo == invoiceNo)
                    .OrderByDescending(p => p.Date)
                    .Select(p => new
                    {
                        id = p.ID,
                        date = p.Date.ToString("yyyy-MM-dd"),
                        amount = p.Amount,
                        type = p.Type.ToString(),
                        reference = p.Reference ?? ""
                    })
                    .ToList();

                return Json(new { success = true, data = invoicePayments });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        private bool IsAdminUser()
        {
            var username = User?.Identity?.Name;
            if (string.IsNullOrWhiteSpace(username)) return false;

            var memberIdentity = _memberManager.FindByNameAsync(username).GetAwaiter().GetResult();
            if (memberIdentity == null) return false;

            var member = _memberService.GetByKey(memberIdentity.Key);
            if (member == null) return false;

            var rawType = member.GetValue<string>("userType") ?? "";
            var userType = rawType.Replace("[", "").Replace("]", "").Replace("\"", "").Trim();
            return string.Equals(userType, "Admin", StringComparison.OrdinalIgnoreCase);
        }
    }
}
