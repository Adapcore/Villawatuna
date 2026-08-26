using HotelManagement.Models.DTO;
using HotelManagement.Models.Entities;
using HotelManagement.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Services;

namespace HotelManagement.Controllers
{
    [Authorize]
    public class CurrencyExchangeController : Controller
    {
        private readonly ICurrencyExchangeService _currencyExchangeService;
        private readonly ICustomerService _customerService;
        private readonly ICurrencyService _currencyService;
        private readonly IMemberManager _memberManager;
        private readonly IMemberService _memberService;
        private readonly int _pageSize;

        public CurrencyExchangeController(
            ICurrencyExchangeService currencyExchangeService,
            ICustomerService customerService,
            ICurrencyService currencyService,
            IOptions<PaginationSettings> paginationSettings,
            IMemberManager memberManager,
            IMemberService memberService)
        {
            _currencyExchangeService = currencyExchangeService;
            _customerService = customerService;
            _currencyService = currencyService;
            _pageSize = paginationSettings.Value.DefaultPageSize;
            _memberManager = memberManager;
            _memberService = memberService;
        }

        public async Task<IActionResult> Index()
        {
            ViewBag.IsAdmin = IsAdminUser();
            await PopulateLookupsAsync(includeAllOption: true);
            return View();
        }

        [HttpGet]
        [Route("CurrencyExchange/GetExchanges")]
        public async Task<IActionResult> GetExchanges(
            int page = 1,
            string? startDate = null,
            string? endDate = null,
            int customerId = 0,
            int foreignCurrencyId = 0)
        {
            int pageNumber = page < 1 ? 1 : page;

            DateTime? startDateParsed = null;
            DateTime? endDateParsed = null;

            if (!string.IsNullOrWhiteSpace(startDate) && DateTime.TryParse(startDate, out var startDateValue))
                startDateParsed = startDateValue;

            if (!string.IsNullOrWhiteSpace(endDate) && DateTime.TryParse(endDate, out var endDateValue))
                endDateParsed = endDateValue;

            int? customerFilter = customerId > 0 ? customerId : null;
            int? currencyFilter = foreignCurrencyId > 0 ? foreignCurrencyId : null;

            var (pagedList, totalLkr) = await _currencyExchangeService.GetPagedAsync(
                pageNumber,
                _pageSize,
                startDateParsed,
                endDateParsed,
                customerFilter,
                currencyFilter);

            var list = pagedList.Select(e => new
            {
                id = e.Id,
                date = e.Date.ToString("yyyy-MM-dd"),
                customerId = e.CustomerId,
                customerName = e.Customer != null
                    ? $"{e.Customer.FirstName} {e.Customer.LastName}".Trim()
                    : "",
                foreignCurrencyId = e.ForeignCurrencyId,
                foreignCurrencyCode = e.ForeignCurrency?.Code ?? "",
                exchangeRate = e.ExchangeRate,
                foreignAmount = e.ForeignAmount,
                lkrAmount = e.LKRAmount,
                remarks = e.Remarks ?? ""
            }).ToList();

            return Json(new
            {
                success = true,
                exchanges = list,
                totalLkrAmount = totalLkr,
                pagination = new
                {
                    pageNumber = pagedList.PageNumber,
                    pageCount = pagedList.PageCount,
                    totalItemCount = pagedList.TotalItemCount,
                    hasPreviousPage = pagedList.HasPreviousPage,
                    hasNextPage = pagedList.HasNextPage,
                    isFirstPage = pagedList.IsFirstPage,
                    isLastPage = pagedList.IsLastPage
                }
            });
        }

