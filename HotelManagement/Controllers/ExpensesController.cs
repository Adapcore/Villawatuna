using HotelManagement.Enums;
using HotelManagement.Helper;
using HotelManagement.Models.DTO;
using HotelManagement.Models.Entities;
using HotelManagement.Services.Interface;
using HotelManagement.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using X.PagedList.Extensions;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Services;

namespace HotelManagement.Controllers
{
    [Authorize]
    public class ExpensesController : Controller
    {
        private readonly IExpenseService _expenseService;
        private readonly IWebHostEnvironment _env;
        private readonly IExpenseTypeService _expenseTypeService;
        private readonly int _pageSize;
        private readonly IMemberManager _memberManager;
        private readonly IMemberService _memberService;

        public ExpensesController(IExpenseService expenseService,
            IWebHostEnvironment env,
            IExpenseTypeService expenseTypeService, 
            IOptions<PaginationSettings> paginationSettings,
            IMemberManager memberManager,
            IMemberService memberService)
        {
            _expenseService = expenseService;
            _env = env;
            _expenseTypeService = expenseTypeService;
            _pageSize = paginationSettings.Value.DefaultPageSize;
            _memberManager = memberManager;
            _memberService = memberService;
        }

        public async Task<IActionResult> Index(int page = 1, string startDate = null, string endDate = null, int expenseTypeId = 0)
        {
            int pageNumber = page < 1 ? 1 : page;
            expenseTypeId = expenseTypeId < 0 ? 0 : expenseTypeId;

            // Parse date strings to DateTime? (null if empty)
            DateTime? startDateParsed = null;
            DateTime? endDateParsed = null;
            
            if (!string.IsNullOrWhiteSpace(startDate) && DateTime.TryParse(startDate, out DateTime startDateValue))
                startDateParsed = startDateValue;
                
            if (!string.IsNullOrWhiteSpace(endDate) && DateTime.TryParse(endDate, out DateTime endDateValue))
                endDateParsed = endDateValue;

            // Get expense types for dropdown
            var expenseTypes = await _expenseTypeService.GetExpenseTypesAsync();
            ViewBag.ExpenseTypes = expenseTypes.Select(c => new SelectListItem
            {
                Value = c.Id.ToString(),
                Text = c.Name,
                Selected = c.Id == expenseTypeId
            }).ToList();

            // Add "All" option
            ViewBag.ExpenseTypes.Insert(0, new SelectListItem
            {
                Value = "0",
                Text = "-- All Expense Types --",
                Selected = expenseTypeId == 0
            });

            // Pass filter values to view
            ViewBag.StartDate = startDate;
            ViewBag.EndDate = endDate;
            ViewBag.ExpenseTypeId = expenseTypeId;

            // Get filtered expenses
            int? expenseTypeFilter = expenseTypeId > 0 ? expenseTypeId : null;
            IEnumerable<Expense> expenses = await _expenseService.GetAllAsync(startDateParsed, endDateParsed, expenseTypeFilter);
            var pagedList = expenses.ToPagedList(pageNumber, _pageSize);

            ViewBag.IsAdmin = IsAdminUser();

            return View(pagedList);
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

            // Skip validation for the non-persistent properties
            ModelState.Remove("ExpenseType");
            ModelState.Remove("CreatedByMember");

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
            // Skip validation for the non-persistent properties
            ModelState.Remove("ExpenseType");
            ModelState.Remove("CreatedByMember");
            
            if ((expense.ExpenseTypeID ==0))
                ModelState.AddModelError(nameof(expense.ExpenseTypeID), "Expense Type is required.");

            if ((expense.ExpenseTypeID ==0))
                ModelState.AddModelError(nameof(expense.PaymentMethod), "Payment Method is required.");
            
            if ((expense.Amount<= 0 ))
                ModelState.AddModelError(nameof(expense.Amount), "Amount is required.");

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

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            if (!IsAdminUser())
            {
                return Json(new { success = false, message = "Only admin users can delete expenses." });
            }

            try
            {
                var expense = await _expenseService.GetByIdAsync(id);
                if (expense == null)
                {
                    return Json(new { success = false, message = "Expense not found." });
                }

                await _expenseService.DeleteAsync(id);
                return Json(new { success = true, message = "Expense deleted successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error deleting expense: {ex.Message}" });
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
