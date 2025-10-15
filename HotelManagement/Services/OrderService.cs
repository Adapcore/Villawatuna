using HotelManagement.Data;
using HotelManagement.Models;
using HotelManagement.Models.Entities;
using HotelManagement.Services.Interface;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Services
{
    public class OrderService : IOrderService
    {
        private readonly HotelContext _context;

        public OrderService(HotelContext context)
        {
            _context = context;
        }

        public async Task<List<Order>> GetAllOrdersAsync()
        {
            return await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.OrderItems)
                .ToListAsync();
        }

        public async Task<Order?> GetOrderByIdAsync(int id)
        {
            return await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.OrderNo == id);
        }

        public async Task<Order> CreateOrderAsync(Order order)
        {
            order.CreatedDate = DateTime.Now;
            order.ServiceCharge = order.SubTotal * 0.10m;
            order.GrossAmount = order.SubTotal + order.ServiceCharge;

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();
            return order;
        }

        public async Task<Order> UpdateOrderAsync(Order order)
        {
            var existingOrder = await _context.Orders.FindAsync(order.OrderNo);
            if (existingOrder == null) throw new Exception("Order not found");

            existingOrder.Date = order.Date;
            existingOrder.CustomerId = order.CustomerId;
            existingOrder.TableNo = order.TableNo;
            existingOrder.IsFreeOfCharge = order.IsFreeOfCharge;
            existingOrder.Dining = order.Dining;
            existingOrder.Notes = order.Notes;
            existingOrder.ModifiedBy = order.ModifiedBy;
            existingOrder.ModifiedDate = DateTime.Now;

            existingOrder.SubTotal = order.SubTotal;
            existingOrder.ServiceCharge = order.SubTotal * 0.10m;
            existingOrder.GrossAmount = existingOrder.SubTotal + existingOrder.ServiceCharge;

            _context.Orders.Update(existingOrder);
            await _context.SaveChangesAsync();
            return existingOrder;
        }

        public async Task<bool> DeleteOrderAsync(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null) return false;

            _context.Orders.Remove(order);
            await _context.SaveChangesAsync();
            return true;
        }

        // ---------- ORDER ITEMS ----------
        public async Task<OrderItem> AddOrderItemAsync(int orderId, OrderItem item)
        {
            var order = await _context.Orders.Include(o => o.OrderItems).FirstOrDefaultAsync(o => o.OrderNo  == orderId);
            if (order == null) throw new Exception("Order not found");

            int nextLineNo = order.OrderItems?.Count > 0 ? order.OrderItems.Max(i => i.LineNumber) + 1 : 1;
            item.LineNumber = nextLineNo;
            item.OrderNo = orderId;

            item.Amount = item.Qty * item.UnitPrice;

            _context.OrderItems.Add(item);
            await _context.SaveChangesAsync();

            // update order totals
            order.SubTotal = order.OrderItems.Sum(i => i.Amount);
            order.ServiceCharge = order.SubTotal * 0.10m;
            order.GrossAmount = order.SubTotal + order.ServiceCharge;
            _context.Orders.Update(order);
            await _context.SaveChangesAsync();

            return item;
        }

        public async Task<bool> RemoveOrderItemAsync(int orderId, int lineNumber)
        {
            var item = await _context.OrderItems.FirstOrDefaultAsync(i => i.OrderNo == orderId && i.LineNumber == lineNumber);
            if (item == null) return false;

            _context.OrderItems.Remove(item);
            await _context.SaveChangesAsync();

            // update totals after removing item
            var order = await _context.Orders.Include(o => o.OrderItems).FirstOrDefaultAsync(o => o.OrderNo == orderId);
            if (order != null)
            {
                order.SubTotal = order.OrderItems.Sum(i => i.Amount);
                order.ServiceCharge = order.SubTotal * 0.10m;
                order.GrossAmount = order.SubTotal + order.ServiceCharge;
                _context.Orders.Update(order);
                await _context.SaveChangesAsync();
            }

            return true;
        }
    }
}