        public async Task<IActionResult> Create()
        {
            await PopulateLookupsAsync();
            return View(new CurrencyExchange { Date = DateTime.Today });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CurrencyExchange model)
        {
            await ValidateExchangeAsync(model);

            if (!ModelState.IsValid)
            {
                await PopulateLookupsAsync(selectedCustomerId: model.CustomerId);
                return View(model);
            }

            try
            {
                await _currencyExchangeService.AddAsync(model);
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                await PopulateLookupsAsync(selectedCustomerId: model.CustomerId);
                return View(model);
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var model = await _currencyExchangeService.GetByIdAsync(id);
            if (model == null)
                return NotFound();

            await PopulateLookupsAsync(selectedCustomerId: model.CustomerId);
            return View("Create", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(CurrencyExchange model)
        {
            await ValidateExchangeAsync(model);

            if (!ModelState.IsValid)
            {
                await PopulateLookupsAsync(selectedCustomerId: model.CustomerId);
                return View("Create", model);
            }

            try
            {
                await _currencyExchangeService.UpdateAsync(model);
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                await PopulateLookupsAsync(selectedCustomerId: model.CustomerId);
                return View("Create", model);
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("CurrencyExchange/Delete")]
        public async Task<IActionResult> Delete(int id)
        {
            if (!IsAdminUser())
                return Json(new { success = false, message = "Only admin users can delete currency exchanges." });

            try
            {
                var existing = await _currencyExchangeService.GetByIdAsync(id);
                if (existing == null)
                    return Json(new { success = false, message = "Currency exchange not found." });

                await _currencyExchangeService.DeleteAsync(id);
                return Json(new { success = true, message = "Currency exchange deleted successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error deleting currency exchange: {ex.Message}" });
            }
        }

        private async Task ValidateExchangeAsync(CurrencyExchange model)
        {
            ModelState.Remove(nameof(model.Customer));
            ModelState.Remove(nameof(model.ForeignCurrency));
            ModelState.Remove(nameof(model.CreatedByMember));
            ModelState.Remove(nameof(model.Invoice));
            ModelState.Remove(nameof(model.Expense));

            if (model.CustomerId <= 0)
                ModelState.AddModelError(nameof(model.CustomerId), "Customer is required.");

            if (model.ForeignCurrencyId <= 0)
                ModelState.AddModelError(nameof(model.ForeignCurrencyId), "Foreign currency is required.");

            if (model.ExchangeRate <= 0)
                ModelState.AddModelError(nameof(model.ExchangeRate), "Exchange rate must be greater than zero.");

            if (model.ForeignAmount <= 0)
                ModelState.AddModelError(nameof(model.ForeignAmount), "Foreign amount must be greater than zero.");

            if (model.Date == default || model.Date.Year < 1900)
                ModelState.AddModelError(nameof(model.Date), "Exchange date is required.");

            if (model.CustomerId > 0)
            {
                var customer = await _customerService.GetByIdAsync(model.CustomerId);
                if (customer == null)
                    ModelState.AddModelError(nameof(model.CustomerId), "Selected customer was not found.");
            }

            if (model.ForeignCurrencyId > 0)
            {
                var currencies = await _currencyService.GetCurencyTypesAsync();
                if (!currencies.Any(c => c.Id == model.ForeignCurrencyId
                    && !string.Equals(c.Code, "LKR", StringComparison.OrdinalIgnoreCase)))
                {
                    ModelState.AddModelError(nameof(model.ForeignCurrencyId), "Selected currency is not valid.");
                }
            }

            model.LKRAmount = Math.Round(model.ExchangeRate * model.ForeignAmount, 2, MidpointRounding.AwayFromZero);
        }

        private async Task PopulateLookupsAsync(bool includeAllOption = false, int selectedCustomerId = 0)
        {
            var customers = await _customerService.GetAllAsync();
            var customerItems = customers
                .Where(c => c.Active || c.ID == selectedCustomerId)
                .Select(c => new SelectListItem
                {
                    Value = c.ID.ToString(),
                    Text = !string.IsNullOrWhiteSpace(c.RoomNo)
                        ? $"#{c.RoomNo} - {c.FirstName} {c.LastName}"
                        : $"{c.FirstName} {c.LastName}"
                })
                .ToList();

            if (includeAllOption)
            {
                customerItems.Insert(0, new SelectListItem
                {
                    Value = "0",
                    Text = "-- All Customers --"
                });
            }

            ViewBag.Customers = customerItems;

            var currencies = (await _currencyService.GetCurencyTypesAsync())
                .Where(c => !string.Equals(c.Code, "LKR", StringComparison.OrdinalIgnoreCase))
                .ToList();

            var currencyItems = currencies.Select(c => new SelectListItem
            {
                Value = c.Id.ToString(),
                Text = string.IsNullOrWhiteSpace(c.Code) ? c.Name : c.Code
            }).ToList();

            if (includeAllOption)
            {
                currencyItems.Insert(0, new SelectListItem
                {
                    Value = "0",
                    Text = "-- All Currencies --"
                });
            }

            ViewBag.Currencies = currencyItems;
            ViewBag.CurrencyData = currencies;
        }

        private bool IsAdminUser()
        {
            try
            {
                var username = User?.Identity?.Name;
                if (string.IsNullOrWhiteSpace(username))
                    return false;

                var memberIdentity = _memberManager.FindByNameAsync(username).GetAwaiter().GetResult();
                if (memberIdentity == null)
                    return false;

                var member = _memberService.GetByKey(memberIdentity.Key);
                if (member == null)
                    return false;

                var rawType = member.GetValue<string>("userType") ?? "";
                var userType = rawType.Replace("[", "").Replace("]", "").Replace("\"", "").Trim();
                return string.Equals(userType, "Admin", StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }
    }
}
