using HotelManagement.Enums;
using HotelManagement.Helper;
using HotelManagement.Models.Entities;
using HotelManagement.Services.Interface;
using HotelManagement.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace HotelManagement.Controllers
{
    [AuthorizeUserType("Admin")]
    public class ExpensesController : Controller
    {
        private readonly IExpenseService _expenseService;
        private readonly IWebHostEnvironment _env;
        private readonly IExpenseTypeService _expenseTypeService;

        public ExpensesController(IExpenseService expenseService, IWebHostEnvironment env, IExpenseTypeService expenseTypeService)
        {
            _expenseService = expenseService;
            _env = env;
            _expenseTypeService = expenseTypeService;
        }

        public async Task<IActionResult> Index()
        {
            var expenses = await _expenseService.GetAllAsync();
            return View(expenses);
        }

        public async Task<IActionResult> Create()
        {
            var expenseTypes = await _expenseTypeService.GetExpenseTypesAsync();
            ViewBag.ExpenseTypes = expenseTypes.Select(c => new SelectListItem
            {
                Value = c.Id.ToString(),
                Text = c.Name
            }).ToList();

            var paymentMethod = Enum.GetValues(typeof(PaymentMethod))
                         .Cast<PaymentMethod>().Select(s => new SelectListItem
                         {
                             Text = s.GetType()
                             .GetMember(s.ToString())
                             .First()
                             .GetCustomAttribute<DisplayAttribute>()?
                             .Name ?? s.ToString(),
                             Value = ((int)s).ToString()
                         }).ToList();
            ViewBag.PaymentMethods = paymentMethod;

            return View(new Expense { Date = DateTime.Now });
        }

        [HttpPost]
        public async Task<IActionResult> Create(Expense expense, IFormFile? attachment)
        {
            if ((expense.ExpenseTypeID == 0))
                ModelState.AddModelError(nameof(expense.ExpenseTypeID), "Expense Type is required.");

            if ((expense.ExpenseTypeID == 0))
                ModelState.AddModelError(nameof(expense.PaymentMethod), "Payment Method is required.");

            if ((expense.Amount <= 0))
                ModelState.AddModelError(nameof(expense.Amount), "Amount is required.");

            // Skip validation for the non-persistent property
            ModelState.Remove("ExpenseType");

            if (!ModelState.IsValid)
            {
                var expenseTypes = await _expenseTypeService.GetExpenseTypesAsync();
                ViewBag.ExpenseTypes = expenseTypes.Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.Name
                }).ToList();

                var paymentMethod = Enum.GetValues(typeof(PaymentMethod))
                             .Cast<PaymentMethod>().Select(s => new SelectListItem
                             {
                                 Text = s.GetType()
                                 .GetMember(s.ToString())
                                 .First()
                                 .GetCustomAttribute<DisplayAttribute>()?
                                 .Name ?? s.ToString(),
                                 Value = ((int)s).ToString()
                             }).ToList();
                ViewBag.PaymentMethods = paymentMethod;
                return View(expense);
            }

            // Step 1: Save the record first to get the Expense ID
            await _expenseService.AddAsync(expense);

            // Step 2: Handle attachment (if any)
            if (attachment != null && attachment.Length > 0)
            {
                var uploadsDir = Path.Combine(_env.WebRootPath, "uploads/expenses");
                Directory.CreateDirectory(uploadsDir);

                var fileName = $"{expense.ID}{Path.GetExtension(attachment.FileName)}";
                var filePath = Path.Combine(uploadsDir, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                    await attachment.CopyToAsync(stream);

                // Update the record with file URL
                expense.Attachment = $"/uploads/expenses/{fileName}";
                await _expenseService.UpdateAsync(expense);
            }

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var expenseTypes = await _expenseTypeService.GetExpenseTypesAsync();
            ViewBag.ExpenseTypes = expenseTypes.Select(c => new SelectListItem
            {
                Value = c.Id.ToString(),
                Text = c.Name
            }).ToList();

            var paymentMethod = Enum.GetValues(typeof(PaymentMethod))
                          .Cast<PaymentMethod>().Select(s => new SelectListItem
                          {
                              Text = s.GetType()
                              .GetMember(s.ToString())
                              .First()
                              .GetCustomAttribute<DisplayAttribute>()?
                              .Name ?? s.ToString(),
                              Value = ((int)s).ToString()
                          }).ToList();
            ViewBag.PaymentMethods = paymentMethod;

            var expense = await _expenseService.GetByIdAsync(id);
            if (expense == null) return NotFound();
            return View(expense);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Expense expense, IFormFile? attachment)
        {
            if ((expense.ExpenseTypeID ==0))
                ModelState.AddModelError(nameof(expense.ExpenseTypeID), "Expense Type is required.");

            if ((expense.ExpenseTypeID ==0))
                ModelState.AddModelError(nameof(expense.PaymentMethod), "Payment Method is required.");
            
            if ((expense.Amount<= 0 ))
                ModelState.AddModelError(nameof(expense.Amount), "Amount is required.");

            // Skip validation for the non-persistent property
            ModelState.Remove("ExpenseType");

            if (ModelState.IsValid)
            {
                if (attachment != null && attachment.Length > 0)
                {
                    var uploadsDir = Path.Combine(_env.WebRootPath, "uploads/expenses");
                    Directory.CreateDirectory(uploadsDir);

                    var fileName = $"{Guid.NewGuid()}{Path.GetExtension(attachment.FileName)}";
                    var filePath = Path.Combine(uploadsDir, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                        await attachment.CopyToAsync(stream);

                    expense.Attachment = $"/uploads/expenses/{fileName}";
                }

                await _expenseService.UpdateAsync(expense);
                return RedirectToAction(nameof(Index));
            }
            else
            {
                var expenseTypes = await _expenseTypeService.GetExpenseTypesAsync();
                ViewBag.ExpenseTypes = expenseTypes.Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.Name
                }).ToList();

                var paymentMethod = Enum.GetValues(typeof(PaymentMethod))
                             .Cast<PaymentMethod>().Select(s => new SelectListItem
                             {
                                 Text = s.GetType()
                                 .GetMember(s.ToString())
                                 .First()
                                 .GetCustomAttribute<DisplayAttribute>()?
                                 .Name ?? s.ToString(),
                                 Value = ((int)s).ToString()
                             }).ToList();
                ViewBag.PaymentMethods = paymentMethod;
            }

            return View(expense);
        }

        public async Task<IActionResult> Details(int id)
        {
            var expense = await _expenseService.GetByIdAsync(id);
            if (expense == null) return NotFound();
            return View(expense);
        }

        public async Task<IActionResult> Delete(int id)
        {
            await _expenseService.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }
    }
}
