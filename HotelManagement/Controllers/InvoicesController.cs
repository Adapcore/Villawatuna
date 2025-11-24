using HotelManagement.Enums;
using HotelManagement.Helper;
using HotelManagement.Models.DTO;
using HotelManagement.Models.Entities;
using HotelManagement.Models.ViewModels;
using HotelManagement.Services.Interface;
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

namespace HotelManagement.Controllers
{
    [Authorize]
    [Route("Internal/Invoices")]
    public class InvoicesController : Controller
    {
        private readonly IInvoiceService _invoiceService;
        private readonly ICustomerService _customerService;
        private readonly IMenuService _menuService;
        private readonly ICurrencyService _currencyService;
        private readonly int _pageSize;
        private readonly IMemberManager _memberManager;
        private readonly IMemberService _memberService;

        public InvoicesController(
            IInvoiceService invoiceService,
            ICustomerService customerService,
            IMenuService menuService,
            ICurrencyService currencyService,
            IOptions<PaginationSettings> paginationSettings,
            IMemberManager memberManager,
            IMemberService memberService)
        {
            _invoiceService = invoiceService;
            _customerService = customerService;
            _menuService = menuService;
            _currencyService = currencyService;
            _pageSize = paginationSettings.Value.DefaultPageSize;
            _memberManager = memberManager;
            _memberService = memberService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(InvoiceStatus? invoiceStatus = null, int customerId = 0, int page = 1)
        {
            int pageNumber = page < 1 ? 1 : page;
            customerId = customerId < 0 ? 0 : customerId;

            var pagedList = await _invoiceService.GetPagedInvoicesAsync(pageNumber, _pageSize, customerId: customerId, invoiceStatus: invoiceStatus);

            // badge counts by status
            ViewBag.CountAll = await _invoiceService.GetPagedInvoicesCountAsync(customerId: customerId);
            ViewBag.CountOpen = await _invoiceService.GetPagedInvoicesCountAsync(customerId: customerId, invoiceStatus: InvoiceStatus.InProgress);
            ViewBag.CountComplete = await _invoiceService.GetPagedInvoicesCountAsync(customerId: customerId, invoiceStatus: InvoiceStatus.Complete);
            ViewBag.CountPartial = await _invoiceService.GetPagedInvoicesCountAsync(customerId: customerId, invoiceStatus: InvoiceStatus.PartiallyPaid);
            ViewBag.CountPaid = await _invoiceService.GetPagedInvoicesCountAsync(customerId: customerId, invoiceStatus: InvoiceStatus.Paid);

            ViewBag.InvoiceStatus = invoiceStatus;
            ViewBag.CustomerId = customerId;
            ViewBag.IsAdmin = IsAdminUser();

            var customers = await _customerService.GetAllAsync();
            //ViewBag.Customers = customers;

            ViewBag.Customers = customers.Where(c => c.Active).Select(c => new SelectListItem
            {
                Value = c.ID.ToString(),
                Text = !string.IsNullOrWhiteSpace(c.RoomNo) 
                    ? $"#{c.RoomNo} - {c.FirstName} {c.LastName}" 
                    : $"{c.FirstName} {c.LastName}"
            }).ToList();

            var model = new InvoiceIndexViewModel
            {
                CustomerId = customerId,
                Invoices = pagedList
            };

            return View(model);
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

            return View("PrintInvoice", invoice);
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
