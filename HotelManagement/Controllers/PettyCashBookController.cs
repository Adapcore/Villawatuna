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
    public class PettyCashBookController : Controller
    {
        private readonly IPettyCashBookService _service;
        private readonly ICurrencyService _currencyService;
        private readonly int _pageSize;
        private readonly IMemberManager _memberManager;
        private readonly IMemberService _memberService;

        public PettyCashBookController(
            IPettyCashBookService service,
            ICurrencyService currencyService,
            IOptions<PaginationSettings> paginationSettings,
            IMemberManager memberManager,
            IMemberService memberService)
        {
            _service = service;
            _currencyService = currencyService;
            _pageSize = paginationSettings.Value.DefaultPageSize;
            _memberManager = memberManager;
            _memberService = memberService;
        }

        public IActionResult Index()
        {
            ViewBag.IsAdmin = IsAdminUser();
            return View();
        }

        [HttpGet]
        [Route("PettyCashBook/GetEntries")]
        public async Task<IActionResult> GetEntries(int page = 1, string? startDate = null, string? endDate = null)
        {
            int pageNumber = page < 1 ? 1 : page;

            DateTime? startDateParsed = null;
            DateTime? endDateParsed = null;

            if (!string.IsNullOrWhiteSpace(startDate) && DateTime.TryParse(startDate, out var startDateValue))
                startDateParsed = startDateValue;

            if (!string.IsNullOrWhiteSpace(endDate) && DateTime.TryParse(endDate, out var endDateValue))
                endDateParsed = endDateValue;

            var pagedList = await _service.GetPagedAsync(
                pageNumber,
                _pageSize,
                startDateParsed,
                endDateParsed);

            var list = pagedList.Select(e => new
            {
                id = e.Id,
                date = e.Date.ToString("yyyy-MM-dd"),
                openingBalance = e.OpeningBalance,
                pettyCashRefillOpening = e.PettyCashRefillOpening != 0
                    ? e.PettyCashRefillOpening
                    : e.RefillOpeningTotal,
                totalIncomeBaseCurry = e.TotalIncomeBaseCurry,
                totalExpences = e.TotalExpences,
                balance = e.Balance,
                handedOver = e.HandedOver,
                pettyCashRefillClosing = e.PettyCashRefillClosing,
                closeBalance = e.ClosingBalance
            }).ToList();

            return Json(new
            {
                success = true,
                entries = list,
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

        [HttpGet]
        public async Task<IActionResult> Create(string? date = null)
        {
            DateTime? entryDate = null;
            if (!string.IsNullOrWhiteSpace(date) && DateTime.TryParse(date, out var parsed))
                entryDate = parsed.Date;

            if (entryDate.HasValue)
            {
                var existing = await _service.GetByDateAsync(entryDate.Value);
                if (existing != null)
                    return RedirectToAction(nameof(Edit), new { id = existing.Id });
            }

            var model = new PettyCashBook
            {
                Date = entryDate ?? default,
                OpeningBalance = 0,
                PettyCashRefillClosing = 0,
                Income = new List<PettyCashIncomeLine>(),
                Expences = new List<PettyCashExpenseLine>(),
                Refills = new List<PettyCashRefillLine>()
            };

            if (entryDate.HasValue)
            {
                model.OpeningBalance = await _service.GetPreviousCloseBalanceAsync(entryDate.Value);
                model.Income = await _service.GetIncomeLinesFromPaymentsAsync(entryDate.Value);
                model.Expences = await _service.GetExpenseLinesFromExpensesAsync(entryDate.Value);
            }

            ViewBag.HasEntryDate = entryDate.HasValue;
            await PopulateCurrencyViewBagAsync();
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var model = await _service.GetByIdAsync(id);
            if (model == null)
                return NotFound();

            model.Income ??= new List<PettyCashIncomeLine>();
            model.Expences ??= new List<PettyCashExpenseLine>();
            model.Refills ??= new List<PettyCashRefillLine>();

            ViewBag.HasEntryDate = true;
            await PopulateCurrencyViewBagAsync();
            return View("Create", model);
        }

        [HttpGet]
        [Route("PettyCashBook/GetOpeningBalance")]
        public async Task<IActionResult> GetOpeningBalance(string date)
        {
            if (!DateTime.TryParse(date, out var entryDate))
                return BadRequest(new { success = false, message = "Invalid date." });

            var existing = await _service.GetByDateAsync(entryDate);
            if (existing != null)
            {
                return Json(new
                {
                    success = true,
                    exists = true,
                    id = existing.Id,
                    openingBalance = existing.OpeningBalance,
                    message = $"An entry already exists for {entryDate:yyyy-MM-dd}."
                });
            }

            var opening = await _service.GetPreviousCloseBalanceAsync(entryDate);
            var income = await _service.GetIncomeLinesFromPaymentsAsync(entryDate);
            var expenses = await _service.GetExpenseLinesFromExpensesAsync(entryDate);
            return Json(new
            {
                success = true,
                exists = false,
                openingBalance = opening,
                income = income.Select(i => new
                {
                    id = i.Id,
                    title = i.Title,
                    currency = i.Currency,
                    amount = i.Amount,
                    invoiceTypeCode = i.InvoiceTypeCode
                }),
                expenses = expenses.Select(e => new
                {
                    id = e.Id,
                    title = e.Title,
                    currency = e.Currency,
                    amount = e.Amount
                })
            });
        }

        [HttpGet]
        [Route("PettyCashBook/GetIncomeFromPayments")]
        public async Task<IActionResult> GetIncomeFromPayments(string date)
        {
            if (!DateTime.TryParse(date, out var entryDate))
                return BadRequest(new { success = false, message = "Invalid date." });

            try
            {
                var income = await _service.GetIncomeLinesFromPaymentsAsync(entryDate.Date);
                return Json(new
                {
                    success = true,
                    income = income.Select(i => new
                    {
                        id = i.Id,
                        title = i.Title,
                        currency = i.Currency,
                        amount = i.Amount,
                        invoiceTypeCode = i.InvoiceTypeCode
                    })
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        [Route("PettyCashBook/GetExpensesFromExpenses")]
        public async Task<IActionResult> GetExpensesFromExpenses(string date)
        {
            if (!DateTime.TryParse(date, out var entryDate))
                return BadRequest(new { success = false, message = "Invalid date." });

            try
            {
                var expenses = await _service.GetExpenseLinesFromExpensesAsync(entryDate.Date);
                return Json(new
                {
                    success = true,
                    expenses = expenses.Select(e => new
                    {
                        id = e.Id,
                        title = e.Title,
                        currency = e.Currency,
                        amount = e.Amount
                    })
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [Route("PettyCashBook/Save")]
        public async Task<IActionResult> Save([FromBody] PettyCashBook model)
        {
            if (model == null)
                return BadRequest(new { success = false, message = "Invalid payload." });

            if (model.Date == default || model.Date.Year < 1900)
                return BadRequest(new { success = false, message = "Please select a date." });

            try
            {
                PettyCashBook saved;
                if (model.Id > 0)
                    saved = await _service.UpdateAsync(model);
                else
                    saved = await _service.CreateAsync(model);

                return Json(new
                {
                    success = true,
                    id = saved.Id,
                    message = "Petty cash entry saved successfully.",
                    redirectUrl = Url.Action(nameof(Index))
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("PettyCashBook/Delete")]
        public async Task<IActionResult> Delete(int id)
        {
            if (!IsAdminUser())
            {
                return Json(new { success = false, message = "Only admin users can delete petty cash entries." });
            }

            try
            {
                await _service.DeleteAsync(id);
                return Json(new { success = true, message = "Entry deleted." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        private async Task PopulateCurrencyViewBagAsync()
        {
            var currencies = (await _currencyService.GetCurencyTypesAsync()).ToList();
            if (currencies.Count == 0)
            {
                currencies = new List<CurrencyDTO>
                {
                    new CurrencyDTO { Code = "LKR", Name = "Sri Lankan Rupee" },
                    new CurrencyDTO { Code = "USD", Name = "US Dollar" },
                    new CurrencyDTO { Code = "GBP", Name = "British Pound" },
                    new CurrencyDTO { Code = "EUR", Name = "Euro" }
                };
            }

            ViewBag.Currencies = currencies.Select(c => new SelectListItem
            {
                Value = c.Code,
                Text = string.IsNullOrWhiteSpace(c.Code) ? c.Name : c.Code
            }).ToList();

            ViewBag.CurrencyCodes = currencies
                .Select(c => c.Code)
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
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
