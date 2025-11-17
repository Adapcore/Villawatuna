using HotelManagement.Services.Interface;
using Umbraco.Cms.Core.Web;

namespace HotelManagement.Services
{
    public class MenuService : IMenuService
    {
        private readonly IUmbracoContextAccessor _umbracoContextAccessor;

        public MenuService(IUmbracoContextAccessor umbracoContextAccessor)
        {
            _umbracoContextAccessor = umbracoContextAccessor;
        }

        public async Task<IEnumerable<ItemDto>> GetItemsAsync()
        {
            var context = _umbracoContextAccessor.GetRequiredUmbracoContext();
            var root = context.Content?.GetAtRoot().FirstOrDefault();

            if (root == null)
                return Enumerable.Empty<ItemDto>();

            // Find the "Menu" node under the root
            var menuNode = root.DescendantsOrSelfOfType("menu").FirstOrDefault();

            if (menuNode == null)
                return Enumerable.Empty<ItemDto>();


            // Get items only under Menu
            var items = menuNode
                .DescendantsOfType("item")
                .Select(x => new ItemDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    Price = x.Value<decimal>("price")
                })
                .ToList();

            return await Task.FromResult(items);
        }

        public async Task<IEnumerable<MenuCategoryDto>> GetItemsWithCategoriesAsync()
        {
            var context = _umbracoContextAccessor.GetRequiredUmbracoContext();
            var root = context.Content?.GetAtRoot().FirstOrDefault();

            if (root == null)
                return Enumerable.Empty<MenuCategoryDto>();

            // Find the "Menu" node under the root
            var menuNode = root.DescendantsOrSelfOfType("menu").FirstOrDefault();

            if (menuNode == null)
                return Enumerable.Empty<MenuCategoryDto>();

            var categories = menuNode.DescendantsOfType("menuCategory");

            List<MenuCategoryDto> itemCategories = new List<MenuCategoryDto>();

            foreach (var category in categories)
            {
                List<ItemDto> items = category.DescendantsOfType("item").Select(x => new ItemDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    Price = x.Value<decimal>("price")
                })
                .ToList();

                itemCategories.Add(new MenuCategoryDto()
                {
                    Category = category.Name,
                    Items = items
                });
            }

            return await Task.FromResult(itemCategories);
        }


        public async Task<decimal> GetServiceChargeAsync()
        {
            var context = _umbracoContextAccessor.GetRequiredUmbracoContext();
            var root = context.Content?.GetAtRoot().FirstOrDefault();

            if (root == null)
                return 0;

            var menu = root
                .DescendantsOfType("menu")
                .FirstOrDefault();

            if (menu == null)
                return 0;

            var serviceCharge = menu.Value<decimal>("serviceCharge");

            return await Task.FromResult(serviceCharge);
        }
    }
}
