using System.Collections.Generic;
using System.Threading.Tasks;

namespace HotelManagement.Services.Interface
{
    public interface IMenuService
    {
        Task<IEnumerable<ItemDto>> GetItemsAsync();
        Task<IEnumerable<MenuCategoryDto>> GetItemsWithCategoriesAsync();
        Task<decimal> GetServiceChargeAsync();
    }

    public class ItemDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public bool NoteRequired { get; set; } = false;
    }

    public class MenuCategoryDto
    {
        public string Category { get; set; }
        public List<ItemDto> Items { get; set; }
    }
}
