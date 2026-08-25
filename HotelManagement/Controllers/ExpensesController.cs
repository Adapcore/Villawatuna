using HotelManagement.Data;
using HotelManagement.Enums;
using HotelManagement.Helper;
using HotelManagement.Models.DTO;
using HotelManagement.Models.Entities;
using HotelManagement.Services;
using HotelManagement.Services.Interface;
using HotelManagement.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
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
        private readonly HotelContext _context;
        private readonly int _pageSize;
        private readonly IMemberManager _memberManager;
        private readonly IMemberService _memberService;

        public ExpensesController(IExpenseService expenseService,
            IWebHostEnvironment env,
            IExpenseTypeService expenseTypeService,
            HotelContext context,
            IOptions<PaginationSettings> paginationSettings,
            IMemberManager memberManager,
            IMemberService memberService)
        {
            _expenseService = expenseService;
            _env = env;
            _expenseTypeService = expenseTypeService;
            _context = context;
            _pageSize = paginationSettings.Value.DefaultPageSize;
            _memberManager = memberManager;
            _memberService = memberService;
        }

        public async Task<IActionResult> Index()
        {
            // Get expense types for dropdown
            var expenseTypes = await _expenseTypeService.GetExpenseTypesAsync();
            ViewBag.ExpenseTypes = expenseTypes.Select(c => new SelectListItem
            {
                Value = c.Id.ToString(),
                Text = c.Name
            }).ToList();

            // Add "All" option
            ViewBag.ExpenseTypes.Insert(0, new SelectListItem
            {
                Value = "0",
                Text = "-- All Expense Types --"
            });

            ViewBag.IsAdmin = IsAdminUser();

            return View();
        }

        [HttpGet]
        [Route("Expenses/GetExpenses")]
        public async Task<IActionResult> GetExpenses(int page = 1, string startDate = null, string endDate = null, int expenseTypeId = 0, string payeeName = null)
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

            // Normalize payee name (trim whitespace, null if empty)
            string payeeFilter = !string.IsNullOrWhiteSpace(payeeName) ? payeeName.Trim() : null;

            // Get filtered expenses
            int? expenseTypeFilter = expenseTypeId > 0 ? expenseTypeId : null;
            IEnumerable<Expense> expenses = await _expenseService.GetAllAsync(startDateParsed, endDateParsed, expenseTypeFilter, payeeFilter);
            
            // Calculate total amount across all pages (before pagination)
            decimal totalAmount = expenses.Sum(e => e.Amount);
            
            var pagedList = expenses.ToPagedList(pageNumber, _pageSize);

            var exchangeTypeId = await GetExchangeExpenseTypeIdAsync();
            var pagedExpenseIds = pagedList.Select(e => e.ID).ToList();
            var linkedExchangeExpenseIds = pagedExpenseIds.Count == 0
                ? new HashSet<int>()
                : (await _context.CurrencyExchanges
                    .AsNoTracking()
                    .Where(x => x.ExpenseId.HasValue && pagedExpenseIds.Contains(x.ExpenseId.Value))
                    .Select(x => x.ExpenseId!.Value)
                    .ToListAsync())
                    .ToHashSet();

            // Load CreatedByMember data from Umbraco
            var createdByIds = pagedList.Where(e => e.CreatedBy > 0).Select(e => e.CreatedBy).Distinct().ToList();
            
            if (createdByIds.Any())
            {
                var memberDict = new Dictionary<int, Models.DTO.MemberDTO>();
                
                foreach (var memberId in createdByIds)
                {
                    try
                    {
                        var member = _memberService.GetById(memberId);
                        if (member != null)
                        {
                            memberDict[memberId] = new Models.DTO.MemberDTO
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
                
                // Populate CreatedByMember for each expense
                foreach (var expense in pagedList)
                {
                    if (expense.CreatedBy > 0 && memberDict.TryGetValue(expense.CreatedBy, out var member))
                    {
                        expense.CreatedByMember = member;
                    }
                }
            }

            var expenseList = pagedList.Select(e => new
            {
                id = e.ID,
                date = e.Date.ToString("yyyy-MM-dd"),
                expenseTypeId = e.ExpenseTypeID,
                expenseTypeName = e.ExpenseType?.Name ?? "",
                payeeName = e.PayeeName ?? "",
                amount = e.Amount,
                paymentMethod = e.PaymentMethod.ToString(),
                paymentMethodDisplay = EnumHelper.GetDisplayName(e.PaymentMethod),
                isExchangeExpense = IsExchangeExpense(e.ID, e.ExpenseTypeID, e.ExpenseType?.Name, exchangeTypeId, linkedExchangeExpenseIds),
                createdByMember = e.CreatedByMember != null ? new
                {
                    name = e.CreatedByMember.Name,
                    username = e.CreatedByMember.Username
                } : null
            }).ToList();

            return Json(new
            {
                success = true,
                expenses = expenseList,
                totalAmount = totalAmount,
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
                filters = new
                {
                    expenseTypeId = expenseTypeId,
                    startDate = startDate,
                    endDate = endDate,
                    payeeName = payeeFilter
                }
            });
        }

        public async Task<IActionResult> Create()
        {
            await PopulateExpenseFormLookupsAsync();
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

            if (await IsExchangeExpenseTypeIdAsync(expense.ExpenseTypeID))
                ModelState.AddModelError(string.Empty, "Exchange expenses can only be created from Currency Exchange.");

            // Skip validation for the non-persistent properties
            ModelState.Remove("ExpenseType");
            ModelState.Remove("CreatedByMember");

            if (!ModelState.IsValid)
            {
                await PopulateExpenseFormLookupsAsync();
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
            var expense = await _expenseService.GetByIdAsync(id);
            if (expense == null) return NotFound();

            if (await IsExchangeExpenseProtectedAsync(expense))
            {
                TempData["ErrorMessage"] = "Exchange expenses are managed from Currency Exchange and cannot be edited here.";
                return RedirectToAction(nameof(Details), new { id });
            }

            await PopulateExpenseFormLookupsAsync(expense.ExpenseTypeID);
            return View(expense);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Expense expense, IFormFile? attachment)
        {
            var existing = await _expenseService.GetByIdAsync(expense.ID);
            if (existing == null) return NotFound();

            if (await IsExchangeExpenseProtectedAsync(existing))
            {
                TempData["ErrorMessage"] = "Exchange expenses are managed from Currency Exchange and cannot be edited here.";
                return RedirectToAction(nameof(Details), new { id = expense.ID });
            }

            // Skip validation for the non-persistent properties
            ModelState.Remove("ExpenseType");
            ModelState.Remove("CreatedByMember");
            
            if ((expense.ExpenseTypeID ==0))
                ModelState.AddModelError(nameof(expense.ExpenseTypeID), "Expense Type is required.");

            if ((expense.ExpenseTypeID ==0))
                ModelState.AddModelError(nameof(expense.PaymentMethod), "Payment Method is required.");
            
            if ((expense.Amount<= 0 ))
                ModelState.AddModelError(nameof(expense.Amount), "Amount is required.");

            if (await IsExchangeExpenseTypeIdAsync(expense.ExpenseTypeID))
                ModelState.AddModelError(string.Empty, "Exchange expenses can only be managed from Currency Exchange.");

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
                await PopulateExpenseFormLookupsAsync(expense.ExpenseTypeID);
            }

            return View(expense);
        }

        public async Task<IActionResult> Details(int id)
        {
            var expense = await _expenseService.GetByIdAsync(id);
            if (expense == null) return NotFound();

            var expenseTypes = await _expenseTypeService.GetExpenseTypesAsync();
            expense.ExpenseType = expenseTypes.FirstOrDefault(x => x.Id == expense.ExpenseTypeID);
            ViewBag.IsExchangeExpense = await IsExchangeExpenseProtectedAsync(expense);

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

                if (await IsExchangeExpenseProtectedAsync(expense))
                {
                    return Json(new { success = false, message = "Exchange expenses can only be deleted from Currency Exchange." });
                }

                await _expenseService.DeleteAsync(id);
                return Json(new { success = true, message = "Expense deleted successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error deleting expense: {ex.Message}" });
            }
        }

        private async Task PopulateExpenseFormLookupsAsync(int? includeExpenseTypeId = null)
        {
            var expenseTypes = await _expenseTypeService.GetExpenseTypesAsync();
            ViewBag.ExpenseTypes = expenseTypes
                .Where(t => !IsExchangeExpenseType(t.Name) || (includeExpenseTypeId.HasValue && t.Id == includeExpenseTypeId.Value))
                .Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.Name
                })
                .ToList();

            ViewBag.PaymentMethods = Enum.GetValues(typeof(PaymentMethod))
                .Cast<PaymentMethod>()
                .Select(s => new SelectListItem
                {
                    Text = s.GetType()
                        .GetMember(s.ToString())
                        .First()
                        .GetCustomAttribute<DisplayAttribute>()?
                        .Name ?? s.ToString(),
                    Value = ((int)s).ToString()
                })
                .ToList();
        }

        private static bool IsExchangeExpenseType(string? expenseTypeName)
        {
            return string.Equals(expenseTypeName, CurrencyExchangeService.ExchangeExpenseTypeName, StringComparison.OrdinalIgnoreCase);
        }

        private async Task<int?> GetExchangeExpenseTypeIdAsync()
        {
            var expenseTypes = await _expenseTypeService.GetExpenseTypesAsync();
            return expenseTypes.FirstOrDefault(t => IsExchangeExpenseType(t.Name))?.Id;
        }

        private static bool IsExchangeExpense(
            int expenseId,
            int expenseTypeId,
            string? expenseTypeName,
            int? exchangeTypeId,
            HashSet<int> linkedExchangeExpenseIds)
        {
            if (IsExchangeExpenseType(expenseTypeName))
                return true;

            if (exchangeTypeId.HasValue && expenseTypeId == exchangeTypeId.Value)
                return true;

            return linkedExchangeExpenseIds.Contains(expenseId);
        }

        private async Task<bool> IsExchangeExpenseTypeIdAsync(int expenseTypeId)
        {
            if (expenseTypeId <= 0)
                return false;

            var exchangeTypeId = await GetExchangeExpenseTypeIdAsync();
            return exchangeTypeId.HasValue && expenseTypeId == exchangeTypeId.Value;
        }

        private async Task<bool> IsExchangeExpenseProtectedAsync(Expense expense)
        {
            if (await IsExchangeExpenseTypeIdAsync(expense.ExpenseTypeID))
                return true;

            return await _context.CurrencyExchanges
                .AsNoTracking()
                .AnyAsync(x => x.ExpenseId == expense.ID);
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
