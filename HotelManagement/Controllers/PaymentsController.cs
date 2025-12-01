using HotelManagement.Models.DTO;
using HotelManagement.Models.Entities;
using HotelManagement.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using X.PagedList.Extensions;

namespace HotelManagement.Controllers
{
    [Authorize]
    public class PaymentsController : Controller
    {
        private readonly IPaymentService _paymentService;
        private readonly int _pageSize;

        public PaymentsController(IPaymentService paymentService,
            IOptions<PaginationSettings> paginationSettings)
        {
            _paymentService = paymentService;
            _pageSize = paginationSettings.Value.DefaultPageSize;
        }

        public async Task<IActionResult> Index(int page = 1)
        {
            int pageNumber = page < 1 ? 1 : page;

            var payments = await _paymentService.GetAllAsync();
            var pagedList = payments.ToPagedList(pageNumber, _pageSize);

            return View(pagedList);
        }

        public async Task<IActionResult> Details(int id)
        {
            var payment = await _paymentService.GetByIdAsync(id);
            if (payment == null)
                return NotFound();

            return View(payment);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Payment payment)
        {
            // Skip validation for the non-persistent property
            ModelState.Remove("CreatedByMember");

            if (!ModelState.IsValid)
                return View(payment);

            await _paymentService.CreateAsync(payment);
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int id)
        {
            await _paymentService.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }
    }
}
