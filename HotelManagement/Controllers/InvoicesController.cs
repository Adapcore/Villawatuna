using HotelManagement.Enums;
using HotelManagement.Helper;
using HotelManagement.Models.ViewModels;
using HotelManagement.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HotelManagement.Controllers
{
    public class InvoicesController : Controller
    {
        private readonly IInvoiceService _invoiceService;
        private readonly ICustomerService _customerService;

        public InvoicesController(IInvoiceService invoiceService, ICustomerService customerService)
        {
            _invoiceService = invoiceService;
            _customerService = customerService;
        }

        public async Task<IActionResult> Index()
        {
            var invoices = await _invoiceService.GetAllInvoicesAsync();
            return View(invoices);
        }
        public IActionResult SelectType()
        {
            var types = EnumHelper.ToSelectList<InvoiceType>();

            ViewBag.InvoiceTypes = types;
            return View();
        }

        [HttpPost]
        public IActionResult SelectType(string selectedType)
        {
            return RedirectToAction("Create", new { type = selectedType });
        }

        public async Task<IActionResult> Create(string type)
        {
            if (string.IsNullOrEmpty(type)) return RedirectToAction("SelectType");

            var customers = await _customerService.GetAllAsync();

            ViewBag.Customers = customers.Select(c => new SelectListItem
            {
                Value = c.ID.ToString(),
                Text = c.FirstName + " " + c.LastName
            }).ToList();
                      

            var model = new CreateInvoiceViewModel
            {
                Date = DateTime.Now,
                Type = int.Parse(type) 
            };
            ViewBag.InvoiceTypeName = Enum.GetName(typeof(InvoiceType), Enum.Parse<InvoiceType>(type));

            return View(model);
        }

        //[HttpPost]
        //public async Task<IActionResult> Create([FromBody] CreateInvoiceViewModel model)
        //{
        //    if (!ModelState.IsValid) return BadRequest(ModelState);

        //    var invoiceId = await _invoiceService.CreateInvoiceAsync(model);
        //    return Ok(new { success = true, invoiceId });
        //}

    }
}
