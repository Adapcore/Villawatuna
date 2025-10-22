using HotelManagement.Models.DTO;
using HotelManagement.Models.Entities;
using HotelManagement.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using X.PagedList.Extensions;

namespace HotelManagement.Controllers
{
    [Authorize]
    public class CustomersController : Controller
    {
        private readonly ICustomerService _customerService;
        private readonly int _pageSize;

        public CustomersController(ICustomerService customerService,
            IOptions<PaginationSettings> paginationSettings)
        {
            _customerService = customerService;
            _pageSize = paginationSettings.Value.DefaultPageSize;
        }

        public async Task<IActionResult> Index(int page = 1)
        {
            int pageNumber = page < 1 ? 1 : page;

            var customers = await _customerService.GetAllAsync();
            var pagedList = customers.ToPagedList(pageNumber, _pageSize);

            return View(pagedList);
        }

        public IActionResult Create()
        {
            return View(new Customer());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Customer model)
        {
            if (string.IsNullOrWhiteSpace(model.FirstName))
                ModelState.AddModelError(nameof(model.FirstName), "First name is required.");

            if (string.IsNullOrWhiteSpace(model.LastName))
                ModelState.AddModelError(nameof(model.LastName), "Last name is required.");

            if (!ModelState.IsValid)
                return View(model);

            if (!string.IsNullOrWhiteSpace(model.Email) && await _customerService.EmailExistsAsync(model.Email))
            {
                ModelState.AddModelError(nameof(model.Email), "Email is already in use.");
                return View(model);
            }

            await _customerService.CreateAsync(model);
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var customer = await _customerService.GetByIdAsync(id);
            if (customer == null) return NotFound();
            return View(customer);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Customer model)
        {
            if (id != model.ID) return BadRequest();

            if (string.IsNullOrWhiteSpace(model.FirstName))
                ModelState.AddModelError(nameof(model.FirstName), "First name is required.");

            if (string.IsNullOrWhiteSpace(model.LastName))
                ModelState.AddModelError(nameof(model.LastName), "Last name is required.");

            if (!ModelState.IsValid) return View(model);

            if (!string.IsNullOrWhiteSpace(model.Email) && await _customerService.EmailExistsAsync(model.Email, model.ID))
            {
                ModelState.AddModelError(nameof(model.Email), "Email is already in use by another customer.");
                return View(model);
            }

            await _customerService.UpdateAsync(model);
            return RedirectToAction(nameof(Index));
        }
    }
}