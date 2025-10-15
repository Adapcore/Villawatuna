using HotelManagement.Models;
using HotelManagement.Models.Entities;

namespace HotelManagement.Services.Interface
{
    public interface IOrderService
    {
        Task<List<Order>> GetAllOrdersAsync();
        Task<Order?> GetOrderByIdAsync(int id);
        Task<Order> CreateOrderAsync(Order order);
        Task<Order> UpdateOrderAsync(Order order);
        Task<bool> DeleteOrderAsync(int id);

        // OrderItem Operations
        Task<OrderItem> AddOrderItemAsync(int orderId, OrderItem item);
        Task<bool> RemoveOrderItemAsync(int orderId, int lineNumber);
    }
}
