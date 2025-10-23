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
    public class EmployeesController : Controller
	{
		private readonly IEmployeeService _employeeService;
        private readonly int _pageSize;

        public EmployeesController(IEmployeeService employeeService,
             IOptions<PaginationSettings> paginationSettings)
		{
			_employeeService = employeeService;
            _pageSize = paginationSettings.Value.DefaultPageSize;
        }

        public async Task<IActionResult> Index(int page = 1)
        {
            int pageNumber = page < 1 ? 1 : page;

            List<Employee> employees = await _employeeService.GetAllAsync();
            var pagedList = employees.ToPagedList(pageNumber, _pageSize);

            return View(pagedList);
        }

		public IActionResult Create()
		{
			return View(new Employee
			{
				JoinedDate = DateTime.UtcNow.Date
			});
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Create(Employee model)
		{
			// Require Password, FirstName, and LastName on create
			if (string.IsNullOrWhiteSpace(model.Password))
			{
				ModelState.AddModelError(nameof(model.Password), "Password is required.");
			}
			if (string.IsNullOrWhiteSpace(model.FirstName))
			{
				ModelState.AddModelError(nameof(model.FirstName), "First name is required.");
			}
			if (string.IsNullOrWhiteSpace(model.LastName))
			{
				ModelState.AddModelError(nameof(model.LastName), "Last name is required.");
			}
			if (string.IsNullOrWhiteSpace(model.ContactNo))
			{
				ModelState.AddModelError(nameof(model.ContactNo), "Contact No is required.");
			}

			if (!ModelState.IsValid)
			{
				return View(model);
			}

			if (!string.IsNullOrWhiteSpace(model.Email))
			{
				bool emailExists = await _employeeService.EmailExistsAsync(model.Email);
				if (emailExists)
				{
					ModelState.AddModelError(nameof(model.Email), "Email is already in use.");
					return View(model);
				}
			}

			await _employeeService.CreateAsync(model);
			return RedirectToAction(nameof(Index));
		}

		public async Task<IActionResult> Edit(int id)
		{
			Employee? employee = await _employeeService.GetByIdAsync(id);
			if (employee == null)
			{
				return NotFound();
			}
			return View(employee);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Edit(int id, Employee model)
		{
			if (id != model.ID)
			{
				return BadRequest();
			}

			// Require Password, FirstName, and LastName on update
			if (string.IsNullOrWhiteSpace(model.Password))
			{
				ModelState.AddModelError(nameof(model.Password), "Password is required.");
			}
			if (string.IsNullOrWhiteSpace(model.FirstName))
			{
				ModelState.AddModelError(nameof(model.FirstName), "First name is required.");
			}
			if (string.IsNullOrWhiteSpace(model.LastName))
			{
				ModelState.AddModelError(nameof(model.LastName), "Last name is required.");
			}

			if (!ModelState.IsValid)
			{
				return View(model);
			}

			if (!string.IsNullOrWhiteSpace(model.Email))
			{
				bool emailExists = await _employeeService.EmailExistsAsync(model.Email, excludeId: model.ID);
				if (emailExists)
				{
					ModelState.AddModelError(nameof(model.Email), "Email is already in use by another employee.");
					return View(model);
				}
			}

			await _employeeService.UpdateAsync(model);
			return RedirectToAction(nameof(Index));
		}
	}
}


