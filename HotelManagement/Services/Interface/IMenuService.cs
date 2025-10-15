using System.Collections.Generic;
using System.Threading.Tasks;

namespace HotelManagement.Services.Interface
{
    public interface IMenuService
    {
        Task<IEnumerable<ItemDto>> GetItemsAsync();
        Task<decimal> GetServiceChargeAsync();
    }

    public class ItemDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
    }
}
