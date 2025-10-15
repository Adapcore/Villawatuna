using HotelManagement.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HotelManagement.Services;
using Microsoft.AspNetCore.Authorization;

namespace HotelManagement.Controllers
{
    [Authorize]
    public class EmployeesController : Controller
	{

		private readonly IEmployeeService _employeeService;

		public EmployeesController(IEmployeeService employeeService)
		{
			_employeeService = employeeService;
		}

		public async Task<IActionResult> Index()
		{
			List<Employee> employees = await _employeeService.GetAllAsync();
			return View(employees);
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


