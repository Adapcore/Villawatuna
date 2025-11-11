using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using HotelManagement.Data;
using HotelManagement.Enums;
using Microsoft.EntityFrameworkCore;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Services;

namespace HotelManagement.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly HotelContext _context;

        private readonly IMemberManager _memberManager;
        private readonly IMemberService _memberService;

        public DashboardController(HotelContext context, IMemberManager memberManager, IMemberService memberService)
        {
            _context = context;
            _memberManager = memberManager;
            _memberService = memberService;
        }

        public IActionResult Index()
        {
            if (!(User?.Identity?.IsAuthenticated ?? false))
            {
                return RedirectToAction("Index", "Home");
            }

            ViewBag.IsAdmin = IsAdminUser();
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetMetrics(DateTime? from, DateTime? to)
        {
            bool isAdmin = IsAdminUser();
            DateTime fromDate = from?.Date ?? DateTime.Today;
            DateTime toDate = (to?.Date ?? DateTime.Today).AddDays(1).AddTicks(-1);

            var invoices = _context.Invoices.AsNoTracking()
                .Where(i => (i.Status == InvoiceStatus.InProgress || i.Status == InvoiceStatus.PartiallyPaid || i.Status == InvoiceStatus.Complete)
                && i.Date >= fromDate && i.Date <= toDate);

            var expenses = _context.Expenses.AsNoTracking()
                .Where(e => e.Date >= fromDate && e.Date <= toDate);

            decimal totalRevenue = await invoices.SumAsync(i => (decimal?)i.TotalPaid) ?? 0m;

            decimal serviceCharges = await invoices.SumAsync(i => (decimal?)i.ServiceCharge) ?? 0m;

            decimal restaurantRevenue = await invoices
                .Where(i => i.Type == InvoiceType.Dining || i.Type == InvoiceType.TakeAway)
                .SumAsync(i => (decimal?)i.TotalPaid) ?? 0m;

            decimal tourRevenue = await invoices
                .Where(i => i.Type == InvoiceType.Tour)
                .SumAsync(i => (decimal?)i.TotalPaid) ?? 0m;

            decimal laundryRevenue = await invoices
                .Where(i => i.Type == InvoiceType.Laundry)
                .SumAsync(i => (decimal?)i.TotalPaid) ?? 0m;

            decimal totalExpenses = await expenses.SumAsync(e => (decimal?)e.Amount) ?? 0m;

            return Json(new
            {
                success = true,
                data = new
                {
                    totalRevenue = isAdmin ? totalRevenue : 0m,
                    totalExpenses,
                    restaurantRevenue,
                    serviceCharges,
                    laundryRevenue,
                    tourRevenue
                },
                isAdmin
            });
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


