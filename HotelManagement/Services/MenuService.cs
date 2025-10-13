using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Umbraco.Cms.Core.Web;
using Umbraco.Extensions;

namespace HotelManagement.Services
{
    public class MenuService : IMenuService
    {
        private readonly IUmbracoContextAccessor _umbracoContextAccessor;

        public MenuService(IUmbracoContextAccessor umbracoContextAccessor)
        {
            _umbracoContextAccessor = umbracoContextAccessor;
        }

        public async Task<IEnumerable<MenuItemDto>> GetItemsAsync()
        {
            var context = _umbracoContextAccessor.GetRequiredUmbracoContext();
            var root = context.Content?.GetAtRoot().FirstOrDefault();

            if (root == null)
                return Enumerable.Empty<MenuItemDto>();

            var items = root
                .DescendantsOfType("menuItem")
                .Select(x => new MenuItemDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    Price = x.Value<decimal>("price")
                })
                .ToList();

            return await Task.FromResult(items);
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
