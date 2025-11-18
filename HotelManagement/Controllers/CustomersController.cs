using HotelManagement.Models.DTO;
using HotelManagement.Models.Entities;
using HotelManagement.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using X.PagedList.Extensions;
using System.Linq;
using HotelManagement.Helper;

namespace HotelManagement.Controllers
{
    [Authorize]
    public class CustomersController : Controller
    {
        private readonly ICustomerService _customerService;
        private readonly int _pageSize;

        // Country list centralized in CountryList helper

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
            ViewBag.Countries = CountryList.All;
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

            if (string.IsNullOrWhiteSpace(model.Email))
                ModelState.AddModelError(nameof(model.Email), "Email is required.");

            if (string.IsNullOrWhiteSpace(model.ContactNo))
                ModelState.AddModelError(nameof(model.ContactNo), "Contact No is required.");

            if (string.IsNullOrWhiteSpace(model.PassportNo))
                ModelState.AddModelError(nameof(model.PassportNo), "Passport No is required.");

            // Validate RoomNo if provided - must be numeric
            if (!string.IsNullOrWhiteSpace(model.RoomNo) && !int.TryParse(model.RoomNo, out _))
            {
                ModelState.AddModelError(nameof(model.RoomNo), "Room No must contain only numeric digits.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Countries = CountryList.All;
                return View(model);
            }

            if (!string.IsNullOrWhiteSpace(model.Email) && await _customerService.EmailExistsAsync(model.Email))
            {
                ModelState.AddModelError(nameof(model.Email), "Email is already in use.");
                return View(model);
            }
            
            if (!string.IsNullOrWhiteSpace(model.ContactNo) && await _customerService.ContactExistsAsync(model.ContactNo))
            {
                ModelState.AddModelError(nameof(model.ContactNo), "Contact is already in use.");
                return View(model);
            }

            await _customerService.CreateAsync(model);
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var customer = await _customerService.GetByIdAsync(id);
            if (customer == null) return NotFound();
            ViewBag.Countries = CountryList.All;
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

            // Validate RoomNo if provided - must be numeric
            if (!string.IsNullOrWhiteSpace(model.RoomNo) && !int.TryParse(model.RoomNo, out _))
            {
                ModelState.AddModelError(nameof(model.RoomNo), "Room No must contain only numeric digits.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Countries = CountryList.All;
                return View(model);
            }

            if (!string.IsNullOrWhiteSpace(model.Email) && await _customerService.EmailExistsAsync(model.Email, model.ID))
            {
                ModelState.AddModelError(nameof(model.Email), "Email is already in use by another customer.");
                ViewBag.Countries = CountryList.All;
                return View(model);
            }

            await _customerService.UpdateAsync(model);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateQuick([FromForm] Customer model)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(model.FirstName))
                errors.Add("First name is required.");

            if (string.IsNullOrWhiteSpace(model.LastName))
                errors.Add("Last name is required.");

            if (string.IsNullOrWhiteSpace(model.Email))
                errors.Add("Email is required.");

            if (string.IsNullOrWhiteSpace(model.ContactNo))
                errors.Add("Contact No is required.");

            if (string.IsNullOrWhiteSpace(model.PassportNo))
                errors.Add("Passport No is required.");

            // Validate RoomNo if provided - must be numeric
            if (!string.IsNullOrWhiteSpace(model.RoomNo) && !int.TryParse(model.RoomNo, out _))
            {
                errors.Add("Room No must contain only numeric digits.");
            }

            // Uniqueness checks: allow updating existing records
            if (!string.IsNullOrWhiteSpace(model.Email) && await _customerService.EmailExistsAsync(model.Email, model.ID > 0 ? model.ID : null))
                errors.Add("Email is already in use by another customer.");

            if (!string.IsNullOrWhiteSpace(model.ContactNo) && await _customerService.ContactExistsAsync(model.ContactNo, model.ID > 0 ? model.ID : null))
                errors.Add("Contact is already in use.");

            if (errors.Any())
                return Json(new { success = false, errors });

            // Update path if ID provided or email exists
            if (model.ID > 0)
            {
                var existing = await _customerService.GetByIdAsync(model.ID);
                if (existing == null)
                    return Json(new { success = false, errors = new[] { "Customer not found." } });

                existing.FirstName = model.FirstName;
                existing.LastName = model.LastName;
                // keep existing.Email unchanged if disabled on UI; but if passed, keep same
                existing.ContactNo = model.ContactNo;
                existing.Address = model.Address;
                existing.Country = model.Country;
                existing.PassportNo = model.PassportNo;
                existing.RoomNo = model.RoomNo;
                existing.Active = model.Active;

                await _customerService.UpdateAsync(existing);

                return Json(new
                {
                    success = true,
                    customer = existing
                });
            }
            else
            {
                // If email exists, treat as update flow
                if (await _customerService.EmailExistsAsync(model.Email))
                {
                    var all = await _customerService.GetAllAsync();
                    var existing = all.FirstOrDefault(c => c.Email == model.Email);
                    if (existing != null)
                    {
                        existing.FirstName = model.FirstName;
                        existing.LastName = model.LastName;
                        existing.ContactNo = model.ContactNo;
                        existing.Address = model.Address;
                        existing.Country = model.Country;
                        existing.PassportNo = model.PassportNo;
                        existing.RoomNo = model.RoomNo;
                        existing.Active = model.Active;

                        await _customerService.UpdateAsync(existing);

                        return Json(new
                        {
                            success = true,
                            customer = new { id = existing.ID, name = $"{existing.FirstName} {existing.LastName}".Trim() }
                        });
                    }
                }

                var created = await _customerService.CreateAsync(model);
                return Json(new
                {
                    success = true,
                    customer = created
                });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetByEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return Json(new { success = false });

            var customers = await _customerService.GetAllAsync();
            var existing = customers.FirstOrDefault(c => c.Email == email);
            if (existing == null) 
                return Json(new { success = true, exists = false });

            return Json(new
            {
                success = true,
                exists = true,
                    customer = new
                    {
                        id = existing.ID,
                        firstName = existing.FirstName,
                        lastName = existing.LastName,
                        email = existing.Email,
                        contactNo = existing.ContactNo,
                        address = existing.Address,
                        country = existing.Country,
                        passportNo = existing.PassportNo,
                        roomNo = existing.RoomNo,
                        active = existing.Active
                    }
            });
        }
    }
}