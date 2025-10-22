using HotelManagement.Enums;
using HotelManagement.Helper;
using HotelManagement.Models.DTO;
using HotelManagement.Models.Entities;
using HotelManagement.Models.ViewModels;
using HotelManagement.Services.Interface;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using X.PagedList.Extensions;

namespace HotelManagement.Controllers
{
    [AuthorizeUserType("Admin")]
    [Route("Internal/Invoices")]
    public class InvoicesController : Controller
    {
        private readonly IInvoiceService _invoiceService;
        private readonly ICustomerService _customerService;
        private readonly IMenuService _menuService;
        private readonly ICurrencyService _currencyService;
        private readonly int _pageSize;

        public InvoicesController(
            IInvoiceService invoiceService,
            ICustomerService customerService,
            IMenuService menuService,
            ICurrencyService currencyService,
            IOptions<PaginationSettings> paginationSettings)
        {
            _invoiceService = invoiceService;
            _customerService = customerService;
            _menuService = menuService;
            _currencyService = currencyService;
            _pageSize = paginationSettings.Value.DefaultPageSize;
        }

        [HttpGet]
        public async Task<IActionResult> Index(int page = 1)
        {
            int pageNumber = page < 1 ? 1 : page;

            var invoices = await _invoiceService.GetAllInvoicesAsync();
            var pagedList = invoices.ToPagedList(pageNumber, _pageSize);

            return View(pagedList);
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
            if (string.IsNullOrEmpty(type)) return RedirectToAction("SelectType");

            var customers = await _customerService.GetAllAsync();
            ViewBag.Customers = customers.Select(c => new SelectListItem
            {
                Value = c.ID.ToString(),
                Text = c.FirstName + " " + c.LastName
            }).ToList();

            var currencyTypes = await _currencyService.GetCurencyTypesAsync();
            ViewBag.CurrencyTypes = currencyTypes.Select(c => new SelectListItem
            {
                Value = c.Code.ToString(),
                Text = c.Name
            }).ToList();

            var model = new CreateInvoiceViewModel
            {
                Date = DateTime.Now,
                Type = int.Parse(type)
            };
            ViewBag.InvoiceTypeName = Enum.GetName(typeof(InvoiceType), Enum.Parse<InvoiceType>(type));

            return View(model);
        }

        [HttpGet("Edit/{id}")]
        public async Task<IActionResult> Edit(int id)
        {
            var invoice = await _invoiceService.GetByIdAsync(id);
            if (invoice == null)
                return NotFound();

            var customers = await _customerService.GetAllAsync();

            ViewBag.Customers = customers.Select(c => new SelectListItem
            {
                Value = c.ID.ToString(),
                Text = c.FirstName + " " + c.LastName
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
                Text = c.Name
            }).ToList();

            var model = new CreateInvoiceViewModel
            {
                InvoiceNo = invoice.InvoiceNo,
                Date = invoice.Date,
                Type = (int)invoice.Type,
                Currency = invoice.Currency,
                ReferenceNo = invoice.ReferenceNo,
                CustomerId = invoice.CustomerId,
                Note = invoice.Note,
                SubTotal = invoice.SubTotal,
                CurySubTotal = invoice.CurySubTotal,
                ServiceCharge = invoice.ServiceCharge,
                GrossAmount = invoice.GrossAmount,
                Status = (int)invoice.Status,
                Balance = invoice.Balance,
                InvoiceDetails = invoice.InvoiceDetails.Select(d => new CreateInvoiceDetailViewModel
                {
                    ItemId = d.ItemId,
                    CheckIn = d.CheckIn,
                    CheckOut = d.CheckOut,
                    Note = d.Note,
                    Quantity = d.Quantity,
                    UnitPrice = d.UnitPrice,
                    Amount = d.Amount
                }).ToList()
            };

            return View(model);
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

        [HttpGet("PrintThermal/{id}")]
        public async Task<IActionResult> PrintThermal(int id)
        {

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
    }
}
