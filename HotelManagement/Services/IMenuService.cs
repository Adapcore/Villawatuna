using System.Collections.Generic;
using System.Threading.Tasks;

namespace HotelManagement.Services
{
    public interface IMenuService
    {
        Task<IEnumerable<MenuItemDto>> GetItemsAsync();
        Task<decimal> GetServiceChargeAsync();
    }

    public class MenuItemDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
    }
}
