using HotelManagement.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using HotelManagement.Data;
using HotelManagement.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using HotelManagement.Services.Interface;

namespace HotelManagement.Controllers
{
    [Authorize]
    public class OrdersController : Controller
    {
        private readonly IOrderService _orderService;
        private readonly ICustomerService _customerService;
        private readonly HotelContext _context;

        public OrdersController(IOrderService orderService, ICustomerService customerService, HotelContext context)
        {
            _orderService = orderService;
            _customerService = customerService;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var orders = await _orderService.GetAllOrdersAsync();
            return View(orders);
        }

        public async Task<IActionResult> Details(int id)
        {
            var order = await _orderService.GetOrderByIdAsync(id);
            if (order == null) return NotFound();
            return View(order);
        }

        public async Task<IActionResult> Create()
        {
            var customers = await _customerService.GetAllAsync();

            ViewBag.Customers = customers.Select(c => new SelectListItem
            {
                Value = c.ID.ToString(),
                Text = c.FirstName + " " + c.LastName
            }).ToList();

            return View(new CreateOrderViewModel
            {
                Date = DateTime.UtcNow
            });
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateOrderViewModel model)
        {
           

            if (!ModelState.IsValid)
            {
                var customers = await _customerService.GetAllAsync();

                ViewBag.Customers = customers.Select(c => new SelectListItem
                {
                    Value = c.ID.ToString(),
                    Text = c.FirstName + " " + c.LastName
                }).ToList();

                //// Re-populate dropdowns before returning the view
                //ViewBag.Customers = new SelectList(
                //    await _customerService.GetAllAsync(),
                //    "ID",
                //    "FullName",
                //    model.CustomerId // reselect chosen value
                //);

                // Keep whatever user typed
                return View(model);
            }

            // subtotal/service/gross calculations 
            decimal subtotal = 0;
            foreach (var item in model.OrderItems)
            {
                item.Amount = item.Qty * item.UnitPrice;
                subtotal += item.Amount;
            }

            if (!model.IsFreeOfCharge)
                model.SubTotal = subtotal;
            else
                model.SubTotal = 0;

            if (model.Dining)
                model.ServiceCharge = model.SubTotal * 0.10m;
            else
                model.ServiceCharge = 0;

            model.GrossAmount = model.SubTotal + model.ServiceCharge;

            var order = new Order
            {
                Date = model.Date,
                CustomerId = model.CustomerId,
                TableNo = model.TableNo,
                IsFreeOfCharge = model.IsFreeOfCharge,
                Dining = model.Dining,
                Notes = model.Notes,
                SubTotal = model.SubTotal,
                ServiceCharge = model.ServiceCharge,
                GrossAmount = model.GrossAmount,
                CreatedDate = DateTime.UtcNow,
                CreatedBy = 1,
                OrderItems = model.OrderItems.Select((i, idx) => new OrderItem
                {
                    LineNumber = idx + 1,
                    ItemId = i.ItemId,
                    Comments = i.Comments,
                    Qty = i.Qty,
                    UnitPrice = i.UnitPrice,
                    Amount = i.Amount
                }).ToList()
            };

            await _orderService.CreateOrderAsync(order);

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var order = await _orderService.GetOrderByIdAsync(id);
            if (order == null) return NotFound();

            ViewData["CustomerId"] = new SelectList(_context.Customers, "CustomerId", "Email", order.CustomerId);
            ViewData["EmployeeId"] = new SelectList(_context.Employees, "Id", "Name", order.CreatedBy);

            return View(order);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Order order)
        {
            if (ModelState.IsValid)
            {
                await _orderService.UpdateOrderAsync(order);
                return RedirectToAction(nameof(Index));
            }
            return View(order);
        }

        public async Task<IActionResult> Delete(int id)
        {
            var order = await _orderService.GetOrderByIdAsync(id);
            if (order == null) return NotFound();
            return View(order);
        }

        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _orderService.DeleteOrderAsync(id);
            return RedirectToAction(nameof(Index));
        }
    }
}
