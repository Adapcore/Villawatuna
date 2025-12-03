using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using HotelManagement.Data;
using HotelManagement.Enums;
using Microsoft.EntityFrameworkCore;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Services;
using HotelManagement.Services.Interface;

namespace HotelManagement.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly HotelContext _context;

        private readonly IMemberManager _memberManager;
        private readonly IMemberService _memberService;
        private readonly IExpenseTypeService _expenseTypeService;

        public HomeController(HotelContext context, IMemberManager memberManager, IMemberService memberService, IExpenseTypeService expenseTypeService)
        {
            _context = context;
            _memberManager = memberManager;
            _memberService = memberService;
            _expenseTypeService = expenseTypeService;
        }

        public IActionResult Index()
        {
            if (!(User?.Identity?.IsAuthenticated ?? false))
            {
                return RedirectToAction("Login", "Account");
            }

            ViewBag.IsAdmin = IsAdminUser();
            return View("Home");
        }

        [HttpGet]
        public async Task<IActionResult> GetMetrics(DateTime? from, DateTime? to)
        {
            bool isAdmin = IsAdminUser();
            DateTime fromDate = from?.Date ?? DateTime.Today;
            DateTime toDate = (to?.Date ?? DateTime.Today).AddDays(1).AddTicks(-1);

            var payments = _context.Payments.AsNoTracking().Where(i => i.Date >= fromDate && i.Date <= toDate);

            decimal totalIncome = await payments.SumAsync(i => (decimal?)i.Amount) ?? 0m;

            decimal serviceCharges = await _context.Invoices.AsNoTracking()
                                          .Where(i => i.Status == InvoiceStatus.Paid && i.Type == InvoiceType.Dining && i.Date >= fromDate && i.Date <= toDate)
                                          .SumAsync(i => (decimal?)i.ServiceCharge) ?? 0m;

            decimal stayRevenue = await payments.Where(e => e.Invoice.Type == InvoiceType.Stay).SumAsync(e => (decimal?)e.Amount) ?? 0m;

            decimal restaurantRevenue = await payments.Where(e => e.Invoice.Type == InvoiceType.Dining || e.Invoice.Type == InvoiceType.TakeAway)
                                            .SumAsync(e => (decimal?)e.Amount) ?? 0m;

            decimal tourRevenue = await payments.Where(e => e.Invoice.Type == InvoiceType.Tour).SumAsync(e => (decimal?)e.Amount) ?? 0m;

            decimal laundryRevenue = await payments.Where(e => e.Invoice.Type == InvoiceType.Laundry).SumAsync(e => (decimal?)e.Amount) ?? 0m;

            decimal otherRevenue = await payments.Where(e => e.Invoice.Type == InvoiceType.Other).SumAsync(e => (decimal?)e.Amount) ?? 0m;

            var expenses = _context.Expenses.AsNoTracking().Where(e => e.Date >= fromDate && e.Date <= toDate);

            decimal totalExpenses = await expenses.SumAsync(e => (decimal?)e.Amount) ?? 0m;

            var invoices = _context.Invoices.AsNoTracking()
                .Where(i => (i.Status == InvoiceStatus.PartiallyPaid || i.Status == InvoiceStatus.Complete || i.Status == InvoiceStatus.Paid)
                && i.Date >= fromDate && i.Date <= toDate);

            /*
            decimal totalRevenue = await invoices.SumAsync(i => (decimal?)i.TotalPaid) ?? 0m;

            decimal serviceCharges = await invoices.Where(i => i.Status == InvoiceStatus.Paid).SumAsync(i => (decimal?)i.ServiceCharge) ?? 0m;

            decimal restaurantRevenue = await invoices
                .Where(i => i.Type == InvoiceType.Dining || i.Type == InvoiceType.TakeAway)
                .SumAsync(i => (decimal?)i.TotalPaid) ?? 0m;

            decimal tourRevenue = await invoices
                .Where(i => i.Type == InvoiceType.Tour)
                .SumAsync(i => (decimal?)i.TotalPaid) ?? 0m;

            decimal laundryRevenue = await invoices
                .Where(i => i.Type == InvoiceType.Laundry)
                .SumAsync(i => (decimal?)i.TotalPaid) ?? 0m;

            decimal stayRevenue = await invoices
                .Where(i => i.Type == InvoiceType.Stay)
                .SumAsync(i => (decimal?)i.TotalPaid) ?? 0m;
           
            decimal otherRevenue = await invoices
                .Where(i => i.Type == InvoiceType.Other)
                .SumAsync(i => (decimal?)i.TotalPaid) ?? 0m;
 */

            //decimal totalIncome = isAdmin ? totalRevenue : 0m;
            decimal netRevenue = totalIncome - totalExpenses;

            return Json(new
            {
                success = true,
                data = new
                {
                    totalIncome,
                    totalRevenue = netRevenue,
                    totalExpenses,
                    restaurantRevenue,
                    serviceCharges,
                    laundryRevenue,
                    tourRevenue,
                    stayRevenue,
                    otherRevenue
                },
                isAdmin
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetTileData(string tileType, DateTime? from, DateTime? to)
        {
            DateTime fromDate = from?.Date ?? DateTime.Today;
            DateTime toDate = (to?.Date ?? DateTime.Today).AddDays(1).AddTicks(-1);
            bool isAdmin = IsAdminUser();

            var data = new List<object>();

            switch (tileType.ToLower())
            {
                case "invoices":
                    var invoices = await _context.Payments
                        .AsNoTracking()
                        .Include(i => i.Invoice)
                        .Where(i => i.Date >= fromDate && i.Date <= toDate)
                        .OrderByDescending(i => i.Date)
                        .ThenByDescending(i => i.InvoiceNo)
                        .Select(i => new
                        {
                            id = i.InvoiceNo,
                            date = i.Date,
                            type = i.Invoice.Type.ToString(),
                            customerName = i.Invoice.Customer != null ? (i.Invoice.Customer.RoomNo != null ? $"#{i.Invoice.Customer.RoomNo} - {i.Invoice.Customer.FirstName} {i.Invoice.Customer.LastName}" : $"{i.Invoice.Customer.FirstName} {i.Invoice.Customer.LastName}") : "",
                            amount = i.Amount,
                            status = i.Invoice.Status.ToString()
                        })
                        .ToListAsync();
                    data = invoices.Cast<object>().ToList();
                    break;

                case "expenses":
                    var expensesList = await _context.Expenses
                        .AsNoTracking()
                        .Where(e => e.Date >= fromDate && e.Date <= toDate)
                        .OrderByDescending(e => e.Date)
                        .ThenByDescending(e => e.ID)
                        .ToListAsync();

                    var expenseTypes = await _expenseTypeService.GetExpenseTypesAsync();

                    var expenses = expensesList.Select(e => new
                    {
                        id = e.ID,
                        date = e.Date,
                        expenseType = e.ExpenseTypeID,
                        expenseTypeName = expenseTypes.FirstOrDefault(et => et.Id == e.ExpenseTypeID)?.Name ?? "",
                        payeeName = e.PayeeName ?? "",
                        amount = e.Amount,
                        paymentMethod = e.PaymentMethod.ToString()
                    }).ToList();

                    data = expenses.Cast<object>().ToList();
                    break;

                case "restaurant":
                    var restaurantInvoices = await _context.Payments
                        .AsNoTracking()
                        .Include(i => i.Invoice)
                        .Where(i => (i.Invoice.Type == InvoiceType.Dining || i.Invoice.Type == InvoiceType.TakeAway) && i.Date >= fromDate && i.Date <= toDate)
                        .OrderByDescending(i => i.Date)
                        .ThenByDescending(i => i.InvoiceNo)
                        .Select(i => new
                        {
                            id = i.InvoiceNo,
                            date = i.Date,
                            type = i.Invoice.Type.ToString(),
                            customerName = i.Invoice.Customer != null ? (i.Invoice.Customer.RoomNo != null ? $"#{i.Invoice.Customer.RoomNo} - {i.Invoice.Customer.FirstName} {i.Invoice.Customer.LastName}" : $"{i.Invoice.Customer.FirstName} {i.Invoice.Customer.LastName}") : "",
                            amount = i.Amount,
                            status = i.Invoice.Status.ToString()
                        })
                        .ToListAsync();
                    data = restaurantInvoices.Cast<object>().ToList();
                    break;

                case "stay":
                    var stayInvoices = await _context.Payments
                        .AsNoTracking()
                        .Include(i => i.Invoice)
                        .Where(i => i.Invoice.Type == InvoiceType.Stay && i.Date >= fromDate && i.Date <= toDate)
                        .OrderByDescending(i => i.Date)
                        .ThenByDescending(i => i.InvoiceNo)
                        .Select(i => new
                        {
                            id = i.InvoiceNo,
                            date = i.Date,
                            type = i.Invoice.Type.ToString(),
                            customerName = i.Invoice.Customer != null ? (i.Invoice.Customer.RoomNo != null ? $"#{i.Invoice.Customer.RoomNo} - {i.Invoice.Customer.FirstName} {i.Invoice.Customer.LastName}" : $"{i.Invoice.Customer.FirstName} {i.Invoice.Customer.LastName}") : "",
                            amount = i.Amount,
                            status = i.Invoice.Status.ToString()
                        })
                        .ToListAsync();
                    data = stayInvoices.Cast<object>().ToList();
                    break;

                case "laundry":
                    var laundryInvoices = await _context.Payments
                        .AsNoTracking()
                        .Include(i => i.Invoice)
                        .Where(i => i.Invoice.Type == InvoiceType.Laundry && i.Date >= fromDate && i.Date <= toDate)
                        .OrderByDescending(i => i.Date)
                        .ThenByDescending(i => i.InvoiceNo)
                        .Select(i => new
                        {
                            id = i.InvoiceNo,
                            date = i.Date,
                            type = i.Invoice.Type.ToString(),
                            customerName = i.Invoice.Customer != null ? (i.Invoice.Customer.RoomNo != null ? $"#{i.Invoice.Customer.RoomNo} - {i.Invoice.Customer.FirstName} {i.Invoice.Customer.LastName}" : $"{i.Invoice.Customer.FirstName} {i.Invoice.Customer.LastName}") : "",
                            amount = i.Amount,
                            status = i.Invoice.Status.ToString()
                        })
                        .ToListAsync();
                    data = laundryInvoices.Cast<object>().ToList();                    
                    break;

                case "tour":
                    var tourInvoices = await _context.Payments
                        .AsNoTracking()
                        .Include(i => i.Invoice)
                        .Where(i => i.Invoice.Type == InvoiceType.Tour && i.Date >= fromDate && i.Date <= toDate)
                        .OrderByDescending(i => i.Date)
                        .ThenByDescending(i => i.InvoiceNo)
                        .Select(i => new
                        {
                            id = i.InvoiceNo,
                            date = i.Date,
                            type = i.Type.ToString(),
                            customerName = i.Invoice.Customer != null ? (i.Invoice.Customer.RoomNo != null ? $"#{i.Invoice.Customer.RoomNo} - {i.Invoice.Customer.FirstName} {i.Invoice.Customer.LastName}" : $"{i.Invoice.Customer.FirstName} {i.Invoice.Customer.LastName}") : "",
                            amount = i.Amount,
                            status = i.Invoice.Status.ToString()
                        })
                        .ToListAsync();
                    data = tourInvoices.Cast<object>().ToList();
                    break;

                case "other":
                    var otherInvoices = await _context.Payments
                        .AsNoTracking()
                        .Include(i => i.Invoice)
                        .Where(i => i.Invoice.Type == InvoiceType.Other && i.Date >= fromDate && i.Date <= toDate)
                        .OrderByDescending(i => i.Date)
                        .ThenByDescending(i => i.InvoiceNo)
                        .Select(i => new
                        {
                            id = i.InvoiceNo,
                            date = i.Date,
                            type = i.Type.ToString(),
                            customerName = i.Invoice.Customer != null ? (i.Invoice.Customer.RoomNo != null ? $"#{i.Invoice.Customer.RoomNo} - {i.Invoice.Customer.FirstName} {i.Invoice.Customer.LastName}" : $"{i.Invoice.Customer.FirstName} {i.Invoice.Customer.LastName}") : "",
                            amount = i.Amount,
                            status = i.Invoice.Status.ToString()
                        })
                        .ToListAsync();
                    data = otherInvoices.Cast<object>().ToList();
                    break;

                case "servicecharges":
                    var serviceChargeInvoices = await _context.Invoices
                        .AsNoTracking()
                        .Include(i => i.Customer)
                        .Where(i => i.Status == InvoiceStatus.Paid && i.Type == InvoiceType.Dining && i.Date >= fromDate && i.Date <= toDate && i.ServiceCharge > 0)
                        .OrderByDescending(i => i.Date)
                        .ThenByDescending(i => i.InvoiceNo)
                        .Select(i => new
                        {
                            id = i.InvoiceNo,
                            date = i.Date,
                            type = i.Type.ToString(),
                            customerName = i.Customer != null ? (i.Customer.RoomNo != null ? $"#{i.Customer.RoomNo} - {i.Customer.FirstName} {i.Customer.LastName}" : $"{i.Customer.FirstName} {i.Customer.LastName}") : "",
                            amount = i.ServiceCharge,
                            status = i.Status.ToString()
                        })
                        .ToListAsync();
                    data = serviceChargeInvoices.Cast<object>().ToList();
                    break;
            }

            return Json(new { success = true, data = data, isAdmin = isAdmin });
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

