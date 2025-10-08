using HotelManagement.Services;
using HotelManagement.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using HotelManagement.Data;
using HotelManagement.Models.Entities;

namespace HotelManagement.Controllers
{
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

        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> Create(Order order) //[Bind("Date,CustomerId,TableNo,IsFreeOfCharge,Dining,Notes,SubTotal")]
        //{
        //    // remove validation errors for fields we set in code
        //    ModelState.Remove(nameof(order.Customer));
        //    ModelState.Remove(nameof(order.ServiceCharge));
        //    ModelState.Remove(nameof(order.Notes));
        //    ModelState.Remove(nameof(order.GrossAmount));
        //    ModelState.Remove(nameof(order.CreatedDate));
        //    ModelState.Remove(nameof(order.CreatedBy));
        //    //ModelState.Remove(nameof(order.OrderItems));

        //    var errors = ModelState.Where(ms => ms.Value.Errors.Count > 0).Select(ms => new
        //    {
        //        Key = ms.Key,
        //        Errors = ms.Value.Errors.Select(e => e.ErrorMessage).ToList()
        //    }).ToList();


        //    if (ModelState.IsValid)
        //    {
        //        // Calculate service + gross
        //        order.ServiceCharge = order.SubTotal * 0.10m;
        //        order.GrossAmount = order.SubTotal + order.ServiceCharge;
        //        order.CreatedDate = DateTime.UtcNow;
        //        order.CreatedBy = 1; // TODO: replace with logged-in employee

        //        // Line numbers + totals
        //        int lineNo = 1;
        //        foreach (var item in order.OrderItems)
        //        {
        //            item.LineNumber = lineNo++;
        //            item.Amount = item.Qty * item.UnitPrice;
        //        }

        //        order.SubTotal = order.OrderItems.Sum(i => i.Amount);
        //        order.ServiceCharge = order.SubTotal * 0.10m;
        //        order.GrossAmount = order.SubTotal + order.ServiceCharge;

        //        await _orderService.CreateOrderAsync(order);
        //        return RedirectToAction(nameof(Index));
        //    }

        //    var customers = await _customerService.GetAllAsync();

        //    ViewBag.Customers = customers.Select(c => new SelectListItem
        //    {
        //        Value = c.ID.ToString(),
        //        Text = c.FirstName + " " + c.LastName
        //    }).ToList();
        //    return View(order);
        //}

        [HttpPost]
        public async Task<IActionResult> Create(CreateOrderViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            int lineNo = 1;
            foreach (var item in model.OrderItems)
            {
                item.Amount = item.Qty * item.UnitPrice;
            }

            model.SubTotal = model.OrderItems.Sum(i => i.Amount);
            model.ServiceCharge = model.SubTotal * 0.10m;
            model.GrossAmount = model.SubTotal + model.ServiceCharge;

            // Map ViewModel to EF entity
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
                CreatedBy = 1, // replace with logged-in employee
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
