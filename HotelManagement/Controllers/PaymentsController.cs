using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using HotelManagement.Models;
using HotelManagement.Services.Interfaces;
using HotelManagement.Models.Entities;
using Microsoft.AspNetCore.Authorization;

namespace HotelManagement.Controllers
{
    [Authorize]
    public class PaymentsController : Controller
    {
        private readonly IPaymentService _paymentService;

        public PaymentsController(IPaymentService paymentService)
        {
            _paymentService = paymentService;
        }

        public async Task<IActionResult> Index()
        {
            var payments = await _paymentService.GetAllAsync();
            return View(payments);
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
