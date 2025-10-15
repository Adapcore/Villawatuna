using HotelManagement.Services.Interface;
using Umbraco.Cms.Core.Web;

namespace HotelManagement.Services
{
    public class OtherTypeService : IOtherTypeService
    {
        private readonly IUmbracoContextAccessor _umbracoContextAccessor;

        public OtherTypeService(IUmbracoContextAccessor umbracoContextAccessor)
        {
            _umbracoContextAccessor = umbracoContextAccessor;
        }

        public async Task<IEnumerable<ItemDto>> GetItemsAsync()
        {
            var context = _umbracoContextAccessor.GetRequiredUmbracoContext();
            var root = context.Content?.GetAtRoot().FirstOrDefault();

            if (root == null)
                return Enumerable.Empty<ItemDto>();

            // Find the "OtherTypes" node under the root
            var otherTypeNode = root.DescendantsOrSelfOfType("otherTypes").FirstOrDefault();

            if (otherTypeNode == null)
                return Enumerable.Empty<ItemDto>();

            // Get items only under Other
            var items = otherTypeNode
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
    }
}
